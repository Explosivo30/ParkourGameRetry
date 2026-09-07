# Parkour Mechanics Investigation & Architectural Specification

**Target Working Directory**: `d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/teamwork_preview_explorer_survey_1`  
**Date**: 2026-08-17  
**Archetype**: Explorer (Survey & Synthesis)

---

## 1. Observation

Direct examination of the codebase scripts, prefabs, and project settings yielded the following verbatim implementation details:

### 1.1 Project Layers and Physics Settings
- **`ProjectSettings/TagManager.asset` (lines 7–16)**:
  - Layer 0: `Default`
  - Layer 1: `TransparentFX`
  - Layer 2: `Ignore Raycast`
  - Layer 3: `Ground` (Layer index 3, bitmask integer `8` or `1 << 3`)
  - Layer 4: `Water`
  - Layer 5: `UI`
  - Layer 6: `WallRun` (Layer index 6, bitmask integer `64` or `1 << 6`)
  - Layer 7: `Grapple` (Layer index 7, bitmask integer `128` or `1 << 7`)
- **`ProjectSettings/DynamicsManager.asset` (lines 7, 20)**:
  - `m_Gravity`: `{x: 0, y: -9.81, z: 0}`
  - `m_QueriesHitTriggers`: `1`

### 1.2 Player Prefab Structure (`Assets/Prefabs/-----Player.prefab`)
- **Root GameObject (`-----Player`)**:
  - `CharacterController` component (lines 1100–1123): `Height: 2.0`, `Radius: 0.5`, `SlopeLimit: 45`, `StepOffset: 0.3`, `SkinWidth: 0.09`, `Center: (0, 0, 0)`.
  - `PlayerStateMachine` component (lines 1073–1098): `_groundMask: 8` (Layer 3 Ground), `_targetVelocity: 12`, `_acceleration: 10`, `_turnaroundStrength: 0.724`, `_gravityForce: 9.81`.
  - `InputReader` component (lines 1148–1160).
  - Child `Head/FreeLook Camera`: Cinemachine Camera with `CinemachinePanTilt` (`TiltAxis` range -70 to 70) and `CinemachineInputAxisController`.
  - Runtime instantiated components (`PlayerStateMachine.Start()` lines 380–408, 500–526):
    - `PlayerMotor`: Added automatically if missing (`gameObject.AddComponent<PlayerMotor>()`).
    - `ParkourStyleHUD`: Added automatically (`showStyleHud = true`).
    - `ParkourAudioManager`: Added automatically (`enableParkourAudioFeedback = true`).
    - `Grapple Rope`: Procedural `LineRenderer` with Cyan material `RGBA(0.15, 1.0, 0.85, 1.0)`.

### 1.3 Inputs & Action Mapping (`Assets/InputSystem_Actions.inputactions` & `InputReader.cs`)
- **Move**: `WASD` / Left Stick (`Vector2 MovementValue`)
- **Look**: Mouse Delta / Right Stick (`Vector2 LookValue`)
- **Jump**: `Space` / Gamepad South (`A`) (`OnJumpStarted` / `OnJumpCanceled`)
- **Crouch**: `C` / `LeftCtrl` / Gamepad East (`B` / `Circle`) (`OnCrouchStarted`, `OnCrouchCanceled`, `CrouchHeld`, `CrouchPressedThisFrame`)
- **Sprint**: `LeftShift` / Gamepad LeftStickPress (`L3`) (`OnSprintStarted`, `SprintHeld`, `SprintPressedThisFrame`)
- **Attack / Grapple**: `Mouse Right Button` / Gamepad West (`X` / `Square`) / `Enter` (`OnAttack` -> `OnGrapplePressed`)

---

### 1.4 Detailed Mechanics Breakdown

#### Mechanic 1: Wall Run (`PlayerWallRunState.cs` & `PlayerStateMachine.cs:257-280, 745-787, 840-849`)
- **Detection**:
  - `CheckWallRight`: Raycast from `transform.position + transform.up * 1.0f` along `transform.right` for distance `wallCheckDistance = 0.8f` on `wallRunMask` (Layer 6 `WallRun`).
  - `CheckWallLeft`: Raycast from `transform.position + transform.up * 1.0f` along `-transform.right` for distance `wallCheckDistance = 0.8f` on `wallRunMask`.
- **Preconditions (`CanWallRun()`)**:
  - Airborne: `!Grounded`.
  - Reattach cooldown: `Time.unscaledTime - lastWallRunExitTime >= wallRunReattachCooldown` (`0.3s`).
  - Minimum speed: `CurrentHorizontalVelocity.magnitude >= wallRunMinSpeed` (`3.0 m/s`).
  - Input active: `input.sqrMagnitude >= 0.1f`.
  - Alignment: Wall tangent $\vec{T} = (\vec{N}_{wall} \times \vec{Up})_{norm}$. Velocity alignment $|\vec{V}_{norm} \cdot \vec{T}| \ge 0.3$, Input alignment $|\vec{I}_{norm} \cdot \vec{T}| \ge 0.2$.
- **Physics**:
  - Speed: `ResolveActionSpeed(incomingSpeed, wallRunSpeed = 8f, wallRunMomentumRetention = 0.99f, wallRunSpeedReward = 0.5f)`.
  - Gravity scale: `wallRunGravityScale = 0.3f` (reduces gravity to 30% of normal $g$).
  - Max duration: `wallRunMaxDuration = 1.5s`.
  - Camera tilt: `wallRunCameraTiltAngle = 15°` (+15° right wall, -15° left wall) at `wallRunCameraTiltSpeed = 5.0`.
- **Wall Jump Exit**:
  - Triggered by `JumpPressed`.
  - Exit horizontal velocity: $\vec{V}_{exit} = \vec{T}_{flat} \cdot v_{along} + \vec{N}_{flat} \cdot 6.0\text{ m/s}$ (`wallRunExitForce = 6f`).
  - Exit vertical impulse: `wallRunExitUpForce = 5.0 m/s`.
  - Style reward: `ParkourActionType.WallRun` (135 pts).

#### Mechanic 2: Vault (`PlayerVaultState.cs` & `PlayerStateMachine.cs:312-330, 851-877`)
- **Detection (`CanVault()`)**:
  - Forward velocity check: `Vector3.Dot(horizontalVel.normalized, forward) >= -0.3f`.
  - Raycast 1 (Front): From `transform.position + Vector3.up * 0.4f` forward `vaultCheckDistance = 1.2m` on `vaultMask`.
  - Raycast 2 (Top): From `(frontHit.point.x, playerY + 1.5f, frontHit.point.z) + forward * 0.1f` downward `3.0m` on `vaultMask`.
  - Height bounds: Ledge top $Y$ must be between `playerY + vaultMinHeight (0.3m)` and `playerY + vaultMaxHeight (1.5m)`.
  - Destination: `VaultTargetPosition = topHit.point + forward * (vaultCheckDistance * 0.5f) = topHit.point + forward * 0.6m`.
- **Motion & Exit**:
  - Traversal duration: `vaultDuration = 0.35s`.
  - Trajectory: Horizontal Lerp from start to target; Vertical Lerp + Sine arc: $\Delta y = \sin(\text{progress} \cdot \pi) \cdot 0.4\text{m}$ (`vaultArcHeight = 0.4f`).
  - Exit speed: `ResolveActionSpeed(entrySpeed, vaultExitSpeed = 4f, vaultMomentumRetention = 0.98f, vaultSpeedReward = 0.6f)`.
  - Exit up force: `vaultExitUpForce = 1.0 m/s`.
  - Style reward: `ParkourActionType.Vault` (115 pts).

#### Mechanic 3: Slide (`PlayerSlideState.cs` & `PlayerStateMachine.cs:88-118, 894-917`)
- **Activation**:
  - Grounded: `Grounded == true`.
  - Crouch pressed: `crouchPressed == true`.
  - Entry speed: `CurrentHorizontalVelocity.magnitude >= slideMinSpeed (5.0 m/s)`.
- **Collider & Camera**:
  - Collider shrink: `cc.height` reduced from `2.0m` to `1.5m` (`slideHeightReduction = 0.5f`), center shifted down by `0.25m`.
  - Camera drop: $-0.55\text{m}$ at speed `10.0`.
- **Friction, Steering & Slopes**:
  - Initial speed: `ResolveActionSpeed(entrySpeed, slideSpeed = 12f, slideMomentumRetention = 0.99f, slideSpeedReward = 0.4f)`.
  - Friction deceleration: $-3.0\text{ m/s}^2$ (`slideFriction = 3f`).
  - Steering: Slerp toward movement input at `slideSteering = 3f`.
  - Slope Boost: If `groundDot < 0.95` (angle $> 18.2^\circ$) and sliding downhill ($\vec{D}_{slide} \cdot \vec{D}_{slopeDown} > 0.1$), adds $+15.0 \cdot (\vec{D}_{slide} \cdot \vec{D}_{slopeDown})\text{ m/s}^2$ (`slideSlopeBoost = 15f`).
- **Exit**:
  - Jump pressed: If `CanVault()` triggers vault, else executes jump (`ExecuteJump()`).
  - Crouch released (`!crouchStillHeld`), timer expired (`slideDuration = 0.8s`), speed $< 2.0\text{ m/s}$, or becoming airborne.
  - Style reward: `ParkourActionType.Slide` (75 pts).

#### Mechanic 4: Ledge Grab / Mantle (`PlayerLedgeGrabState.cs` & `PlayerStateMachine.cs:137-159, 1064-1114`)
- **Detection (`CanLedgeGrab()`)**:
  - Airborne: `!Grounded`.
  - Moving toward or pressing toward ledge: $(|\vec{V}_H| > 0.5 \land \vec{V}_{H,norm} \cdot \vec{F} > 0.15) \lor (|\vec{I}| > 0.1 \land \vec{I}_{norm} \cdot \vec{F} > 0.25)$.
  - Vertical falling speed limit: `GetVerticalSpeed() >= -3.0 m/s` (cannot grab if falling $> 3.0\text{ m/s}$ downward).
  - Raycast 1 (Wall): From `playerPos + Vector3.up * 0.5m` forward `1.0m` (`ledgeCheckDistance = 1.0f`) on `vaultMask`.
  - Raycast 2 (Top): From `wallHit.point + Vector3.up * 2.5m + forward * 0.1m` downward `5.0m` on `vaultMask`.
  - Height limits: Ledge top height relative to player must be between `1.6m` (`ledgeGrabMinHeight`) and `2.5m` (`ledgeGrabMaxHeight`).
  - Hanging position: $Y_{hang} = Y_{top} - 1.0\text{m} - 0.12\text{m}$ (`ledgeHangClearance = 0.12f`).
  - Climb target: $Y_{climb} = Y_{top} + 1.0\text{m} + 0.09\text{m} + 0.12\text{m}$ (`ledgeClimbLandingClearance = 0.12f`), $XZ_{climb} = \text{topHit.point} - \vec{N}_{wall} \cdot (0.5\text{m} + 0.3\text{m})$ (`ledgeClimbForwardOffset = 0.3f`).
- **Hanging Actions**:
  1. **Climb / Mantle**: Jump pressed with forward or neutral input ($I_y > 0.1 \lor |I| \le 0.1$). CharacterController disabled during climb (`controllerDisabledForClimb = true`).
     - Lift phase ($0.0 \to 0.62$ progress): Vertical interpolation to $Y_{climb}$ using `Mathf.SmoothStep`.
     - Step phase ($0.62 \to 1.0$ progress): Horizontal interpolation onto ledge surface using `Mathf.SmoothStep`.
     - Total duration: `ledgeClimbDuration = 0.4s`.
  2. **Directional Jump**: Jump pressed with lateral/backward input ($I_y \le 0.1 \land |I| > 0.1$). Exit horizontal speed $7.0\text{ m/s}$, vertical impulse $10.0\text{ m/s}$ (`ledgeJumpForce = 10f`, `ledgeJumpHorizontalForce = 7f`).
  3. **Drop**: Crouch pressed ($C$ / $LeftCtrl$) drops player straight down.
  - Style reward: `ParkourActionType.LedgeGrab` (80 pts).

#### Mechanic 5: Grappling Hook (`PlayerGrappleState.cs` & `PlayerStateMachine.cs:188-208, 444-548`)
- **Detection & Target Acquisition**:
  - SphereCast from Camera forward: `aimRay = Ray(aimTransform.position, aimTransform.forward)`, radius `0.35m` (`grappleAimAssistRadius = 0.35f`), distance `30.0m` (`grappleMaxDistance = 30f`), mask `grappleMask` (Layer 7 `Grapple`).
  - Refire delay: `0.2s` (`grappleRefireDelay = 0.2f`).
  - Crosshair HUD changes color: White (no target) $\to$ Green (target available) $\to$ Cyan (grapple active).
- **Flight Dynamics**:
  - Travel speed: $\max(v_{entry}, 20.0\text{ m/s})$ (`grappleSpeed = 20f`).
  - Gravity disabled during flight (`VerticalVelocity = 0`).
  - Obstruction timeout: If player is blocked for $> 0.25\text{s}$ against geometry (`distanceToTarget >= previousDistance - 0.01`), automatically disengages.
- **Exit & Boost**:
  - **Arrival (`distance < 1.75m`)**: Launch exit horizontal speed: `ResolveActionSpeed(travelSpeed, 20f, 1f, 20f * (1.5 - 1.0) = 10f boost)` $\implies$ up to $30.0\text{ m/s}$ horizontal velocity! Vertical boost: $\text{clamp}(D_y \cdot v_{travel} \cdot 0.65, 0, 8.0\text{ m/s})$.
  - **Jump Cancel**: Jump pressed during flight preserves horizontal velocity + executes standard vertical jump.
  - **Attack Re-press Cancel**: Re-pressing attack cancels grapple into Idle.
  - Style reward: `ParkourActionType.Grapple` (175 pts).

#### Mechanic 6: Zipline (`PlayerZiplineState.cs` & `PlayerStateMachine.cs:209-220, 419`)
- **Activation**: External trigger volume switches player state to `PlayerZiplineState`.
- **Motion**:
  - Direction: Forward vector flattened on $XZ$ ($\vec{D}_{zip} = \vec{F}_{flat}$).
  - Speed: `ResolveActionSpeed(currentSpeed, ziplineSpeed = 15f, 1f, 15f * (1.3 - 1.0) = 4.5f boost)` $\implies 19.5\text{ m/s}$ default boost.
  - Zero gravity during slide.
- **Exit**:
  - Jump pressed: Maintains horizontal speed $+ \text{ExecuteJump()}$ ($8.0\text{ m/s}$ vertical impulse).
  - Rail end reached: Exits to `PlayerIdleState`.
  - Style reward: `ParkourActionType.Zipline` (125 pts).

#### Mechanic 7: Dash (`PlayerDashState.cs` & `PlayerStateMachine.cs:120-135, 996-1006`)
- **Activation**: Sprint pressed (`LeftShift`), not holding crouch (`!crouchHeld`), cooldown ready (`CanDash()`).
- **Cooldown & Air Limit**:
  - Cooldown: `dashCooldown = 1.5s`.
  - Air Dash: Exactly 1 air dash allowed per airborne state (`airDashConsumed = true`, reset upon `Grounded == true`). Unlimited ground dashes (subject to cooldown).
- **Motion**:
  - Direction: Movement input vector in world space, or `transform.forward` if neutral. Blends with existing momentum if aligned ($Dot > 0.15$).
  - Speed: `ResolveActionSpeed(incomingSpeed, dashForce = 20f, 1f, dashSpeedReward = 0.75f)`.
  - Duration: `dashDuration = 0.15s` (travels $\approx 3.0 - 3.5\text{m}$).
  - Gravity continues during dash.
  - Style reward: `ParkourActionType.Dash` (65 pts).

#### Mechanic 8: Landing Roll / Fall Hard Landing (`PlayerLandingRollState.cs`, `PlayerIdleState.cs:58-81`, `PlayerStateMachine.cs:160-184`)
- **Threshold**:
  - High fall threshold: Downward vertical impact speed $> 8.0\text{ m/s}$ (`rollMinFallSpeed = 8f`).
- **Input Timing**:
  - Crouch input buffered: `rollInputBufferTime = 0.35s` (press or hold Crouch up to 0.35s before touching ground).
- **Outcomes**:
  1. **Landing Roll (Success)**:
     - Duration: `rollDuration = 0.5s`.
     - Speed conserved & boosted: `ResolveActionSpeed(incomingSpeed, rollMinimumSpeed = 7.5f, 1f, rollSpeedBoost = 0.75f)`.
     - Full 360° camera somersault pitch (`RollCameraAngle = 360°`) with smooth dip of $0.35\text{m}$.
     - Exits at full sprint speed. Style reward: `ParkourActionType.LandingRoll` (150 pts).
  2. **Hard Landing (Failure / Stumble Penalty)**:
     - No crouch buffered on high fall.
     - Speed penalty: `CurrentHorizontalVelocity *= hardLandingPenalty (0.65f)` (loses 35% momentum).
     - Camera landing dip & violent screen shake triggered.

#### Mechanic 9: Player Motor Base Physics & Flow Style System
- **`PlayerMotor.cs` Single-Pass Physics**:
  - Run Target Speed: `targetVelocity = 10.0 m/s` (Prefab default `12.0 m/s`).
  - Ground Acceleration: `acceleration = 12.0 m/s²` (Prefab `10.0 m/s²`).
  - Air Acceleration: `airAcceleration = 4.0 m/s²` (Prefab `3.0 m/s²`).
  - Braking Deceleration: `brakingDeceleration = 22.0 m/s²` (Prefab `20.0 m/s²`).
  - Air Deceleration: `airDeceleration = 0.0 m/s²` (Zero air drag; preserves full airborne horizontal velocity!).
  - Gravity: $20.0\text{ m/s}^2$ downward (in motor) / $9.81\text{ m/s}^2$ (in SM).
  - Ground Snapping: $-2.0\text{ m/s}$ vertical force when grounded.
  - Slope Handling: Max slope limit $= 45^\circ$. Slopes $> 45^\circ$ cause uncontrolled sliding along $\text{ProjectOnPlane}(\vec{Down}, \vec{N}_{slope})$.
  - Jump Force: `jumpForce = 8.0 m/s`.
  - Jump Cut: Releasing jump button multiplies vertical velocity by `jumpCutMultiplier = 0.5f` (allows short hops).
  - Coyote Time: `0.15s`. Jump Buffer: `0.15s`.
- **Flow Style Ranks & Speed Caps (`PlayerStateMachine.cs:224-247, 924-994, 1015-1062`)**:
  - Ranks:
    - Rank **D** ($0 - 99\text{ pts}$): Speed Cap = **$14.0\text{ m/s}$**
    - Rank **C** ($100 - 249\text{ pts}$): Speed Cap = **$15.5\text{ m/s}$**
    - Rank **B** ($250 - 449\text{ pts}$): Speed Cap = **$17.0\text{ m/s}$**
    - Rank **A** ($450 - 699\text{ pts}$): Speed Cap = **$19.0\text{ m/s}$**
    - Rank **S** ($700 - 899\text{ pts}$): Speed Cap = **$21.0\text{ m/s}$**
    - Rank **SS** ($900 - 1000\text{ pts}$): Speed Cap = **$23.0\text{ m/s}$**
  - Score Decay: Decays at $95\text{ pts/s}$ after $1.6\text{s}$ grace period without parkour actions.
  - Repeating Penalty: Repeating same action reduces points to $35\%$ (1st repeat) and $10\%$ (continued repeats).
  - Speed Resolution: $\text{ResolvedSpeed} = \min(\text{RetainedSpeed} + \text{EarnedBoost} \cdot (1 + \text{Rank} \times 0.08) \cdot \text{VarietyMult}, \text{StyleSpeedCap})$.

---

## 2. Logic Chain

1. **Layer Configuration**: `TagManager.asset` allocates Layer 3 to `Ground`, Layer 6 to `WallRun`, and Layer 7 to `Grapple`. The Player prefab assigns `_groundMask = 8` ($2^3$), while `ConfigureDefaultLayers()` automatically assigns `grappleMask = 128` ($2^7$). Therefore, all level geometry must strictly assign floors/platforms to Layer 3, runnable walls to Layer 6, and grapple anchors to Layer 7.
2. **Raycast Geometry Constraints**:
   - `CanVault()` casts forward at $y+0.4\text{m}$ for $1.2\text{m}$ and down from $y+1.5\text{m}$. Any obstacle between $0.3\text{m}$ and $1.5\text{m}$ height and $\le 1.2\text{m}$ depth triggers a vault when the player moves toward it and presses jump.
   - `CanLedgeGrab()` casts forward at $y+0.5\text{m}$ for $1.0\text{m}$ and down from $y+2.5\text{m}$. Any obstacle between $1.6\text{m}$ and $2.5\text{m}$ height triggers a ledge hang when airborne.
   - `CanWallRun()` casts lateral rays at $y+1.0\text{m}$ for $0.8\text{m}$. Walls must be within $0.8\text{m}$ of the player and at least $2.0\text{m}$ high.
3. **Trajectory & Jump Envelope Derivation**:
   - Standard Jump: $v_{y0} = 8.0\text{ m/s}, g = 20\text{ m/s}^2 \implies h_{jump} = \frac{v_y^2}{2g} = \frac{64}{40} = 1.60\text{m}$. Time in air $t_{air} = \frac{2 v_y}{g} = 0.80\text{s}$.
   - Jump Gap Coverage: At base run speed ($10\text{ m/s}$), horizontal jump distance $= 10 \times 0.8 = 8.0\text{m}$. At Dash speed ($20\text{ m/s}$), air dash + jump can cover $12 - 16\text{m}$.
   - Wall Run Jump Exit: Horizontal kick $= 6.0\text{ m/s}$ away from wall, vertical lift $= 5.0\text{ m/s}$. Parallel wall spacing for chained wall-running must be $4.0\text{m} - 7.0\text{m}$.
   - Fall Damage / Roll Threshold: $v_{impact} \ge 8.0\text{ m/s} \implies h_{fall} \ge \frac{v^2}{2g} = \frac{64}{40} = 1.60\text{m}$ free fall. Any drop $> 1.6\text{m}$ triggers hard landing unless crouch roll is timed within the $0.35\text{s}$ buffer window.
4. **Grapple Range & Kinematics**:
   - Maximum range is $30.0\text{m}$ with $0.35\text{m}$ SphereCast assist. Anchor targets should be $\ge 1.5\text{m}$ diameter spheres/cubes on Layer 7 with line-of-sight clearance.
   - Boost on arrival reaches up to $30.0\text{m/s}$ (clamped to rank speed cap, max $23\text{ m/s}$ at SS), enabling massive gap clearance.

---

## 3. Caveats

1. **PhysicsManager Gravity vs PlayerMotor Gravity**: Unity's global gravity in `DynamicsManager.asset` is $-9.81\text{ m/s}^2$, but `PlayerMotor.cs` defines its internal `gravityForce = 20f`. Because `CharacterController.Move()` is driven explicitly by `PlayerMotor`, the effective player gravity is $20.0\text{ m/s}^2$, which makes movement snappy and responsive.
2. **Vault Layer Mask Default**: In `PlayerStateMachine`, `vaultMask` defaults to `0` (Nothing) if not assigned in Inspector, but in Unity Physics raycasts, a mask of `0` or unset might miss geometry if not properly initialized. In prefab/scene, `vaultMask` should include `Default` (0) and `Ground` (3).
3. **Zipline Activation**: `PlayerZiplineState` is designed to be triggered externally by entering a trigger collider on a zipline GameObject.
4. No further uninvestigated areas remain.

---

## 4. Conclusion & Level Design Metrics Matrix

The parkour system in `ParkourGameRetry` is a fully integrated, state-machine driven movement engine with momentum chaining, style ranks, single-pass character controller physics, and procedural audio/camera feedback.

### Authoritative Dimensional Reference Sheet for Level Creation

| Mechanic | Input / Trigger | Required Layer | Min Height / Range | Max Height / Range | Optimal Spacing / Sizing | Key Behavior / Exit Impulse |
|---|---|---|---|---|---|---|
| **Base Run / Sprint** | WASD / Left Stick | Layer 3 (`Ground`) | Flat / Slope $0^\circ$ | Slope $45^\circ$ | Floor width $\ge 2.5\text{m}$ | Speed: $10 - 12\text{ m/s}$, Accel: $10 - 12\text{ m/s}^2$ |
| **Normal Jump** | `Space` / `BtnSouth` | Layer 3 (`Ground`) | $0.0\text{m}$ | $1.6\text{m}$ vertical | Gap: $4.0 - 8.0\text{m}$ | $v_y = 8.0\text{ m/s}$, Jump cut $0.5\times$, Coyote: $0.15\text{s}$ |
| **Wall Run** | Run + Air + W into wall | Layer 6 (`WallRun`) | Min wall $h: 2.0\text{m}$ | Max duration: $1.5\text{s}$ ($15 - 25\text{m}$) | Parallel gap: $4.0 - 7.0\text{m}$ | $g \times 0.3$, Exit jump: $6\text{ m/s}$ normal, $5\text{ m/s}$ up |
| **Vault** | Run + Jump into block | `Default` / `Ground` | $0.3\text{m}$ height | $1.5\text{m}$ height | Depth: $0.5 - 1.2\text{m}$ | Duration: $0.35\text{s}$, arc $+0.4\text{m}$, exit: $4\text{ m/s} + 1\text{ m/s}$ up |
| **Slide** | Run ($>5\text{ m/s}$) + Crouch | Layer 3 (`Ground`) | Tunnel $h: 1.6\text{m}$ | Duration: $0.8\text{s}$ ($8 - 12\text{m}$) | Slope: $18.5^\circ - 40^\circ$ | Shrinks CC to $1.5\text{m}$, downhill $+15\text{ m/s}^2$ boost |
| **Ledge Grab / Mantle** | Air + Move toward ledge | `Default` / `Ground` | $1.6\text{m}$ above feet | $2.5\text{m}$ above feet | Top clearance $\ge 2.2\text{m}$ | Mantle: $0.4\text{s}$; Jump off: $7\text{ m/s} + 10\text{ m/s}$ up |
| **Grapple** | `Right Click` / `BtnWest` | Layer 7 (`Grapple`) | Range: $1.75\text{m}$ | Max Range: $30.0\text{m}$ | Anchor size: $1.5 - 2.5\text{m}$ | Speed: $20\text{ m/s}$, Arrival boost: $1.5\times$ (up to $30\text{ m/s}$) |
| **Zipline** | Trigger volume | `Default` / Trigger | Start anchor | End anchor | Spline/Line trajectory | Speed: $15 - 19.5\text{ m/s}$, Exit jump gives $+8\text{ m/s}$ up |
| **Dash** | `LeftShift` / `L3` | Any | $0.0\text{m}$ | Distance: $3.0 - 3.5\text{m}$ | Cooldown: $1.5\text{s}$ | Speed: $20\text{ m/s}$, 1 air dash per jump |
| **Landing Roll** | Crouch buffered ($0.35\text{s}$) | Layer 3 (`Ground`) | Drop $> 1.6\text{m}$ ($v_y > 8\text{ m/s}$) | Any survivable drop | Landing pad $\ge 4.0\text{m}$ | Conserves speed $+ 0.75\text{ m/s}$; Hard landing loses $35\%$ |

---

## 5. Verification Method

To independently verify these findings:

1. **Codebase Inspection**:
   - Verify `PlayerStateMachine.cs` lines 257–349, 745–877, 981–1114 for all numeric constants and layer mask variables.
   - Verify `PlayerMotor.cs` lines 15–35, 82–137, 143–219, 224–246 for single-pass physics, ground probe spherecast, and slope sliding equations.
   - Verify each state class in `Assets/Scripts/PlayerStates/` for state transition logic.
2. **Layer Mask & Tag Verification**:
   - Inspect `ProjectSettings/TagManager.asset` lines 7–16 to confirm Layer 3 = `Ground`, Layer 6 = `WallRun`, Layer 7 = `Grapple`.
   - Inspect `Assets/Prefabs/-----Player.prefab` lines 1088–1090 (`_groundMask: 8`).
3. **Reference Arena**:
   - Inspect `Assets/Scripts/GrappleTestArena.cs` lines 87–139 to observe working programmatic construction of Ground platforms, WallRun walls (scale $0.6 \times 6 \times 15$), Grapple Anchors (Spheres on Layer 7), and Vault Blocks ($1.2\text{m}$ and $1.8\text{m}$ high).
