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
        const int InitialSyncTimeoutMilliseconds = 120000;

        TcpClient client;
        NetworkStream stream;
        CancellationTokenSource cts;
        SemaphoreSlim sendLock;
        TaskCompletionSource<bool> initialSyncReceived;
        volatile bool connected;
        volatile bool unexpectedDisconnect;

        sealed class NetworkReadBuffer
        {
            public readonly byte[] length = new byte[4];
            public byte[] payload = new byte[32768];
        }

        public readonly ConcurrentQueue<EZMessage> incomingMessages = new ConcurrentQueue<EZMessage>();

        public bool isConnected => connected;
        public bool connectionLostUnexpectedly => unexpectedDisconnect;

        public async Task<bool> Connect(string host, int port, long lastSequence = 0)
        {
            cts = new CancellationTokenSource();
            sendLock = new SemaphoreSlim(1, 1);
            initialSyncReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                    peerName = EZCollabState.localPeerName,
                    lastSequence = lastSequence
                };
                EZMessage joinMessage = EZMessage.Create(EZMessageType.PeerJoined, EZCollabState.localPeerId, peerPayload);
                await Send(joinMessage);

                _ = Task.Run(ReceiveLoop);

                Task syncTimeout = Task.Delay(InitialSyncTimeoutMilliseconds, cts.Token);
                Task completedSyncTask = await Task.WhenAny(initialSyncReceived.Task, syncTimeout);
                if (completedSyncTask != initialSyncReceived.Task)
                    throw new TimeoutException("The host did not complete synchronization within 120 seconds.");

                await initialSyncReceived.Task;
                _ = Task.Run(HeartbeatLoop);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(EZCollabLocalization.Tr(EZCollabText.ConnectionFailedLog, exception.Message));
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
                Debug.LogWarning(EZCollabLocalization.Tr(EZCollabText.SendFailedLog, exception.Message));
            }
            finally
            {
                if (lockAcquired)
                    sendLock.Release();
            }
        }

        async Task ReceiveLoop()
        {
            NetworkReadBuffer readBuffer = new NetworkReadBuffer();

            try
            {
                while (!cts.Token.IsCancellationRequested && connected)
                {
                    EZMessage message = await ReadMessage(readBuffer, cts.Token);
                    if (message == null) break;

                    incomingMessages.Enqueue(message);
                    if (message.type == EZMessageType.RecoveryComplete)
                        initialSyncReceived.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (!cts.Token.IsCancellationRequested)
            {
                if (!(exception is IOException))
                    Debug.LogWarning(EZCollabLocalization.Tr(EZCollabText.ReceiveFailedLog, exception.Message));
            }
            finally
            {
                bool wasUnexpected = !cts.Token.IsCancellationRequested;
                connected = false;

                if (!initialSyncReceived.Task.IsCompleted)
                    initialSyncReceived.TrySetException(new IOException("The host closed the connection before synchronization completed."));

                if (wasUnexpected)
                    unexpectedDisconnect = true;
            }
        }

        async Task<EZMessage> ReadMessage(NetworkReadBuffer buffer, CancellationToken token)
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
