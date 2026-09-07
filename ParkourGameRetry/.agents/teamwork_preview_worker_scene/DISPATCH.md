## 2026-08-17T17:00:31Z
You are Worker Scene Builder on the Parkour Scene Creation project.
Your working directory is: d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/teamwork_preview_worker_scene
You MUST read the authoritative user request at: d:/Unity/ParkourGameRetry/ParkourGameRetry/.agents/ORIGINAL_REQUEST.md
You MUST read the project architecture and interface contracts at: d:/Unity/ParkourGameRetry/ParkourGameRetry/PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write Ownership:
You exclusively own:
- `Assets/Editor/ParkourSceneGenerator.cs`
- `Assets/Scenes/parkourTest.unity`
- Any helper materials in `Assets/Materials/Parkour/` or `Assets/AiHide/Material/`

Mission - Construct `Assets/Scenes/parkourTest.unity` using ProBuilder meshes across all 5 progressive thematic sectors:

1. **Master Scene Builder Editor Script (`Assets/Editor/ParkourSceneGenerator.cs`)**:
   - Provide a static menu item `[MenuItem("Parkour/Generate Parkour Scene (parkourTest)")]` and batchmethod `public static void GenerateSceneBatch()`.
   - Creates a new scene `Assets/Scenes/parkourTest.unity` via `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)`.

2. **Environment, Lighting, Post-Processing, and Camera**:
   - Directional Sun: `Color(1.0f, 0.96f, 0.88f)`, Intensity `1.3f`, Soft Shadows enabled, Euler angles `(50f, -30f, 0f)`.
   - Skybox: Default skybox material.
   - RenderSettings Fog: `RenderSettings.fog = true`, `RenderSettings.fogMode = FogMode.ExponentialSquared`, `RenderSettings.fogDensity = 0.0035f`, `RenderSettings.fogColor = new Color(0.12f, 0.15f, 0.22f)`.
   - Global Volume: Create GameObject `Global Post Process Volume` with `Volume` component, set to `isGlobal = true`, assign `Assets/Settings/SampleSceneProfile.asset` profile.
   - Scene Main Camera: Create GameObject `Main Camera`, tag `MainCamera`, add `Camera`, `AudioListener`, `CinemachineBrain` (`UpdateMethod: LateUpdate`), and `UniversalAdditionalCameraData`.

3. **Player Prefab Placement**:
   - Load prefab `Assets/Prefabs/-----Player.prefab` via `AssetDatabase.LoadAssetAtPath`.
   - Instantiate as prefab instance at Sector 1 start `(0f, 1.5f, 0f)`, rotation `Quaternion.identity`.
   - Ensure layer masks and components are intact (`_groundMask = 8` / Layer 3 `Ground`, `wallRunMask = 64` / Layer 6 `WallRun`, `grappleMask = 128` / Layer 7 `Grapple`).

4. **ProBuilder Geometry for 5 Sectors**:
   Use `UnityEngine.ProBuilder` (`ShapeGenerator.CreateShape(ShapeType.Cube)`, `ShapeGenerator.CreateShape(ShapeType.Stairs)`, `mesh.ToMesh()`, `mesh.Refresh()`, `mesh.Optimize()`, and `MeshCollider`):
   - **Sector 1 (Basics)**:
     - Start platform: $10\text{m} \times 1\text{m} \times 15\text{m}$ on Layer 3 (`Ground`).
     - Vault obstacles: 3 distinct vault blocks ($h=0.8\text{m}, 1.0\text{m}, 1.2\text{m}$, depth $0.8\text{m}$) on Layer 3.
     - Jump gaps: $4.5\text{m}$, $5.5\text{m}$, $6.5\text{m}$ across elevated platforms.
     - Slide tunnel: $10^\circ$ downward ramp ($14\text{m}$ long) with low overhead ceiling ($1.1\text{m}$ clearance) requiring slide to pass.
     - Landing Roll drop: High elevated ledge with $5.5\text{m}$ vertical drop ($v_{fall} \approx 10.5\text{m/s}$) onto large landing platform ($8\text{m} \times 16\text{m}$).
   - **Sector 2 (Verticality)**:
     - Grand staircase & ascent towers up to $+25\text{m}$ elevation.
     - Right Wall Run wall: $14\text{m} \times 6\text{m} \times 0.6\text{m}$ on Layer 6 (`WallRun`).
     - Transfer gap ($5.5\text{m}$) to Left Wall Run wall: $16\text{m} \times 6\text{m} \times 0.6\text{m}$ on Layer 6 (`WallRun`).
     - Ledge Grab & Climb: Vertical wall with ledge top $2.2\text{m}$ above platform feet, requiring mantle pull-up.
   - **Sector 3 (Aerial Agility)**:
     - Floating island platforms over bottomless abyss.
     - Air Dash gap: $11\text{m}$ jump + air dash across void.
     - Grappling Hook anchors: 3 suspended Layer 7 (`Grapple`) spheres ($\varnothing 2.0\text{m}$) with glowing emissive green materials, spanning $20\text{m}-25\text{m}$ grapple swings across high void.
     - Narrow precision landing pads ($4\text{m} \times 4\text{m}$).
   - **Sector 4 (Speed & Momentum)**:
     - Zipline 1: $42\text{m}$ descending zipline wire ($15^\circ$ slope) with `ParkourZipline` trigger and `LineRenderer` cable.
     - 3-stage Chained Wall Runs: Array of 3 alternating wall-run panels (Layer 6) with $4.5\text{m}$ transfer jumps.
     - Steep Momentum Slide: $25^\circ$ steep slide chute ($18\text{m}$ long) giving maximum slope acceleration into Flow Rank S.
   - **Sector 5 (Mastery Gauntlet)**:
     - The ultimate parkour course chaining all 8 mechanics in sequence:
       High platform Zipline entry $\to$ High-speed Left Wall Run $\to$ Vault hurdle $\to$ Long-range Grapple Swing ($26\text{m}$) $\to$ Air Dash over hazard gap $\to$ Ledge Climb up monolithic tower $\to$ $9\text{m}$ vertical drop requiring Landing Roll $\to$ Speed Slide under hydraulic gate $\to$ Final Grapple Slingshot $\to$ Grand Victory Monolith Platform with celebratory golden arch.

5. **Checkpoints & Kill Zones**:
   - Create `CheckpointManager` singleton in scene.
   - Place 6 `Checkpoint` instances with emissive beacon pillars:
     1. `CP_01_Start` (Sector 1 Start)
     2. `CP_02_Verticality` (Sector 2 Entry)
     3. `CP_03_Aerial` (Sector 3 Entry)
     4. `CP_04_Momentum` (Sector 4 Entry)
     5. `CP_05_Gauntlet` (Sector 5 Entry)
     6. `CP_06_Victory` (Victory Platform)
   - Create Global Void `KillZone` trigger at $Y = -25\text{m}$ ($600\text{m} \times 10\text{m} \times 600\text{m}$) and sector-specific pit hazard triggers.

6. **Execution & Verification**:
   - Run the scene generator via Unity command line or batchmode (or execute generator in Unity).
   - Verify that `Assets/Scenes/parkourTest.unity` is generated, saved, and valid.
   - Verify all GameObjects, layers, colliders, and components exist and have no errors.
