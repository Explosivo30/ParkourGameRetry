using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EZCollabTool
{
    public class EZCollabClient
    {
        const int ConnectTimeoutMilliseconds = 10000;
        const int SnapshotTimeoutMilliseconds = 20000;

        TcpClient client;
        NetworkStream stream;
        CancellationTokenSource cts;
        SemaphoreSlim sendLock;
        TaskCompletionSource<bool> snapshotReceived;
        volatile bool connected;
        volatile bool unexpectedDisconnect;

        public readonly ConcurrentQueue<EZMessage> incomingMessages = new ConcurrentQueue<EZMessage>();

        public bool isConnected => connected;
        public bool connectionLostUnexpectedly => unexpectedDisconnect;

        public async Task<bool> Connect(string host, int port)
        {
            cts = new CancellationTokenSource();
            sendLock = new SemaphoreSlim(1, 1);
            snapshotReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            unexpectedDisconnect = false;

            client = new TcpClient();
            client.NoDelay = true;

            try
            {
                Task connectTask = client.ConnectAsync(host, port);
                Task connectTimeout = Task.Delay(ConnectTimeoutMilliseconds, cts.Token);
                Task completedConnectTask = await Task.WhenAny(connectTask, connectTimeout);
                if (completedConnectTask != connectTask)
                    throw new TimeoutException("Connection timed out after 10 seconds.");

                await connectTask;
                stream = client.GetStream();
                connected = true;

                PeerPayload peerPayload = new PeerPayload
                {
                    peerId = EZCollabState.localPeerId,
                    peerName = EZCollabState.localPeerName
                };
                EZMessage joinMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, peerPayload);
                await Send(joinMessage);

                _ = Task.Run(ReceiveLoop);

                Task snapshotTimeout = Task.Delay(SnapshotTimeoutMilliseconds, cts.Token);
                Task completedSnapshotTask = await Task.WhenAny(snapshotReceived.Task, snapshotTimeout);
                if (completedSnapshotTask != snapshotReceived.Task)
                    throw new TimeoutException("The host did not send a scene snapshot within 20 seconds.");

                await snapshotReceived.Task;
                _ = Task.Run(HeartbeatLoop);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EZCollab] Connection failed: {exception.Message}");
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            unexpectedDisconnect = false;
            connected = false;
            cts?.Cancel();

            try
            {
                client?.Close();
            }
            catch
            {
            }
        }

        public async Task Send(EZMessage message)
        {
            if (!connected || stream == null || cts == null) return;

            bool lockAcquired = false;
            try
            {
                await sendLock.WaitAsync(cts.Token);
                lockAcquired = true;

                byte[] payloadBytes = message.ToBytes();
                byte[] lengthBytes = BitConverter.GetBytes(payloadBytes.Length);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cts.Token);
                await stream.WriteAsync(payloadBytes, 0, payloadBytes.Length, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[EZCollab] Send error: {exception.Message}");
            }
            finally
            {
                if (lockAcquired)
                    sendLock.Release();
            }
        }

        async Task ReceiveLoop()
        {
            byte[] lengthBuffer = new byte[4];
            byte[] payloadBuffer = new byte[32768];

            try
            {
                while (!cts.Token.IsCancellationRequested && connected)
                {
                    EZMessage message = await ReadMessage(lengthBuffer, payloadBuffer, cts.Token);
                    if (message == null) break;

                    incomingMessages.Enqueue(message);
                    if (message.type == EZMessageType.SceneSnapshot)
                        snapshotReceived.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (!cts.Token.IsCancellationRequested)
            {
                if (!(exception is IOException))
                    Debug.LogWarning($"[EZCollab] Receive error: {exception.Message}");
            }
            finally
            {
                bool wasUnexpected = !cts.Token.IsCancellationRequested;
                connected = false;

                if (!snapshotReceived.Task.IsCompleted)
                    snapshotReceived.TrySetException(new IOException("The host closed the connection before sending the snapshot."));

                if (wasUnexpected)
                    unexpectedDisconnect = true;
            }
        }

        async Task<EZMessage> ReadMessage(byte[] lengthBuffer, byte[] payloadBuffer, CancellationToken token)
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

        static async Task<int> ReadExact(NetworkStream networkStream, byte[] buffer, int count, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await networkStream.ReadAsync(buffer, totalRead, count - totalRead, token);
                if (read == 0) return 0;
                totalRead += read;
            }

            return totalRead;
        }

        async Task HeartbeatLoop()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested && connected)
                {
                    await Task.Delay(5000, cts.Token);
                    EZMessage heartbeat = EZMessage.Create(EZMessageType.Heartbeat, EZCollabState.localPeerId, null);
                    await Send(heartbeat);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
