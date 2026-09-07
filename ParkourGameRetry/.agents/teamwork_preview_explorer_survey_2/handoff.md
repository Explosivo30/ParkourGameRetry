# Parkour Scene Creation — Survey Report (Explorer 2)

**Mission Scope**: Investigation of Player Prefabs, Camera & Cinemachine Rigging, Checkpoint System, Kill/Recovery Zones, and Scene Architecture for the creation of `Assets/Scenes/parkourTest.unity`.

---

## 1. Observation

### 1.1 Player Prefab (`-----Player.prefab`)
- **File Location**: `Assets/Prefabs/-----Player.prefab` (GUID: `33a9e2acf122a9f439cebf07555492bc`)
- **GameObject Hierarchy**:
  ```text
  -----Player (Root Transform, initial pos: [-28.84, -2.72, -14.35], rot: [0, 35.27°, 0])
  ├── Player (Transform, PlayerStateMachine, CharacterController, CapsuleCollider [disabled], InputReader)
  │   ├── Skin (Transform)
  │   │   └── Capsule (Transform, MeshFilter [Capsule], MeshRenderer [Default-Material])
  │   └── Head (Transform [LocalPos: (0, 1.0, 0)])
  │       └── FreeLook Camera (Transform, CinemachineCamera [FOV: 90, Near: 0.3, Far: 1000], CinemachineFollow [Damping: 0], CinemachinePanTilt [Tilt Range: -70° to 70°], CinemachineBasicMultiChannelPerlin [gain: 0], CinemachineInputAxisController)
  │           ├── TopRig (CinemachineCamera child) -> cm (CinemachineFollow, CinemachinePanTilt, CinemachineHardLookAt)
  │           ├── MiddleRig (CinemachineCamera child) -> cm (CinemachineFollow, CinemachinePanTilt, CinemachineHardLookAt)
  │           ├── BottomRig (CinemachineCamera child) -> cm (CinemachineFollow, CinemachinePanTilt, CinemachineHardLookAt)
  │           └── Eyes (Transform [LocalPos: (0, -0.19, 0)])
  │               ├── Sphere (MeshFilter, MeshRenderer, SphereCollider) [Left Eye]
  │               └── Sphere (1) (MeshFilter, MeshRenderer, SphereCollider) [Right Eye]
  └── Reproductor Musica (Transform)
  ```
- **Attached Components & Serialized Configurations**:
  - **`CharacterController`** (`-----Player.prefab:1100-1124`):
    - `Height`: 2.0m, `Radius`: 0.5m, `Center`: `(0, 0, 0)`
    - `SlopeLimit`: 45.0°, `StepOffset`: 0.3m, `SkinWidth`: 0.09m, `MinMoveDistance`: 0.001m
  - **`PlayerStateMachine`** (`PlayerStateMachine.cs:1-1116`):
    - `Acceleration`: 10f (locomotion: 8f–14f), `TargetVelocity`: 12f (default base speed)
    - `TurnaroundStrength`: 0.724 (frictional braking on sharp direction reverses)
    - `JumpForce`: 8f (or 10f in arena), `CoyoteTime`: 0.15s, `JumpBufferTime`: 0.15s, `JumpCutMultiplier`: 0.5
    - `GroundMask`: Layer 3 (`Ground`, bitmask `1 << 3 = 8`)
    - `WallRunMask`: Layer 6 (`WallRun`, bitmask `1 << 6 = 64`)
    - `GrappleMask`: Layer 7 (`Grapple`, bitmask `1 << 7 = 128`)
    - `VaultMask`: Layer 0 / Everything (`bits: 4294967295` in test scenes)
    - **Wall Run**: `wallRunSpeed = 8f`, `wallRunMaxDuration = 1.5f`, `wallRunGravityScale = 0.3f`, `wallCheckDistance = 0.8f`, `wallRunMinSpeed = 3f`, `wallRunExitForce = 6f`, `wallRunExitUpForce = 5f`, `wallRunCameraTiltAngle = 15f`, `wallRunCameraTiltSpeed = 5f`.
    - **Vault**: `vaultCheckDistance = 1.2f`, `vaultMinHeight = 0.3f`, `vaultMaxHeight = 1.5f`, `vaultDuration = 0.35f`, `vaultArcHeight = 0.4f`, `vaultExitSpeed = 4f`, `vaultExitUpForce = 1f`.
    - **Slide**: `slideSpeed = 12f`, `slideDuration = 0.8f`, `slideMinSpeed = 5f`, `slideFriction = 3f`, `slideSlopeBoost = 15f`, `slideSteering = 3f`, `slideHeightReduction = 0.5f`, `slideCameraDropAmount = 0.55f`, `slideCameraDropSpeed = 10f`.
    - **Dash**: `dashForce = 20f`, `dashDuration = 0.15f`, `dashCooldown = 1.5f`, `dashSpeedReward = 0.75f` (1 air-dash permitted per airborne state).
    - **Ledge Grab / Climb**: `ledgeCheckDistance = 1.0f`, `ledgeGrabMinHeight = 1.6f`, `ledgeGrabMaxHeight = 2.5f`, `ledgeClimbDuration = 0.4f`, `ledgeJumpForce = 10f`, `ledgeJumpHorizontalForce = 7f`.
    - **Landing Roll**: `rollMinFallSpeed = 8f`, `rollDuration = 0.5f`, `rollInputBufferTime = 0.35f`, `rollMinimumSpeed = 7.5f`, `rollSpeedBoost = 0.75f`, `hardLandingPenalty = 0.65f`, `rollCameraAngle = 360f`, `rollCameraDropAmount = 0.35f`.
    - **Grapple Hook**: `grappleMaxDistance = 30f`, `grappleSpeed = 20f`, `grappleExitBoost = 1.5f`, `grappleStopDistance = 1.75f`, `grappleAimAssistRadius = 0.35f`, `grappleRefireDelay = 0.2f`. Dynamically spawns `LineRenderer` rope with cyan unlit material.
    - **Zipline**: `ziplineSpeed = 15f`, `ziplineSpeedBoost = 1.3f`, `ziplineExitUpForce = 4f`.
    - **Flow Style Ranks**: D (<100 pts), C (100–250 pts), B (250–450 pts), A (450–700 pts), S (700–900 pts), SS (900–1000 pts). Speed caps: D=14m/s, C=15.5m/s, B=17m/s, A=19m/s, S=21m/s, SS=23m/s.
  - **`PlayerMotor`** (`PlayerMotor.cs:1-333`):
    - Single-Pass integration executing 1 `cc.Move()` call per frame.
    - Ground detection via downward `Physics.SphereCast` (probe radius: `cc.radius * 0.9 = 0.45m`, distance: `0.22m`, mask: `GroundMask`).
    - Dynamic slope angle limit `maxSlopeAngle = 45f`; slopes > 45° trigger sliding.
  - **`InputReader`** (`InputReader.cs:1-147`):
    - Subscribes to `InputSystem_Actions.IPlayerActions`.
    - Maps: Move (WASD/Left Stick), Look (Mouse/Right Stick), Jump (Space/Button South), Crouch (C/Left Ctrl/Button East), Sprint (Left Shift/Button West), Attack/Grapple (Right Click/Right Trigger).
  - **Procedural Subsystems Auto-Instantiated at Runtime**:
    - `ParkourStyleHUD` (`ParkourStyleHUD.cs`): OnGUI display with Rank badge, speed meter, fall speed / "ROLL READY" buffer notification, and dynamic grapple crosshair (cyan/green/white).
    - `ParkourAudioManager` (`ParkourAudioManager.cs`): Dynamic wind loop scaling with speed, procedural footstep synthesis, jump/vault/slide/landing audio grunts, combo feedback chimes.

---

### 1.2 Camera & Cinemachine Architecture
- **Installed Version**: Cinemachine `3.1.3` (namespace `Unity.Cinemachine`).
- **Scene Camera Setup** (as observed in `ParkourScene.unity:1159-1328` and `GrappleTestArena.cs:47-74`):
  - `Main Camera` GameObject (Tag: `MainCamera`):
    - `Camera` component: Field of View = 90°, Near Plane = 0.1m–0.3m, Far Plane = 500m–1000m.
    - `AudioListener` component.
    - `CinemachineBrain` component: `UpdateMethod: LateUpdate`, `DefaultBlend: EaseInOut (2s)` or `Cut`.
    - `UniversalAdditionalCameraData` component.
- **Player Camera Rigging**:
  - `Player/Head/FreeLook Camera` contains `CinemachineCamera` with `CinemachinePanTilt` and `CinemachineInputAxisController`.
  - Horizontal look (yaw) rotates the `Player` root transform around Y: `transform.Rotate(0f, yawDelta, 0f)`.
  - Vertical look (pitch) drives `CinemachinePanTilt.TiltAxis.Value` clamped between -70° and +70°.
- **Dynamic Camera Feedback Integrations**:
  - **Dynamic FOV** (`PlayerStateMachine.cs:550-560`): Lerps FOV between 90° (base) and 105° (max) based on velocity (`fovSpeedReference = 23f`, `fovLerpSpeed = 6f`).
  - **Wall Run Camera Tilt** (`PlayerStateMachine.cs:815-839`): Sets roll angle to ±15° on `cameraTiltTarget` (Head) smoothly lerped at speed 5.
  - **Slide Drop** (`PlayerStateMachine.cs:34-38, 895`): Lowers camera by 0.55m at transition speed 10.
  - **Landing Roll 360° Pitch** (`PlayerLandingRollState.cs:60-70`): Rotates camera pitch 360° via smoothstep along with a 0.35m sine dip.
  - **Effects Component** (`PlayerCameraEffects.cs:1-198`): Standalone optional script for Head Bob, Landing Dip (0.15m), Sprint Lean (3° roll), and 3D Perlin Camera Shake.

---

### 1.3 Checkpoint System Survey
- **Status**: **No existing checkpoint script or prefab exists** in the repository.
- **Requirements for New Standard Checkpoint System**:
  1. **Checkpoint Trigger Component (`Checkpoint.cs`)**:
     - Uses `BoxCollider` or `SphereCollider` with `isTrigger = true`.
     - Layer: `Default` (Layer 0) or `Ignore Raycast` (Layer 2).
     - Identifiers: `int sectorIndex`, `string sectorName`, `Transform spawnPoint`.
     - Visual/Audio state: Emissive beacon/gate color change (inactive orange -> active neon green/cyan), trigger sound effect.
  2. **Checkpoint Manager (`CheckpointManager.cs`)**:
     - Singleton (`Instance`) maintaining `Checkpoint currentCheckpoint`, `List<Checkpoint> checkpoints`.
     - Default checkpoint set to Initial Player Spawn at scene start.
     - Event/Method: `ActivateCheckpoint(Checkpoint checkpoint)`, `RespawnPlayer(PlayerStateMachine player)`.
  3. **Respawn Contract (Crucial Physics Rule)**:
     ```csharp
     // 1. Temporarily disable CharacterController to avoid collision jitter / reposition lock
     player.CC.enabled = false;
     // 2. Teleport to checkpoint spawn position and rotation
     player.transform.position = activeCheckpoint.spawnPoint.position;
     player.transform.rotation = activeCheckpoint.spawnPoint.rotation;
     // 3. Re-enable CharacterController
     player.CC.enabled = true;
     // 4. Zero velocity and cancel vertical velocity
     player.CurrentHorizontalVelocity = Vector3.zero;
     player.ResetVerticalVelocity();
     // 5. Force state to PlayerIdleState
     player.SwitchState(typeof(PlayerIdleState));
     ```

---

### 1.4 Kill Zones & Recovery Zones Survey
- **Status**: No dedicated `KillZone.cs` script exists in the repository.
- **Reference Implementations Found**:
  - `GrappleTestArena.cs:109-116` creates a flat "Safety Floor" cube (`70 x 1 x 90`) on Layer `Ground` at `y = -0.5f`.
  - `ParkourScene.unity:529-636` has a flat floor plane `r` (scale 76.5) at `y = 0`.
- **Standard Kill Zone Contract (`KillZone.cs` / `HazardZone.cs`)**:
  - Large Trigger Collider (`BoxCollider` with `isTrigger = true`) covering the void below each sector (e.g. `Y = -20f`, dimensions `400 x 5 x 400`).
  - Sector-specific hazard volumes (e.g. death pits between platforms).
  - `OnTriggerEnter(Collider other)` detects player via `other.GetComponentInParent<PlayerStateMachine>()` and triggers immediate respawn via `CheckpointManager.Instance.RespawnPlayer(player)`.

---

### 1.5 Existing Scenes & Visual/Gameplay Setup
- **Project Layer Setup (`TagManager.asset`)**:
  - Layer 0: `Default`
  - Layer 1: `TransparentFX`
  - Layer 2: `Ignore Raycast`
  - Layer 3: `Ground` (Crucial: all runnable surfaces must be on Layer 3)
  - Layer 4: `Water`
  - Layer 5: `UI`
  - Layer 6: `WallRun` (Crucial: all wall-runnable vertical walls must be on Layer 6)
  - Layer 7: `Grapple` (Crucial: all grapple anchors must be on Layer 7)
- **`ParkourScene.unity`**:
  - Static geometry with 14 modular prefabs numbered 1 to 14.
  - Sun: Directional Light (Rotation: 50°, -30°, 0°; Color: Warm white; Intensity: 1.0; Soft Shadows).
  - Main Camera with `CinemachineBrain` in scene root.
  - `-----Player` prefab placed at `(6.33, 20.65, 0.63)`.
- **`GrappleTestScene.unity`**:
  - Dynamic procedural arena generated via `GrappleTestArena.cs`.
  - Dark cyberpunk aesthetic (Skybox dark blue, Ambient `[0.12, 0.17, 0.25]`, Fog density 0.01).
  - Emissive visual cues: Grapple points in neon cyan (`#0DFFAD`), vault obstacles in bright accent orange (`#FF3D1F`), wall-run walls in deep blue (`#26577A`).

---

## 2. Logic Chain

1. **Player Prefab Mechanics Compatibility**:
   - `PlayerStateMachine` requires `CharacterController`, `PlayerMotor`, `InputReader`, and camera references.
   - `DetectGround()` uses `Physics.SphereCast` against `GroundMask` (`Layer 3: Ground`). Any ProBuilder mesh intended for player walking/landing must have a Collider and be assigned to Layer 3 (`Ground`).
   - `CanWallRun()` casts rays on `transform.right` and `-transform.right` against `WallRunMask` (`Layer 6: WallRun`). Any wall intended for wall running must be assigned to Layer 6 (`WallRun`).
   - `TryGetGrappleHit()` spherecasts against `GrappleMask` (`Layer 7: Grapple`). Any grapple anchor sphere/hook must be assigned to Layer 7 (`Grapple`).
   - `CanVault()` and `CanLedgeGrab()` cast rays against `vaultMask` (`Everything` / `Ground`). Any obstacle or ledge box must have standard collision.
2. **Camera Rigging Integrity**:
   - The scene `Main Camera` must have `CinemachineBrain` attached and set to `LateUpdate`.
   - The player prefab contains the `CinemachineCamera` (`FreeLook Camera`), which Cinemachine automatically blends to and controls.
   - No second `Main Camera` should be added inside the scene or player to avoid audio listener and rendering conflicts.
3. **Checkpoint & Kill Zone Integration**:
   - Because no checkpoint or kill zone scripts exist in `Assets/Scripts/`, standard, clean, decoupled `Checkpoint.cs`, `CheckpointManager.cs`, and `KillZone.cs` scripts must be implemented for `parkourTest.unity`.
   - The respawn contract must disable `CharacterController.enabled` before `transform.position` updates to avoid collision penetration prevention locking the player.
4. **Scene Setup for `parkourTest.unity`**:
   - The scene must contain:
     1. `Directional Light` + Global Volume (Skybox, Fog, Exposure, Bloom, Vignette).
     2. `Main Camera` (Tagged `MainCamera`) with `CinemachineBrain` + `AudioListener`.
     3. `-----Player` prefab instance positioned at Sector 1 Start.
     4. `CheckpointManager` GameObject with at least 5 Sector Checkpoints (Start + 4 Sector checkpoints + Finish).
     5. Universal `KillZone` box trigger at `Y = -25f` below the entire course, plus localized sector hazard pits.
     6. ProBuilder meshes organized into 5 distinct thematic sectors with appropriate Layer 3 (`Ground`), Layer 6 (`WallRun`), and Layer 7 (`Grapple`) assignments.

---

## 3. Caveats

1. **CharacterController Teleportation**: In Unity, setting `transform.position` while `CharacterController.enabled == true` can cause physics drift or fail if the new position is near geometry. Always disable `CC` before teleporting and re-enable immediately after.
2. **Layer Mask Serialized Values**: In prefabs or scene overrides, ensure `GroundMask` (8), `WallRunMask` (64), and `GrappleMask` (128) are properly bound in the Inspector or verified in `OnValidate()`.
3. **Cinemachine 3.x API**: The project uses Cinemachine 3.1.3 (`Unity.Cinemachine`), which uses `CinemachineCamera`, `CinemachinePanTilt`, `CinemachineBrain`, and `CinemachineFollow` instead of the legacy `CinemachineVirtualCamera` / `CinemachineFreeLook`.

---

## 4. Conclusion

- **Player Prefab**: Fully operational with rich parkour locomotion (Run, Jump, Vault, Wall Run, Slide, Dash, Ledge Grab & Climb, Landing Roll, Grapple Hook, Zipline, Flow Style scoring D–SS, and dynamic audio). Prefab is at `Assets/Prefabs/-----Player.prefab`.
- **Layers & Physics Rules**:
  - `Layer 3 (Ground)`: Platforms, floors, ramps, vault blocks.
  - `Layer 6 (WallRun)`: Vertical walls for wall running.
  - `Layer 7 (Grapple)`: Grapple anchors / floating spheres (radius ~1–2m).
- **Camera Integration**: Scene `Main Camera` with `CinemachineBrain` binds automatically to `-----Player`'s internal `FreeLook Camera`.
- **System Gaps to Implement**:
  - `Checkpoint.cs` (Trigger with visual feedback)
  - `CheckpointManager.cs` (Singleton managing active checkpoint & player teleport)
  - `KillZone.cs` (Trigger causing death reset to last checkpoint)

---

## 5. Verification Method

To independently verify these findings:
1. **Player Prefab Hierarchy**:
   - Inspect `Assets/Prefabs/-----Player.prefab` via `view_file` to confirm root `-----Player`, child `Player` with `PlayerStateMachine`, `CharacterController`, `InputReader`, and `Head/FreeLook Camera`.
2. **Layer & Mask Index Alignment**:
   - Inspect `ProjectSettings/TagManager.asset` lines 7–16: Layer 3 = `Ground`, Layer 6 = `WallRun`, Layer 7 = `Grapple`.
   - Inspect `PlayerStateMachine.cs:444-451` and `GrappleTestArena.cs:99-102` for exact layer name strings.
3. **Mechanics & Camera Code**:
   - Inspect `PlayerStateMachine.cs:550-560` (Dynamic FOV), `PlayerStateMachine.cs:815-839` (Camera Tilt), `PlayerLandingRollState.cs:60-70` (360° Roll).
   - Inspect `PlayerMotor.cs:82-137` (Ground detection SphereCast) and `PlayerMotor.cs:252-292` (Single-pass Move).
