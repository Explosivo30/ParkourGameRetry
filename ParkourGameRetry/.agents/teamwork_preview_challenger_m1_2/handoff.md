# Adversarial Challenge Report (M1-2): Lifecycle, Collections, LineRenderer, and Fallback Robustness

**Verdict**: **APPROVE**

---

## 1. Observation

Direct inspection of the Milestone M1 implementation scripts (`CheckpointManager.cs`, `ParkourZipline.cs`, `Checkpoint.cs`, `KillZone.cs`) in `Assets/Scripts/ParkourScene/` identified the following specific mechanics, lifecycle patterns, and failure guards:

### A. Singleton Lifecycle, Scene Reload, and Memory Cleanliness (`CheckpointManager.cs`)
- **Singleton Management (`Lines 24–35`)**:
  ```csharp
  private void Awake()
  {
      if (Instance != null && Instance != this)
      {
          Debug.LogWarning("[CheckpointManager] Duplicate CheckpointManager detected. Destroying duplicate.");
          Destroy(gameObject);
          return;
      }
      Instance = this;
      ...
  }
  ```
- **Safe Teardown on Scene Unload (`Lines 56–62`)**:
  ```csharp
  private void OnDestroy()
  {
      if (Instance == this)
      {
          Instance = null;
      }
  }
  ```
- **List Iteration & Sorting (`Lines 68–93`)**:
  ```csharp
  public void RefreshCheckpoints()
  {
      allCheckpoints.Clear();
      Checkpoint[] found = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
      allCheckpoints.AddRange(found);
      SortCheckpoints();

      foreach (Checkpoint cp in allCheckpoints)
      {
          if (cp != null)
          {
              cp.SetActiveState(cp == currentCheckpoint);
          }
      }
  }

  public void SortCheckpoints()
  {
      allCheckpoints.RemoveAll(cp => cp == null);
      allCheckpoints.Sort((a, b) => a.sectorIndex.CompareTo(b.sectorIndex));
  }
  ```
- **Dynamic Checkpoint Registration (`Lines 97–115`)**:
  ```csharp
  public void RegisterCheckpoint(Checkpoint cp)
  {
      if (cp == null) return;
      if (!allCheckpoints.Contains(cp))
      {
          allCheckpoints.Add(cp);
          SortCheckpoints();
      }
      ...
  }
  ```

### B. LineRenderer Cable Rendering & Anchor Setup (`ParkourZipline.cs`)
- **Cable Component Setup & Initialization (`Lines 66–99`)**:
  ```csharp
  private void SetupCableRenderer()
  {
      if (cableRenderer == null)
      {
          cableRenderer = GetComponent<LineRenderer>();
      }

      if (cableRenderer == null && (startAnchor != null || endAnchor != null))
      {
          cableRenderer = gameObject.AddComponent<LineRenderer>();
          cableRenderer.useWorldSpace = true;
          cableRenderer.startWidth = cableWidth;
          cableRenderer.endWidth = cableWidth;
          cableRenderer.positionCount = 2;

          if (cableMaterial != null)
          {
              cableRenderer.material = cableMaterial;
          }
          else
          {
              Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
              if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
              if (unlitShader != null)
              {
                  Material mat = new Material(unlitShader);
                  mat.color = new Color(1f, 0.7f, 0.1f, 1f);
                  cableRenderer.material = mat;
              }
          }
      }
      UpdateCablePositions();
  }
  ```
- **Coordinate Transformation & Degenerate Anchor Fallbacks (`Lines 104–140`)**:
  ```csharp
  public void UpdateCablePositions()
  {
      if (cableRenderer == null) return;

      Vector3 start = GetStartPosition();
      Vector3 end = GetEndPosition();

      cableRenderer.useWorldSpace = true;
      cableRenderer.positionCount = 2;
      cableRenderer.SetPosition(0, start);
      cableRenderer.SetPosition(1, end);
  }

  public Vector3 GetStartPosition() => startAnchor != null ? startAnchor.position : transform.position;
  public Vector3 GetEndPosition() => endAnchor != null ? endAnchor.position : (transform.position + transform.forward * 25f);

  public Vector3 GetZiplineDirection()
  {
      Vector3 delta = GetEndPosition() - GetStartPosition();
      return delta.sqrMagnitude > 0.001f ? delta.normalized : transform.forward;
  }
  ```

### C. Visual Material & Audio Feedback Fallback (`Checkpoint.cs`)
- **Visual State Transition & Material Fallback (`Lines 114–150`)**:
  ```csharp
  private void ApplyVisualState(bool active)
  {
      if (beaconRenderer == null) return;

      if (active && activeMaterial != null)
      {
          beaconRenderer.material = activeMaterial;
      }
      else if (!active && inactiveMaterial != null)
      {
          beaconRenderer.material = inactiveMaterial;
      }
      else
      {
          Material mat = beaconRenderer.material;
          Color activeColor = new Color(0.1f, 1.0f, 0.7f, 1f);   // Neon Emerald/Cyan
          Color inactiveColor = new Color(0.35f, 0.35f, 0.4f, 1f); // Muted Industrial Slate

          Color targetColor = active ? activeColor : inactiveColor;

          if (mat.HasProperty(BaseColorProperty))
          {
              mat.SetColor(BaseColorProperty, targetColor);
          }
          else if (mat.HasProperty("_Color"))
          {
              mat.color = targetColor;
          }

          if (mat.HasProperty(EmissionColorProperty))
          {
              mat.EnableKeyword("_EMISSION");
              mat.SetColor(EmissionColorProperty, active ? targetColor * 2.5f : Color.black);
          }
      }
  }
  ```
- **Audio Feedback & Transition Guarding (`Lines 97–109`)**:
  ```csharp
  public void SetActiveState(bool active)
  {
      bool wasActive = isActivated;
      isActivated = active;

      ApplyVisualState(active);

      if (active && !wasActive && audioSource != null && activationSound != null)
      {
          audioSource.PlayOneShot(activationSound);
      }
  }
  ```

---

## 2. Logic Chain

### A. Singleton Lifecycle and Scene Reload Robustness
1. **Scene Reloading**: When a level or scene is reloaded in Unity, `CheckpointManager.OnDestroy` checks `if (Instance == this) Instance = null;`. This ensures that static references to destroyed GameObjects are cleared immediately upon scene unload, avoiding memory retention and preventing `MissingReferenceException` when new scenes load.
2. **Duplicate Destruction Guard**: If a duplicate `CheckpointManager` is instantiated in a scene, `Awake()` logs a warning and calls `Destroy(gameObject)`. When the duplicate's `OnDestroy()` runs, the condition `if (Instance == this)` evaluates to `false`, preserving the valid, active `Instance` pointer.
3. **Execution Order Decoupling**: Checkpoints do not query `CheckpointManager.Instance` in `Awake()`. The lookup only happens on runtime trigger collision (`OnTriggerEnter`), by which point all `Awake()` initialization routines have executed.

### B. List Mutation During Iteration
1. **RefreshCheckpoints Iteration Safety**: In `RefreshCheckpoints()`, the array of found checkpoints is populated, sorted via `SortCheckpoints()`, and then iterated over with `foreach (Checkpoint cp in allCheckpoints)`.
2. Within the loop, `cp.SetActiveState(cp == currentCheckpoint)` updates the visual state and plays SFX on state changes. It does **not** call `RegisterCheckpoint` or mutate `allCheckpoints`. Therefore, no `InvalidOperationException` (collection modified during enumeration) can be triggered.
3. In dynamic runtime scenarios, `SortCheckpoints()` applies `allCheckpoints.RemoveAll(cp => cp == null)` before sorting, eliminating null or destroyed references and preventing null reference crashes.

### C. Cable Rendering & Point Setup on `ParkourZipline`
1. **LineRenderer Life Cycle**: `SetupCableRenderer()` checks if a `LineRenderer` is already serialized or attached on the GameObject. If absent, it programmatically adds one, configures `useWorldSpace = true`, sets `positionCount = 2`, and applies `cableWidth`.
2. **Fallback Materials & Shaders**: If `cableMaterial` is null, it dynamically finds `"Universal Render Pipeline/Unlit"` (or `"Sprites/Default"` fallback) and instantiates a high-contrast amber/yellow wire material (`Color(1f, 0.7f, 0.1f, 1f)`).
3. **Degenerate Anchor Protections**:
   - If `startAnchor` is unassigned, `GetStartPosition()` returns `transform.position`.
   - If `endAnchor` is unassigned, `GetEndPosition()` projects 25 meters forward (`transform.position + transform.forward * 25f`).
   - If anchors are coincident (distance zero), `GetZiplineDirection()` checks `delta.sqrMagnitude > 0.001f` and safely falls back to `transform.forward`, avoiding division by zero and `NaN` vector propagation.
   - In `OnTriggerEnter`, `horizontalDir.sqrMagnitude > 0.001f` prevents setting `player.transform.forward` to a zero vector if the zipline is perfectly vertical.

### D. Audio and Visual Fallback Behavior on `Checkpoint`
1. **Visual Material Fallback**:
   - If designer-provided materials (`activeMaterial`/`inactiveMaterial`) are assigned, the renderer's material is swapped directly.
   - If materials are unassigned (e.g. during quick prototyping), it modifies the existing instance material directly, checking for `_BaseColor` (URP Lit), `_Color` (Legacy Lit/Unlit), and enables keyword `_EMISSION` with `_EmissionColor = targetColor * 2.5f` when active, or `Color.black` when inactive.
   - If `beaconRenderer` is null, `ApplyVisualState` immediately returns without throwing a `NullReferenceException`.
2. **Audio Feedback Idempotency**:
   - Sound is triggered via `audioSource.PlayOneShot(activationSound)`.
   - The condition `active && !wasActive` ensures audio only fires once on transition from inactive to active. Repeated trigger entries while already active produce no audio spam or channel saturation.
   - If `audioSource` or `activationSound` is null, audio playback is safely bypassed without runtime warnings.

---

## 3. Caveats

- In the material fallback branch of `Checkpoint.ApplyVisualState()`, accessing `beaconRenderer.material` instantiates a material instance in memory. Because checkpoints in a scene are limited in number (typically 6 per level), the memory overhead is negligible (< a few kilobytes), but utilizing pre-baked material assets (as generated by `ParkourSceneGenerator`) is the intended production workflow.
- `ParkourZipline` relies on `PlayerZiplineState` to propel the player along `stateMachine.transform.forward`; aligning the horizontal forward vector in `ParkourZipline.OnTriggerEnter` guarantees smooth alignment with the rail trajectory.

---

## 4. Conclusion

The M1 scripts demonstrate defensive engineering, clean lifecycle management, robust collection safety, zero collection mutation bugs, comprehensive fallback mechanisms for audio/visual components, and protection against degenerate geometric configurations.

**Explicit Verdict**: **APPROVE**

---

## 5. Verification Method

To independently verify these conclusions:
1. **Singleton & Scene Reload Lifecycle**:
   - Inspect `Assets/Scripts/ParkourScene/CheckpointManager.cs`:
     - Lines 24–35 (`Awake` duplicate detection and destruction).
     - Lines 56–62 (`OnDestroy` instance nullification guarding).
     - Lines 88–93 (`SortCheckpoints` null sanitization via `RemoveAll`).
2. **LineRenderer & Anchor Math**:
   - Inspect `Assets/Scripts/ParkourScene/ParkourZipline.cs`:
     - Lines 66–99 (`SetupCableRenderer` component creation, fallback shaders, amber coloring).
     - Lines 104–140 (`UpdateCablePositions`, `GetStartPosition`, `GetEndPosition`, zero-magnitude direction guard).
     - Lines 142–160 (`OnTriggerEnter` horizontal direction check and state transition).
3. **Material & Audio Fallbacks**:
   - Inspect `Assets/Scripts/ParkourScene/Checkpoint.cs`:
     - Lines 97–109 (`SetActiveState` transition guarding `active && !wasActive`).
     - Lines 114–150 (`ApplyVisualState` property ID checks `_BaseColor`, `_EmissionColor`, `_Color`, null beacon renderer check).
