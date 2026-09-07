using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for parkour checkpoints and player respawn coordination.
/// Discovers, sorts, and tracks checkpoints by sector index, and safely teleports
/// the player upon death/respawn with proper CharacterController and physics resets.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Current State")]
    [Tooltip("The currently active checkpoint where the player will respawn.")]
    public Checkpoint currentCheckpoint;

    [Tooltip("All registered checkpoints in the scene, sorted by sectorIndex.")]
    public List<Checkpoint> allCheckpoints = new List<Checkpoint>();

    [Header("Default Fallback")]
    [Tooltip("Fallback spawn point if no checkpoint is active yet.")]
    public Transform defaultSpawnPoint;

    private void Awake()
    {
        // Singleton pattern enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CheckpointManager] Duplicate CheckpointManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Auto-discover and register all checkpoints in scene if list is empty
        if (allCheckpoints == null || allCheckpoints.Count == 0)
        {
            RefreshCheckpoints();
        }
        else
        {
            SortCheckpoints();
        }
    }

    private void Start()
    {
        // If no active checkpoint is designated, activate the first discovered checkpoint
        if (currentCheckpoint == null && allCheckpoints.Count > 0)
        {
            ActivateCheckpoint(allCheckpoints[0]);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Scans the active scene for all Checkpoint components, sorts them by sectorIndex,
    /// and initializes their active states.
    /// </summary>
    public void RefreshCheckpoints()
    {
        allCheckpoints.Clear();
        Checkpoint[] found = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        allCheckpoints.AddRange(found);
        SortCheckpoints();

        // Initialize visual states
        foreach (Checkpoint cp in allCheckpoints)
        {
            if (cp != null)
            {
                cp.SetActiveState(cp == currentCheckpoint);
            }
        }
    }

    /// <summary>
    /// Sorts all registered checkpoints in ascending order of sectorIndex.
    /// </summary>
    public void SortCheckpoints()
    {
        allCheckpoints.RemoveAll(cp => cp == null);
        allCheckpoints.Sort((a, b) => a.sectorIndex.CompareTo(b.sectorIndex));
    }

    /// <summary>
    /// Dynamically registers a checkpoint with the manager.
    /// </summary>
    public void RegisterCheckpoint(Checkpoint cp)
    {
        if (cp == null) return;

        if (!allCheckpoints.Contains(cp))
        {
            allCheckpoints.Add(cp);
            SortCheckpoints();
        }

        if (currentCheckpoint == null)
        {
            ActivateCheckpoint(cp);
        }
        else
        {
            cp.SetActiveState(cp == currentCheckpoint);
        }
    }

    /// <summary>
    /// Activates a given checkpoint, deactivating the previously active one.
    /// </summary>
    /// <param name="cp">The checkpoint to activate.</param>
    public void ActivateCheckpoint(Checkpoint cp)
    {
        if (cp == null) return;

        if (currentCheckpoint != null && currentCheckpoint != cp)
        {
            currentCheckpoint.SetActiveState(false);
        }

        currentCheckpoint = cp;
        currentCheckpoint.SetActiveState(true);

        Debug.Log($"[CheckpointManager] Checkpoint Activated -> Sector {cp.sectorIndex}: '{cp.sectorName}' at {cp.transform.position}");
    }

    /// <summary>
    /// Safely respawns the player at the current checkpoint (or default spawn point).
    /// Handles CharacterController lifecycle, position/rotation teleportation,
    /// velocity zeroing, and state transition to PlayerIdleState.
    /// </summary>
    /// <param name="player">The PlayerStateMachine instance to respawn.</param>
    public void RespawnPlayer(PlayerStateMachine player)
    {
        if (player == null)
        {
            Debug.LogWarning("[CheckpointManager] Attempted to respawn a null player reference.");
            return;
        }

        // Determine destination position and rotation
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (currentCheckpoint != null && currentCheckpoint.spawnPoint != null)
        {
            targetPosition = currentCheckpoint.spawnPoint.position;
            targetRotation = currentCheckpoint.spawnPoint.rotation;
        }
        else if (defaultSpawnPoint != null)
        {
            targetPosition = defaultSpawnPoint.position;
            targetRotation = defaultSpawnPoint.rotation;
        }
        else
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        // Step a: Temporarily disable CharacterController to allow transform manipulation without physics interference
        CharacterController cc = player.CC != null ? player.CC : player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // Step b: Reposition and rotate player
        player.transform.position = targetPosition;
        player.transform.rotation = targetRotation;

        // Step c: Re-enable CharacterController
        if (cc != null)
        {
            cc.enabled = true;
        }

        // Step d: Reset horizontal and vertical velocities
        player.CurrentHorizontalVelocity = Vector3.zero;
        player.ResetVerticalVelocity();
        if (player.Motor != null)
        {
            player.Motor.HorizontalVelocity = Vector3.zero;
            player.Motor.VerticalVelocity = 0f;
        }

        // Step e: Reset auxiliary states (grapple, vault, camera tilt, collider height)
        player.SetGrappleActive(false);
        player.RestoreCollider();
        player.ResetVaultPhysics();
        player.SetCameraTilt(0f);
        player.SetCameraRollPitch(0f);

        // Step f: Switch state back to PlayerIdleState
        player.SwitchState(typeof(PlayerIdleState));

        Debug.Log($"[CheckpointManager] Player successfully respawned at Sector { (currentCheckpoint != null ? currentCheckpoint.sectorIndex.ToString() : "Default") } ({targetPosition})");
    }
}
