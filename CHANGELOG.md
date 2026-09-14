# Changelog

All notable changes to Shredsquatch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Project compiles again in the Editor: `ProjectSetupValidator` caught an
  unqualified `Exception` with no `using System;`, and the PlayMode tests had
  an ambiguous `Object` reference plus `SafeExecution.Try` calls that resolved
  to the `Func<T>` overload. Added `com.unity.test-framework` to the manifest,
  which the test asmdefs already referenced
- Runs no longer start in the void: `PlayerController` reset the rider to the
  world origin (below the start plane and inside the terrain). Runs now start
  from a spawn point that `SceneInitializer` snaps onto the generated terrain,
  and restarts regenerate the terrain around it first
- Terrain chunks now line up: noise offsets were in metres but sampled by
  vertex index, so every chunk border was a cliff. The mesh row index also ran
  toward -Z while the slope bias and obstacle placement assumed +Z, leaving
  trees and rails floating or buried. The mountain now descends continuously
  (`_slopeBias` is the drop per metre) instead of flattening to zero after
  ~2.5 km
- `TerrainGenerator` no longer throws when `SceneInitializer` generates the
  first chunks before its own `Start` has run
- Jumping works: the ground check re-grounded the rider on the frame after a
  jump and wiped the vertical velocity, so no airtime, landing, or trick score
  ever happened
- Crashes work: `CrashHandler` only listened for `OnCollisionEnter`, which a
  CharacterController never sends. Obstacle hits now come through
  `OnControllerColliderHit`, stop the board, and lock movement during ragdoll
- Speed constants (km/h) were compared directly against metres-per-second
  speeds, giving a 180 km/h cruise cap and a 108 km/h post-crash speed
- Pausing could never be undone because pause input was ignored while paused
- Steering did nothing to the first-person view: the camera holder was given
  a world-space rotation that cancelled the rider's yaw
- Rail prefabs were tagged `Rail_Fence`/`Rail_Metal`/`Rail_Pipe`/
  `Rail_FallenPine`, none of which exist; grinding only checks for `Rail`
- Nitro had no effect on the scene player (`PowerupManager` had no physics
  reference fallback); coins and powerups are now placed on the terrain
  surface instead of at a fixed world height
- Sasquatch follows the terrain surface and no longer carries a NavMeshAgent
  that logged errors on every enable (no NavMesh exists on procedural terrain)

### Added

- Minimal in-game overlay for Game Over ("press R"), Paused, and the reset
  state error recovery returns to; the scene has no menu or game-over canvas

- Project now imports cleanly: added the 66 missing `.meta` files (scene,
  shaders, scripts, asmdefs, materials, WebGL plugin, folders) so GUID
  references no longer break on first import
- Added `EditorBuildSettings.asset` (GameScene was missing from the build
  list), `ProjectVersion.txt`, and renamed `Physics.asset` to Unity's
  expected `DynamicsManager.asset`
- Consolidated runtime code into a single `Shredsquatch` assembly; the
  previous per-feature asmdefs had circular type dependencies
  (Player<->Tricks, GameFeedback<->Player/Powerups, UI->Progression) and
  could not compile
- Repaired `guid: 0` references: Aurora/Trail materials now point at their
  shaders, PrefabRegistry.asset at its script, powerup/coin prefabs at
  their pickup scripts, and obstacle/rail/ramp/chairlift renderers at real
  materials (previously magenta)
- Populated PrefabRegistry.asset with the existing prefabs (player,
  sasquatch, terrain chunk, trees, rocks, ramps, rails, collectibles)
- Wired the game to actually boot: SceneInitializer, AutoSetup,
  PowerupSpawner, and AudioManager are now in GameScene; SceneInitializer
  adopts the scene-placed player instead of requiring a prefab spawn
- HUD text now renders: switched HUD to built-in UI Text (no TextMeshPro
  font assets exist in the project); AudioManager self-configures with
  generated placeholder audio; scene uses the procedural skybox
- Removed the orphaned `Prefabs/UI.meta` and the dead duplicate
  `Assets/InputSystem` input actions asset

## [0.1.0-alpha] - 2025-01-02

### Added

#### Core Systems
- **GameManager** - Central game state machine with MainMenu, Playing, Paused, and GameOver states
- **Error Recovery System** - Safe execution wrappers and automatic recovery from runtime errors
- **Performance Monitor** - Runtime performance tracking and optimization
- **Constants System** - Centralized tunable values for all game parameters

#### Player Systems
- **PlayerController** - Main player orchestrator coordinating all player subsystems
- **SnowboardPhysics** - Gravity-driven descent with carving, momentum, and powder drag
- **JumpController** - Charge-jump mechanics with ramp height bonuses
- **CrashHandler** - Ragdoll physics with recovery and invincibility frames
- **FirstPersonCamera** - Dynamic FOV and immersive first-person view

#### Trick System
- **TrickController** - 14 base tricks (6 spins, 4 grabs, 4 flips)
- **Combo System** - Chain multipliers up to x3 with 1-second chain window
- **Repetition Penalty** - Diminishing returns for repeated tricks
- **RailGrindController** - 7 rail types with balance mechanics
- **Style Bonuses** - Spin+grab, flip+spin, and triple combinations

#### Terrain Generation
- **TerrainGenerator** - Infinite procedural terrain using Perlin noise
- **TerrainChunk** - 1024x1024m chunk streaming with 2km load distance
- **NoiseGenerator** - Layered noise for slopes, forests, jumps, and chutes
- **Distance-Based Variety** - Tutorial (0-2km), forests (2-5km), extreme (5km+)

#### Sasquatch AI
- **SasquatchAI** - NavMesh-based pathfinding with rubber-band chase mechanics
- **Dynamic Speed Adjustment** - Speeds up when far, slows when close
- **Spawn at 5km** - Dramatic entrance with roar and fog parting
- **Sasquatch Skins** - Classic Yeti and Abominable skins unlockable

#### Powerups & Collectibles
- **Golden Board** - x2 trick points for 10 seconds
- **Nitro Tuck** - +50 km/h instant boost for 5 seconds
- **Repellent Cloud** - Sasquatch slows 50% for 15 seconds
- **Coins** - 50 trick points each with magnetic pull during combos
- **PowerupSpawner** - Distance-based spawn rates

#### UI System
- **HUDController** - Distance, speed, trick combo, and proximity display
- **MainMenuController** - Game mode selection and settings
- **GameOverScreen** - Run stats, personal bests, and unlock notifications
- **TutorialController** - First-run tutorial guidance
- **Proximity Bar** - Color-coded Sasquatch distance (green/yellow/red)

#### Audio System
- **AudioManager** - Centralized audio playback and mixing
- **AudioPlaceholderGenerator** - Procedural audio for development
- **Positional SFX** - Carve squeals, wind, and Sasquatch roars

#### Visual Effects
- 9 Custom URP Shaders:
  - **SnowSparkle** - Dynamic sparkle based on sun angle
  - **SnowTracks** - Board trail displacement
  - **SasquatchFur** - Fur rendering with eye glow
  - **CoinGlow** - Animated collectible effects
  - **PowerupGlow** - Pulsing powerup visuals
  - **TrailFire** - Fire particle trail (15km unlock)
  - **TrailRainbow** - Rainbow cycling trail (20km unlock)
  - **TrailLightning** - Electric blue lightning trail (25km unlock)
  - **AuroraBorealis** - Northern lights for night mode

#### Game Modes
- **Standard Mode** - Endless run with Sasquatch chase
- **Avalanche Mode** - Rolling boulder hazards (15km unlock)
- **Storm Mode** - Blizzard conditions with 50m visibility (20km unlock)
- **Challenger Mode** - Alternate trick-survival gameplay (infrastructure ready)

#### Progression System
- **AchievementManager** - 16 Steam-ready achievements
- **LeaderboardManager** - Distance and trick score tracking
- **Unlock System** - Distance-based skin and trail unlocks
- **Save System** - JSON-based local progression

#### Editor Tools
- **ProjectSetupValidator** - Validates project configuration
- **SceneWiringUtility** - Automates prefab connections
- **ShaderSetupUtility** - Shader property configuration

### Technical Details

- **Engine**: Unity 2023.2
- **Render Pipeline**: Universal Render Pipeline (URP) 16.0.4
- **Input System**: Unity Input System 1.7.0
- **AI Navigation**: com.unity.ai.navigation 1.1.5
- **Code**: ~14,369 lines of C# across 10 assembly definitions
- **Target Platforms**: WebGL (primary), Windows, macOS, Linux

### Known Limitations

- Challenger Mode requires external asset approval for full activation
- macOS and Linux builds are planned but not yet tested
- Some audio assets are placeholder-generated

---

*Shred far. Squatch hard.*
