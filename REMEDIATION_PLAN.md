# Shredsquatch Remediation Plan

Based on the [Vibe-Code Detection Audit v2.0](VIBE_CHECK_REPORT.md) findings. Organized into 4 phases ordered by impact — each phase is self-contained and leaves the project in a better state than before.

---

## Phase 0: Documentation Honesty (Day 1)

**Goal:** Make the README reflect what actually exists. This is the single highest-impact change — it converts the project from "overclaimed vibe-code" to "honest alpha with a solid core."

**Estimated scope:** ~200 lines changed across 3 files, no code changes.

### 0.1 — Rewrite README.md feature claims

Strip or clearly mark every undocumented feature:

| Section | Action |
|---------|--------|
| Challenger Mode (lines 503-528) | Remove entirely. Zero code exists. |
| Avalanche Mode mechanics (lines 487-497) | Move to a new "Planned Features" section at the bottom. Mark as "Not Yet Implemented — enum defined, no unique gameplay." |
| Storm Mode mechanics (lines 498-501) | Same — move to "Planned Features." |
| "Object pooling for powerups and obstacles" (line 699) | Remove. Not implemented. |
| "Dynamic LOD for terrain chunks" (line 698) | Remove. Not implemented. |
| "25+ reusable prefabs" (line 601) | Rewrite to "Procedural placeholder prefabs generated at runtime via ProceduralAssetFactory." |
| Moonlight phases (lines 248-254) | Move to "Planned Features." No moon phase code exists. |
| "Steam/itch.io integration for global distance/trick highs" (line 37) | Rewrite to "Local leaderboards. Platform integration planned." |
| Connected Repositories section (lines 732-755) | Delete entirely. SEO cross-linking adds no value to the game. |

Add a new section after "Overview":

```markdown
## Current Status (v0.1.0-alpha)

**What works:**
- Snowboard physics with slope acceleration, carving, tuck, braking, powder drag
- 14 trick types (6 spins, 4 grabs, 4 flips) with combo multipliers and repetition penalties
- Procedural infinite terrain with seeded generation and chunk streaming
- Sasquatch chase AI with rubber-band distance tracking
- Rail grinding with 7 rail types and balance mechanics
- HUD with distance/speed, trick display, combo counter, Sasquatch proximity bar
- Achievement system (17 achievements) and local leaderboards
- Crash/ragdoll system with recovery and invincibility frames
- Screen shake and controller haptic feedback
- Error recovery system with automatic reset

**What doesn't work yet:**
- Avalanche Mode (selectable but plays identically to Standard)
- Storm Mode (selectable but plays identically to Standard)
- Challenger Mode (documented only, no code)
- Platform leaderboard integration (local only)
- Object pooling (uses Instantiate/Destroy)
- Dynamic LOD
- Moon phases
```

### 0.2 — Delete KEYWORDS.md

This file is SEO padding with no development value.

### 0.3 — Add TODO markers to code

Add honest markers for known gaps. These serve as breadcrumbs for any developer (human or AI) working on the project:

| File | Line | TODO |
|------|------|------|
| `MainMenuController.cs:117` | `OnAvalancheClicked` | `// TODO: Avalanche mode plays identically to Standard — needs boulder spawning, shadow warnings` |
| `MainMenuController.cs:125` | `OnStormClicked` | `// TODO: Storm mode plays identically to Standard — needs wind gusts, reduced visibility` |
| `LeaderboardManager.cs:11` | Class header | `// TODO: Platform integration (Steam, itch.io) deferred — currently local-only` |
| `SafeExecution.cs:142` | Bare catch | `// TODO: Bare catch swallows all exceptions including OOM — should be catch(Exception)` |
| `TerrainGenerator.cs:45` | `_activeChunks` | `// TODO: This list is maintained but never read — remove or use it` |

---

## Phase 1: Test Suite Overhaul (Days 2-3)

**Goal:** Replace existence-checking tests with behavioral tests. After this phase, the test suite provides actual confidence in the codebase.

**Estimated scope:** ~400 lines changed across 4 test files + 1 new test file.

### 1.1 — Fix `TerrainStreamingTests.cs` (11 tests → 11 real tests)

**Replace IsNotNull assertions with behavioral checks:**

| Test | Current (Broken) | Remediated |
|------|------------------|------------|
| `TerrainGenerator_InitializesWithSeed` | `Assert.IsNotNull(_terrainGenerator)` | Set seed via `SetSeed(12345)`, call `GenerateInitialChunks()`, verify chunk count > 0 |
| `TerrainGenerator_HasPlayerReference` | `Assert.IsNotNull(_terrainGenerator)` | Set player ref, call `GenerateInitialChunks()`, verify chunks generated near player position |
| `ChunkLoading_TriggersWhenPlayerMoves` | `Assert.IsNotNull(_terrainGenerator)` | Move player 500m, wait for Update, verify `_terrainGenerator.transform.childCount > 0` (chunks are children of container) |
| `SameSeeds_ProduceDeterministicResults` | Tests `System.Random`, not terrain | Create two TerrainGenerators with same seed, generate chunk at (0,0), compare heightmaps |
| `DifferentSeeds_ProduceDifferentResults` | Tests `System.Random`, not terrain | Two generators, different seeds, verify heightmaps differ |
| `TerrainChunk_CanBeCreatedProgrammatically` | Just adds component | Create chunk, call `Initialize()` and `GenerateMesh()`, verify mesh has vertices |
| `LoadDistance_IsGreaterThanUnloadDistance` | Tests hardcoded local vars | Access actual generator fields via reflection or expose as test-visible properties |
| `NoiseGenerator_ProducesValuesInRange` | Tests `Mathf.PerlinNoise` | Call actual `NoiseGenerator.GenerateNoiseMap()`, verify all values in [0,1] range |
| `ObstacleSpawning_UsesSeededRandom` | Tests `System.Random` | Two generators with same seed, generate same chunk, verify tree/rock positions match |

### 1.2 — Fix `PlayerTrickScoringTests.cs` (9 tests → 9 real tests)

| Test | Current (Broken) | Remediated |
|------|------------------|------------|
| `TrickController_CompletesSpinTrick_WhenRotationThresholdMet` | Calls `AddTrickScore` directly (bypasses trick system) | Simulate jump via `JumpController`, rotate player 360°, trigger landing, verify score matches `TrickDatabase.GetDefinition(TrickType.Spin360).BasePoints` |
| `TrickController_NotifiesOnTrickComplete` | Subscribes to event, never triggers it, asserts `IsNotNull` | Simulate full trick flow, verify event fires with correct name and points |

### 1.3 — Fix `ErrorRecoveryTests.cs` (17 tests → 17 real tests)

| Test | Current (Broken) | Remediated |
|------|------------------|------------|
| `ErrorRecoveryManager_RegistersRecoverables` | `Assert.IsNotNull(_errorManager)` | Trigger recovery, verify `AttemptRecovery()` was called on GameManager (check state changed to MainMenu) |
| `PlayerController_AttemptRecovery_ResetsPosition` | `Assert.IsNotNull(_player)` | Set known safe position, move player far away, call recovery, assert `transform.position == safePosition` |
| `TerrainGenerator_AttemptRecovery_ClearsState` | `Assert.IsNotNull(_terrainGenerator)` | Generate chunks, call recovery, verify chunk container has 0 children |
| `MultipleErrors_TriggerRecoveryAtThreshold` | `Assert.IsNotNull(_errorManager)` | Assert `recoveryStarted == true` (the variable is already set up, just not asserted properly) |

### 1.4 — Fix `SasquatchChaseTests.cs` (9 tests → 9 real tests)

| Test | Current (Broken) | Remediated |
|------|------------------|------------|
| `Sasquatch_RubberBanding_SpeedsUpWhenFar` | Only checks constants | Position Sasquatch 900m behind player, wait for Update, verify `_sasquatch.CurrentSpeed > Constants.Sasquatch.BaseSpeed` |
| `Sasquatch_ApplyRepellent_SlowsChase` | Checks constant value | Apply repellent, verify `_sasquatch.CurrentSpeed < normalSpeed` after Update |

### 1.5 — Add error-path tests (new file: `ErrorPathTests.cs`)

Create `Assets/Tests/PlayMode/ErrorPathTests.cs` with these tests:

```
Test: GameManager_EndRun_WhenNotPlaying_DoesNotCrash
  - Call EndRun() from MainMenu state
  - Verify state doesn't change to GameOver (guard against invalid transitions)

Test: GameManager_StartRun_WhenAlreadyPlaying_ResetsState
  - Start a run, then start another without ending first
  - Verify stats are reset, state is Playing

Test: SafeExecution_Try_WithNullAction_ReturnsFalse
  - Pass null Action to Try()
  - Verify returns false, no exception

Test: SafeExecution_TryInvoke_WithNullEvent_DoesNotThrow
  - Pass null event to TryInvoke()
  - Verify no exception

Test: TerrainGenerator_GenerateInitialChunks_WithNullPlayer_DoesNotCrash
  - Set player reference to null
  - Call GenerateInitialChunks()
  - Verify no exception

Test: TrickController_EndCombo_WhenNoComboActive_DoesNotCrash
  - Call EndCombo(false) with no active combo
  - Verify no exception, combo count stays 0

Test: LeaderboardManager_SubmitScore_Zero_StillRecords
  - Submit a score of 0.0
  - Load leaderboard, verify entry exists

Test: AchievementManager_TryUnlock_AlreadyUnlocked_ReturnsFalse
  - Unlock an achievement
  - Try to unlock it again
  - Verify returns false
```

### 1.6 — Add parametrized trick scoring tests

Add to `PlayerTrickScoringTests.cs`:

```
[TestCase(180, 500)]    // Spin180 = 500 pts
[TestCase(360, 1500)]   // Spin360 = 1500 pts
[TestCase(540, 3000)]   // Spin540 = 3000 pts
[TestCase(720, 5000)]   // Spin720 = 5000 pts
[TestCase(900, 8000)]   // Spin900 = 8000 pts
[TestCase(1080, 12000)] // Spin1080 = 12000 pts
Test: SpinRotation_MapsToCorrectTrickPoints(float rotation, int expectedPoints)

[TestCase(0.1f, 150)]   // Panic grab
[TestCase(0.4f, 300)]   // Standard
[TestCase(0.7f, 450)]   // Skilled
[TestCase(1.2f, 600)]   // Expert
Test: GrabTiming_MapsToCorrectPoints(float grabStartTime, int expectedPoints)

[TestCase(1, 1.0f)]
[TestCase(2, 1.5f)]
[TestCase(3, 2.0f)]
[TestCase(4, 2.5f)]
[TestCase(5, 3.0f)]
Test: ComboLength_MapsToCorrectMultiplier(int combo, float expectedMult)
```

---

## Phase 2: Error Handling & Code Cleanup (Days 4-5)

**Goal:** Replace blanket exception catching with typed handling. Remove dead code. Fix API inconsistencies.

**Estimated scope:** ~150 lines changed across 6 files.

### 2.1 — Fix bare catch blocks in SafeExecution.cs

**File:** `Assets/Scripts/Core/SafeExecution.cs`

**Change at lines 141-144 and 157-160:**

```csharp
// BEFORE (swallows OutOfMemoryException, StackOverflowException, etc.)
catch
{
    return null;
}

// AFTER
catch (Exception)
{
    return null;
}
```

This is a 2-line change with outsized impact. Bare `catch` blocks capture `ThreadAbortException`, `OutOfMemoryException`, and other CLR-level exceptions that should never be silently swallowed.

### 2.2 — Create typed exception hierarchy

**New file:** `Assets/Scripts/Core/GameExceptions.cs`

```csharp
namespace Shredsquatch.Core
{
    /// <summary>
    /// Thrown when terrain generation fails (corrupt heightmap, missing prefabs).
    /// </summary>
    public class TerrainGenerationException : System.Exception
    {
        public Vector2Int ChunkCoord { get; }
        public TerrainGenerationException(Vector2Int coord, string message, System.Exception inner = null)
            : base(message, inner) { ChunkCoord = coord; }
    }

    /// <summary>
    /// Thrown when save/load operations fail (corrupt PlayerPrefs, JSON parse error).
    /// </summary>
    public class SaveDataException : System.Exception
    {
        public string Key { get; }
        public SaveDataException(string key, string message, System.Exception inner = null)
            : base(message, inner) { Key = key; }
    }
}
```

Then update `GameManager.LoadProgress()` (line 197-207) to catch `System.Exception` specifically and wrap it:

```csharp
private void LoadProgress()
{
    if (PlayerPrefs.HasKey("PlayerProgress"))
    {
        try
        {
            string json = PlayerPrefs.GetString("PlayerProgress");
            Progress = JsonUtility.FromJson<PlayerProgress>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameManager] Corrupt save data, resetting: {ex.Message}");
            Progress = new PlayerProgress();
        }
    }
    else
    {
        Progress = new PlayerProgress();
    }
}
```

Apply the same pattern to `LeaderboardManager.LoadLocalLeaderboard()` (line 197-207) and `AchievementManager.LoadProgress()` (line 443-463).

### 2.3 — Remove dead `_activeChunks` field

**File:** `Assets/Scripts/Terrain/TerrainGenerator.cs`

Remove these lines:
- Line 45: `private List<Vector2Int> _activeChunks = new List<Vector2Int>();`
- Line 217: `_activeChunks.Add(coord);`
- Line 455: `_activeChunks.Remove(coord);`
- Line 591: `_activeChunks.Clear();`

### 2.4 — Consolidate duplicate SetPlayer methods

**Files:** `GameManager.cs`, `TerrainGenerator.cs`

Both have `SetPlayerReference(Transform)` AND `SetPlayer(Transform)` where the latter just calls the former. The `SetPlayer` alias was added "for SceneInitializer compatibility" but `SceneInitializer.cs` should just call `SetPlayerReference` directly.

**Action:**
1. In `SceneInitializer.cs`, replace `SetPlayer(` calls with `SetPlayerReference(` (lines 144, 150)
2. Remove `SetPlayer()` methods from `GameManager.cs` (lines 218-221) and `TerrainGenerator.cs` (lines 525-529)

### 2.5 — Add TerrainGenerator cleanup in OnDestroy

**File:** `Assets/Scripts/Terrain/TerrainGenerator.cs`

Update `OnDestroy()` (lines 85-91):

```csharp
private void OnDestroy()
{
    if (ErrorRecoveryManager.Instance != null)
    {
        ErrorRecoveryManager.Instance.UnregisterRecoverable(this);
    }

    // Clean up chunk container
    if (_chunkContainer != null)
    {
        Destroy(_chunkContainer.gameObject);
    }
}
```

### 2.6 — Add user-visible message on error recovery

**File:** `Assets/Scripts/Core/ErrorRecoveryManager.cs`

The recovery flow currently resets silently. Add a callback that UI can subscribe to:

In the existing `OnRecoveryComplete` event handler (which already exists), ensure `GameOverScreen` or a new `ErrorNotification` UI component displays a brief message like "Something went wrong. Returning to menu." for 3 seconds.

This requires:
1. A new `[SerializeField] private string _recoveryMessage` in `ErrorRecoveryManager`
2. Firing the existing `OnRecoveryComplete` event (already happens)
3. A UI subscriber that displays the message temporarily

---

## Phase 3: Object Pooling (Days 6-7)

**Goal:** Replace `Instantiate()`/`Destroy()` with pooled allocation for terrain obstacles. This is the most impactful performance improvement, especially for WebGL target.

**Estimated scope:** ~200 lines new code, ~50 lines changed.

### 3.1 — Create generic ObjectPool class

**New file:** `Assets/Scripts/Core/ObjectPool.cs`

```csharp
namespace Shredsquatch.Core
{
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Queue<GameObject> _available;
        private readonly int _maxSize;

        public ObjectPool(GameObject prefab, Transform container, int initialSize, int maxSize);
        public GameObject Get(Vector3 position, Quaternion rotation, Vector3 scale);
        public void Return(GameObject obj);
        public void Clear();
    }
}
```

Key design decisions:
- **Max size cap** — prevents unbounded growth (addresses audit finding about unbounded `_chunks` dict)
- **Pre-warm** — initial pool size avoids first-frame allocation spikes
- **Container parent** — all pooled objects live under a single Transform for hierarchy cleanliness
- **Return resets** — returned objects are deactivated and re-parented to container

### 3.2 — Create TerrainObjectPoolManager

**New file:** `Assets/Scripts/Terrain/TerrainObjectPoolManager.cs`

Manages per-prefab pools for each obstacle type:

```csharp
namespace Shredsquatch.Terrain
{
    public class TerrainObjectPoolManager : MonoBehaviour
    {
        // Pool per prefab type
        private Dictionary<GameObject, ObjectPool> _pools;

        public void Initialize(GameObject[] treePrefabs, GameObject[] rockPrefabs,
                               GameObject[] rampPrefabs, GameObject[] railPrefabs,
                               GameObject coinPrefab);

        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale);
        public void Despawn(GameObject obj);
        public void DespawnAll(); // For chunk unloading
    }
}
```

Pool sizes based on typical visible density:

| Prefab Type | Initial Pool | Max Pool | Rationale |
|-------------|-------------|----------|-----------|
| Trees | 50 | 200 | Highest density obstacle |
| Rocks | 20 | 80 | Medium density |
| Ramps | 5 | 20 | Sparse |
| Rails | 5 | 20 | Sparse |
| Coins | 30 | 100 | Lines of 5-10 per chunk |

### 3.3 — Integrate pools into TerrainGenerator

**File:** `Assets/Scripts/Terrain/TerrainGenerator.cs`

Changes:
1. Add `[SerializeField] private TerrainObjectPoolManager _poolManager` field
2. In `Start()`, initialize pool manager with prefab arrays
3. Replace all `chunk.SpawnObject(prefab, ...)` calls with `_poolManager.Spawn(prefab, ...)` (affects `SpawnTrees`, `SpawnRocks`, `SpawnRamps`, `SpawnCoins`, `SpawnRails`)
4. In `UnloadChunk()`, return all chunk children to pool via `_poolManager.Despawn()` instead of `Destroy()`

### 3.4 — Update TerrainChunk to track pooled objects

**File:** `Assets/Scripts/Terrain/TerrainChunk.cs`

`TerrainChunk.SpawnObject()` currently instantiates and parents to the chunk. Change it to:
1. Accept a pre-spawned (pooled) GameObject instead of a prefab
2. Track spawned objects in a `List<GameObject>` for return-to-pool on unload
3. `Clear()` method returns all tracked objects to pool

---

## Phase 4: Game Mode Implementation (Days 8-12)

**Goal:** Make Avalanche and Storm modes playable with distinct gameplay. This converts 2 major audit findings from "stub" to "implemented."

**Estimated scope:** ~400 lines new code across 3-4 new files.

### 4.1 — Create GameModeModifier base class

**New file:** `Assets/Scripts/Core/GameModeModifier.cs`

```csharp
namespace Shredsquatch.Core
{
    /// <summary>
    /// Base class for game mode-specific gameplay modifiers.
    /// Activated when GameManager starts a run with the corresponding mode.
    /// </summary>
    public abstract class GameModeModifier : MonoBehaviour
    {
        public abstract GameMode Mode { get; }
        public abstract void Activate();
        public abstract void Deactivate();
    }
}
```

### 4.2 — Implement AvalancheModifier

**New file:** `Assets/Scripts/GameModes/AvalancheModifier.cs`

Implements the documented mechanics:
- Boulders spawn every 5-10 seconds after 2km distance
- Boulder speed: 60-80 km/h (slower than max player speed)
- Shadow warning appears 1.5 seconds before impact
- Direct hit = instant ragdoll + 100m tumble
- Boulders destroy trees on contact

Core implementation:
1. Subscribe to `GameManager.OnDistanceChanged`
2. After 2km, start a repeating coroutine that spawns boulders
3. Boulder is a sphere with `Rigidbody` moving downhill at random speed
4. Shadow projector (or simple dark circle) placed at predicted landing zone
5. OnTriggerEnter with player → call `CrashHandler.TriggerCrash()`
6. OnTriggerEnter with Tree tag → destroy tree

### 4.3 — Implement StormModifier

**New file:** `Assets/Scripts/GameModes/StormModifier.cs`

Implements the documented mechanics:
- Visibility reduced to 50m (via fog distance or camera far clip)
- Wind gusts every 8-15 seconds, 2-3 second duration
- Wind push: 10-20 km/h lateral force
- Wind direction shown by particle system angle

Core implementation:
1. On activate, reduce `RenderSettings.fogEndDistance` to 50m
2. Start wind gust coroutine: random interval, random direction
3. During gust, apply lateral force to player via `SnowboardPhysics` (add new `ApplyExternalForce(Vector3)` method)
4. Rotate snow particle system to show wind direction
5. On deactivate, restore fog settings

### 4.4 — Wire modifiers into GameManager

**File:** `Assets/Scripts/Core/GameManager.cs`

In `StartRun()`:
1. Find all `GameModeModifier` components in scene
2. Activate the one matching `_currentMode`
3. Deactivate all others

In `EndRun()` / `ReturnToMenu()`:
1. Deactivate all modifiers

### 4.5 — Add SnowboardPhysics.ApplyExternalForce

**File:** `Assets/Scripts/Player/SnowboardPhysics.cs`

Add a method for external forces (wind, explosions):

```csharp
private Vector3 _externalForce;

public void ApplyExternalForce(Vector3 force)
{
    _externalForce = force;
}
```

In `MovePlayer()`, add `_externalForce * Time.deltaTime` to the movement vector. Decay `_externalForce` each frame.

---

## Summary & Expected Impact

| Phase | Effort | Audit Score Impact | Key Metric |
|-------|--------|-------------------|------------|
| Phase 0: Doc Honesty | 1 day | A6: 1→3 (+2) | README matches reality |
| Phase 1: Test Overhaul | 2 days | A3: 1→3 (+2) | 0 IsNotNull-only tests, error-path coverage |
| Phase 2: Error Handling | 2 days | B1: 1→2 (+1) | Typed exceptions, no bare catches |
| Phase 3: Object Pooling | 2 days | B3: 2→3 (+1), B7: 2→3 (+1) | Zero Instantiate/Destroy in gameplay loop |
| Phase 4: Game Modes | 5 days | B3: 2→3 (already counted), A6: (already counted) | 2 playable alternate modes |

**Projected post-remediation scores:**

| Domain | Before | After | Change |
|--------|--------|-------|--------|
| A. Surface Provenance | 10/21 (47.6%) | 16/21 (76.2%) | +28.6% |
| B. Behavioral Integrity | 16/21 (76.2%) | 19/21 (90.5%) | +14.3% |
| C. Interface Authenticity | 16/21 (76.2%) | 17/21 (81.0%) | +4.8% |

**Projected Vibe-Code Confidence:** 100% - [(76.2% × 0.20) + (90.5% × 0.50) + (81.0% × 0.30)] = 100% - 84.8% = **15.2%**

**Projected Classification: Human-Authored** (0-15% range) — down from AI-Assisted (29.5%).

The single biggest driver is documentation honesty (Phase 0) — a day of work that eliminates 80% of the credibility damage.
