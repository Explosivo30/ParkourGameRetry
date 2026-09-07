# E2E Test Infra: Unity Parkour Scene (`parkourTest`)

## Test Philosophy
- Opaque-box, requirement-driven verification of scene integrity, game object hierarchy, layer masks, ProBuilder meshes, Cinemachine brain binding, player prefab placement, checkpoint respawn mechanics, kill/recovery triggers, and sector-by-sector obstacle dimensional constraints.

## Feature Inventory & Test Coverage
| # | Feature | Requirement Source | Tier 1 (Feature) | Tier 2 (Boundary & Constraints) | Tier 3 (Cross-System Integration) |
|---|---------|-------------------|:----------------:|:-------------------------------:|:----------------------------------:|
| 1 | Scene Asset & Structure | `parkourTest.unity` exists, loads without errors, root objects present | 5 | 5 | ✓ |
| 2 | Lighting & Post-Processing | Directional Sun, URP Global Volume, ACES Tonemapping, Bloom, Fog | 5 | 5 | ✓ |
| 3 | Camera & Cinemachine | Main Camera tagged `MainCamera`, CinemachineBrain attached, FreeLook Camera bound | 5 | 5 | ✓ |
| 4 | Player Prefab Setup | `-----Player.prefab` placed at Sector 1 Start, CC, SM, Motor, InputReader configured | 5 | 5 | ✓ |
| 5 | Sector 1 (Basics) | Vault obstacles ($0.3-1.5\text{m}$), Slide tunnels ($1.1\text{m}$), Landing Roll drops ($>5\text{m}$) | 5 | 5 | ✓ |
| 6 | Sector 2 (Verticality) | Layer 6 WallRun walls ($h=6\text{m}, l=14\text{m}$, gap $5.5\text{m}$), Ledge Grab ($1.6-2.5\text{m}$) | 5 | 5 | ✓ |
| 7 | Sector 3 (Aerial Agility) | Air Dash gaps ($10-12\text{m}$), Layer 7 Grapple anchors ($18-25\text{m}$ range, max 30m) | 5 | 5 | ✓ |
| 8 | Sector 4 (Speed & Momentum) | Zipline triggers ($40\text{m}$ length), 3-stage Wall Run chains, Steep 25° Slide ramps | 5 | 5 | ✓ |
| 9 | Sector 5 (Mastery Gauntlet) | Full 8-mechanic combo sequence leading to Victory Monolith | 5 | 5 | ✓ |
| 10 | Checkpoints & Kill Zones | 6 Checkpoints (Start to Finish), CC safe respawn contract, Void Kill triggers | 5 | 5 | ✓ |

## Test Architecture
- Test Runner: Unity NUnit / Editor batch validation runner (`Assets/Editor/Tests/ParkourSceneValidationTests.cs` or CLI batchmode runner)
- Automated verification script checking:
  1. Scene loading and asset integrity.
  2. Hierarchy presence of all 5 sectors, checkpoints, lighting, cameras, player.
  3. Layer compliance: All floors on Layer 3 (`Ground`), all wall-run panels on Layer 6 (`WallRun`), all grapple targets on Layer 7 (`Grapple`).
  4. Collider compliance: All ProBuilder meshes have valid MeshColliders / BoxColliders; all triggers have `isTrigger = true`.
  5. CheckpointManager and Checkpoint components correctly wired with valid spawn points.
  6. Player CharacterController and PlayerStateMachine parameters aligned with physics specifications.
