using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EZCollabTool
{
    [InitializeOnLoad]
    public static class EZCollabHooks
    {
        const double TransformSendInterval = 0.05;

        static readonly HashSet<string> pendingLockRequests = new HashSet<string>();
        static double lastTransformSend;
        static bool wasInSession;

        static EZCollabHooks()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.postprocessModifications += OnModifications;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (!EZCollabState.inSession)
            {
                if (wasInSession)
                    pendingLockRequests.Clear();

                wasInSession = false;
                return;
            }

            wasInSession = true;

            if (EZCollabState.isHost)
            {
                EZCollabSession.server?.ProcessMainThreadRequests();
            }
            else if (EZCollabSession.client != null && EZCollabSession.client.connectionLostUnexpectedly)
            {
                Debug.LogWarning("[EZCollab] The host connection was closed.");
                EZCollabSession.Stop();
                return;
            }

            ProcessIncomingMessages();
        }

        static void OnSelectionChanged()
        {
            if (!EZCollabState.inSession || EZCollabState.isApplyingRemoteChange) return;

            List<string> toRelease = new List<string>();
            foreach (string guid in EZCollabState.myLocks)
            {
                bool stillSelected = false;
                foreach (GameObject selectedObject in Selection.gameObjects)
                {
                    if (EZCollabState.TryGetGuid(selectedObject, out string selectedGuid) && selectedGuid == guid)
                    {
                        stillSelected = true;
                        break;
                    }
                }

                if (!stillSelected)
                    toRelease.Add(guid);
            }

            foreach (string guid in toRelease)
                ReleaseLock(guid);

            List<GameObject> selectableObjects = new List<GameObject>();
            bool removedLockedObject = false;
            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                if (EZCollabState.TryGetGuid(selectedObject, out string selectedGuid) &&
                    EZCollabState.IsLockedByOther(selectedGuid))
                {
                    removedLockedObject = true;
                    continue;
                }

                selectableObjects.Add(selectedObject);
            }

            if (removedLockedObject)
            {
                Selection.objects = selectableObjects.ToArray();
                return;
            }

            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                if (!EZCollabState.TryGetGuid(selectedObject, out string guid)) continue;
                if (EZCollabState.IsLockedByMe(guid)) continue;
                if (pendingLockRequests.Contains(guid)) continue;

                RequestLock(guid);
            }
        }

        static void OnHierarchyChanged()
        {
            if (!EZCollabState.inSession || EZCollabState.isApplyingRemoteChange) return;

            List<string> destroyedGuids = EZCollabState.GetDestroyedObjectGuids();
            foreach (string guid in destroyedGuids)
            {
                if (EZCollabState.IsLockedByMe(guid))
                    ReleaseLock(guid);

                EZCollabState.RemoveGuid(guid);
                SendDestroyObject(guid);
            }

            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
                CheckForNewObjects(root);
        }

        static void CheckForNewObjects(GameObject go)
        {
            if (!EZCollabState.objectToGuid.ContainsKey(go))
            {
                string guid = EZCollabState.GetOrAssignGuid(go);
                SendCreateObject(go, guid);
            }

            for (int i = 0; i < go.transform.childCount; i++)
                CheckForNewObjects(go.transform.GetChild(i).gameObject);
        }

        static UndoPropertyModification[] OnModifications(UndoPropertyModification[] modifications)
        {
            if (!EZCollabState.inSession || EZCollabState.isApplyingRemoteChange)
                return modifications;

            double now = EditorApplication.timeSinceStartup;
            if (now - lastTransformSend < TransformSendInterval)
                return modifications;

            HashSet<GameObject> dirtyObjects = new HashSet<GameObject>();
            foreach (UndoPropertyModification modification in modifications)
            {
                if (modification.currentValue.target is GameObject gameObject)
                    dirtyObjects.Add(gameObject);
                else if (modification.currentValue.target is Component component)
                    dirtyObjects.Add(component.gameObject);
            }

            foreach (GameObject gameObject in dirtyObjects)
            {
                if (!EZCollabState.TryGetGuid(gameObject, out string guid)) continue;
                if (!EZCollabState.IsLockedByMe(guid)) continue;

                TransformPayload payload = TransformPayload.FromTransform(guid, gameObject.transform);
                EZMessage message = EZMessage.Create(EZMessageType.TransformDelta, EZCollabState.localPeerId, payload);
                SendLocalMessage(message);
            }

            lastTransformSend = now;
            return modifications;
        }

        static void SendCreateObject(GameObject go, string guid)
        {
            CreateObjectPayload payload = EZSceneSerializer.CreatePayload(go, guid);
            EZMessage message = EZMessage.Create(EZMessageType.CreateObject, EZCollabState.localPeerId, payload);
            SendLocalMessage(message);
        }

        static void SendDestroyObject(string guid)
        {
            DestroyObjectPayload payload = new DestroyObjectPayload { guid = guid };
            EZMessage message = EZMessage.Create(EZMessageType.DestroyObject, EZCollabState.localPeerId, payload);
            SendLocalMessage(message);
        }

        static void SendLocalMessage(EZMessage message)
        {
            if (EZCollabState.isHost)
                _ = EZCollabSession.server?.SendFromHost(message);
            else
                _ = EZCollabSession.client?.Send(message);
        }

        static void RequestLock(string guid)
        {
            EZCollabEditGuard.BeginPendingLock(guid);
            pendingLockRequests.Add(guid);

            if (EZCollabState.isHost)
            {
                bool granted = EZCollabSession.server != null &&
                               EZCollabSession.server.TryAcquireHostLock(guid, EZCollabState.localPeerName);
                pendingLockRequests.Remove(guid);

                if (granted)
                {
                    EZCollabState.ApplyLock(guid, EZCollabState.localPeerId);
                    EZCollabEditGuard.ResolvePendingLock(guid, true);
                    return;
                }

                EZCollabEditGuard.ResolvePendingLock(guid, false);
                RemoveFromSelection(guid);
                return;
            }

            LockPayload payload = new LockPayload
            {
                guid = guid,
                peerId = EZCollabState.localPeerId,
                peerName = EZCollabState.localPeerName
            };
            EZMessage message = EZMessage.Create(EZMessageType.LockRequest, EZCollabState.localPeerId, payload);
            _ = EZCollabSession.client?.Send(message);
        }

        static void ReleaseLock(string guid)
        {
            EZCollabState.ReleaseLock(guid);

            if (EZCollabState.isHost)
            {
                EZCollabSession.server?.ReleaseHostLock(guid);
                return;
            }

            LockPayload payload = new LockPayload { guid = guid, peerId = EZCollabState.localPeerId };
            EZMessage message = EZMessage.Create(EZMessageType.LockRelease, EZCollabState.localPeerId, payload);
            _ = EZCollabSession.client?.Send(message);
        }

        static void ProcessIncomingMessages()
        {
            ConcurrentQueue<EZMessage> queue = EZCollabState.isHost
                ? EZCollabSession.server?.incomingMessages
                : EZCollabSession.client?.incomingMessages;

            if (queue == null) return;

            int processed = 0;
            while (queue.TryDequeue(out EZMessage message) && processed < 32)
            {
                ApplyMessage(message);
                processed++;
            }
        }

        static void ApplyMessage(EZMessage message)
        {
            switch (message.type)
            {
                case EZMessageType.SceneSnapshot:
                {
                    SceneSnapshot snapshot = message.GetPayload<SceneSnapshot>();
                    EZSceneSerializer.ApplySnapshot(snapshot);
                    break;
                }

                case EZMessageType.TransformDelta:
                {
                    TransformPayload payload = message.GetPayload<TransformPayload>();
                    if (!EZCollabState.guidToObject.TryGetValue(payload.guid, out GameObject go) || go == null)
                        break;

                    EZCollabState.isApplyingRemoteChange = true;
                    try
                    {
                        go.transform.position = payload.GetPosition();
                        go.transform.rotation = payload.GetRotation();
                        go.transform.localScale = payload.GetScale();
                    }
                    finally
                    {
                        EZCollabState.isApplyingRemoteChange = false;
                    }

                    break;
                }

                case EZMessageType.CreateObject:
                {
                    CreateObjectPayload payload = message.GetPayload<CreateObjectPayload>();
                    if (EZCollabState.guidToObject.ContainsKey(payload.guid)) break;

                    EZCollabState.isApplyingRemoteChange = true;
                    try
                    {
                        EZSceneSerializer.CreateFromPayload(payload);
                    }
                    finally
                    {
                        EZCollabState.isApplyingRemoteChange = false;
                    }

                    break;
                }

                case EZMessageType.DestroyObject:
                {
                    DestroyObjectPayload payload = message.GetPayload<DestroyObjectPayload>();
                    if (!EZCollabState.guidToObject.TryGetValue(payload.guid, out GameObject go) || go == null)
                        break;

                    EZCollabState.isApplyingRemoteChange = true;
                    try
                    {
                        EZCollabState.RemoveHierarchy(go);
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                    finally
                    {
                        EZCollabState.isApplyingRemoteChange = false;
                    }

                    break;
                }

                case EZMessageType.ObjectLocked:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    EZCollabState.ApplyLock(payload.guid, payload.peerId);
                    pendingLockRequests.Remove(payload.guid);
                    EZCollabEditGuard.ResolvePendingLock(
                        payload.guid,
                        payload.peerId == EZCollabState.localPeerId);
                    SceneView.RepaintAll();
                    break;
                }

                case EZMessageType.ObjectUnlocked:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    EZCollabState.ReleaseLock(payload.guid);
                    SceneView.RepaintAll();
                    break;
                }

                case EZMessageType.LockGranted:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    EZCollabState.ApplyLock(payload.guid, EZCollabState.localPeerId);
                    pendingLockRequests.Remove(payload.guid);
                    EZCollabEditGuard.ResolvePendingLock(payload.guid, true);
                    break;
                }

                case EZMessageType.LockDenied:
                {
                    LockPayload payload = message.GetPayload<LockPayload>();
                    pendingLockRequests.Remove(payload.guid);
                    EZCollabEditGuard.ResolvePendingLock(payload.guid, false);
                    RemoveFromSelection(payload.guid);

                    break;
                }

                case EZMessageType.PeerJoined:
                {
                    PeerPayload payload = message.GetPayload<PeerPayload>();
                    if (payload.peerId != EZCollabState.localPeerId)
                        EZCollabState.connectedPeers[payload.peerId] = payload.peerName;
                    break;
                }

                case EZMessageType.PeerLeft:
                {
                    PeerPayload payload = message.GetPayload<PeerPayload>();
                    EZCollabState.connectedPeers.Remove(payload.peerId);
                    EZCollabState.ReleaseAllLocksForPeer(payload.peerId);
                    SceneView.RepaintAll();
                    break;
                }
            }
        }

        static void RemoveFromSelection(string guid)
        {
            if (!EZCollabState.guidToObject.TryGetValue(guid, out GameObject go) || go == null)
                return;

            List<GameObject> newSelection = new List<GameObject>(Selection.gameObjects);
            newSelection.Remove(go);
            Selection.objects = newSelection.ToArray();
        }
    }
}
