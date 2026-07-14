using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EZCollabTool
{
    [InitializeOnLoad]
    public static class EZCollabOverlay
    {
        static readonly Color lockedByOtherColor = new Color(1f, 0.3f, 0.3f, 0.35f);
        static readonly Color lockedByMeColor = new Color(0.3f, 0.8f, 0.3f, 0.25f);

        static EZCollabOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            if (!EZCollabState.inSession) return;

            foreach (KeyValuePair<string, string> pair in EZCollabState.lockedObjects)
            {
                if (!EZCollabState.guidToObject.TryGetValue(pair.Key, out GameObject go) || go == null)
                    continue;

                bool mine = pair.Value == EZCollabState.localPeerId;
                string ownerName = EZCollabState.GetLockOwnerName(pair.Key);
                DrawLockIndicator(go, mine, ownerName);
            }
        }

        static void DrawLockIndicator(GameObject go, bool mine, string ownerName)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            Bounds bounds = renderer != null
                ? renderer.bounds
                : new Bounds(go.transform.position, Vector3.one * 0.5f);

            Handles.color = mine ? lockedByMeColor : lockedByOtherColor;
            Handles.DrawWireCube(bounds.center, bounds.size * 1.05f);

            if (!mine && ownerName != null)
            {
                Handles.color = Color.white;
                Handles.Label(
                    bounds.center + Vector3.up * (bounds.extents.y + 0.3f),
                    ownerName,
                    EditorStyles.boldLabel);
            }
        }
    }
}
