using UnityEngine;

/// <summary>
/// Trigger-based checkpoint component for the parkour scene.
/// Detects player arrival, triggers activation in CheckpointManager,
/// and updates visual and audio feedback.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Sector Information")]
    [Tooltip("Index of the sector this checkpoint belongs to (0-indexed).")]
    public int sectorIndex;

    [Tooltip("Display name or identifier of the sector/area.")]
    public string sectorName = "Sector";

    [Header("Spawn Configuration")]
    [Tooltip("Transform position and rotation where the player will respawn upon death.")]
    public Transform spawnPoint;

    [Header("Visual & Audio Feedback")]
    [Tooltip("Material used when this checkpoint is active (e.g. Neon Green/Cyan).")]
    public Material activeMaterial;

    [Tooltip("Material used when this checkpoint is inactive (e.g. Slate Grey/Orange).")]
    public Material inactiveMaterial;

    [Tooltip("MeshRenderer of the beacon or indicator to apply materials/colors to.")]
    public MeshRenderer beaconRenderer;

    [Tooltip("Audio clip played when activating this checkpoint.")]
    public AudioClip activationSound;

    [Tooltip("Optional AudioSource component for activation SFX.")]
    public AudioSource audioSource;

    [Header("State")]
    [Tooltip("Whether this checkpoint is currently the active respawn point.")]
    public bool isActivated;

    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Default spawn point to own transform if unassigned
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Try to find beaconRenderer if unassigned
        if (beaconRenderer == null)
        {
            beaconRenderer = GetComponentInChildren<MeshRenderer>();
        }

        // Try to find AudioSource if unassigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Apply initial visual state
        ApplyVisualState(isActivated);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect player arrival
        PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
        if (player != null && !isActivated)
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.ActivateCheckpoint(this);
            }
            else
            {
                SetActiveState(true);
            }
        }
    }

    /// <summary>
    /// Sets the active/inactive state of this checkpoint and updates visual/audio feedback.
    /// </summary>
    /// <param name="active">True if this checkpoint should be activated, false to deactivate.</param>
    public void SetActiveState(bool active)
    {
        bool wasActive = isActivated;
        isActivated = active;

        ApplyVisualState(active);

        // Play activation audio feedback on transition to active
        if (active && !wasActive && audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }

    /// <summary>
    /// Updates renderer materials or colors depending on active state.
    /// </summary>
    private void ApplyVisualState(bool active)
    {
        if (beaconRenderer == null) return;

        if (active && activeMaterial != null)
        {
            beaconRenderer.material = activeMaterial;
        }
        else if (!active && inactiveMaterial != null)
        {
            beaconRenderer.material = inactiveMaterial;
        }
        else
        {
            // Fallback: modify emission and color on the instance material directly
            Material mat = beaconRenderer.material;
            Color activeColor = new Color(0.1f, 1.0f, 0.7f, 1f);   // Neon Emerald/Cyan
            Color inactiveColor = new Color(0.35f, 0.35f, 0.4f, 1f); // Muted Industrial Slate

            Color targetColor = active ? activeColor : inactiveColor;

            if (mat.HasProperty(BaseColorProperty))
            {
                mat.SetColor(BaseColorProperty, targetColor);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.color = targetColor;
            }

            if (mat.HasProperty(EmissionColorProperty))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorProperty, active ? targetColor * 2.5f : Color.black);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Visual indicator in Scene view
        Gizmos.color = isActivated ? new Color(0f, 1f, 0.6f, 0.7f) : new Color(1f, 0.6f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 1.2f);

        Transform spawn = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(spawn.position, spawn.forward * 2f);
        Gizmos.DrawWireCube(spawn.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
    }
}
