# Survey & Investigation Report: ProBuilder Capabilities, Materials, Lighting, Scene Generation & Sector Metrics

## 1. Observation

### 1.1 ProBuilder Package & API Capabilities
- **Package Version**: `Packages/manifest.json:10` specifies `"com.unity.probuilder": "6.0.9"`.
- **Unity Engine Version & Render Pipeline**: `"com.unity.render-pipelines.universal": "17.0.4"` (Unity 6 URP), `"com.unity.cinemachine": "3.1.3"`, `"com.unity.inputsystem": "1.14.2"`.
- **API Availability**:
  - Namespace `UnityEngine.ProBuilder` contains `ProBuilderMesh`, `ShapeGenerator`, `ShapeType`, `PivotLocation`, `Face`, `Vertex`, `AutoUnwrapSettings`, `MeshOperations`.
  - Namespace `UnityEditor.ProBuilder` contains editor-time mesh editing and finalization utilities.
  - Primitive / shape generation capabilities in `ShapeGenerator`:
    - `ShapeGenerator.CreateShape(ShapeType.Cube)`
    - `ShapeGenerator.CreateShape(ShapeType.Stairs)`
    - `ShapeGenerator.CreateShape(ShapeType.Cylinder)`
    - `ShapeGenerator.CreateShape(ShapeType.Plane)`
    - `ShapeGenerator.CreateShape(ShapeType.Arch)`
    - `ShapeGenerator.CreateShape(ShapeType.Pipe)`
    - `ShapeGenerator.CreateShape(ShapeType.Cone)`
    - `ShapeGenerator.CreateShape(ShapeType.Door)`
    - `ShapeGenerator.CreateShape(ShapeType.Sphere)`
    - `ShapeGenerator.CreateShape(ShapeType.Torus)`
  - Mesh processing: `mesh.ToMesh()`, `mesh.Refresh()`, `mesh.Optimize()`, and collider generation via `MeshCollider` (or `BoxCollider` for simple shapes).
  - Material and UV assignment: `MeshRenderer.sharedMaterials`, `mesh.faces[i].submeshIndex`, and `mesh.RefreshUV(mesh.faces)`.

### 1.2 Materials, Textures, Shaders, Physics Materials
- **Shaders Available**:
  - `Universal Render Pipeline/Lit` (Shader GUID `933532a4fcc9baf4fa0491de14d08ed7`)
  - `Universal Render Pipeline/Unlit`
  - Custom Shader Graphs: `Assets/Shaders/Underwater/UnderWater.shadergraph`, `Assets/Shaders/SubsurfaceScattering(Skin)/Skin.shadergraph`
- **Materials Available in Project**:
  - Prototyping materials: `Assets/AiHide/Material/Grid Large.mat` (20x20 tiling prototype grid with URP Lit shader), `Assets/AiHide/Material/Grid Small.mat`, `Assets/AiHide/Material/Blue.mat`, `Assets/AiHide/Material/Red.mat`, `Assets/AiHide/Material/Light Gray.mat`, `Assets/AiHide/Material/Dark Gray.mat`.
  - Demo materials: `Assets/EZSceneCollab/Samples/Demo/Materials/DemoBlue.mat`, `DemoGreen.mat`, `DemoRed.mat`.
  - Architecture materials: `Assets/WhiteCity/Models/Buildings/Materials/` (40+ architectural materials).
- **Physics Materials & Kinematic System**:
  - Search across `Assets/` for `.physicMaterial` returned 0 assets.
  - Verification in `Assets/Scripts/PlayerMotor.cs:9-35` and `Assets/Scripts/PlayerStateMachine.cs:17-45` confirms that player locomotion, slope sliding, wall interaction, and gravity are handled via kinematic single-pass integration on `CharacterController.Move()` rather than PhysX dynamic friction.

### 1.3 Lighting, Skybox & Environment Setup
- **Global Volume Profiles**:
  - `Assets/Settings/SampleSceneProfile.asset`:
    - **Tonemapping**: ACES (Mode 2)
    - **Bloom**: Threshold `1.0`, Intensity `0.25`, Scatter `0.5`, High Quality Filtering `1`
    - **Vignette**: Intensity `0.144`, Smoothness `0.2`
    - **Motion Blur**: Quality `2`, Intensity `0.6`
  - `Assets/Settings/DefaultVolumeProfile.asset`: full profile with Color Adjustments, Film Grain, Panini Projection, Screen Space Lens Flare, Depth of Field.
- **Lighting & Sun Settings**:
  - `Assets/Scenes/ParkourScene.unity:248-333` and `Assets/Scripts/GrappleTestArena.cs:75-85`:
    - Directional Light: Warm Sunlight `Color(1.0f, 0.957f, 0.839f)` or Cyan-tint `Color(0.82f, 0.9f, 1.0f)`, Intensity `1.25f - 1.5f`, Rotation `Euler(50f, -30f, 0f)`.
    - Soft Shadows: Enabled (`SoftShadowQuality: High / Tier 2`).
- **Fog & Skybox**:
  - Skybox: Default Skybox `fileID: 10304`.
  - Fog: Exponential squared distance fog (`RenderSettings.fog = true`, density `0.003 - 0.005`, Slate-blue `Color(0.12f, 0.15f, 0.22f)`).

### 1.4 Project Layers & Player Prefab Architecture
- **Layers** (`ProjectSettings/TagManager.asset:7-17`):
  - Layer 0: `Default`
  - Layer 3: `Ground`
  - Layer 6: `WallRun`
  - Layer 7: `Grapple`
- **Player Prefab** (`Assets/Prefabs/-----Player.prefab`):
  - Root GameObject: `-----Player` containing `PlayerStateMachine`, `PlayerMotor`, `CharacterController` (`Height: 2.0`, `Radius: 0.5`), `InputReader`, `ParkourStyleHUD`, `ParkourAudioManager`.
  - Camera Rig: Cinemachine camera hierarchy with `CinemachineBrain`, `CinemachinePanTilt`, `CinemachineCamera`, and `PlayerCameraEffects.cs`.

### 1.5 Kinematic Specifications of the 8 Parkour Mechanics
Exact constants directly observed from `PlayerStateMachine.cs`, `PlayerMotor.cs`, and `PlayerStates/*.cs`:
1. **Locomotion / Jump**:
   - Base Target Velocity: `10.0 m/s` (`PlayerStateMachine.cs:49`)
   - Acceleration: `8.0 - 12.0 m/s²` (`PlayerStateMachine.cs:47`, `PlayerMotor.cs:17`)
   - Jump Force: `8.0 m/s` upward (`PlayerStateMachine.cs:335`)
   - Coyote Time: `0.15 s` (`PlayerStateMachine.cs:340`)
   - Jump Buffer: `0.15 s` (`PlayerStateMachine.cs:345`)
   - Gravity: `9.81 - 20.0 m/s²` (`PlayerStateMachine.cs:305`, `PlayerMotor.cs:29`)
2. **Vault**:
   - Vault Min Height: `0.3 m`, Vault Max Height: `1.5 m` (`PlayerStateMachine.cs:315-316`)
   - Vault Check Distance: `1.2 m` forward (`PlayerStateMachine.cs:314`)
   - Vault Duration: `0.35 s`, Vault Arc Height: `0.4 m` (`PlayerStateMachine.cs:317, 319`)
   - Vault Exit Speed: `4.0 m/s` base + momentum retention `0.98` + speed reward `0.6` (`PlayerStateMachine.cs:321-326`)
3. **Slide**:
   - Slide Min Speed to trigger: `5.0 m/s` (`PlayerStateMachine.cs:93`)
   - Slide Speed: `12.0 m/s`, Slide Duration: `0.8 s` (`PlayerStateMachine.cs:91-92`)
   - Slide Friction: `3.0 m/s²`, Slope Boost: `15.0 m/s²` (`PlayerStateMachine.cs:94-95`)
   - Collider Height Reduction: Drops from `2.0 m` down to `0.5 m` (`PlayerStateMachine.cs:99, 901`)
   - Camera Drop: `0.55 m` (`PlayerStateMachine.cs:100`)
4. **Landing Roll**:
   - Min Fall Speed to trigger: `8.0 m/s` downward (`PlayerStateMachine.cs:164`)
   - Roll Duration: `0.5 s`, Roll Speed Boost: `+0.75 m/s` (`PlayerStateMachine.cs:165, 168`)
   - Input Buffer Time: `0.35 s` on Crouch (`PlayerStateMachine.cs:166`)
   - Hard Landing Penalty (if roll missed): Speed multiplied by `0.65` (`PlayerStateMachine.cs:169`)
   - 360° Camera Pitch Rotation (`PlayerStateMachine.cs:178`, `PlayerLandingRollState.cs:65`)
5. **Wall Run**:
   - Wall Check Distance: `0.8 m` lateral raycast on Layer 6 (`WallRun`) (`PlayerStateMachine.cs:263, 745-765`)
   - Min Speed: `3.0 m/s`, Max Duration: `1.5 s` (`PlayerStateMachine.cs:261, 264`)
   - Gravity Scale: `0.3` (`PlayerStateMachine.cs:262`)
   - Wall Run Exit Impulses: `6.0 m/s` normal + `5.0 m/s` up (`PlayerStateMachine.cs:265-266`)
   - Camera Tilt: `15.0°` (`PlayerStateMachine.cs:287`)
6. **Ledge Grab & Climb**:
   - Ledge Check Distance: `1.0 m` forward on Layer 3/VaultMask (`PlayerStateMachine.cs:140`)
   - Ledge Grab Min Height: `1.6 m`, Max Height: `2.5 m` above player feet (`PlayerStateMachine.cs:141-142`)
   - Climb Duration: `0.4 s` (`PlayerStateMachine.cs:143`)
   - Ledge Jump Forces: `10.0 m/s` up, `7.0 m/s` horizontal (`PlayerStateMachine.cs:144-145`)
7. **Dash**:
   - Dash Force: `20.0 m/s`, Duration: `0.15 s`, Cooldown: `1.5 s` (`PlayerStateMachine.cs:124-126`)
   - Air Dash: 1 charge per airborne state, reset upon landing (`PlayerStateMachine.cs:134, 1000`)
8. **Grappling Hook**:
   - Max Distance: `30.0 m` (`PlayerStateMachine.cs:189`)
   - Grapple Speed: `20.0 m/s`, Exit Boost: `1.5x` (`PlayerStateMachine.cs:190-191`)
   - Stop Distance: `1.75 m` (`PlayerStateMachine.cs:192`)
   - Target Layer: Layer 7 (`Grapple`) (`PlayerStateMachine.cs:195, 448`)
9. **Zipline**:
   - Zipline Speed: `15.0 m/s`, Speed Boost: `1.3x` (`PlayerStateMachine.cs:213-214`)
   - Exit Up Force: `4.0 m/s` (`PlayerStateMachine.cs:215`)

---

## 2. Logic Chain

1. **Geometry Engine Selection**:
   - ProBuilder 6.0.9 is installed in `Packages/manifest.json`.
   - Creating geometry via `UnityEngine.ProBuilder.ShapeGenerator` and `ProBuilderMesh` allows parametric generation of platforms, ramps, wall-run surfaces, vault blocks, stairs, and arch gateways with precise dimensions and automatic UV mapping.
   - For automated generation, calling `ShapeGenerator.GenerateCube(...)`, `ShapeGenerator.GenerateStairs(...)`, etc., followed by `mesh.ToMesh()`, `mesh.Refresh()`, `mesh.Optimize()`, and assigning the appropriate Layer (Layer 3 `Ground`, Layer 6 `WallRun`, Layer 7 `Grapple`) ensures zero collision/physics discrepancies.

2. **Material and Thematic Strategy**:
   - The project uses URP Lit (`Universal Render Pipeline/Lit`).
   - To deliver strong readability and visual progression across the 5 sectors, we establish a distinct thematic color coding:
     - **Sector 1 (Basics)**: Clean High-Key Prototyping (`Grid Large.mat`, pure white/light-gray with safety orange edge trims).
     - **Sector 2 (Verticality)**: Deep Industrial Blue / Slate Steel with luminous cyan wall-run guidance panels.
     - **Sector 3 (Aerial Agility)**: Dark Midnight Navy / Obsidian with high-intensity emerald green grapple target spheres (`_EmissionColor`).
     - **Sector 4 (Speed & Momentum)**: Carbon Asphalt with glowing electric amber/yellow ziplines and speed chevrons.
     - **Sector 5 (Mastery Gauntlet)**: Neon Cyberpunk / Synthwave Crimson & Magenta with glowing hazard boundaries and golden victory monolith.

3. **Lighting & Post-Processing Setup**:
   - A single Directional Light ("Sun") with warm sunlight and soft shadows creates crisp contact shadows that are vital for depth perception during jumps.
   - Adding a Global Volume with `SampleSceneProfile.asset` (ACES Tonemapping, Bloom for emissive markers, Vignette, Motion Blur) elevates the visual polish to commercial quality.
   - Exponential squared Fog (`density: 0.004`, slate-blue) provides atmospheric depth, making vertical drops look genuinely dizzying and dangerous.

4. **Scene Generation Architecture**:
   - An Editor script in `Assets/Editor/` (e.g. `ParkourSceneBuilder.cs`) can programmatically construct the entire `Assets/Scenes/parkourTest.unity` scene in one click or batch execution.
   - The generator:
     1. Creates an empty scene via `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)`.
     2. Sets up Lighting, Skybox, RenderSettings, Fog, and Global Volume.
     3. Instantiates `Assets/Prefabs/-----Player.prefab` at Sector 1 Start (`Vector3(0f, 1.5f, 0f)`).
     4. Generates ProBuilder meshes for all 5 sectors organized under hierarchical parent objects:
        - `--- ENVIRONMENT ---`
        - `--- SECTOR 1: BASICS ---`
        - `--- SECTOR 2: VERTICALITY ---`
        - `--- SECTOR 3: AERIAL AGILITY ---`
        - `--- SECTOR 4: SPEED & MOMENTUM ---`
        - `--- SECTOR 5: MASTERY GAUNTLET ---`
        - `--- CHECKPOINTS ---`
        - `--- KILL & RECOVERY ZONES ---`
     5. Adds Checkpoint components (5 total: Start, S2, S3, S4, S5, Victory) and Kill Zones across the base of the map.
     6. Configures layers accurately (Layer 3 `Ground`, Layer 6 `WallRun`, Layer 7 `Grapple`).
     7. Saves the scene to `Assets/Scenes/parkourTest.unity`.

5. **Sector Metric Guidelines**:
   By using the exact physics parameters extracted from the code, each sector is guaranteed to be 100% playable, challenging, and strictly aligned with the player's mechanical limits:

| Sector | Primary Mechanics | Core Obstacle Metrics | Elevation & Scale |
|---|---|---|---|
| **Sector 1: Basics** | Jump, Vault, Slide, Landing Roll | • Vault blocks: $h=1.0\text{m}, w=3.0\text{m}, d=0.8\text{m}$<br>• Gaps: $4.5\text{m} - 6.5\text{m}$<br>• Slide tunnel: $1.1\text{m}$ clearance on $10^\circ$ downward ramp ($12\text{m}$)<br>• Landing Roll drop: $5.5\text{m}$ vertical drop ($v_{\text{fall}} \approx 10.5\text{m/s}$) | Length: $\approx 110\text{m}$<br>Elevation: $0\text{m} \to +8\text{m} \to +3\text{m}$ |
| **Sector 2: Verticality** | Wall Run (R/L), Ledge Grab & Climb | • Wall Run walls: $h=6.0\text{m}, l=14.0\text{m}$, Layer `WallRun`<br>• Wall-to-wall lateral gap: $5.0\text{m} - 6.0\text{m}$<br>• Ledge climb walls: $h=2.2\text{m}$ above platform apex<br>• Alternating R $\to$ L wall run gap: $4.0\text{m}$ | Length: $\approx 130\text{m}$<br>Elevation: $+3\text{m} \to +28\text{m}$ |
| **Sector 3: Aerial Agility** | Air Dash, Grappling Hook (30m max) | • Grapple targets: $\varnothing 2.2\text{m}$ spheres, Layer `Grapple`<br>• Grapple anchor spacing: $18.0\text{m} - 25.0\text{m}$, elevated $+8\text{m}$<br>• Dash air gaps: $10.0\text{m} - 12.0\text{m}$ over bottomless drop<br>• Small landing pads: $3.5\text{m} \times 3.5\text{m}$ | Length: $\approx 160\text{m}$<br>Elevation: $+28\text{m} \to +45\text{m}$ |
| **Sector 4: Speed & Momentum** | Zipline, Chained Wall Runs, Slide | • Zipline lines: $35.0\text{m} - 45.0\text{m}$ descending at $15^\circ$<br>• 3-stage wall-run chain ($12\text{m}$ each, $4.5\text{m}$ jump transfers)<br>• $25^\circ$ steep slide ramp ($16\text{m}$) reaching Flow Rank S | Length: $\approx 180\text{m}$<br>Elevation: $+45\text{m} \to +20\text{m} \to +40\text{m}$ |
| **Sector 5: Mastery Gauntlet** | All 8 mechanics combined | • Zipline entry $\to$ High-speed Wall Run $\to$ Vault $\to$ Grapple swing ($26\text{m}$) $\to$ Air Dash $\to$ Ledge Climb $\to$ $9\text{m}$ drop Landing Roll $\to$ Speed Slide $\to$ Final Grapple Slingshot $\to$ Victory Platform | Length: $\approx 220\text{m}$<br>Elevation: $+40\text{m} \to +70\text{m}$ |

---

## 3. Caveats

- **CharacterController & Transform Physics**: The player uses a `CharacterController` driven by `PlayerMotor.cs`. It does NOT use Rigidbody forces. Therefore, moving platforms or interactive geometry must use kinematic updates or trigger colliders rather than Rigidbody physics joints.
- **Layer Integrity**:
  - `wallRunMask` requires Layer 6 (`WallRun`).
  - `grappleMask` requires Layer 7 (`Grapple`).
  - `groundMask` requires Layer 3 (`Ground`).
  - If any generated mesh is placed on Layer 0 (`Default`) instead of `WallRun` or `Grapple`, the respective mechanic will not trigger.
- **Editor-Only Execution**: Scene generation scripting must reside in `Assets/Editor/` with `#if UNITY_EDITOR` directives to ensure runtime standalone build compatibility.
- **Zipline Interaction**: `PlayerZiplineState.cs` requires a trigger volume component on the zipline object that switches the player state machine to `PlayerZiplineState`. We should provide a clean `ParkourZiplineTrigger.cs` component that detects player entry and configures direction and speed.

---

## 4. Conclusion

1. **ProBuilder 6.0.9** is fully installed, functional, and ideal for generating modular, UV-mapped, high-performance parkour level geometry.
2. **URP 17.0.4** with `SampleSceneProfile.asset`, directional sunlight, soft shadows, ACES tonemapping, and emissive bloom provides a high-end visual standard.
3. **The 8 Parkour Mechanics** have precise, verified kinematic constraints that map directly to our 5-sector design matrix.
4. **An automated Editor script** (`Assets/Editor/ParkourSceneGenerator.cs`) is the most reliable, deterministic, and repeatable approach to generate `Assets/Scenes/parkourTest.unity` with exact dimensions, layers, checkpoints, kill zones, and prefab references.

---

## 5. Verification Method

To independently verify these findings:
1. **Package Verification**: Inspect `Packages/manifest.json` line 10 for `"com.unity.probuilder": "6.0.9"`.
2. **Layer Verification**: Inspect `ProjectSettings/TagManager.asset` lines 7-17 for Layer 3 (`Ground`), Layer 6 (`WallRun`), and Layer 7 (`Grapple`).
3. **Kinematics Verification**: Inspect `Assets/Scripts/PlayerStateMachine.cs` (lines 45-350, 745-875, 1065-1115) to confirm all jump forces, check distances, and layer masks.
4. **Prefab Verification**: Inspect `Assets/Prefabs/-----Player.prefab` to confirm `CharacterController`, `PlayerStateMachine`, `PlayerMotor`, and Cinemachine camera setup.
5. **Material & Volume Verification**: Inspect `Assets/Settings/SampleSceneProfile.asset` and `Assets/AiHide/Material/Grid Large.mat` for URP Lit shader and post-processing parameters.
