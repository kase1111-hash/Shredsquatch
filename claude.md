# Shredsquatch

A 3D first-person snowboarding infinite runner game built in Unity - a spiritual successor to SkiFree with modern 3D graphics.

## Quick Reference

- **Engine**: Unity 2023.2 LTS with URP
- **Language**: C# 9+
- **Primary Platform**: WebGL/HTML5
- **Version**: 0.1.0-alpha

## Project Structure

```
Assets/
├── Scripts/           # C# source code (~14.4k lines, 65 files)
│   ├── Core/          # GameManager, state, constants, error handling
│   ├── Player/        # PlayerController, physics, input, camera
│   ├── Tricks/        # TrickController, RailGrindController
│   ├── Terrain/       # TerrainGenerator, procedural generation
│   ├── Sasquatch/     # SasquatchAI, chase mechanics
│   ├── Powerups/      # Collectibles, power-ups, spawning
│   ├── UI/            # HUD, menus, game over screens
│   ├── Audio/         # AudioManager, sound handling
│   ├── Progression/   # Achievements, leaderboards, saves
│   ├── Configuration/ # PrefabRegistry, configs
│   ├── Procedural/    # ProceduralAssetFactory, mesh generation
│   ├── Rendering/     # ShaderManager, visual effects
│   └── Editor/        # Editor tools, validators
├── Shaders/           # 10 custom URP shaders
├── Prefabs/           # Game object prefabs
├── Materials/         # Material assets
├── Scenes/            # GameScene.unity (main scene)
├── Input/             # ShredsquatchControls.inputactions
└── Tests/             # PlayMode tests
```

## Building & Testing

### Running in Editor
1. Open Unity 2023.2 LTS
2. Open `Assets/Scenes/GameScene.unity`
3. Press Play

### Building
- **WebGL**: Build Settings → Switch to WebGL → Build
- **Standalone**: Build Settings → PC, Mac & Linux Standalone → Build

### Running Tests
- Open Window → Testing → Test Runner
- Select Play Mode tab
- Run all tests

Key test files:
- `TerrainStreamingTests.cs` - Chunk loading/unloading
- `ErrorRecoveryTests.cs` - Safe execution and recovery
- `SasquatchChaseTests.cs` - Chase AI behavior
- `PlayerTrickScoringTests.cs` - Trick and combo system

### Validation
- Run `Tools → Shredsquatch → Project Setup Validator` in Unity

## Coding Conventions

### Naming (enforced by .editorconfig)
- Private fields: `_camelCase` (underscore prefix)
- Public members/properties: `PascalCase`
- Constants: `PascalCase`
- Interfaces: `IPascalCase`
- Local variables/parameters: `camelCase`

### Style
- 4 spaces indentation (no tabs)
- LF line endings
- Allman brace style (opening brace on new line)
- No unsafe code

## Architecture

### Assembly Definitions
The codebase uses 10 modular assemblies:
- `Shredsquatch.Core` - No dependencies, foundation
- `Shredsquatch.Player` - Depends on Core, Tricks
- `Shredsquatch.Tricks` - Depends on Core
- `Shredsquatch.Terrain` - Depends on Core
- `Shredsquatch.Sasquatch` - Depends on Core, Player
- `Shredsquatch.Powerups` - Depends on Core, Player, Tricks, Sasquatch
- `Shredsquatch.UI` - Depends on Core, Player, Tricks
- `Shredsquatch.Audio` - Depends on Core
- `Shredsquatch.Editor` - Editor-only, depends on all

### Key Systems
| Class | Purpose |
|-------|---------|
| `GameManager` | Central state machine, run statistics |
| `PlayerController` | Orchestrates physics, input, jumping, crashing |
| `SnowboardPhysics` | Gravity-driven descent, carving, momentum |
| `TrickController` | Trick execution, combo tracking |
| `SasquatchAI` | NavMesh pathfinding, rubber-band chase |
| `TerrainGenerator` | Infinite procedural terrain with chunk streaming |
| `RailGrindController` | Rail grinding with balance mechanics |
| `ErrorRecoveryManager` | Safe execution wrappers |
| `ShaderManager` | Runtime shader property management |

### Design Patterns
- **Singleton**: GameManager, ErrorRecoveryManager, AudioManager
- **Observer**: Events (OnStateChanged, OnDistanceChanged, OnGameOver)
- **State Machine**: GameState enum (MainMenu, Playing, Paused, GameOver)
- **Object Pooling**: For powerups and obstacles
- **Component**: MonoBehaviour-based composition

### Error Handling
- Use `ErrorRecoveryManager.SafeExecute()` for critical operations
- Implement `IRecoverable` interface for components that can recover from errors
- Try-catch patterns in critical systems

## Key Configuration

### Constants
All tunable game values are in `Assets/Scripts/Core/Constants.cs`:
- Physics settings (gravity, carving, momentum)
- Trick scoring and combo multipliers
- Terrain generation parameters
- Sasquatch AI behavior

### Input
Control mappings in `Assets/Input/ShredsquatchControls.inputactions`:
- Keyboard: A/D steer, W accelerate, Space jump, Q/E spin
- Xbox controller: Full gamepad support
- Generic gamepad support included

## Performance Guidelines

- Use object pooling for frequently spawned objects
- Cache GetComponent calls in Awake/Start
- Use PropertyToID for shader property access
- Avoid FindObjectOfType in gameplay code
- Target 60 FPS for WebGL builds
- Terrain chunks: load 2km ahead, unload at 2.5km

## Common Tasks

### Adding a New Trick
1. Add trick definition in `TrickController.cs`
2. Update scoring in `Constants.cs`
3. Add input mapping if needed in `.inputactions`

### Creating a New Obstacle
1. Create prefab in `Assets/Prefabs/Obstacles/`
2. Register in `PrefabRegistry.cs`
3. Add spawn logic in `PowerupSpawner.cs`

### Adding a New Shader
1. Create shader in `Assets/Shaders/`
2. Include `ShredsquatchCommon.hlsl` for shared utilities
3. Register in `ShaderManager.cs`

## CI/CD

GitHub Actions workflow in `.github/workflows/ci.yml`:
- EditorConfig compliance checking
- Unity project structure validation
- Code analysis and pattern detection
- Security scanning
