using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EZCollabTool
{
    public class EZCollabWindow : EditorWindow
    {
        string peerName = "";
        string hostIp = "";
        int port = 9876;
        bool isConnecting;

        [MenuItem("Window/EZCollabTool")]
        static void Open()
        {
            GetWindow<EZCollabWindow>("EZCollab");
        }

        void OnEnable()
        {
            peerName = System.Environment.UserName;
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        void OnGUI()
        {
            if (EZCollabState.inSession)
                DrawSessionView();
            else
                DrawConnectView();
        }

        void DrawConnectView()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("EZCollabTool", EditorStyles.boldLabel);
            GUILayout.Space(4);

            peerName = EditorGUILayout.TextField("Tu nombre", peerName);
            port = EditorGUILayout.IntField("Puerto", port);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Host", EditorStyles.boldLabel);

            if (!isConnecting && GUILayout.Button("Start session (host)"))
            {
                if (!ValidateNameAndPort()) return;

                bool started = EZCollabSession.StartHost(peerName.Trim(), port);
                if (!started)
                    EditorUtility.DisplayDialog("EZCollab", $"No se pudo abrir el puerto TCP {port}.", "Ok");
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Cliente", EditorStyles.boldLabel);
            hostIp = EditorGUILayout.TextField("IP del host", hostIp);

            if (!isConnecting && GUILayout.Button("Join session"))
            {
                if (!ValidateNameAndPort()) return;
                if (string.IsNullOrWhiteSpace(hostIp))
                {
                    EditorUtility.DisplayDialog("EZCollab", "Introduce la IP o el nombre del host.", "Ok");
                    return;
                }

                ConnectAsClient();
            }

            if (isConnecting)
                EditorGUILayout.HelpBox("Conectando y esperando la sincronización inicial...", UnityEditor.MessageType.Info);
        }

        bool ValidateNameAndPort()
        {
            if (string.IsNullOrWhiteSpace(peerName))
            {
                EditorUtility.DisplayDialog("EZCollab", "Introduce tu nombre antes de empezar.", "Ok");
                return false;
            }

            if (port < 1 || port > 65535)
            {
                EditorUtility.DisplayDialog("EZCollab", "El puerto debe estar entre 1 y 65535.", "Ok");
                return false;
            }

            return true;
        }

        async void ConnectAsClient()
        {
            isConnecting = true;
            Repaint();

            bool connected = await EZCollabSession.JoinAsClient(peerName.Trim(), hostIp.Trim(), port);
            isConnecting = false;

            if (!connected)
                EditorUtility.DisplayDialog("EZCollab", $"No se pudo conectar a {hostIp}:{port}", "Ok");

            Repaint();
        }

        void DrawSessionView()
        {
            GUILayout.Space(8);

            string role = EZCollabState.isHost ? "HOST" : "cliente";
            EditorGUILayout.LabelField($"Sesión activa — {role}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Tú: {EZCollabState.localPeerName}");
            EditorGUILayout.LabelField(
                $"Objetos indexados: {EZCollabState.guidToObject.Count} · Revisión: {EZCollabState.lastServerSequence}",
                EditorStyles.miniLabel);

            if (EZCollabSession.isReconnecting)
                EditorGUILayout.HelpBox(
                    "Conexión perdida. Recuperando únicamente las revisiones pendientes...",
                    UnityEditor.MessageType.Warning);

            if (EZCollabState.isHost)
            {
                int count = EZCollabSession.server?.clientCount ?? 0;
                EditorGUILayout.LabelField($"Clientes conectados: {count}");
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Peers en sesión", EditorStyles.boldLabel);

            if (EZCollabState.connectedPeers.Count == 0)
            {
                EditorGUILayout.LabelField("Nadie más conectado aún", EditorStyles.miniLabel);
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in EZCollabState.connectedPeers)
                    EditorGUILayout.LabelField($"  {pair.Value}");
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Locks activos", EditorStyles.boldLabel);

            bool anyLocks = false;
            foreach (KeyValuePair<string, string> pair in EZCollabState.lockedObjects)
            {
                if (!EZCollabState.guidToObject.TryGetValue(pair.Key, out GameObject go) || go == null)
                    continue;

                string ownerName = EZCollabState.GetLockOwnerName(pair.Key) ?? pair.Value;
                string marker = pair.Value == EZCollabState.localPeerId ? " (tú)" : "";
                EditorGUILayout.LabelField($"  {go.name} → {ownerName}{marker}", EditorStyles.miniLabel);
                anyLocks = true;
            }

            if (!anyLocks)
                EditorGUILayout.LabelField("Ninguno", EditorStyles.miniLabel);

            if (EZCollabState.assetWarnings.Count > 0)
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField(
                    $"Assets incompatibles ({EZCollabState.assetWarnings.Count})",
                    EditorStyles.boldLabel);

                int displayed = 0;
                foreach (string warning in EZCollabState.assetWarnings)
                {
                    EditorGUILayout.HelpBox(warning, UnityEditor.MessageType.Warning);
                    if (++displayed >= 5)
                        break;
                }

                if (EZCollabState.assetWarnings.Count > displayed)
                    EditorGUILayout.LabelField(
                        $"Y {EZCollabState.assetWarnings.Count - displayed} advertencias más en la consola.",
                        EditorStyles.miniLabel);

                if (GUILayout.Button("Limpiar advertencias de assets"))
                    EZCollabState.assetWarnings.Clear();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Stop session") &&
                EditorUtility.DisplayDialog("EZCollab", "¿Terminar la sesión?", "Sí", "Cancelar"))
            {
                EZCollabSession.Stop();
            }

            GUILayout.Space(4);
        }
    }
}
