## 2026-08-17T16:53:20Z
You are Worker M1 on the Parkour Scene Creation project.
Your working directory is: d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/teamwork_preview_worker_m1
You MUST read the authoritative user request at: d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/ORIGINAL_REQUEST.md
You MUST read the project architecture and interface contracts at: d:/Unity/ParkourGameRetry/ParkourGameRetry/PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write Ownership:
You exclusively own and will create files in: `Assets/Scripts/ParkourScene/`.

Mission - Implement Core Framework & Scene Scripts for the parkour scene:
1. `Assets/Scripts/ParkourScene/Checkpoint.cs`:
   - Trigger-based checkpoint (`BoxCollider` or `SphereCollider` with `isTrigger = true`).
   - Properties: `int sectorIndex`, `string sectorName`, `Transform spawnPoint`, `Material activeMaterial`, `Material inactiveMaterial`, `MeshRenderer beaconRenderer`, `bool isActivated`.
   - Methods: `SetActiveState(bool active)`, `OnTriggerEnter(Collider other)` detecting `PlayerStateMachine` and notifying `CheckpointManager.Instance.ActivateCheckpoint(this)`.
   - Visual and audio feedback: beacon color change (e.g. inactive orange/grey -> active neon green/cyan), sound effect playback if audio source present.

2. `Assets/Scripts/ParkourScene/CheckpointManager.cs`:
   - Singleton pattern (`Instance`).
   - Fields: `Checkpoint currentCheckpoint`, `List<Checkpoint> allCheckpoints`.
   - Methods:
     - `Awake()`: Register singleton instance, discover/sort checkpoints by `sectorIndex`.
     - `ActivateCheckpoint(Checkpoint cp)`: Deactivate previous visual, activate new visual, log activation.
     - `RespawnPlayer(PlayerStateMachine player)`: Crucial physics respawn logic:
       a) Temporarily disable CharacterController (`player.CC.enabled = false` or `player.GetComponent<CharacterController>().enabled = false`).
       b) Reposition and rotate player to `currentCheckpoint.spawnPoint.position` and `currentCheckpoint.spawnPoint.rotation` (or start position if none).
       c) Re-enable CharacterController (`player.CC.enabled = true`).
       d) Reset horizontal and vertical velocity (`player.CurrentHorizontalVelocity = Vector3.zero; player.ResetVerticalVelocity();`).
       e) Transition state to `PlayerIdleState` (`player.SwitchState(typeof(PlayerIdleState));`).

3. `Assets/Scripts/ParkourScene/KillZone.cs`:
   - Trigger volume detecting player fall/death.
   - `OnTriggerEnter(Collider other)` detects `PlayerStateMachine` (using `GetComponentInParent<PlayerStateMachine>()`) and triggers `CheckpointManager.Instance.RespawnPlayer(player)`.

4. `Assets/Scripts/ParkourScene/ParkourZipline.cs`:
   - Zipline rail and trigger system.
   - Fields: `Transform startAnchor`, `Transform endAnchor`, `float zipSpeed = 15f`.
   - `OnTriggerEnter(Collider other)` detects player and handles zipline engagement or state transition to `PlayerZiplineState`.

Verification:
- Ensure all scripts compile cleanly without errors or warnings.
- Check namespace and script references against existing codebase (`PlayerStateMachine`, `PlayerIdleState`, `PlayerZiplineState`).
- Write full report to `d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/teamwork_preview_worker_m1/handoff.md` and send a completion message when done.
