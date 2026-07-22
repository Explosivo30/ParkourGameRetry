using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EZCollabTool
{
    [InitializeOnLoad]
    public static class EZCollabOverlay
    {
        const float GhostFillOpacity = 0.18f;
        const float WireAlpha = 0.9f;

        static EZCollabOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            if (!EZCollabState.inSession || Event.current.type != EventType.Repaint)
                return;

            foreach (KeyValuePair<string, string> pair in EZCollabState.lockedObjects)
            {
                if (pair.Value == EZCollabState.localPeerId)
                    continue;

                if (!EZCollabState.guidToObject.TryGetValue(pair.Key, out GameObject go) || go == null)
                    continue;

                string ownerName = EZCollabState.GetLockOwnerName(pair.Key);
                DrawLockIndicator(go, EZCollabPeerColors.Get(pair.Value), ownerName);
            }
        }

        static void DrawLockIndicator(GameObject go, Color peerColor, string ownerName)
        {
            Renderer[] renderers = go.GetComponents<Renderer>();
            Bounds bounds = GetBounds(go, renderers);

            if (renderers.Length > 0)
                Handles.DrawOutline(renderers, peerColor, GhostFillOpacity);

            Color wireColor = peerColor;
            wireColor.a = WireAlpha;
            using (new Handles.DrawingScope(wireColor))
                Handles.DrawWireCube(bounds.center, bounds.size * 1.035f);

            if (!string.IsNullOrEmpty(ownerName))
            {
                Handles.Label(
                    bounds.center + Vector3.up * (bounds.extents.y + 0.3f),
                    EZCollabLocalization.Tr(EZCollabText.LockSceneLabel, ownerName),
                    EditorStyles.boldLabel);
            }
        }

        static Bounds GetBounds(GameObject go, Renderer[] renderers)
        {
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return bounds;
            }

            Collider collider = go.GetComponent<Collider>();
            return collider != null
                ? collider.bounds
                : new Bounds(go.transform.position, Vector3.one * 0.5f);
        }
    }

    internal static class EZCollabPeerColors
    {
        static readonly Color[] palette =
        {
            FromRgb(67, 160, 255),
            FromRgb(255, 99, 99),
            FromRgb(62, 207, 142),
            FromRgb(255, 174, 66),
            FromRgb(186, 104, 200),
            FromRgb(0, 188, 212),
            FromRgb(240, 98, 146),
            FromRgb(139, 195, 74),
            FromRgb(255, 112, 67),
            FromRgb(126, 87, 194),
            FromRgb(38, 198, 218),
            FromRgb(255, 202, 40)
        };

        public static Color Get(string peerId)
        {
            uint hash = 2166136261;
            if (!string.IsNullOrEmpty(peerId))
            {
                unchecked
                {
                    for (int i = 0; i < peerId.Length; i++)
                    {
                        hash ^= peerId[i];
                        hash *= 16777619;
                    }
                }
            }

            return palette[hash % (uint)palette.Length];
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Color FromRgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 255);
        }
    }
}
