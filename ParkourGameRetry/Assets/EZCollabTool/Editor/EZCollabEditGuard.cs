using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EZCollabTool
{
    [InitializeOnLoad]
    public static class EZCollabEditGuard
    {
        static readonly Dictionary<GameObject, bool> previousPickingState =
            new Dictionary<GameObject, bool>();
        static readonly Dictionary<string, PendingLockState> pendingLocks =
            new Dictionary<string, PendingLockState>();
        static readonly Color lockedRowColor = new Color(0.35f, 0.35f, 0.35f, 0.45f);
        static bool toolsWereHidden;

        sealed class PendingLockState
        {
            public GameObject gameObject;
            public Transform parent;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public int siblingIndex;
        }

        static EZCollabEditGuard()
        {
            EditorApplication.update += RefreshPickingState;
            Selection.selectionChanged += RemoveLockedObjectsFromSelection;
            EditorApplication.hierarchyWindowItemOnGUI += DrawLockedHierarchyRow;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAllState;
            EditorApplication.quitting += RestoreAllState;
        }

        public static void BeginPendingLock(string guid)
        {
            if (string.IsNullOrEmpty(guid) || pendingLocks.ContainsKey(guid) ||
                !EZCollabState.guidToObject.TryGetValue(guid, out GameObject go) || go == null)
            {
                return;
            }

            Transform transform = go.transform;
            pendingLocks[guid] = new PendingLockState
            {
                gameObject = go,
                parent = transform.parent,
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                localScale = transform.localScale,
                siblingIndex = transform.GetSiblingIndex()
            };

            if (pendingLocks.Count == 1)
                toolsWereHidden = Tools.hidden;
            Tools.hidden = true;
        }

        public static void ResolvePendingLock(string guid, bool granted)
        {
            if (!pendingLocks.TryGetValue(guid, out PendingLockState state))
                return;

            RestorePendingTransform(state);

            pendingLocks.Remove(guid);
            if (pendingLocks.Count == 0)
                Tools.hidden = toolsWereHidden;
        }

        static void RestorePendingTransform(PendingLockState state)
        {
            if (state.gameObject == null)
                return;

            bool wasApplyingRemoteChange = EZCollabState.isApplyingRemoteChange;
            EZCollabState.isApplyingRemoteChange = true;
            try
            {
                Transform transform = state.gameObject.transform;
                transform.SetParent(state.parent, false);
                transform.localPosition = state.localPosition;
                transform.localRotation = state.localRotation;
                transform.localScale = state.localScale;
                transform.SetSiblingIndex(state.siblingIndex);
                EditorUtility.SetDirty(state.gameObject);
            }
            finally
            {
                EZCollabState.isApplyingRemoteChange = wasApplyingRemoteChange;
            }
        }

        static void CancelPendingLocks()
        {
            List<string> guids = new List<string>(pendingLocks.Keys);
            foreach (string guid in guids)
                ResolvePendingLock(guid, false);
        }

        static void RefreshPickingState()
        {
            if (!EZCollabState.inSession && pendingLocks.Count > 0)
                CancelPendingLocks();

            HashSet<GameObject> shouldBeProtected = new HashSet<GameObject>();
            if (EZCollabState.inSession)
            {
                foreach (KeyValuePair<string, string> pair in EZCollabState.lockedObjects)
                {
                    if (pair.Value == EZCollabState.localPeerId)
                        continue;

                    if (EZCollabState.guidToObject.TryGetValue(pair.Key, out GameObject go) && go != null)
                        shouldBeProtected.Add(go);
                }
            }

            List<GameObject> toRestore = new List<GameObject>();
            foreach (KeyValuePair<GameObject, bool> pair in previousPickingState)
            {
                if (pair.Key == null || !shouldBeProtected.Contains(pair.Key))
                    toRestore.Add(pair.Key);
            }

            foreach (GameObject go in toRestore)
                RestorePickingState(go);

            foreach (GameObject go in shouldBeProtected)
            {
                if (previousPickingState.ContainsKey(go))
                    continue;

                bool wasDisabled = SceneVisibilityManager.instance.IsPickingDisabled(go);
                previousPickingState[go] = wasDisabled;
                if (!wasDisabled)
                    SceneVisibilityManager.instance.DisablePicking(go, false);
            }

            if (shouldBeProtected.Count > 0)
                RemoveLockedObjectsFromSelection();
        }

        static void RemoveLockedObjectsFromSelection()
        {
            if (!EZCollabState.inSession || Selection.gameObjects.Length == 0)
                return;

            List<GameObject> allowed = new List<GameObject>();
            bool removedAny = false;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (EZCollabState.TryGetGuid(go, out string guid) && EZCollabState.IsLockedByOther(guid))
                {
                    removedAny = true;
                    continue;
                }

                allowed.Add(go);
            }

            if (removedAny)
            {
                Selection.objects = allowed.ToArray();
                SceneView.RepaintAll();
            }
        }

        static void DrawLockedHierarchyRow(int instanceId, Rect selectionRect)
        {
            if (!EZCollabState.inSession)
                return;

            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null || !EZCollabState.TryGetGuid(go, out string guid) ||
                !EZCollabState.IsLockedByOther(guid))
            {
                return;
            }

            EditorGUI.DrawRect(selectionRect, lockedRowColor);
            Rect badgeRect = new Rect(selectionRect.xMax - 48f, selectionRect.y, 46f, selectionRect.height);
            GUI.Label(badgeRect, "LOCK", EditorStyles.centeredGreyMiniLabel);
        }

        static void RestorePickingState(GameObject go)
        {
            if (ReferenceEquals(go, null) || !previousPickingState.TryGetValue(go, out bool wasDisabled))
                return;

            previousPickingState.Remove(go);
            if (go != null && !wasDisabled)
                SceneVisibilityManager.instance.EnablePicking(go, false);
        }

        static void RestoreAllState()
        {
            CancelPendingLocks();

            List<GameObject> objects = new List<GameObject>(previousPickingState.Keys);
            foreach (GameObject go in objects)
                RestorePickingState(go);
        }
    }
}
