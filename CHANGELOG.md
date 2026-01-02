# Changelog

All notable changes to Shredsquatch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
