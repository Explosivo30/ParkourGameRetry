using System;
using System.Threading.Tasks;
using UnityEngine;

namespace EZCollabTool
{
    public static class EZCollabSession
    {
        public static EZCollabServer server { get; private set; }
        public static EZCollabClient client { get; private set; }

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
            client = new EZCollabClient();
            bool connected = await client.Connect(host, port);

            if (!connected)
            {
                client.Disconnect();
                client = null;
                EZCollabState.Shutdown();
            }

            return connected;
        }

        public static void Stop()
        {
            bool hadSession = EZCollabState.inSession;
            server?.Stop();
            client?.Disconnect();
            server = null;
            client = null;
            EZCollabState.Shutdown();

            if (hadSession)
                Debug.Log("[EZCollab] Session stopped");
        }
    }
}
