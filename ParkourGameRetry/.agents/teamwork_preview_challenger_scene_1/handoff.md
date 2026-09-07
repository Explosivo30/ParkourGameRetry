# Empirical Challenge Report: Parkour Scene Generation & Mechanics Integrity

**Explicit Verdict**: `CHALLENGE_FAILED`

---

## 1. Observation

Direct inspection of code, configuration parameters, and scene geometry yielded the following observations:

### A. Checkpoint Respawn Geometry & CharacterController Centering
- In `Assets/Prefabs/-----Player.prefab` (lines 1117-1123), the `CharacterController` on GameObject `Player` has:
  ```yaml
  m_Height: 2
  m_Radius: 0.5
  m_Center: {x: 0, y: 0, z: 0}
  ```
  The capsule extends from $Y = -1.0\text{m}$ (bottom) to $Y = +1.0\text{m}$ (top) in local coordinates of the `Player` transform.
- In `Assets/Editor/ParkourSceneGenerator.cs` (lines 914-933), `CreateCheckpointInstance` defines:
  ```csharp
  GameObject cpObj = new GameObject(name);
  cpObj.transform.position = position; // e.g. (0f, 0.5f, 2.0f) where platform top is Y = 0.5m
  ...
  GameObject spawnObj = new GameObject("SpawnPoint");
  spawnObj.transform.SetParent(cpObj.transform, false);
  spawnObj.transform.localPosition = new Vector3(0f, 0.2f, 0f); // World Y = Y_platform_top + 0.2m
  ```
- In `Assets/Scripts/ParkourScene/CheckpointManager.cs` (lines 178-185), `RespawnPlayer` teleports the player:
  ```csharp
  player.transform.position = targetPosition; // targetPosition = spawnPoint.position
  ```
  Setting `player.transform.position` to $Y_{floor} + 0.2\text{m}$ places the CharacterController center at $Y_{floor} + 0.2\text{m}$. The bottom of the capsule is at $(Y_{floor} + 0.2) - 1.0 = Y_{floor} - 0.8\text{m}$, embedding $0.8\text{m}$ of the CharacterController capsule inside the platform's solid ProBuilder `MeshCollider`.

### B. Slide Clearances vs Collider Dimensions
- In `Assets/Scripts/PlayerStateMachine.cs` (line 99) and `PlayerMotor.cs` (lines 316-321):
  ```csharp
  [SerializeField] float slideHeightReduction = 0.5f;
  ...
  cc.height = originalCCHeight - heightReduction; // 2.0m - 0.5m = 1.5m
  ```
- In `Assets/Editor/ParkourSceneGenerator.cs`:
  - Sector 1 Slide Tunnel (lines 476-482): Floor thickness $0.6\text{m}$ (half $0.3\text{m}$), ceiling thickness $0.4\text{m}$ (half $0.2\text{m}$), ceiling center offset $0.3 + 1.1 + 0.2 = 1.6\text{m} \implies$ **Clearance = $1.1\text{m}$**.
  - Sector 5 Hydraulic Gate (lines 831-836): Floor top $Y = 9.0\text{m}$, overhead beam center $Y = 11.6\text{m}$, height $3.0\text{m}$ (bottom $Y = 10.1\text{m}$) $\implies$ **Clearance = $1.1\text{m}$**.
  - The sliding CharacterController is $1.5\text{m}$ tall, exceeding the $1.1\text{m}$ tunnel/gate clearance by $0.4\text{m}$.

### C. Kinematic Verification of Jump Gaps & Physics Reachability
- Player Motor constants: `targetVelocity` $= 10\text{m/s}$, `jumpForce` $= 8\text{m/s}$, `gravityForce` $= 20\text{m/s}^2$.
- Flight time for flat jump: $t = 2 v_y / g = 2(8)/20 = 0.8\text{s}$.
- Maximum horizontal reach at base speed: $d = 10\text{m/s} \times 0.8\text{s} = 8.0\text{m}$.
- Sector 1 Gaps:
  - Gap 1: $19.5\text{m} - 15.0\text{m} = 4.5\text{m} \le 8.0\text{m}$ (Pass).
  - Gap 2: $35.0\text{m} - 29.5\text{m} = 5.5\text{m}$ with $+0.5\text{m}$ ascent ($t \approx 0.732\text{s} \implies d = 7.32\text{m} \ge 5.5\text{m}$) (Pass).
  - Gap 3: $51.5\text{m} - 45.0\text{m} = 6.5\text{m}$ with $+0.5\text{m}$ ascent ($d = 7.32\text{m} \ge 6.5\text{m}$) (Pass).
- Sector 3 Air Dash Gap: $289.0\text{m} - 278.0\text{m} = 11.0\text{m}$. Dash adds $+3.0\text{m}$ horizontal displacement ($8.0 + 3.0 = 11.0\text{m}$) onto a $9.0\text{m}$ wide landing island (Pass).
- Sector 5 Hazard Gap: $632.0\text{m} - 621.0\text{m} = 11.0\text{m}$ into $8.0\text{m}$ pad (Pass).

### D. Layer 6 (WallRun) Orientation & Normal Vectors
- Sector 2 Right Wall (`X = +3.5m`, $Z \in [189, 203]$): Inward normal $(-1, 0, 0)$.
- Sector 2 Transfer Gap: $\Delta X = 6.4\text{m}$, $\Delta Z = 5.5\text{m}$. Exit impulse $v_x = -6.0\text{m/s}$, $v_y = +5.0\text{m/s}$, $v_z = 8.0\text{m/s}$. Lateral crossing time $t = 1.067\text{s} \implies$ forward travel $8.53\text{m}$, landing at $Z = 211.5\text{m}$ on Left Wall ($Z \in [208.5, 224.5]$) (Pass).
- Sector 4 3-Stage WallRun Array: Stages at $X = -3.0\text{m}, +3.0\text{m}, -3.0\text{m}$ with $4.5\text{m}$ gaps. Crossing time $0.9\text{s}$, landing cleanly on each subsequent stage (Pass).
- Sector 5 Left Wall (`X = -3.5m`, $Z \in [562, 576]$): Aligned with Zipline 2 release point at $(0, 11.5, 560)$ (Pass).

### E. Layer 7 (Grapple) Range & Line of Sight
- `S3_GrappleAnchor_1`: $(0, 34, 311) \implies \text{Distance} = 15.81\text{m} \le 30\text{m}$ (LOS clear).
- `S3_GrappleAnchor_2`: $(1.5, 35, 341) \implies \text{Distance} = 18.09\text{m} - 19.78\text{m} \le 30\text{m}$ (LOS clear).
- `S3_GrappleAnchor_3`: $(-1.5, 36, 373) \implies \text{Distance} = 18.37\text{m} - 20.04\text{m} \le 30\text{m}$ (LOS clear).
- `S5_GrappleAnchor_Swing`: $(0, 23, 601) \implies \text{Distance} = 19.80\text{m} \le 30\text{m}$ (LOS clear).
- `S5_FinalGrapple_Slingshot`: $(0, 26, 712) \implies \text{Distance} = 24.04\text{m} - 26.40\text{m} \le 30\text{m}$ (LOS clear).

### F. Landing Roll Drops
- `RollMinFallSpeed` = $8.0\text{m/s}$.
- Sector 1 Drop: $\Delta Y = 5.5\text{m} \implies v_{fall} = \sqrt{2(20)(5.5)} = 14.83\text{m/s} > 8.0\text{m/s}$ (Pass).
- Sector 5 Drop: $\Delta Y = 9.0\text{m} \implies v_{fall} = \sqrt{2(20)(9.0)} = 18.97\text{m/s} > 8.0\text{m/s}$ (Pass).

---

## 2. Logic Chain

1. **Premise**: In Unity, a `CharacterController` with height $H=2.0\text{m}$ and center $(0,0,0)$ spans from $Y = -1.0\text{m}$ to $Y = +1.0\text{m}$ relative to its GameObject transform position.
2. **Observation**: `CreateCheckpointInstance` sets `spawnObj.localPosition = (0, 0.2, 0)`.
3. **Inference**: Teleporting the player to `spawnPoint.position` sets `player.transform.position.y = Y_floor + 0.2m`, causing the bottom of the capsule at $Y = Y_{floor} - 0.8\text{m}$ to intersect the solid floor mesh collider by $0.8\text{m}$.
4. **Consequence**: Upon respawning, physics depenetration jitter or clipping through the floor occurs.
5. **Premise**: In `PlayerStateMachine.cs`, `slideHeightReduction = 0.5f`, which reduces the CharacterController height during slide from $2.0\text{m}$ to $1.5\text{m}$.
6. **Observation**: Sector 1 slide tunnel ceiling and Sector 5 hydraulic gate beam have an overhead clearance of only $1.1\text{m}$ above the floor.
7. **Inference**: A $1.5\text{m}$ tall sliding capsule cannot pass through a $1.1\text{m}$ opening without colliding with the ceiling/gate collider ($0.4\text{m}$ vertical penetration).
8. **Consequence**: Sliding players will be physically blocked by the overhead obstacles.

---

## 3. Caveats

- All jump gap trajectories, wall run normals, grapple distances, and landing roll drop calculations are mathematically sound and robust.
- The 2 identified failure modes are strictly geometric/offset configuration bugs that can be resolved directly in `ParkourSceneGenerator.cs` (and re-running generation).

---

## 4. Conclusion & Required Mitigations

**VERDICT**: `CHALLENGE_FAILED`

### Actionable Fixes Required:
1. **Fix Checkpoint Spawn Height Offset**:
   In `Assets/Editor/ParkourSceneGenerator.cs`, line 931:
   Change:
   ```csharp
   spawnObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
   ```
   To:
   ```csharp
   spawnObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);
   ```
   *(This ensures the CC center is at $Y_{floor} + 1.1\text{m}$, leaving the CC bottom $0.1\text{m}$ above the floor for safe, clean landing).*

2. **Fix Slide Tunnel & Gate Overhead Clearances**:
   In `Assets/Editor/ParkourSceneGenerator.cs`:
   - Line 480 (Sector 1 Slide Tunnel Ceiling):
     Change `(0.3f + 1.1f + 0.2f)` to `(0.3f + 1.65f + 0.2f)` so clearance is $1.65\text{m} > 1.5\text{m}$.
   - Line 835 (Sector 5 Hydraulic Gate Overhead Beam):
     Change `new Vector3(0f, 11.6f, 689.0f)` to `new Vector3(0f, 12.2f, 689.0f)` (or adjust beam height) so clearance above $Y = 9.0\text{m}$ floor is $1.7\text{m} > 1.5\text{m}$.

---

## 5. Verification Method

1. **Inspect Spawn Height**:
   Verify that `spawnObj.transform.localPosition.y` $\ge 1.0\text{m}$ across all checkpoints so CharacterController bottom $(Y_{center} - 1.0\text{m}) \ge Y_{floor}$.
2. **Inspect Slide Clearance**:
   Verify that distance between floor top and overhead collider bottom $\ge 1.6\text{m}$ in Sector 1 slide tunnel and Sector 5 hydraulic gate.
3. **Regenerate & Inspect Scene**:
   Execute `ParkourSceneGenerator.GenerateParkourScene(false)` and verify the resulting `Assets/Scenes/parkourTest.unity`.
