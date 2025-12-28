# Shredsquatch - Next Steps

## Phase 1: Unity Project Setup
- [ ] Create Unity 2023.2+ project with URP (Universal Render Pipeline)
- [ ] Import scripts from Assets/Scripts into Unity
- [ ] Configure Input System package for new input handling
- [ ] Set up TextMeshPro for UI text
- [ ] Configure project settings (physics, quality, WebGL build)

## Phase 2: Scene & Prefab Setup
- [ ] Create main scene hierarchy (GameManager, Player, Terrain, UI Canvas)
- [ ] Build Player prefab with all required components
- [ ] Create Sasquatch prefab with NavMeshAgent and animator
- [ ] Set up UI prefabs (HUD, GameOver, MainMenu, PauseMenu)
- [ ] Create terrain chunk prefab with MeshFilter/MeshRenderer/MeshCollider

## Phase 3: 3D Assets
- [ ] Model/import snowboarder character (first-person hands/board visible)
- [ ] Model Sasquatch with rig for animations
- [ ] Create tree variants (pine, dead trees, fallen logs)
- [ ] Create rock variants (boulders, outcrops)
- [ ] Model ramp types (snow bumps, kickers, cabin A-frames, cliffs)
- [ ] Model rail types (fallen pine, fence, metal barrier, pipe)
- [ ] Create powerup models (golden board, nitro canister, repellent cloud, coins)
- [ ] Model abandoned chairlift (tower, cable, chairs)

## Phase 4: Materials & Shaders
- [ ] Create snow terrain shader with displacement/tracks
- [ ] Set up powder particle material
- [ ] Create Sasquatch fur shader
- [ ] Build board trail shader (fire, rainbow, lightning variants)
- [ ] Set up fog/volumetric effects
- [ ] Create neon glow materials for night mode chairlifts

## Phase 5: Animations
- [ ] Player animations: idle, tuck, carve left/right, jump, tricks, crash, recovery
- [ ] Sasquatch animations: run cycle, spawn, catch, repelled, tree smash
- [ ] UI animations: trick popup, combo counter, proximity pulse

## Phase 6: Audio Assets
- [ ] Compose/source menu music track
- [ ] Compose/source gameplay music track
- [ ] Compose/source chase music track (tense, building)
- [ ] Record/source SFX: carving, wind, crash, trick complete, coin collect
- [ ] Record/source Sasquatch roars and footsteps
- [ ] Set up FMOD or native Unity audio mixing

## Phase 7: Visual Effects
- [ ] Snow spray particle system (speed-based)
- [ ] Board trail particle system
- [ ] Powder explosion on crash
- [ ] Powerup collect effects
- [ ] Sasquatch breath/snow kick-up
- [ ] Day/night lighting presets
- [ ] Aurora borealis skybox for night mode

## Phase 8: Gameplay Polish
- [ ] Implement camera controller (first-person with smoothing)
- [ ] Add screen shake on crash/near-miss
- [ ] Implement tutorial prompts for first 2km
- [ ] Add haptic feedback for controllers
- [ ] Create accessibility options (colorblind modes, control remapping)

## Phase 9: Game Modes
- [ ] Implement Avalanche mode (boulder spawning, shadows)
- [ ] Implement Storm mode (wind gusts, reduced visibility)
- [ ] Add daily seed system for leaderboards
- [ ] Implement custom seed input

## Phase 10: Persistence & Progression
- [ ] Test save/load of PlayerProgress
- [ ] Implement achievement system
- [ ] Add unlock notifications
- [ ] Create skin/trail selection UI

## Phase 11: Platform & Build
- [ ] Configure WebGL build settings (60fps target, compression)
- [ ] Test browser compatibility
- [ ] Set up Steam build configuration
- [ ] Create itch.io page assets (screenshots, banner)
- [ ] Implement leaderboard integration (Steam/itch.io)

## Phase 12: Testing & QA
- [ ] Playtest trick detection accuracy
- [ ] Balance Sasquatch rubber-band feel
- [ ] Test terrain generation edge cases
- [ ] Performance profiling (maintain 60fps)
- [ ] Memory usage optimization for long runs
- [ ] Browser testing (Chrome, Firefox, Safari, Edge)

## Phase 13: Final Polish
- [ ] Add loading screen with tips
- [ ] Create credits screen
- [ ] Add Easter eggs (Yeti skin unlock, SkiFree references)
- [ ] Final audio mix pass
- [ ] Localization support (if needed)

---

## Quick Start Priority
1. Unity project + import scripts
2. Player prefab + basic terrain
3. Simple placeholder assets
4. Get core loop playable (move, jump, crash)
5. Add Sasquatch chase
6. Iterate on feel before polish
