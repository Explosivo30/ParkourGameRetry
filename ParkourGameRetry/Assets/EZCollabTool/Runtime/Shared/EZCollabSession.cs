using System;
using System.Threading.Tasks;
using UnityEngine;

namespace EZCollabTool
{
    public static class EZCollabSession
    {
        public static EZCollabServer server { get; private set; }
        public static EZCollabClient client { get; private set; }
        public static bool isReconnecting { get; private set; }

        static string lastHost;
        static int lastPort;
        static int reconnectGeneration;

        public static bool StartHost(string peerName, int port)
        {
            EZCollabState.Initialize(peerName, true);
            server = new EZCollabServer();

            try
            {
                server.Start(port);
                Debug.Log($"[EZCollab] Session started on port {port} as \"{peerName}\"");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EZCollab] Could not start the server: {exception.Message}");
                server.Stop();
                server = null;
                EZCollabState.Shutdown();
                return false;
            }
        }

        public static async Task<bool> JoinAsClient(string peerName, string host, int port)
        {
            EZCollabState.Initialize(peerName, false);
            lastHost = host;
            lastPort = port;
            isReconnecting = false;
            client = new EZCollabClient();
            bool connected = await client.Connect(host, port, 0);

            if (!connected)
            {
                client.Disconnect();
                client = null;
                EZCollabState.Shutdown();
            }

            return connected;
        }

        public static async void BeginReconnect()
        {
            if (!EZCollabState.inSession || EZCollabState.isHost || isReconnecting ||
                string.IsNullOrEmpty(lastHost))
            {
                return;
            }

            isReconnecting = true;
            int generation = ++reconnectGeneration;
            long lastSequence = EZCollabState.lastServerSequence;
            EZCollabState.PrepareForReconnect();
            client?.Disconnect();
            Debug.LogWarning($"[EZCollab] Connection lost. Reconnecting from revision {lastSequence}...");

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                if (!EZCollabState.inSession || generation != reconnectGeneration)
                    return;

                if (attempt > 1)
                    await Task.Delay(Math.Min(5000, attempt * 1000));

                EZCollabClient replacement = new EZCollabClient();
                client = replacement;
                if (await replacement.Connect(lastHost, lastPort, lastSequence))
                {
                    if (generation != reconnectGeneration)
                    {
                        replacement.Disconnect();
                        return;
                    }

                    isReconnecting = false;
                    Debug.Log("[EZCollab] Connection restored. Incremental changes were requested.");
                    return;
                }
            }

            if (generation == reconnectGeneration && EZCollabState.inSession)
            {
                isReconnecting = false;
                Debug.LogError("[EZCollab] Could not reconnect after 5 attempts. The session was stopped.");
                Stop();
            }
        }

        public static void Stop()
        {
            bool hadSession = EZCollabState.inSession;
            reconnectGeneration++;
            isReconnecting = false;
            server?.Stop();
            client?.Disconnect();
            server = null;
            client = null;
            EZCollabState.Shutdown();
            lastHost = null;
            lastPort = 0;

            if (hadSession)
                Debug.Log("[EZCollab] Session stopped");
        }
    }
}
