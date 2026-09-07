using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum NpcParkourType
{
    Vault,
    WallRun,
    LedgeGrab,
    Jump
}

// Opcional: Requiere NavMeshLink de la package de AI Navigation (Unity.AI.Navigation)
// public class ParkourLinkData : MonoBehaviour ...

public class ParkourLinkData : MonoBehaviour
{
    public NpcParkourType ParkourType;
    public float Duration = 1.0f;
    public float ArcHeight = 0f;
    public string AnimationTrigger = "Vault";

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Handles.Label(transform.position + Vector3.up, ParkourType.ToString());
    }
#endif
}
