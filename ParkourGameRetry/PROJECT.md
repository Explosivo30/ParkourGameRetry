# Project: Unity Parkour Scene Creation (`parkourTest.unity`)

## Architecture
The `parkourTest` scene (`Assets/Scenes/parkourTest.unity`) is a high-performance, modular 3D parkour environment built using Unity's ProBuilder (`UnityEngine.ProBuilder`) API, Universal Render Pipeline (URP 17.0.4), and Cinemachine 3.1.3.

- **Scene Hierarchy**:
  - `--- LIGHTING & ENVIRONMENT ---`: Directional Light (Sun), Global Volume (ACES Tonemapping, Bloom, Motion Blur, Vignette), Skybox, Slate-blue Fog.
  - `--- CAMERAS ---`: Scene `Main Camera` with `CinemachineBrain` and `AudioListener`.
  - `--- PLAYER ---`: Instance of `Assets/Prefabs/-----Player.prefab` placed at Sector 1 Spawn `(0, 1.5, 0)`.
  - `--- SECTOR 1: BASICS ---`: ProBuilder platforms, vault blocks, slide tunnels ($1.75\text{m}$ clearance), high drops for landing rolls (Thematic: Clean Prototyping White / Orange Trim).
  - `--- SECTOR 2: VERTICALITY ---`: ProBuilder vertical structures, Layer 6 wall-run surfaces (L/R), ledge climb walls, stairs (Thematic: Industrial Slate Blue / Cyan Trim).
  - `--- SECTOR 3: AERIAL AGILITY ---`: Air dash gaps, elevated Layer 7 grapple anchor spheres (Thematic: Midnight Obsidian / Emerald Green Emissive).
  - `--- SECTOR 4: SPEED & MOMENTUM ---`: Descending ziplines, 3-stage chained wall-run arrays, steep 25° momentum slide ramps (Thematic: Carbon Asphalt / Amber Glow).
  - `--- SECTOR 5: MASTERY GAUNTLET ---`: Full obstacle gauntlet chaining all 8 mechanics, leading to the Victory Monolith (Thematic: Synthwave Crimson / Gold).
  - `--- CHECKPOINTS ---`: 6 sequential checkpoints with neon visual beacons and respawn coordination (`CheckpointManager`).
  - `--- HAZARD & KILL ZONES ---`: Global void kill volume at $Y = -25\text{m}$ and local sector hazard triggers.

- **Layer Standards**:
  - `Layer 3` (`Ground`): All walkable floors, landing pads, ramps, stairs, and vault obstacle surfaces.
  - `Layer 6` (`WallRun`): All vertical wall-run panels.
  - `Layer 7` (`Grapple`): All grappling hook anchor spheres.
  - `Layer 0` (`Default`): Triggers, zipline volumes, checkpoint beacons, kill volumes.

---

## Feature Inventory
Every feature from the user request and survey phase:
| # | Feature | Description | Milestone | Status |
|---|---------|-------------|-----------|--------|
| 1 | Checkpoint System | `Checkpoint.cs` and `CheckpointManager.cs` handling safe `CharacterController` teleport respawning, active checkpoint tracking, and visual feedback | M1 | DONE |
| 2 | Kill / Fall Recovery Zone | `KillZone.cs` trigger resetting player to last activated checkpoint upon falling into void/hazards | M1 | DONE |
| 3 | Zipline Trigger System | `ParkourZipline.cs` trigger detecting player entry, activating `PlayerZiplineState`, guiding player along zipline vector | M1 | DONE |
| 4 | Scene Environment & Lighting Setup | Directional sun with soft shadows, URP Global Volume (SampleSceneProfile), Fog, Skybox, Main Camera with `CinemachineBrain` | M2 | DONE |
| 5 | Player Prefab Integration | Instantiation of `-----Player.prefab` at start position with proper camera and motor initialization | M2 | DONE |
| 6 | ProBuilder Mesh Generator Engine | Editor utility in `Assets/Editor/ParkourSceneGenerator.cs` utilizing `UnityEngine.ProBuilder` API (`ShapeGenerator`, `ProBuilderMesh`, `MeshCollider`, UVs, Materials) | M2 | DONE |
| 7 | Sector 1: Basics | ProBuilder course testing Jump, Vault ($0.3\text{m}-1.2\text{m}$), Slide ($1.75\text{m}$ clearance, $10^\circ$ slope), and Landing Roll ($5.5\text{m}$ drop) | M3 | DONE |
| 8 | Sector 2: Verticality | ProBuilder course testing Left & Right Wall Runs (Layer 6, $h=6\text{m}, l=14\text{m}$, gap $5.5\text{m}$) and Ledge Grab/Climb ($h=2.2\text{m}$) | M3 | DONE |
| 9 | Sector 3: Aerial Agility | ProBuilder course testing Air Dash ($10-12\text{m}$ gaps) and Grappling Hook (Layer 7 anchors, $18-25\text{m}$ ranges) | M4 | DONE |
| 10 | Sector 4: Speed & Momentum | ProBuilder course testing Zipline ($42\text{m}$ line), 3-stage chained Wall Runs, and steep $25^\circ$ Slide ramp reaching Flow Rank S | M4 | DONE |
| 11 | Sector 5: Mastery Gauntlet | Integrated obstacle course chaining all 8 mechanics into a continuous flow climax, ending at the Victory Monolith | M5 | DONE |
| 12 | Scene Assembly & Checkpoint Placement | Full placement of 6 Checkpoints (Start, S2, S3, S4, S5, Finish) and Kill Zones across the entire map, saving `Assets/Scenes/parkourTest.unity` | M5 | DONE |
| 13 | Comprehensive Verification & Validation | Automated test suite verifying scene existence, hierarchy, ProBuilder meshes, layers, colliders, triggers, player prefab, Cinemachine brain, and mechanics metrics | M6 | DONE |

---

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Core Framework & Scene Scripts | `Checkpoint.cs`, `CheckpointManager.cs`, `KillZone.cs`, `ParkourZipline.cs` | none | DONE |
| M2 | ProBuilder Generator Engine & Scene Base | `ParkourSceneGenerator.cs` (Base setup, Lighting, Skybox, Fog, Volume, Main Camera with CinemachineBrain, Player Prefab) | M1 | DONE |
| M3 | Sector 1 & Sector 2 Geometry | Sector 1 (Basics) & Sector 2 (Verticality) ProBuilder procedural geometry generators and material configs | M2 | DONE |
| M4 | Sector 3 & Sector 4 Geometry | Sector 3 (Aerial Agility) & Sector 4 (Speed & Momentum) ProBuilder procedural geometry generators and zipline/grapple configs | M3 | DONE |
| M5 | Sector 5 Gauntlet & Scene Finalization | Sector 5 (Mastery Gauntlet) generator, Checkpoints/KillZone placement, Full Scene build to `Assets/Scenes/parkourTest.unity` | M4 | DONE |
| M6 | Final Verification & Validation | Automated verification of scene components, layers, mechanics parameters, and gameplay integrity | M5 | DONE |

---

## Interface Contracts

### Checkpoint ↔ CheckpointManager
```csharp
public class Checkpoint : MonoBehaviour {
    public int sectorIndex;
    public string sectorName;
    public Transform spawnPoint;
    public Material activeMaterial;
    public Material inactiveMaterial;
    public MeshRenderer beaconRenderer;
    public bool isActivated;
    public void SetActiveState(bool active);
}

public class CheckpointManager : MonoBehaviour {
    public static CheckpointManager Instance { get; }
    public Checkpoint currentCheckpoint;
    public List<Checkpoint> allCheckpoints;
    public void ActivateCheckpoint(Checkpoint cp);
    public void RespawnPlayer(PlayerStateMachine player);
}
```

### KillZone ↔ CheckpointManager
```csharp
public class KillZone : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
        if (player != null && CheckpointManager.Instance != null) {
            CheckpointManager.Instance.RespawnPlayer(player);
        }
    }
}
```

### ParkourZipline ↔ PlayerStateMachine
```csharp
public class ParkourZipline : MonoBehaviour {
    public Transform startPoint;
    public Transform endPoint;
    public float speed = 15f;
    private void OnTriggerEnter(Collider other) {
        PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
        if (player != null) {
            Vector3 direction = (endPoint.position - startPoint.position).normalized;
            player.SwitchState(typeof(PlayerZiplineState));
        }
    }
}
```

---

## Code Layout
- `Assets/Scripts/ParkourScene/`
  - `Checkpoint.cs` (Created & Verified)
  - `CheckpointManager.cs` (Created & Verified)
  - `KillZone.cs` (Created & Verified)
  - `ParkourZipline.cs` (Created & Verified)
- `Assets/Editor/`
  - `ParkourSceneGenerator.cs` (Created & Verified)
- `Assets/Scenes/`
  - `parkourTest.unity` (Created, Populated & Verified)
- `Assets/Materials/Parkour/`
  - 19 Thematic URP Lit materials for Sectors 1-5 & Checkpoints (Created & Verified)
