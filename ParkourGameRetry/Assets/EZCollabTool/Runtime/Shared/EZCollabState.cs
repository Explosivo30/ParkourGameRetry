using System;
using System.Collections.Generic;
using UnityEngine;

namespace EZCollabTool
{
    public static class EZCollabState
    {
        public static string localPeerId { get; private set; }
        public static string localPeerName { get; private set; }

        public static bool isHost { get; private set; }
        public static bool inSession { get; private set; }
        public static bool isApplyingRemoteChange = false;

        public static Dictionary<string, GameObject> guidToObject = new Dictionary<string, GameObject>();
        public static Dictionary<GameObject, string> objectToGuid = new Dictionary<GameObject, string>();
        public static Dictionary<string, string> connectedPeers = new Dictionary<string, string>();
        public static Dictionary<string, string> lockedObjects = new Dictionary<string, string>();
        public static HashSet<string> myLocks = new HashSet<string>();

        public static void Initialize(string peerName, bool asHost)
        {
            localPeerId = Guid.NewGuid().ToString("N").Substring(0, 8);
            localPeerName = peerName;
            isHost = asHost;
            inSession = true;
            isApplyingRemoteChange = false;

            guidToObject.Clear();
            objectToGuid.Clear();
            connectedPeers.Clear();
            lockedObjects.Clear();
            myLocks.Clear();

            IndexExistingScene();
        }

        public static void Shutdown()
        {
            inSession = false;
            isHost = false;
            isApplyingRemoteChange = false;
            guidToObject.Clear();
            objectToGuid.Clear();
            connectedPeers.Clear();
            lockedObjects.Clear();
            myLocks.Clear();
        }

        public static string GetOrAssignGuid(GameObject go)
        {
            if (objectToGuid.TryGetValue(go, out string existing))
                return existing;

            string guid = Guid.NewGuid().ToString("N").Substring(0, 12);
            RegisterGuid(go, guid);
            return guid;
        }

        public static void RegisterGuid(GameObject go, string guid)
        {
            if (go == null || string.IsNullOrEmpty(guid)) return;

            if (objectToGuid.TryGetValue(go, out string previousGuid) && previousGuid != guid)
                guidToObject.Remove(previousGuid);

            if (guidToObject.TryGetValue(guid, out GameObject previousObject) && previousObject != go)
                objectToGuid.Remove(previousObject);

            guidToObject[guid] = go;
            objectToGuid[go] = guid;
        }

        public static bool TryGetGuid(GameObject go, out string guid)
        {
            return objectToGuid.TryGetValue(go, out guid);
        }

        public static void RemoveObject(GameObject go)
        {
            if (objectToGuid.TryGetValue(go, out string guid))
                RemoveGuid(guid);
        }

        public static void RemoveGuid(string guid)
        {
            if (guidToObject.TryGetValue(guid, out GameObject go))
                objectToGuid.Remove(go);

            guidToObject.Remove(guid);
            lockedObjects.Remove(guid);
            myLocks.Remove(guid);
        }

        public static void RemoveHierarchy(GameObject go)
        {
            if (go == null) return;

            for (int i = go.transform.childCount - 1; i >= 0; i--)
                RemoveHierarchy(go.transform.GetChild(i).gameObject);

            RemoveObject(go);
        }

        public static List<string> GetDestroyedObjectGuids()
        {
            List<string> destroyedGuids = new List<string>();
            foreach (KeyValuePair<string, GameObject> pair in guidToObject)
            {
                if (pair.Value == null)
                    destroyedGuids.Add(pair.Key);
            }

            return destroyedGuids;
        }

        public static void ClearObjectMappings()
        {
            guidToObject.Clear();
            objectToGuid.Clear();
            lockedObjects.Clear();
            myLocks.Clear();
        }

        public static bool IsLockedByOther(string guid)
        {
            if (!lockedObjects.TryGetValue(guid, out string owner)) return false;
            return owner != localPeerId;
        }

        public static bool IsLockedByMe(string guid)
        {
            return myLocks.Contains(guid);
        }

        public static string GetLockOwnerName(string guid)
        {
            if (!lockedObjects.TryGetValue(guid, out string peerId)) return null;
            if (connectedPeers.TryGetValue(peerId, out string name)) return name;
            return peerId;
        }

        public static void ApplyLock(string guid, string peerId)
        {
            lockedObjects[guid] = peerId;
            if (peerId == localPeerId)
                myLocks.Add(guid);
        }

        public static void ReleaseLock(string guid)
        {
            if (lockedObjects.TryGetValue(guid, out string peerId) && peerId == localPeerId)
                myLocks.Remove(guid);

            lockedObjects.Remove(guid);
        }

        public static void ReleaseAllLocksForPeer(string peerId)
        {
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, string> pair in lockedObjects)
            {
                if (pair.Value == peerId)
                    toRemove.Add(pair.Key);
            }

            foreach (string guid in toRemove)
                ReleaseLock(guid);
        }

        static void IndexExistingScene()
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
                IndexHierarchy(root);
        }

        static void IndexHierarchy(GameObject go)
        {
            GetOrAssignGuid(go);
            for (int i = 0; i < go.transform.childCount; i++)
                IndexHierarchy(go.transform.GetChild(i).gameObject);
        }
    }
}
