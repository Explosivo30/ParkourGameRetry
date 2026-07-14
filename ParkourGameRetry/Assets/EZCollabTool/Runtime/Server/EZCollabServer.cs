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
        const int SnapshotTimeoutMilliseconds = 15000;

        TcpListener tcpListener;
        CancellationTokenSource cts;

        readonly ConcurrentDictionary<string, ConnectedClient> clients = new ConcurrentDictionary<string, ConnectedClient>();
        readonly ConcurrentQueue<SnapshotRequest> snapshotRequests = new ConcurrentQueue<SnapshotRequest>();
        readonly EZCollabLockManager lockManager = new EZCollabLockManager();

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

        sealed class SnapshotRequest
        {
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

            while (snapshotRequests.TryDequeue(out SnapshotRequest request))
                request.completion.TrySetCanceled();
        }

        public void ProcessMainThreadRequests()
        {
            while (snapshotRequests.TryDequeue(out SnapshotRequest request))
            {
                if (request.completion.Task.IsCompleted) continue;

                try
                {
                    SceneSnapshot snapshot = EZSceneSerializer.CaptureScene();
                    request.completion.TrySetResult(snapshot);
                }
                catch (Exception exception)
                {
                    request.completion.TrySetException(exception);
                }
            }
        }

        public Task SendFromHost(EZMessage message)
        {
            if (cts == null || cts.IsCancellationRequested)
                return Task.CompletedTask;

            return BroadcastAll(message, cts.Token);
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
                    Debug.LogError($"[EZCollab] Accept error: {exception.Message}");
                }
            }
        }

        async Task HandleClient(TcpClient tcpClient, CancellationToken token)
        {
            ConnectedClient connectedClient = null;
            string peerId = null;
            string peerName = null;
            NetworkStream stream = tcpClient.GetStream();
            byte[] lengthBuffer = new byte[4];
            byte[] payloadBuffer = new byte[32768];

            try
            {
                EZMessage joinMessage = await ReadMessage(stream, lengthBuffer, payloadBuffer, token);
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

                if (!clients.TryAdd(peerId, connectedClient))
                    throw new InvalidOperationException($"Peer ID '{peerId}' is already connected.");

                await SendSnapshot(connectedClient, token);
                await SendSessionPeers(connectedClient, token);
                await SendSessionLocks(connectedClient, token);
                await FlushPendingMessages(connectedClient, token);

                PeerPayload joinedPayload = new PeerPayload { peerId = peerId, peerName = peerName };
                EZMessage joinedMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, joinedPayload);
                await BroadcastExcept(joinedMessage, peerId, token);
                incomingMessages.Enqueue(joinedMessage);

                while (!token.IsCancellationRequested)
                {
                    EZMessage message = await ReadMessage(stream, lengthBuffer, payloadBuffer, token);
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
                    Debug.LogWarning($"[EZCollab] Client error ({peerId}): {exception.Message}");
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
            byte[] lengthBuffer,
            byte[] payloadBuffer,
            CancellationToken token)
        {
            int read = await ReadExact(stream, lengthBuffer, 4, token);
            if (read == 0) return null;

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > EZMessage.MaxPayloadBytes)
                throw new InvalidOperationException($"Invalid payload length: {length}.");

            if (payloadBuffer.Length < length)
                payloadBuffer = new byte[length];

            read = await ReadExact(stream, payloadBuffer, length, token);
            return read == 0 ? null : EZMessage.FromBytes(payloadBuffer, length);
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

                case EZMessageType.ComponentDelta:
                case EZMessageType.CreateObject:
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;

                case EZMessageType.TransformDelta:
                {
                    TransformPayload payload = message.GetPayload<TransformPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, fromPeerId))
                        break;

                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.DestroyObject:
                {
                    DestroyObjectPayload payload = message.GetPayload<DestroyObjectPayload>();
                    if (payload == null || !lockManager.IsLockedBy(payload.guid, fromPeerId))
                        break;

                    lockManager.Release(payload.guid, fromPeerId);
                    await BroadcastExcept(message, fromPeerId, token);
                    incomingMessages.Enqueue(message);
                    break;
                }

                case EZMessageType.Heartbeat:
                    break;
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
            SnapshotRequest request = new SnapshotRequest();
            snapshotRequests.Enqueue(request);

            Task timeoutTask = Task.Delay(SnapshotTimeoutMilliseconds, token);
            Task completedTask = await Task.WhenAny(request.completion.Task, timeoutTask);
            if (completedTask != request.completion.Task)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                request.completion.TrySetCanceled();
                throw new TimeoutException("Unity did not produce a scene snapshot within 15 seconds.");
            }

            SceneSnapshot snapshot = await request.completion.Task;
            EZMessage message = EZMessage.Create(EZMessageType.SceneSnapshot, EZCollabState.localPeerId, snapshot);
            await SendInternal(connectedClient, message, token);
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
