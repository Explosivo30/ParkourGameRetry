# Milestone M1 Adversarial Challenge Report

**Verdict**: **APPROVE**

---

## 1. Observation

Direct examination of production scripts in `Assets/Scripts/ParkourScene/` and related player motor/state machine components revealed the following implementations:

### A. `Assets/Scripts/ParkourScene/CheckpointManager.cs`
- **Lines 170–186**: CharacterController lifecycle during respawn:
  ```csharp
  CharacterController cc = player.CC != null ? player.CC : player.GetComponent<CharacterController>();
  if (cc != null)
  {
      cc.enabled = false;
  }

  player.transform.position = targetPosition;
  player.transform.rotation = targetRotation;

  if (cc != null)
  {
      cc.enabled = true;
  }
  ```
- **Lines 187–205**: Complete state and velocity resets:
  ```csharp
  player.CurrentHorizontalVelocity = Vector3.zero;
  player.ResetVerticalVelocity();
  if (player.Motor != null)
  {
      player.Motor.HorizontalVelocity = Vector3.zero;
      player.Motor.VerticalVelocity = 0f;
  }

  player.SetGrappleActive(false);
  player.RestoreCollider();
  player.ResetVaultPhysics();
  player.SetCameraTilt(0f);
  player.SetCameraRollPitch(0f);

  player.SwitchState(typeof(PlayerIdleState));
  ```
- **Lines 150–169**: Robust fallback hierarchy for spawn coordinates:
  - Priority 1: `currentCheckpoint.spawnPoint` (position & rotation)
  - Priority 2: `defaultSpawnPoint` (position & rotation)
  - Priority 3: `transform` (manager's own position & rotation)

### B. `Assets/Scripts/ParkourScene/KillZone.cs`
- **Lines 28–39**: Trigger collision querying root/parent component:
  ```csharp
  private void OnTriggerEnter(Collider other)
  {
      PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
      if (player == null) return;

      Debug.Log($"[KillZone] Player entered '{zoneName}' at position {player.transform.position}. Initiating respawn.");

      if (CheckpointManager.Instance != null)
      {
          CheckpointManager.Instance.RespawnPlayer(player);
      }
      ...
  ```
- **Lines 40–54**: Independent fallback respawn mechanism that also disables `CharacterController` before setting transform coordinates if `CheckpointManager.Instance` is not in the scene.

### C. `Assets/Scripts/ParkourScene/Checkpoint.cs`
- **Lines 76–91**: Trigger collision resolution using `other.GetComponentInParent<PlayerStateMachine>()` and idempotency guard (`!isActivated`).
- **Lines 114–150**: Robust fallback visual styling applying `_EmissionColor` and `_BaseColor` keyword properties if pre-baked materials are unassigned.

### D. `Assets/Scripts/ParkourScene/ParkourZipline.cs`
- **Lines 142–160**: Direction calculation, orientation alignment, and state transition:
  ```csharp
  private void OnTriggerEnter(Collider other)
  {
      PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
      if (player == null) return;

      Vector3 direction = GetZiplineDirection();
      Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);

      if (horizontalDir.sqrMagnitude > 0.001f)
      {
          player.transform.forward = horizontalDir.normalized;
      }

      player.SwitchState(typeof(PlayerZiplineState));
      Debug.Log($"[ParkourZipline] Player engaged zipline towards {GetEndPosition()} at speed {zipSpeed} m/s.");
  }
  ```
- **Lines 32–38**: Property aliases (`startPoint`, `endPoint`, `speed`) matching interface contracts defined in `PROJECT.md`.

---

## 2. Logic Chain

1. **Edge Condition — Terminal Velocity Fall (-45 m/s) into KillZone**:
   - *Observation*: `CheckpointManager.RespawnPlayer` sets `player.CurrentHorizontalVelocity = Vector3.zero`, `player.ResetVerticalVelocity()`, and directly clears `player.Motor.HorizontalVelocity = Vector3.zero` and `player.Motor.VerticalVelocity = 0f`.
   - *Reasoning*: Because `CharacterController.enabled = false` is called before position assignment and velocity fields on both `PlayerStateMachine` and `PlayerMotor` are zeroed, no downward momentum leaks into the respawn frame.
   - *Result*: Pass.

2. **Edge Condition — Respawn while in Grapple State (`PlayerGrappleState`)**:
   - *Observation*: `CheckpointManager.RespawnPlayer` explicitly invokes `player.SetGrappleActive(false)` and transitions to `PlayerIdleState`.
   - *Reasoning*: `SetGrappleActive(false)` immediately disables the `LineRenderer` rope visual and sets `GrappleActive = false`. The state machine exit routine in `PlayerGrappleState.Exit()` also cleanly unsubscribes from jump inputs.
   - *Result*: Pass.

3. **Edge Condition — Respawn while in Slide State (`PlayerSlideState`)**:
   - *Observation*: `CheckpointManager.RespawnPlayer` calls `player.RestoreCollider()`, `player.SetCameraTilt(0f)`, and `player.SetCameraRollPitch(0f)`.
   - *Reasoning*: `RestoreCollider()` resets the CharacterController height and center back to `originalCCHeight` and `originalCCCenter`. Transitioning to `PlayerIdleState` also triggers `PlayerSlideState.Exit()`, resetting camera height offset to `0f`.
   - *Result*: Pass.

4. **Edge Condition — Respawn while in Vault / WallRun / Dash States**:
   - *Observation*: `CheckpointManager.RespawnPlayer` invokes `player.ResetVaultPhysics()`, `player.SetCameraTilt(0f)`, and `player.SwitchState(typeof(PlayerIdleState))`.
   - *Reasoning*: Vault flags, momentum overrides, and camera tilts are fully restored, preventing stuck animations or disoriented view angles.
   - *Result*: Pass.

5. **Edge Condition — Physics Teleport Snapping**:
   - *Observation*: `CharacterController.enabled = false` precedes `player.transform.position = targetPosition; player.transform.rotation = targetRotation;`, after which `cc.enabled = true` is invoked.
   - *Reasoning*: Unity PhysX restricts immediate transform movement of active `CharacterController` components. Disabling the controller before teleportation and re-enabling it afterwards guarantees that physics sweep queries do not cause positional snapping or phantom collision callbacks.
   - *Result*: Pass.

6. **Edge Condition — Deep/Nested Collider Hierarchy Detection**:
   - *Observation*: `KillZone.cs`, `Checkpoint.cs`, and `ParkourZipline.cs` all utilize `other.GetComponentInParent<PlayerStateMachine>()`.
   - *Reasoning*: If the player prefab contains child colliders (such as sub-object hitboxes or feet triggers), `GetComponentInParent` correctly resolves the root state machine component rather than failing with null.
   - *Result*: Pass.

7. **Edge Condition — Zipline Alignment & Speed Transfer**:
   - *Observation*: `ParkourZipline` computes `GetZiplineDirection()`, projects onto the XZ plane, and sets `player.transform.forward`. `PlayerZiplineState.Enter()` uses `ResolveActionSpeed` with minimum speed `ZiplineSpeed` (15 m/s) and boosts momentum based on flow rank.
   - *Reasoning*: The player smoothly transitions into the zipline vector without abrupt 90-degree angle snaps, retains incoming speed, and can cleanly disengage with jump force.
   - *Result*: Pass.

---

## 3. Caveats

- Play-mode rendering and runtime audio playback rely on Unity runtime assets (URP materials, AudioClips) being present in the scene/project.
- When `CinemachineCamera` is active, camera pitch and yaw are managed via `CinemachinePanTilt`; `RespawnPlayer` resets player transform rotation, which updates yaw orientation as expected.

---

## 4. Conclusion

All 4 scripts (`Checkpoint.cs`, `CheckpointManager.cs`, `KillZone.cs`, `ParkourZipline.cs`) fully satisfy the architectural and interface contracts in `PROJECT.md`, handle all investigated edge cases robustly, protect physics lifecycles, and cleanly reset player state machine dynamics.

**Explicit Verdict**: **APPROVE**

---

## 5. Verification Method

To independently verify this evaluation:
1. Inspect the source files in `Assets/Scripts/ParkourScene/`:
   - `Checkpoint.cs` (lines 76–91 for `GetComponentInParent`, lines 114–150 for visual fallbacks)
   - `CheckpointManager.cs` (lines 170–205 for `RespawnPlayer` CC disable and velocity resets)
   - `KillZone.cs` (lines 30–55 for parent query and fallback respawn)
   - `ParkourZipline.cs` (lines 142–160 for forward vector alignment and state engagement)
2. Verify matching method calls against `Assets/Scripts/PlayerStateMachine.cs` (`ResetVerticalVelocity`, `RestoreCollider`, `ResetVaultPhysics`, `SetGrappleActive`, `SetCameraTilt`).
