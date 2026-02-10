# SHREDSQUATCH REFOCUS PLAN

**Status:** The game has 14,400 lines of clean C# across 65 scripts but cannot be played. The infrastructure-to-content ratio is inverted. This plan gets the game playable.

**Goal:** A browser-playable vertical slice where you snowboard downhill, dodge procedural obstacles, perform tricks, and get chased by a Sasquatch. 60 seconds of fun in a WebGL tab.

---

## PHASE 0: FIX THE BLOCKER (Day 1)

The game literally cannot initialize. There is a compile/runtime error that prevents the Sasquatch from spawning.

### Bug 1: Wrong namespace and method in SceneInitializer (CRITICAL)

**File:** `Assets/Scripts/Core/SceneInitializer.cs:155-158`
```csharp
// BROKEN — namespace 'Enemy' doesn't exist, method 'SetTarget' doesn't exist
var sasquatchAI = _sasquatchInstance.GetComponent<Enemy.SasquatchAI>();
if (sasquatchAI != null)
{
    sasquatchAI.SetTarget(_playerInstance.transform);
}
```

**Fix:**
1. Add `using Shredsquatch.Sasquatch;` to the imports (line 1-7 area)
2. Change `Enemy.SasquatchAI` → `SasquatchAI` on line 155
3. Change `sasquatchAI.SetTarget(...)` → `sasquatchAI.SetPlayerReference(...)` on line 158

### Bug 2: HUD SetSasquatch passes broken reference

**File:** `Assets/Scripts/Core/SceneInitializer.cs:164`
```csharp
_hudController.SetSasquatch(sasquatchAI);
```
This passes the result of the broken `GetComponent<Enemy.SasquatchAI>()` call, which will always be null. The fix to Bug 1 also fixes this — once the type resolves correctly, the HUD gets a valid reference.

### Bug 3: RuntimeDebugger uses static access on instance property

**File:** `Assets/Scripts/Core/RuntimeDebugger.cs:94-95`
```csharp
_sb.AppendLine($"Distance: {GameManager.CurrentRun.Distance:F2} km");
_sb.AppendLine($"Score: {GameManager.CurrentRun.TrickScore}");
```
`CurrentRun` is an instance property on `GameManager`, not static. Should be `GameManager.Instance.CurrentRun`. This will throw a compile error or NRE.

### Verification
After these fixes, the bootstrap chain should execute:
```
AutoSetup.Awake() → creates ErrorRecoveryManager + GameManager
SceneInitializer.Start() → wires terrain, spawns player, schedules Sasquatch
GameStarter.Start() → wires references, invokes StartGame after 0.5s
... 30s later ...
SceneInitializer.SpawnSasquatch() → creates Sasquatch, wires to player + HUD ✓
```

---

## PHASE 1: CUT THE DEAD WEIGHT (Day 1-2)

Remove code that adds complexity without contributing to the playable loop.

### Delete Entirely

| Target | Path | Lines | Why |
|--------|------|-------|-----|
| Challenger Mode (6 files) | `Assets/Scripts/Challenger/` | ~600+ | Self-contained island — no file outside this directory references these classes. Marked `[PLACEHOLDER]` in source. Entire alternate game mode that isn't wired to anything. |
| Store Pages | `StorePages/` | ~200 | Marketing copy for a game with no playable build. Write these when there's something to sell. |
| SEO Keywords | `KEYWORDS.md` | ~300 | Same reason. |

### Gut to Stubs

| Target | File | Action |
|--------|------|--------|
| **LeaderboardManager** | `Assets/Scripts/Progression/LeaderboardManager.cs` | Remove `InitializeSteam()`, `InitializeItchIO()`, `SubmitScoreSteam()`, `SubmitScoreItchIO()`, `LoadLeaderboardSteam()`, `LoadLeaderboardItchIO()`, `ReportAchievementSteam()` — all stubs with commented-out code. Keep `LeaderboardPlatform` enum but only `Local` path. Saves ~150 lines of dead code. |
| **AchievementManager** | `Assets/Scripts/Progression/AchievementManager.cs` | Keep the data model (`Achievement` class, `AchievementId` enum) and save/load. Remove all the event subscription wiring in `SubscribeToEvents()` — it uses `FindFirstObjectByType` which is slow and fragile. Re-add when the game loop is stable and you're ready for retention mechanics. |
| **WebGLCompatibility** | `Assets/Scripts/Core/WebGLCompatibility.cs` | Keep `IsWebGL` detection and `OnLowMemory`. Delete `ApplySafariOptimizations()`, `ApplyFirefoxOptimizations()`, `ApplyChromiumOptimizations()` — each is a single `QualitySettings` tweak wrapped in 20 lines of ceremony. Replace with one method: `ApplyWebGLDefaults()`. |
| **PerformanceMonitor** | `Assets/Scripts/Core/PerformanceMonitor.cs` | Keep FPS tracking (`TrackFrameRate()`). Delete `ApplyLongRunOptimizations()`, `EvaluateQualityAdjustment()`, `ReduceQuality()`, `IncreaseQuality()` — all speculative optimization without profiling data. Delete `TriggerMemoryCleanup()` — calling `System.GC.Collect()` causes the exact frame spikes it's trying to prevent. |

### Disable in UI (Don't Delete)

| Feature | Where | Action |
|---------|-------|--------|
| Avalanche Mode | `MainMenuController` | Disable button/hide option. `GameMode.Avalanche` enum value stays, but UI shouldn't offer it. |
| Storm Mode | `MainMenuController` | Same. |
| Sasquatch skin variants | `SasquatchSkin.cs` | Leave file, don't wire to UI. Nobody can see skins without a working game. |

---

## PHASE 2: CONSOLIDATE THE BOOTSTRAP (Day 2-3)

The game has **three** overlapping initialization systems: `AutoSetup`, `SceneInitializer`, and `GameStarter`. They duplicate work and create fragile timing dependencies.

### Problem
- `AutoSetup` (execution order -1000) creates GameManager, finds Player, adds components
- `SceneInitializer` spawns Player from prefab, wires to GameManager + TerrainGenerator
- `GameStarter` also wires player to GameManager + TerrainGenerator (redundant)
- Both `GameStarter.Start()` and `SceneInitializer.Start()` use `Invoke()` with different delays

### Resolution

**Keep `SceneInitializer` as the single source of truth for scene wiring.** It's the most complete and correct.

**Simplify `GameStarter` to only call `StartRun()`:**
```csharp
// GameStarter.cs — simplified
private void Start()
{
    if (_autoStart)
    {
        Invoke(nameof(StartGame), _startDelay);
    }
}

public void StartGame()
{
    GameManager.Instance?.StartRun(_defaultMode);
}
```

Remove all reference-wiring code from `GameStarter.Start()` (lines 38-55). Let `SceneInitializer` handle it.

**Keep `AutoSetup` as a safety net** — it ensures GameManager and ErrorRecoveryManager exist even if the scene is missing them. But remove `EnsurePlayerComponents()` — `SceneInitializer` spawns the player from a prefab that should already have the right components. Auto-adding components at runtime masks prefab configuration bugs.

---

## PHASE 3: SILENCE THE ERROR RECOVERY (Day 3)

### Problem
`SafeExecution.TryUpdate()` silently swallows exceptions in every `Update()` loop:

- `GameManager.cs:69` — wraps `UpdateRunStats()`
- `PlayerController.cs:94-98` — wraps entire Update body
- `TerrainGenerator.cs:90-91` — wraps chunk updates AND queue processing

During alpha, **you need crashes to be loud.** Silent failures hide bugs. The `_updateHasErrored` flag means a system errors once and then permanently stops running — with no visible indication except the game quietly breaking.

### Action
Replace `SafeExecution.TryUpdate()` calls with direct calls in all gameplay code. Keep `SafeExecution.Try()` for truly defensive spots (save/load, event invocation). Keep the `ErrorRecoveryManager` and `IRecoverable` infrastructure — just don't use it to suppress Update loops.

**Before:**
```csharp
SafeExecution.TryUpdate(UpdateRunStats, ref _updateHasErrored, "GameManager.Update");
```

**After:**
```csharp
UpdateRunStats();
```

Do this in: `GameManager.Update()`, `PlayerController.Update()`, `TerrainGenerator.Update()`.

Keep `SafeExecution.TryInvoke()` for event dispatch — that's legitimately useful (prevents one bad handler from breaking all listeners).

---

## PHASE 4: GET THE LOOP RUNNING (Day 3-5)

This is the core work. Everything above is cleanup to make this possible.

### The Minimum Viable Loop
```
Player spawns on snow →
  terrain generates ahead →
    player accelerates downhill →
      player steers around trees/rocks →
        player hits ramp, goes airborne →
          player lands (trick scored) →
            ... repeat for 5km ...
              Sasquatch spawns behind →
                tension builds →
                  caught = Game Over OR keep going
```

### What Already Works (verified in code)
- `SnowboardPhysics.cs` — slope acceleration, carving, speed capping, ground raycast
- `TerrainGenerator.cs` — chunk streaming, heightmap noise, obstacle spawning by zone
- `ProceduralAssetFactory.cs` — generates pine trees, boulders, ramps with colored materials
- `JumpController.cs` — charge jump, ramp detection, air time tracking
- `TrickController.cs` — spin/grab/flip detection, scoring, combos
- `SasquatchAI.cs` — rubber-band chase, spawn at 5km, catch → EndRun
- `HUDController.cs` — distance/speed display, trick popups, proximity bar
- `CrashHandler.cs` — ragdoll trigger, recovery timer, invincibility frames
- `GameManager.cs` — state machine, distance tracking, run stats

### What Needs To Exist (assets/prefabs)
These are Unity Editor tasks, not code tasks:

1. **Player Prefab** — A GameObject with: `CharacterController`, `PlayerInput`, `SnowboardPhysics`, `PlayerController`, `JumpController`, `CrashHandler`, `TrickController`, `FirstPersonCamera` (on child camera), a simple capsule or box mesh for the body, a child object for the snowboard visual. Assign to `PrefabRegistry.PlayerPrefab`.

2. **Sasquatch Prefab** — A GameObject with: `SasquatchAI`, `NavMeshAgent` (optional — has direct-movement fallback), a larger capsule or procedural mesh, eye glow child objects (optional). Assign to `PrefabRegistry.SasquatchPrefab`.

3. **Obstacle Prefabs** — `PrefabRegistry` needs arrays for trees, rocks, and ramps. Can use `ProceduralAssetFactory` to create these at startup and register them, OR create simple prefabs in the editor with primitive meshes + colliders.

4. **Ground Layer** — The `SnowboardPhysics._groundMask` LayerMask must include whatever layer the terrain chunks use. Terrain chunks need colliders (they add `MeshCollider` at `TerrainGenerator.cs:185`).

5. **Scene Wiring** — `SceneInitializer` needs Inspector references: `_prefabRegistry`, `_terrainGenerator`, `_hudController`. `GameStarter` needs `_autoStart = true`.

### Tuning Pass (After It Runs)
Once the loop is running, these `Constants.cs` values will need iteration:

| Constant | Current | What To Watch |
|----------|---------|---------------|
| `Speed.Cruise` | 50 km/h | Does cruising feel too slow/fast? |
| `Speed.TuckMax` | 120 km/h | Is max speed exciting or nauseating in first-person? |
| `Carving.EdgeCatchAngle` | 45° | Does edge catch happen too easily? |
| `Jump.BaseHeight` | 2m | Are jumps satisfying? |
| `Sasquatch.SpawnDistance` | 5 km | Too early? Too late? |
| `Sasquatch.TargetDistance` | 400m | Does the chase feel tense? |
| `Terrain.ChunkSize` | 1024m | Chunk load hitching? Try smaller chunks. |

---

## PHASE 5: FIRST PLAYTEST (Day 5-7)

### Playtest Checklist

- [ ] Can you snowboard downhill for 60 seconds without crashing (the engine, not the player)?
- [ ] Do terrain chunks load seamlessly ahead of you?
- [ ] Do terrain chunks unload behind you (check memory)?
- [ ] Are trees and rocks visible and collidable?
- [ ] Does hitting a tree trigger crash/ragdoll?
- [ ] Does jumping off a ramp give airtime?
- [ ] Do spins/grabs register and score?
- [ ] Does the HUD show distance and speed?
- [ ] Does the Sasquatch spawn at ~5km?
- [ ] Does the Sasquatch chase feel tense?
- [ ] Does getting caught end the run?
- [ ] Does the Game Over screen show stats?
- [ ] Can you restart?
- [ ] Does it run at 60fps in a WebGL build?

### Expected Bugs (Prepare For These)
- **Terrain seams** — chunks may not align perfectly at boundaries. Height interpolation at edges needed.
- **Player falls through terrain** — `CharacterController.Move()` can clip through thin meshes. May need to increase mesh collider thickness or use continuous collision.
- **Sasquatch stuck on terrain** — NavMesh won't exist on procedural terrain. The direct-movement fallback (`SasquatchAI.cs:190-192`) will be the actual path. It ignores obstacles — the Sasquatch will walk through trees. This is fine for v1 (the original SkiFree yeti also ignored obstacles).
- **Camera clips into ground** — First-person on slopes with aggressive lean angles. May need camera collision avoidance.
- **Trick scoring feels wrong** — spin thresholds (`TrickController.cs:262-268`) use `rotation - 45°` tolerance. May be too generous or too strict.

---

## PHASE 6: ITERATE (Week 2+)

Once the loop runs, priorities shift from infrastructure to feel.

### Priority 1: Make Movement Feel Good
- Tune `SnowboardPhysics` constants through play
- Add screen shake on landing (`GameFeedback.cs` exists, wire it)
- Adjust FOV scaling curve in `FirstPersonCamera.cs`
- Consider adding slight momentum/drift to carving

### Priority 2: Make The Mountain Interesting
- Improve obstacle placement in `TerrainGenerator.SpawnObstacles()`
- Add terrain variety (steeper sections, flat meadows, cliff drops)
- Add coin lines that guide player toward interesting paths
- Add rail segments along the path

### Priority 3: Make The Chase Scary
- Tune Sasquatch rubber-band constants
- Add audio cue when Sasquatch spawns (roar)
- Make proximity bar pulse more aggressively
- Add visual effect when Sasquatch is close (screen vignette, heartbeat)

### Priority 4: Ship WebGL Build
- Create itch.io page with actual gameplay screenshots
- Get build size under 50MB for fast loading
- Add loading screen
- Test in Chrome, Firefox, Safari

---

## WHAT NOT TO TOUCH

These systems are done and correct. Do not modify them during refocus:

| System | Why Leave It |
|--------|-------------|
| `Constants.cs` | Clean, well-organized. Only change values during tuning, not structure. |
| `TrickController.cs` scoring math | Complex but correct. Test through play, don't refactor. |
| `GameState.cs` enums and data classes | Stable foundation. |
| Assembly definitions (10 `.asmdef` files) | Dependency boundaries are correct. |
| `.editorconfig` | Style enforcement is fine. |
| CI pipeline (`.github/workflows/ci.yml`) | Works. Doesn't build Unity (can't in CI without license), but lint/validation is useful. |
| Custom shaders (all 10) | Can't evaluate without art. Leave them for later. |

---

## SUMMARY: THE CRITICAL PATH

```
Day 1:   Fix SceneInitializer bugs (3 lines of code)
         Delete Challenger/, StorePages/, KEYWORDS.md
         Gut LeaderboardManager stubs

Day 2:   Consolidate bootstrap (simplify GameStarter)
         Remove SafeExecution from Update loops

Day 3:   Create Player prefab (capsule + components)
         Create Sasquatch prefab (capsule + SasquatchAI)
         Wire PrefabRegistry in Inspector

Day 4:   Create obstacle prefabs (or wire ProceduralAssetFactory)
         Set up Ground layer + terrain colliders
         Wire SceneInitializer Inspector references

Day 5:   First playtest — press Play, snowboard for 60 seconds
         Fix whatever breaks

Day 6-7: Tune Constants.cs values through play
         Fix terrain/physics/camera issues found in playtest

Week 2:  WebGL build test
         Iterate on feel
```

**The game is closer to playable than it looks.** The systems work — they just aren't connected. Three lines fix the blocker, a day of cleanup removes the noise, and two days of Unity Editor work (prefabs, wiring) gets you to a playable build. The hard part — physics, terrain generation, trick scoring, AI chase — is already written.
