using UnityEngine;

/// <summary>
/// Hazard / Void trigger volume.
/// Detects player entry (e.g. falling off platforms or touching death planes)
/// and triggers immediate respawn via CheckpointManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Header("Hazard Configuration")]
    [Tooltip("Optional custom label for this kill zone (e.g., 'Global Void', 'Sector 2 Hazard Pit').")]
    public string zoneName = "KillZone";

    [Tooltip("Optional override respawn position if not using CheckpointManager.")]
    public Transform customRespawnPoint;

    private void Awake()
    {
        // Ensure the attached collider is configured as a trigger volume
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect player presence via root/parent PlayerStateMachine
        PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
        if (player == null) return;

        Debug.Log($"[KillZone] Player entered '{zoneName}' at position {player.transform.position}. Initiating respawn.");

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer(player);
        }
        else if (customRespawnPoint != null)
        {
            // Fallback manual respawn if CheckpointManager is not present
            CharacterController cc = player.CC != null ? player.CC : player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = customRespawnPoint.position;
            player.transform.rotation = customRespawnPoint.rotation;

            if (cc != null) cc.enabled = true;

            player.CurrentHorizontalVelocity = Vector3.zero;
            player.ResetVerticalVelocity();
            player.SwitchState(typeof(PlayerIdleState));
        }
        else
        {
            Debug.LogWarning($"[KillZone] Cannot respawn player: CheckpointManager.Instance is null and no customRespawnPoint set on '{zoneName}'.");
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.35f);

        if (col is BoxCollider box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
