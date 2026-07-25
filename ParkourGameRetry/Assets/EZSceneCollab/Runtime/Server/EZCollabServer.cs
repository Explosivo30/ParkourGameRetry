using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EZCollabTool
{
    public class EZCollabServer
    {
        const int SnapshotTimeoutMilliseconds = 120000;
        const int SnapshotChunkSize = 64;
        const int MaxHistoryMessages = 4096;
        const long MaxHistoryPayloadChars = 32L * 1024L * 1024L;

        TcpListener tcpListener;
        CancellationTokenSource cts;

        readonly ConcurrentDictionary<string, ConnectedClient> clients = new ConcurrentDictionary<string, ConnectedClient>();
        readonly ConcurrentDictionary<string, string> objectCreators =
            new ConcurrentDictionary<string, string>();
        readonly ConcurrentQueue<SnapshotRequest> snapshotRequests = new ConcurrentQueue<SnapshotRequest>();
        readonly EZCollabLockManager lockManager = new EZCollabLockManager();
        readonly SemaphoreSlim stateMessageOrder = new SemaphoreSlim(1, 1);
        readonly object historyLock = new object();
        readonly LinkedList<EZMessage> stateHistory = new LinkedList<EZMessage>();
        long historyPayloadChars;
        long nextSequence;

        public readonly ConcurrentQueue<EZMessage> incomingMessages = new ConcurrentQueue<EZMessage>();

        public int clientCount => clients.Count;

        sealed class ConnectedClient
        {
            public string peerId;
            public string peerName;
            public TcpClient client;
            public NetworkStream stream;
            public readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
            public readonly object readinessLock = new object();
            public readonly Queue<EZMessage> pendingMessages = new Queue<EZMessage>();
            public bool isReady;
        }

        sealed class NetworkReadBuffer
        {
            public readonly byte[] length = new byte[4];
            public byte[] payload = new byte[32768];
        }

        sealed class SnapshotRequest
        {
            public long baselineSequence;
            public readonly TaskCompletionSource<SceneSnapshot> completion =
                new TaskCompletionSource<SceneSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Start(int port)
        {
            cts = new CancellationTokenSource();
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            _ = Task.Run(() => AcceptLoop(cts.Token));
        }

        public void Stop()
        {
            cts?.Cancel();
            tcpListener?.Stop();

            foreach (ConnectedClient connectedClient in clients.Values)
            {
                try
                {
                    connectedClient.client.Close();
                }
                catch
                {
                }
            }

            clients.Clear();
            objectCreators.Clear();

            while (snapshotRequests.TryDequeue(out SnapshotRequest request))
                request.completion.TrySetCanceled();

            lock (historyLock)
            {
                stateHistory.Clear();
                historyPayloadChars = 0;
                nextSequence = 0;
            }
        }

        public void ProcessMainThreadRequests()
        {
            if (!stateMessageOrder.Wait(0))
                return;

            try
            {
                // The checkpoint is valid only after every older state message has
                // reached and been applied by the host's main-thread queue.
                if (!incomingMessages.IsEmpty)
                    return;

                while (snapshotRequests.TryDequeue(out SnapshotRequest request))
                {
                    if (request.completion.Task.IsCompleted) continue;

                    try
                    {
                        request.baselineSequence = CreateSnapshotCheckpoint();
                        SceneSnapshot snapshot = EZSceneSerializer.CaptureScene();
                        request.completion.TrySetResult(snapshot);
                    }
                    catch (Exception exception)
                    {
                        request.completion.TrySetException(exception);
                    }
                }
            }
            finally
            {
                stateMessageOrder.Release();
            }
        }

        public Task SendFromHost(EZMessage message)
        {
            if (message == null || cts == null || cts.IsCancellationRequested)
                return Task.CompletedTask;

            string releaseGuidAfterBroadcast = null;
            switch (message.type)
            {
                case EZMessageType.CreateObject:
                {
                    CreateObjectPayload payload = message.GetPayload<CreateObjectPayload>();
                    if (payload == null || string.IsNullOrEmpty(payload.guid))
                        return Task.CompletedTask;
                    objectCreators.TryAdd(payload.guid, EZCollabState.localPeerId);
                    break;
                }
                case EZMessageType.TransformDelta:
                {
                    TransformPayload payload = message.GetPayload<TransformPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, EZCollabState.localPeerId))
                        return Task.CompletedTask;
                    break;
                }
                case EZMessageType.ComponentDelta:
                {
                    CreateObjectPayload payload = message.GetPayload<CreateObjectPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, EZCollabState.localPeerId))
                        return Task.CompletedTask;
                    if (payload.releaseLockAfterApply)
                        releaseGuidAfterBroadcast = payload.guid;
                    break;
                }
                case EZMessageType.DestroyObject:
                {
                    DestroyObjectPayload payload = message.GetPayload<DestroyObjectPayload>();
                    bool isCreator = payload != null && objectCreators.TryGetValue(payload.guid, out string creator) &&
                                     creator == EZCollabState.localPeerId;
                    string owner = payload == null ? null : lockManager.GetOwner(payload.guid);
                    bool allowed = owner == EZCollabState.localPeerId || (isCreator && string.IsNullOrEmpty(owner));
                    if (payload == null || !allowed)
                        return Task.CompletedTask;
                    break;
                }
            }

            if (IsRecoverableStateMessage(message.type))
                return SendHostStateOrdered(message, releaseGuidAfterBroadcast, cts.Token);

            return BroadcastAll(message, cts.Token);
        }

        async Task SendHostStateOrdered(EZMessage message, string releaseGuid, CancellationToken token)
        {
            bool acquired = false;
            try
            {
                await stateMessageOrder.WaitAsync(token);
                acquired = true;

                if (message.type == EZMessageType.TransformDelta)
                {
                    TransformPayload transform = message.GetPayload<TransformPayload>();
                    if (transform == null || !lockManager.IsLockedBy(transform.guid, EZCollabState.localPeerId))
                        return;
                }
                else if (message.type == EZMessageType.ComponentDelta)
                {
                    CreateObjectPayload component = message.GetPayload<CreateObjectPayload>();
                    if (component == null || !lockManager.IsLockedBy(component.guid, EZCollabState.localPeerId))
                        return;
                }

                if (message.type == EZMessageType.DestroyObject)
                {
                    DestroyObjectPayload destroyed = message.GetPayload<DestroyObjectPayload>();
                    bool isCreator = destroyed != null &&
                                     objectCreators.TryGetValue(destroyed.guid, out string creator) &&
                                     creator == EZCollabState.localPeerId;
                    string owner = destroyed == null ? null : lockManager.GetOwner(destroyed.guid);
                    if (destroyed == null ||
                        (owner != EZCollabState.localPeerId && !(isCreator && string.IsNullOrEmpty(owner))))
                    {
                        return;
                    }

                    lockManager.Release(destroyed.guid, EZCollabState.localPeerId);
                    objectCreators.TryRemove(destroyed.guid, out _);
                }

                StampAndRemember(message);
                EZCollabState.ObserveServerSequence(message.sequence);
                await BroadcastAll(message, token);

                if (!string.IsNullOrEmpty(releaseGuid))
                {
                    lockManager.Release(releaseGuid, EZCollabState.localPeerId);
                    EZMessage unlocked = EZMessage.Create(
                        EZMessageType.ObjectUnlocked,
                        EZCollabState.localPeerId,
                        new LockPayload { guid = releaseGuid, peerId = EZCollabState.localPeerId });
                    await BroadcastAll(unlocked, token);
                    incomingMessages.Enqueue(unlocked);
                }
            }
            finally
            {
                if (acquired)
                    stateMessageOrder.Release();
            }
        }

        public bool TryAcquireHostLock(string guid, string peerName)
        {
            if (!lockManager.TryAcquire(guid, EZCollabState.localPeerId))
                return false;

            LockPayload payload = new LockPayload
            {
                guid = guid,
                peerId = EZCollabState.localPeerId,
                peerName = peerName
            };
            EZMessage message = EZMessage.Create(EZMessageType.ObjectLocked, EZCollabState.localPeerId, payload);
            _ = SendFromHost(message);
            return true;
        }

        public void ReleaseHostLock(string guid)
        {
            if (!lockManager.IsLockedBy(guid, EZCollabState.localPeerId))
                return;

            lockManager.Release(guid, EZCollabState.localPeerId);
            LockPayload payload = new LockPayload { guid = guid, peerId = EZCollabState.localPeerId };
            EZMessage message = EZMessage.Create(EZMessageType.ObjectUnlocked, EZCollabState.localPeerId, payload);
            _ = SendFromHost(message);
        }

        async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    tcpClient.NoDelay = true;
                    _ = Task.Run(() => HandleClient(tcpClient, token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    Debug.LogError(EZCollabLocalization.Tr(EZCollabText.AcceptFailedLog, exception.Message));
                }
            }
        }

        async Task HandleClient(TcpClient tcpClient, CancellationToken token)
        {
            ConnectedClient connectedClient = null;
            string peerId = null;
            string peerName = null;
            NetworkStream stream = tcpClient.GetStream();
            NetworkReadBuffer readBuffer = new NetworkReadBuffer();

            try
            {
                EZMessage joinMessage = await ReadMessage(stream, readBuffer, token);
                if (joinMessage == null || joinMessage.type != EZMessageType.PeerJoined)
                    return;

                PeerPayload peerInfo = joinMessage.GetPayload<PeerPayload>();
                if (peerInfo == null || string.IsNullOrWhiteSpace(peerInfo.peerId))
                    return;

                peerId = peerInfo.peerId;
                peerName = string.IsNullOrWhiteSpace(peerInfo.peerName) ? peerId : peerInfo.peerName;
                connectedClient = new ConnectedClient
                {
                    peerId = peerId,
                    peerName = peerName,
                    client = tcpClient,
                    stream = stream
                };

                if (clients.TryRemove(peerId, out ConnectedClient previousConnection))
                {
                    try { previousConnection.client.Close(); }
                    catch { }
                }
                clients[peerId] = connectedClient;

                bool recovered = await SendRecovery(connectedClient, peerInfo.lastSequence, token);
                if (!recovered)
                    await SendSnapshot(connectedClient, token);
                await SendSessionPeers(connectedClient, token);
                await SendSessionLocks(connectedClient, token);
                await FlushPendingMessages(connectedClient, token);

                EZMessage recoveryComplete = EZMessage.Create(
                    EZMessageType.RecoveryComplete,
                    EZCollabState.localPeerId,
                    null);
                recoveryComplete.sequence = GetCurrentSequence();
                await SendInternal(connectedClient, recoveryComplete, token);

                PeerPayload joinedPayload = new PeerPayload { peerId = peerId, peerName = peerName };
                EZMessage joinedMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, joinedPayload);
                await BroadcastExcept(joinedMessage, peerId, token);
                incomingMessages.Enqueue(joinedMessage);

                while (!token.IsCancellationRequested)
                {
                    EZMessage message = await ReadMessage(stream, readBuffer, token);
                    if (message == null) break;

                    await ProcessMessage(message, peerId, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                if (!(exception is System.IO.IOException) && !(exception is OperationCanceledException))
                    Debug.LogWarning(EZCollabLocalization.Tr(EZCollabText.ClientFailedLog, peerId, exception.Message));
            }
            finally
            {
                try
                {
                    tcpClient.Close();
                }
                catch
                {
                }

                if (connectedClient != null)
                    await HandleDisconnect(connectedClient, token);
            }
        }

        async Task<EZMessage> ReadMessage(
            NetworkStream stream,
            NetworkReadBuffer buffer,
            CancellationToken token)
        {
            int read = await ReadExact(stream, buffer.length, 4, token);
            if (read == 0) return null;

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer.length);

            int length = BitConverter.ToInt32(buffer.length, 0);
            if (length <= 0 || length > EZMessage.MaxPayloadBytes)
                throw new InvalidOperationException($"Invalid payload length: {length}.");

            if (buffer.payload.Length < length)
                buffer.payload = new byte[length];

            read = await ReadExact(stream, buffer.payload, length, token);
            return read == 0 ? null : EZMessage.FromBytes(buffer.payload, length);
        }

        static async Task<int> ReadExact(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
                if (read == 0) return 0;
                totalRead += read;
            }

            return totalRead;
        }

        async Task ProcessMessage(EZMessage message, string fromPeerId, CancellationToken token)
        {
            bool acquiredStateOrder = false;
            if (IsRecoverableStateMessage(message.type))
            {
                await stateMessageOrder.WaitAsync(token);
                acquiredStateOrder = true;
            }

            try
            {
                switch (message.type)
                {
                case EZMessageType.LockRequest:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    if (payload == null || string.IsNullOrEmpty(payload.guid)) break;

                    bool granted = lockManager.TryAcquire(payload.guid, fromPeerId);
                    LockPayload responsePayload = new LockPayload
                    {
                        guid = payload.guid,
                        peerId = fromPeerId,
                        peerName = payload.peerName
                    };
                    EZMessage response = EZMessage.Create(
                        granted ? EZMessageType.LockGranted : EZMessageType.LockDenied,
                        EZCollabState.localPeerId,
                        responsePayload);

                    await SendTo(fromPeerId, response, token);

                    if (granted)
                    {
                        EZMessage broadcast = EZMessage.Create(
                            EZMessageType.ObjectLocked,
                            EZCollabState.localPeerId,
                            responsePayload);
                        await BroadcastExcept(broadcast, fromPeerId, token);
                        incomingMessages.Enqueue(broadcast);
                    }

                    break;
                }

                case EZMessageType.LockRelease:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, fromPeerId)) break;

                    lockManager.Release(payload.guid, fromPeerId);
                    EZMessage unlocked = EZMessage.Create(
                        EZMessageType.ObjectUnlocked,
                        EZCollabState.localPeerId,
                        new LockPayload { guid = payload.guid, peerId = fromPeerId });
                    await BroadcastExcept(unlocked, fromPeerId, token);
                    incomingMessages.Enqueue(unlocked);
                    break;
                }

                case EZMessageType.CreateObject:
                {
                    CreateObjectPayload payload = message.GetPayload<CreateObjectPayload>();
                    if (payload == null || string.IsNullOrEmpty(payload.guid)) break;
                    objectCreators.TryAdd(payload.guid, fromPeerId);
                    StampAndRemember(message);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.ComponentDelta:
                {
                    CreateObjectPayload payload = message.GetPayload<CreateObjectPayload>();
                    if (payload == null || string.IsNullOrEmpty(payload.guid) ||
                        !lockManager.IsLockedBy(payload.guid, fromPeerId))
                    {
                        break;
                    }

                    StampAndRemember(message);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    if (payload.releaseLockAfterApply)
                    {
                        lockManager.Release(payload.guid, fromPeerId);
                        EZMessage unlocked = EZMessage.Create(
                            EZMessageType.ObjectUnlocked,
                            EZCollabState.localPeerId,
                            new LockPayload { guid = payload.guid, peerId = fromPeerId });
                        await BroadcastAll(unlocked, token);
                        incomingMessages.Enqueue(unlocked);
                    }
                    break;
                }

                case EZMessageType.MaterialDelta:
                {
                    SerializedMaterial payload = message.GetPayload<SerializedMaterial>();
                    if (payload == null ||
                        !EZCollabMaterialSerializer.TryGetMaterialKey(payload, out _))
                        break;

                    StampAndRemember(message);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.TransformDelta:
                {
                    TransformPayload payload = message.GetPayload<TransformPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, fromPeerId))
                        break;

                    StampAndRemember(message);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.DestroyObject:
                {
                    DestroyObjectPayload payload = message.GetPayload<DestroyObjectPayload>();
                    bool isCreator = payload != null && objectCreators.TryGetValue(payload.guid, out string creator) &&
                                     creator == fromPeerId;
                    string owner = payload == null ? null : lockManager.GetOwner(payload.guid);
                    bool allowed = owner == fromPeerId || (isCreator && string.IsNullOrEmpty(owner));
                    if (payload == null || !allowed)
                        break;

                    StampAndRemember(message);
                    lockManager.Release(payload.guid, fromPeerId);
                    objectCreators.TryRemove(payload.guid, out _);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.Heartbeat:
                    break;
                }
            }
            finally
            {
                if (acquiredStateOrder)
                    stateMessageOrder.Release();
            }
        }

        async Task HandleDisconnect(ConnectedClient disconnectedClient, CancellationToken token)
        {
            if (!clients.TryGetValue(disconnectedClient.peerId, out ConnectedClient currentClient) ||
                !ReferenceEquals(currentClient, disconnectedClient))
            {
                return;
            }

            clients.TryRemove(disconnectedClient.peerId, out ConnectedClient removedClient);
            if (token.IsCancellationRequested) return;

            List<string> releasedGuids = lockManager.ReleaseAll(disconnectedClient.peerId);
            foreach (string guid in releasedGuids)
            {
                EZMessage unlocked = EZMessage.Create(
                    EZMessageType.ObjectUnlocked,
                    EZCollabState.localPeerId,
                    new LockPayload { guid = guid, peerId = disconnectedClient.peerId });
                await BroadcastAll(unlocked, token);
                incomingMessages.Enqueue(unlocked);
            }

            EZMessage leftMessage = EZMessage.Create(
                EZMessageType.PeerLeft,
                EZCollabState.localPeerId,
                new PeerPayload { peerId = disconnectedClient.peerId, peerName = disconnectedClient.peerName });
            await BroadcastAll(leftMessage, token);
            incomingMessages.Enqueue(leftMessage);
        }

        async Task SendSnapshot(ConnectedClient connectedClient, CancellationToken token)
        {
            await SendInternal(connectedClient,
                EZMessage.Create(EZMessageType.SceneSnapshotBegin, EZCollabState.localPeerId, null), token);

            SnapshotRequest request = new SnapshotRequest();
            snapshotRequests.Enqueue(request);

            Task timeoutTask = Task.Delay(SnapshotTimeoutMilliseconds, token);
            Task completedTask = await Task.WhenAny(request.completion.Task, timeoutTask);
            if (completedTask != request.completion.Task)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                request.completion.TrySetCanceled();
                throw new TimeoutException("Unity did not produce a scene snapshot within 120 seconds.");
            }

            SceneSnapshot snapshot = await request.completion.Task;
            for (int offset = 0; offset < snapshot.objects.Count; offset += SnapshotChunkSize)
            {
                SceneSnapshot chunk = new SceneSnapshot();
                int count = Math.Min(SnapshotChunkSize, snapshot.objects.Count - offset);
                chunk.objects.AddRange(snapshot.objects.GetRange(offset, count));
                if (offset == 0 && snapshot.materials != null)
                    chunk.materials.AddRange(snapshot.materials);

                EZMessage message = EZMessage.Create(
                    EZMessageType.SceneSnapshotChunk,
                    EZCollabState.localPeerId,
                    chunk);
                await SendInternal(connectedClient, message, token);
            }

            EZMessage snapshotEnd = EZMessage.Create(
                EZMessageType.SceneSnapshotEnd, EZCollabState.localPeerId, null);
            snapshotEnd.sequence = request.baselineSequence;
            await SendInternal(connectedClient, snapshotEnd, token);
        }

        async Task<bool> SendRecovery(
            ConnectedClient connectedClient,
            long lastSequence,
            CancellationToken token)
        {
            List<EZMessage> recovery = null;
            lock (historyLock)
            {
                if (lastSequence <= 0 || lastSequence > nextSequence)
                    return false;

                if (lastSequence == nextSequence)
                {
                    recovery = new List<EZMessage>();
                }
                else if (stateHistory.First == null || lastSequence < stateHistory.First.Value.sequence - 1)
                {
                    return false;
                }
                else
                {
                    recovery = new List<EZMessage>();
                    foreach (EZMessage message in stateHistory)
                        if (message.sequence > lastSequence)
                            recovery.Add(message);
                }
            }

            foreach (EZMessage message in recovery)
                await SendInternal(connectedClient, message, token);
            return true;
        }

        long CreateSnapshotCheckpoint()
        {
            EZMessage checkpoint = EZMessage.Create(EZMessageType.Info, EZCollabState.localPeerId, null);
            lock (historyLock)
            {
                checkpoint.sequence = ++nextSequence;
                stateHistory.AddLast(checkpoint);
                while (stateHistory.Count > MaxHistoryMessages)
                {
                    EZMessage removed = stateHistory.First.Value;
                    historyPayloadChars -= removed.payload?.Length ?? 0;
                    stateHistory.RemoveFirst();
                }
                return checkpoint.sequence;
            }
        }

        void StampAndRemember(EZMessage message)
        {
            lock (historyLock)
            {
                message.sequence = ++nextSequence;
                stateHistory.AddLast(message);
                historyPayloadChars += message.payload?.Length ?? 0;
                while (stateHistory.Count > MaxHistoryMessages || historyPayloadChars > MaxHistoryPayloadChars)
                {
                    EZMessage removed = stateHistory.First.Value;
                    historyPayloadChars -= removed.payload?.Length ?? 0;
                    stateHistory.RemoveFirst();
                }
            }
        }

        long GetCurrentSequence()
        {
            lock (historyLock)
                return nextSequence;
        }

        static bool IsRecoverableStateMessage(EZMessageType type)
        {
            return type == EZMessageType.CreateObject || type == EZMessageType.DestroyObject ||
                   type == EZMessageType.TransformDelta || type == EZMessageType.ComponentDelta ||
                   type == EZMessageType.MaterialDelta;
        }

        async Task SendSessionPeers(ConnectedClient connectedClient, CancellationToken token)
        {
            PeerPayload hostPayload = new PeerPayload
            {
                peerId = EZCollabState.localPeerId,
                peerName = EZCollabState.localPeerName
            };
            EZMessage hostMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, hostPayload);
            await SendInternal(connectedClient, hostMessage, token);

            foreach (ConnectedClient otherClient in clients.Values)
            {
                if (ReferenceEquals(otherClient, connectedClient)) continue;

                PeerPayload peerPayload = new PeerPayload
                {
                    peerId = otherClient.peerId,
                    peerName = otherClient.peerName
                };
                EZMessage peerMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, peerPayload);
                await SendInternal(connectedClient, peerMessage, token);
            }
        }

        async Task SendSessionLocks(ConnectedClient connectedClient, CancellationToken token)
        {
            Dictionary<string, string> locks = lockManager.GetAll();
            foreach (KeyValuePair<string, string> pair in locks)
            {
                string ownerName = pair.Value == EZCollabState.localPeerId
                    ? EZCollabState.localPeerName
                    : clients.TryGetValue(pair.Value, out ConnectedClient owner) ? owner.peerName : pair.Value;

                LockPayload payload = new LockPayload
                {
                    guid = pair.Key,
                    peerId = pair.Value,
                    peerName = ownerName
                };
                EZMessage message = EZMessage.Create(
                    EZMessageType.ObjectLocked,
                    EZCollabState.localPeerId,
                    payload);
                await SendInternal(connectedClient, message, token);
            }
        }

        async Task FlushPendingMessages(ConnectedClient connectedClient, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<EZMessage> messages = new List<EZMessage>();
                lock (connectedClient.readinessLock)
                {
                    while (connectedClient.pendingMessages.Count > 0)
                        messages.Add(connectedClient.pendingMessages.Dequeue());

                    if (messages.Count == 0)
                    {
                        connectedClient.isReady = true;
                        return;
                    }
                }

                foreach (EZMessage message in messages)
                    await SendInternal(connectedClient, message, token);
            }
        }

        Task SendTo(string peerId, EZMessage message, CancellationToken token)
        {
            if (!clients.TryGetValue(peerId, out ConnectedClient connectedClient))
                return Task.CompletedTask;

            return SendOrQueue(connectedClient, message, token);
        }

        Task SendOrQueue(ConnectedClient connectedClient, EZMessage message, CancellationToken token)
        {
            lock (connectedClient.readinessLock)
            {
                if (!connectedClient.isReady)
                {
                    connectedClient.pendingMessages.Enqueue(message);
                    return Task.CompletedTask;
                }
            }

            return SendInternal(connectedClient, message, token);
        }

        async Task SendInternal(ConnectedClient connectedClient, EZMessage message, CancellationToken token)
        {
            bool lockAcquired = false;
            try
            {
                await connectedClient.sendLock.WaitAsync(token);
                lockAcquired = true;

                byte[] payloadBytes = message.ToBytes();
                byte[] lengthBytes = BitConverter.GetBytes(payloadBytes.Length);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                await connectedClient.stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, token);
                await connectedClient.stream.WriteAsync(payloadBytes, 0, payloadBytes.Length, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
            }
            finally
            {
                if (lockAcquired)
                    connectedClient.sendLock.Release();
            }
        }

        async Task BroadcastExcept(EZMessage message, string excludePeerId, CancellationToken token)
        {
            List<Task> sends = new List<Task>();
            foreach (KeyValuePair<string, ConnectedClient> pair in clients)
            {
                if (pair.Key == excludePeerId) continue;
                sends.Add(SendOrQueue(pair.Value, message, token));
            }

            await Task.WhenAll(sends);
        }

        async Task BroadcastAll(EZMessage message, CancellationToken token)
        {
            List<Task> sends = new List<Task>();
            foreach (ConnectedClient connectedClient in clients.Values)
                sends.Add(SendOrQueue(connectedClient, message, token));

            await Task.WhenAll(sends);
        }
    }
}
