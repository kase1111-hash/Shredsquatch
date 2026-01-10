# Shredsquatch

[![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue.svg)](CHANGELOG.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-2023.2-black.svg)](https://unity.com/)

A 3D first-person snowboarding game and SkiFree spiritual successor—an infinite runner snowboard experience where a relentless Sasquatch pursues you down procedurally generated peaks. This winter sports game combines the nostalgia of the retro ski game with modern 3D graphics and a thrilling yeti chase mechanic. If you're looking for games like SkiFree or a snowboard endless runner, shred the powder, nail tricks, and survive the chase—distance is king.

## Overview

Shredsquatch is an addictive snowboarding infinite runner built in Unity (WebGL/HTML5 playable)—the 3D infinite winter runner you've been waiting for. You carve down an ever-unfurling, procedurally generated mountain in first-person view, dodging obstacles, chaining tricks, and racking up distance while a hulking Sasquatch emerges from the mist to hunt you.

Unlike the top-down original, this is a full 3D skiing game experience with realistic physics, dynamic powder effects, and heart-pounding chase tension. It's not a clone—it's a SkiFree remake evolved for modern players: snowboards replace skis, Sasquatch swaps in for the Yeti (with a nod via hidden Easter eggs), and rubber-banded pursuit ensures you're always on edge. Primary score is distance traveled (in kilometers), with a separate trick score for style points. Top speeds let you pull ahead, but one mistake and those glowing red eyes close in.

**Core Loop:** Accelerate → Steer & trick → Survive → Outrun or get squatched. Never-ending until caught. High-score chases fuel replayability.

**Playtime:** 2-10 minutes per run. Perfect for itch.io and browsers.

### Homages to SkiFree

- Distance-based monster spawn (~5km in)
- Classic obstacles (trees, rocks) with modern flair
- "Get me off this mountain!" achievement unlock
- Hidden Yeti skin for Sasquatch (unlock via 10km run)

## Features

- **Procedural Infinite World:** Seamless terrain generation using Perlin/Simplex noise for slopes, forests, jumps, and chutes. Chunks load 2km ahead, unload behind—zero loading screens.
- **3D First-Person Physics:** Arcade-style snowboarding with momentum, carve turns, powder drag, and crash recovery.
- **Dynamic Chase System:** Sasquatch rubber-bands to keep 200-800m behind at top speeds. Faster if you slow/crash; lags if you're flawless.
- **Trick System:** 14 base tricks (6 spins, 4 grabs, 4 flips) with 50+ combinations, multipliers, and a dedicated score/counter.
- **Rail Grinding:** 7 rail types including fallen pines, fences, log piles, metal barriers, cabin ridges, pipes, and chairlift cables.
- **Visual/SFX Polish:** 9 custom URP shaders, volumetric fog, snow particles, dynamic lighting (dawn-to-dusk cycle), booming Sasquatch roars, and carve squeals.
- **Collectibles & Powerups:** Golden boards (trick multipliers), speed bursts, and "Yeti Repellent" (temp slow for beast).
- **UI/Meters:** Clean HUD—distance/speed, trick combo/counter/score, Sasquatch proximity bar (green >600m, yellow 300-600m, red <300m, pulsing red <150m).
- **Challenger Mode:** Alternate gameplay with trick-based survival mechanics (pending external assets).
- **Achievements/Leaderboards:** Steam/itch.io integration for global distance/trick highs.

## Controls

### Keyboard/Mouse (Default)

| Action | Key/Input |
|--------|-----------|
| Steer Left/Right | A/D or Mouse Tilt |
| Accelerate (Tuck) | Hold W |
| Brake/Slow | S |
| Jump | Space |
| Look Around | Mouse Free-Look |
| Spin Left/Right | Q/E or Mouse X (in air) |
| Grab Tricks | 1-4 Keys (Nose/Indy/Melon/Stale) |
| Pause/Menu | Esc |

### Xbox 360 Controller (Windows)

| Action | Button |
|--------|--------|
| Steer Left/Right | Left Stick |
| Accelerate (Tuck) | RT (Right Trigger) |
| Brake/Slow | LT (Left Trigger) |
| Jump | A Button |
| Look Around | Right Stick |
| Spin Left/Right | LB/RB (in air) |
| Flip (at ramp) | Left Stick Up/Down + A |
| Grab Tricks | D-Pad (Up/Down/Left/Right) |
| Pause/Menu | Start |

**Windows Setup:** Xbox 360 controllers are natively supported on Windows 10/11. Simply plug in via USB or connect wirelessly with an Xbox 360 Wireless Gaming Receiver. The game auto-detects the controller—no additional drivers needed. For wired controllers, Windows will automatically install the driver. For wireless, ensure the receiver is plugged in and the controller is synced (press the sync button on both receiver and controller).

### Generic Gamepad

| Action | Input |
|--------|-------|
| Steer Left/Right | Left Stick L/R |
| Accelerate (Tuck) | Right Trigger |
| Brake/Slow | Left Trigger |
| Jump | Face Button South (A/Cross) |
| Look Around | Right Stick |
| Spin Left/Right | Left/Right Bumpers (in air) |
| Flip (at ramp) | Left Stick Up/Down + Jump |
| Grab Tricks | D-Pad |
| Pause/Menu | Start/Options |

**Trick Inputs:** In air, combine spins (Q/E or Mouse X) + grabs for multipliers (e.g., 540 Indy = x5).

**Pro Tip:** Tuck + perfect carves = top speed (~120 km/h). Strafe mid-air for style.

## Mechanics

### Snowboarding Physics

- **Gravity-Driven Descent:** Auto-forward on slopes; steeper = faster. Powder slows you (~20% drag).
- **Carving & Momentum:** Lean into turns to build speed (+5 km/h per sustained carve). Over-lean past 45° = edge catch → wipeout.
- **Crashes:** Hit obstacles at >50 km/h = ragdoll spinout. Below 50 km/h = powder spray (minor slow, no fall).

**Speed Curve:**

| State | Speed (km/h) | Notes |
|-------|--------------|-------|
| Cruise | 40-60 | Flat/default |
| Tuck | 80-120 | Hold accelerate; max outrun |
| Carve Boost | +5 per turn | Sustained lean into slope |
| Powder Drag | -20% | Deep snow penalty |
| Wipeout Recovery | 0-30 | Ragdoll → stand-up |

**Carving Details:**
- Lean angle 0-30°: Normal steering, no bonus
- Lean angle 30-45°: Carve zone, +5 km/h per second held
- Lean angle >45°: Edge catch triggers, wipeout begins

### Jump Mechanics

- **Jump Height:** Base 2m from flat ground. Ramps add 1-4m depending on ramp size.
- **Charge Jump:** Hold jump to charge (max 1.5s). Full charge = +50% height/distance.
- **Airtime Windows:**
  - 0-0.5s: No tricks possible (too short)
  - 0.5-1.5s: Basic tricks (single spin or grab)
  - 1.5-3.0s: Combo tricks (spin + grab)
  - 3.0s+: Full combo potential (multiple spins + grab)
- **Landing:** Must be within 30° of slope angle for clean land. Steeper = stumble. Perpendicular = crash.

**Ramp Types:**

| Ramp Type | Height Boost | Speed Boost | Notes |
|-----------|--------------|-------------|-------|
| Small Bump | +1m | None | Natural snow mounds, common |
| Medium Ramp | +2m | +10 km/h | Packed snow kickers |
| Large Kicker | +4m | +20 km/h | Constructed jump, rare |
| Half-pipe Lip | +3m | Maintains speed | Found in chutes |
| Cabin A-Frame | +3m | +15 km/h | Abandoned cabin roofs, grindable peak |
| Cliff Jump | +5-8m | +25 km/h | Natural rock ledges, high risk/reward |
| Log Ramp | +2m | +5 km/h | Fallen trees angled upward |

**Cabin A-Frames:**
- Spawn in 3km+ zone near ruins
- Approach from downhill side to launch off roof peak
- Can grind the roof ridge (see Rails section)
- Miss the roof = crash through window (full ragdoll)

**Cliff Jumps:**
- Natural rock outcrops, 5km+ zone
- Variable height (5-8m) based on cliff size
- Must jump before edge or suffer cliff drop penalty
- Longest airtime potential (3-4 seconds)
- Often have powder landing zones below (+soft landing bonus)

### Rails & Grinds

Grindable surfaces appear throughout the mountain. Approach and jump onto rails to grind for points and speed:

**Rail Types:**

| Rail Type | Length | Points/sec | Spawn Zone | Notes |
|-----------|--------|------------|------------|-------|
| Fallen Pine | 8-15m | 150 | 1km+ | Toppled trees, bark texture |
| Fence Rail | 5-10m | 200 | 2km+ | Wooden fence posts, weathered |
| Log Pile | 3-6m | 250 | 2km+ | Stacked lumber, unstable |
| Metal Barrier | 10-20m | 300 | 4km+ | Highway guardrails, rusted |
| Cabin Ridge | 6-12m | 350 | 3km+ | A-frame roof peaks |
| Pipe Rail | 8-15m | 400 | 5km+ | Industrial pipes, smooth |
| Chairlift Cable | 50-80m | 200-400 | 5km+ | See Chairlift section |

**Fallen Pine Trees:**
- Naturally toppled trees lying across the slope
- Bark provides good grip (no balance wobble)
- Branch stubs act as obstacles—jump over or bail
- Can spawn at angles (diagonal grinds)
- Occasional Y-splits let you choose left or right path

**Fence Rails:**
- Old ski area boundary fences
- Wooden posts every 3m (jump over while grinding)
- Some sections missing—gap jumps required
- Connect multiple fence segments for combo chains

**Log Piles:**
- Stacked cut lumber near abandoned camps
- Short but high-value grinds
- Unstable: wobble increases over time
- Launch bonus when exiting (+1m height)

**Metal Barriers:**
- Highway-style guardrails from mountain roads
- Longest common rail type
- Smooth surface = faster acceleration while grinding
- Rust patches cause micro-stumbles (visual only)

**Grind Mechanics:**

| Action | Effect |
|--------|--------|
| Jump onto rail | Start grind (must be within 0.5m of center) |
| Balance (L/R input) | Keep centered, prevent fall |
| Jump while grinding | Hop over obstacle or gap |
| No input | Gradual wobble, fall after 2s |
| Ollie off end | +200 bonus, extra height |

**Grind Combos:**
- Rail-to-rail transfers (jump from one rail to another): x2 multiplier
- Spin onto rail (180/360 entry): +500 bonus
- Grind + grab: +300 bonus (grab while grinding)
- Perfect dismount (ollie in last 0.5m): +200 bonus

### Crash & Recovery

- **Ragdoll Duration:** 2-4 seconds based on impact speed
- **Tumble Distance:** 50m (at 50 km/h) to 100m (at 100+ km/h)
- **Recovery Time:** 1.5 seconds to stand up after ragdoll stops
- **Invincibility:** 2 seconds of invincibility after standing (flashing visual)
- **Speed After Recovery:** Always resets to 30 km/h regardless of pre-crash speed

**Crash Triggers:**

| Cause | Result |
|-------|--------|
| Tree/Rock (>50 km/h) | Full ragdoll |
| Tree/Rock (<50 km/h) | Powder spray, -20 km/h |
| Edge catch (over-lean) | Tumble forward, half ragdoll time |
| Bad landing (>30° off) | Stumble, -30 km/h |
| Cliff drop (no jump) | Full ragdoll + extra 50m tumble |

### Collision System

- **Player Hitbox:** Capsule, 0.5m radius around rider center
- **Tree Hitbox:** Cylinder, 0.3-0.8m radius (varies by tree size)
- **Rock Hitbox:** Box/sphere, 0.5-2m (varies by rock size)
- **Grazing:** If overlap <0.1m at <50 km/h, triggers powder spray instead of crash
- **Powerup Collection:** 1.5m radius magnetic pull when in combo

### Day/Night Cycle

The mountain transitions through lighting phases as you descend:

| Distance | Time of Day | Visibility | Notes |
|----------|-------------|------------|-------|
| 0-3km | Dawn | 150m | Orange/pink sky, long shadows |
| 3-6km | Midday | 200m (max) | Full brightness, clearest visibility |
| 6-10km | Dusk | 120m | Golden hour, shadows lengthen |
| 10km+ | Night | 80m | Headlamp mode (if unlocked), stars visible |

**Dynamic Lighting:**
- Real-time sun position affects shadow direction and length
- Shadows affect obstacle visibility (trees harder to see in shadow)
- Snow sparkle intensity changes with sun angle (brightest at low angles)
- Tree shadows sweep across the slope as time progresses
- Powder spray catches light differently based on sun position

**Moonlight (Night Phase):**
- Full moon provides 40m ambient visibility (bluish tint)
- Moon phases cycle daily: Full → Half → New → Half → Full
- New moon nights = near-total darkness (20m visibility without headlamp)
- Moonlight reflects off snow creating subtle glow on open slopes
- Sasquatch silhouette visible against moonlit sky at 150m

**Night Mode (10km unlock):**
- Headlamp provides 40m cone of bright visibility
- Peripheral vision reduced to 20m
- Sasquatch eyes visible at 100m (glowing red dots)
- Stars and aurora borealis visual effects

### Fog System

Fog dynamically limits visibility, creating tension and preventing players from planning too far ahead:

**Fog Behavior:**
- Forward visibility: Capped at phase maximum (80-200m depending on time of day)
- Rear visibility: Always limited to 50m (can't see Sasquatch until close)
- Fog density increases at dawn/dusk transitions (+20% thickness)
- Fog rolls in waves—momentary clear patches followed by dense banks

**Fog Density by Terrain:**

| Location | Fog Modifier | Effect |
|----------|--------------|--------|
| Open slopes | Standard | Normal visibility |
| Forest | +30% density | Trees fade into mist |
| Valleys/Chutes | +50% density | Very limited sightlines |
| Peaks/Ridges | -20% density | Clearer views, exposed |

**Gameplay Impact:**
- Cannot see upcoming obstacles beyond fog limit—react, don't plan
- Sasquatch roars echo in fog (audio cue more important than visual)
- Powerups glow through light fog (visible at 1.5x normal range)
- Coin trails shimmer to help navigation in dense fog

### Procedural Terrain Generation

**Infinite Heightmap:** 1024x1024m chunks. Noise layers:
- Base slope (steep downhill bias)
- Forests (clustered trees via Poisson sampling)
- Jumps/Ramps (raised ridges + splats)
- Half-pipes/Chutes (valleys for speed runs)
- Cliffs (rare drops—jump or death)

**Variety Over Distance:**
- 0-2km: Tutorial slopes, sparse trees
- 2-5km: Dense forests, jumps
- 5km+: Extreme terrain, powder fields, abandoned chairlifts

**Seeding:** Daily global seed for leaderboards + player-custom seeds.

### Abandoned Chairlifts

Derelict ski infrastructure appears in the 5km+ zone, serving as both obstacles and trick opportunities:

**Structure:**
- Towers: 15m tall steel pylons, spaced 50-80m apart
- Cable: Steel cable runs between towers at 8m height
- Chairs: 2-person chairs hang from cable every 10m, swaying slightly

**Cable Grinding:**

Jump onto the cable from a ramp or high point to grind:

| Grind Duration | Points | Speed |
|----------------|--------|-------|
| 0-2 seconds | 500 base | Maintains entry speed |
| 2-5 seconds | +200/sec | Gradual acceleration (+5 km/h/sec) |
| 5+ seconds | +400/sec | Max grind speed (80 km/h) |

- **Entry:** Must hit cable within 1m of center; too far off = miss and fall
- **Balance:** Slight left/right input keeps you centered; no input = slow wobble
- **Exit:** Jump off anytime, or auto-dismount at tower (launches you forward)
- **Chair dodge:** Chairs are obstacles while grinding—jump over or bail before impact

**Night Lighting:**

At night (10km+), chairlifts become beacons:

| Element | Lighting |
|---------|----------|
| Safety bars | Neon glow (pink/cyan alternating per chair) |
| Towers | Red warning lights at top (blink every 2s) |
| Cable | Faint reflective shimmer in moonlight |
| Tower base | Floodlight pools (30m radius, warm yellow) |

- Neon chairs visible at 100m through fog
- Tower lights help navigation in darkness
- Grinding the cable at night leaves a neon trail matching your board trail color

### Sasquatch Chase

- **Spawn:** 5km mark—roar SFX, fog parts, eyes glow in distance.
- **AI Behavior:**
  - Pathfinds via A* on terrain mesh (NavMesh-based)
  - Base speed: 90 km/h (foot bounds + unnatural stamina)
- **Rubber-Banding:** Distance target 400m avg.

| Player Lead Distance | Sasq Speed Mod |
|----------------------|----------------|
| >800m ahead | +30% (bursts) |
| 200-800m ahead | Base (90 km/h) |
| <200m ahead | -20% (tired) |

- **Catches you?** Game Over screen with slow-mo squash.
- **Visuals:** 3m tall, furry beast with snowboard? No—raw primal chase (bounds over powder, smashes trees).

## Trick System

### Complete Trick List

**Spins (Q/E or Bumpers):**

| Trick | Rotation | Points | Min Airtime |
|-------|----------|--------|-------------|
| 180 | Half rotation | 500 | 0.5s |
| 360 | Full rotation | 1,500 | 1.0s |
| 540 | 1.5 rotations | 3,000 | 1.5s |
| 720 | 2 rotations | 5,000 | 2.0s |
| 900 | 2.5 rotations | 8,000 | 2.5s |
| 1080 | 3 rotations | 12,000 | 3.0s |

**Grabs (1-4 Keys or D-Pad):**

Grab points are based on timing—waiting longer before initiating the grab scores higher:

| Trick | Input | Description |
|-------|-------|-------------|
| Nose Grab | 1 / D-Up | Grab front of board |
| Indy Grab | 2 / D-Right | Grab toe edge, between bindings |
| Melon Grab | 3 / D-Left | Grab heel edge, between bindings |
| Stalefish | 4 / D-Down | Grab heel edge, behind back foot |

**Grab Timing Points:**

| Grab Start | Points | Difficulty |
|------------|--------|------------|
| 0-0.3s into air | 150 | Easy (panic grab) |
| 0.3-0.6s into air | 300 | Standard |
| 0.6-1.0s into air | 450 | Skilled |
| 1.0s+ into air | 600 | Expert (late grab) |

**Grab Hold Bonus:** +50 points per 0.5s held (max 2s hold = +200 bonus)

**Flips (W+Jump or Up+Jump at ramp):**

| Trick | Points | Requirement |
|-------|--------|-------------|
| Frontflip | 2,000 | Medium ramp or larger |
| Backflip | 2,000 | Medium ramp or larger |
| Double Front | 5,000 | Large kicker only |
| Double Back | 5,000 | Large kicker only |

### Combo System

**Chain Rules:**
- Combos chain when you land a trick and immediately jump again within 1 second
- Combo counter resets on: crash, 1+ second ground time, or bad landing
- Each trick in chain adds to multiplier

**Combo Multipliers:**

| Chain Length | Multiplier |
|--------------|------------|
| 1 trick | x1 |
| 2 tricks | x1.5 |
| 3 tricks | x2 |
| 4 tricks | x2.5 |
| 5+ tricks | x3 (max) |

**Repetition Penalty:**

Repeating the same trick consecutively degrades its point value—variety is rewarded:

| Same Trick in a Row | Point Modifier |
|---------------------|----------------|
| 1st-2nd time | 100% (full points) |
| 3rd time | 50% points |
| 4th time | 25% points |
| 5th+ time | 10% points (minimum) |

*Note: The counter resets when you perform a different trick. Spins of different degrees count as different tricks (e.g., 360 → 360 → 540 resets the penalty on the 540).*

**Clean Land Bonus:** +500 points for landing within 15° of slope. Requires completing rotation before touchdown.

**Style Bonuses:**

| Condition | Bonus |
|-----------|-------|
| Spin + Grab same jump | x1.5 to that trick |
| Flip + Spin same jump | x2 to that trick |
| Flip + Spin + Grab | x3 to that trick |
| Grab held until landing (release in last 0.3s) | +100 points |

## Scoring System

- **Primary: Distance** – Real-time km counter. High score = farthest run.
- **Trick Score** – Separate total/this-run counter.

| Trick Type | Base Points | Combo Multiplier |
|------------|-------------|------------------|
| Basic Jump (no trick) | 100 | x1 |
| Single Spin (180-360) | 500-1,500 | x2 (chain 3+) |
| Big Spin (540+) | 3,000-12,000 | x2 (chain 3+) |
| Grab (timing-based) | 150-600 (+hold bonus) | x1.5 airtime |
| Flip | 2,000-5,000 | x2 |
| Full Combo (Spin + Grab + Clean) | Sum + bonuses | x5 max |

**Total Run Score:** Distance x (1 + Tricks/10000) – Encourages style without sacrificing survival.

**Multipliers:**
- No-Crash Streak: x1.1 per km survived without crashing.
- Speed Avg >100km/h: x1.5 end-of-run bonus.
- Collect 10 Golds: x2 trick points for remainder of run.

## Powerups & Collectibles

**Spawn Rates:**

| Distance | Powerup Chance | Coin Density |
|----------|----------------|--------------|
| 0-2km | Every 200m | 10 per 100m |
| 2-5km | Every 300m | 8 per 100m |
| 5km+ | Every 500m | 5 per 100m |

- **Golden Board:** x2 trick points (10s duration).
- **Nitro Tuck:** +50 km/h instant boost (5s duration).
- **Repellent Cloud:** Sasquatch slows 50% (15s duration). Does not stack.
- **Coins:** 50 trick pts each. During active combo, coins within 5m are magnetically pulled toward you.

## Game Modes

### Standard Mode
The default endless run experience. Survive as long as possible, rack up distance, and escape the Sasquatch.

### Avalanche Mode (15km unlock)
- Boulders spawn every 5-10 seconds after 2km
- Boulder speed: 60-80 km/h (slower than max player speed)
- Boulder size: 2-4m diameter
- Shadow warning appears 1.5 seconds before impact zone
- Direct hit = instant ragdoll + 100m tumble
- Boulders destroy trees on contact (creates clear paths)

### Storm Mode (20km unlock)
- Visibility range: 50m (vs 200m normal)
- Wind gusts every 8-15 seconds
- Wind push: 10-20 km/h lateral force for 2-3 seconds
- Wind direction shown by snow particle angle
- Sasquatch visibility: 30m (harder to see coming)
- Audio cues amplified (roars echo longer)

### Challenger Mode (Hidden)
An alternate gameplay mode featuring creature mounts and trick-based survival mechanics:

**Gameplay:**
- Ride a creature mount instead of a snowboard
- No Sasquatch chase—purely skill-based survival
- Energy constantly drains over time
- Perform tricks to refill energy
- 10-second grace period at start
- Game ends when energy depletes ("poof")

**Mount Types:**

| Mount Style | Turn Speed | Max Speed | Jump | Description |
|-------------|------------|-----------|------|-------------|
| Waddle | 80% | 90% | 110% | Penguin-style movement |
| Slide | 60% | 110% | 90% | Belly slide |
| Hop | 100% | 80% | 130% | Bouncy movement |
| Glide | 70% | 100% | 100% | Smooth glide |

**Energy System:**
- Starting energy: 100
- Drain rate: 5 per second
- Energy per trick point: 0.1
- Minimum trick score for energy: 100 points
- Combo bonuses: +2 energy per combo count

*Note: This mode requires external asset approval and is placeholder-enabled by configuration.*

## Game Over & Progression

**Defeat:** Sasq touches you → Explosive squash anim, scores screen.

**Stats Shown:**
```
Distance: 12.4 km [New PB!]
Tricks: 247 (Score: 1,245,600)
Max Speed: 118 km/h
Max Combo: 7
```

**Unlocks (local/savefile):**

| Distance PB | Unlock |
|-------------|--------|
| 5km | Sasquatch Skins: Classic Yeti (white fur), Abominable (ice blue) |
| 10km | Night Mode: Headlamp visibility, starfield sky |
| 15km | Board Trail: Fire (orange/red particle trail) + Avalanche Mode |
| 20km | Board Trail: Rainbow (multicolor cycling trail) + Storm Mode |
| 25km | Board Trail: Lightning (electric blue with sparks) |
| 30km | Golden Sasquatch skin + all trails unlocked |

## Achievements

| Achievement | Description |
|-------------|-------------|
| First Run | Complete your first run |
| 5K Club | Reach 5 kilometers |
| 10K Legend | Reach 10 kilometers |
| 20K Master | Reach 20 kilometers |
| 30K Immortal | Reach 30 kilometers |
| Trick Novice | Land 100 tricks |
| Trick Master | Land 1,000 tricks |
| Combo King | Achieve a 10-trick combo |
| Rail Rider | Grind for 100 total meters |
| Night Owl | Survive past 10km in Night Mode |
| Avalanche Survivor | Survive 5km in Avalanche Mode |
| Storm Chaser | Survive 5km in Storm Mode |
| Speed Demon | Reach 120 km/h |
| Close Call | Escape when proximity bar is pulsing red |
| Old Friend | Unlock the Classic Yeti skin |
| Golden Legend | Unlock the Golden Sasquatch |

---

## Technical Documentation

### Project Architecture

Shredsquatch uses a modular architecture with assembly definitions for clean dependency management:

```
Assets/
├── Scripts/
│   ├── Core/              # GameManager, GameState, Constants, error handling
│   ├── Player/            # PlayerController, physics, input, camera, crash handling
│   ├── Tricks/            # TrickController, TrickData, RailGrindController
│   ├── Terrain/           # TerrainGenerator, TerrainChunk, NoiseGenerator
│   ├── Sasquatch/         # SasquatchAI, SasquatchSkin
│   ├── Powerups/          # PowerupBase, individual powerups, spawner
│   ├── UI/                # HUD, menus, game over, tutorials
│   ├── Audio/             # AudioManager, placeholder generator
│   ├── Progression/       # AchievementManager, LeaderboardManager
│   ├── Challenger/        # ChallengerMode, CreatureMount, TrickSurvival
│   ├── Configuration/     # PrefabRegistry, audio/visual configs
│   ├── Procedural/        # ProceduralAssetFactory, mesh generation
│   ├── Rendering/         # ShaderManager
│   └── Editor/            # Editor tools, validators, scene wiring
├── Shaders/               # 9 custom URP shaders
├── Prefabs/               # 25+ reusable game objects
├── Scenes/                # GameScene.unity
├── Audio/                 # Music/ and SFX/ subdirectories
├── Materials/             # 20+ materials
├── Input/                 # Input action mappings
└── Settings/              # Project configuration
```

### Assembly Definitions

| Assembly | Purpose |
|----------|---------|
| Shredsquatch.Core | Core systems, state management, utilities |
| Shredsquatch.Player | Player controller and movement systems |
| Shredsquatch.Tricks | Trick system and rail grinding |
| Shredsquatch.Terrain | Procedural terrain generation |
| Shredsquatch.Sasquatch | Enemy AI and skins |
| Shredsquatch.Powerups | Collectibles and power-up effects |
| Shredsquatch.UI | All user interface components |
| Shredsquatch.Audio | Audio management and generation |
| Shredsquatch.Editor | Editor-only tools and utilities |
| Shredsquatch | Main assembly (references all others) |

### Custom Shaders

9 custom URP shaders for enhanced visual effects:

| Shader | Description |
|--------|-------------|
| SnowSparkle | Sparkle effect on snow, intensity varies with sun angle |
| SnowTracks | Dynamic snow displacement for board trails |
| SasquatchFur | Fur rendering with eye glow and frost effects |
| CoinGlow | Animated glow effect for collectibles |
| PowerupGlow | Pulsing glow for powerup items |
| TrailFire | Fire particle trail effect |
| TrailRainbow | Rainbow cycling color trail |
| TrailLightning | Electric blue lightning trail with sparks |
| AuroraBorealis | Northern lights sky effect for night mode |

**Shader Features:**
- Dynamic snow sparkle based on time of day
- Sasquatch eye glow intensity controlled by distance/state
- Frost accumulation during blizzard conditions
- Aurora visibility tied to night phase

### Key Systems

**GameManager (`Core/GameManager.cs`)**
- Central game controller and state machine
- Handles game states: MainMenu, Playing, Paused, GameOver
- Manages run statistics and progression
- Singleton pattern for global access

**PlayerController (`Player/PlayerController.cs`)**
- Main player orchestrator
- Coordinates SnowboardPhysics, JumpController, CrashHandler
- Works with PlayerInput for control handling

**TrickController (`Tricks/TrickController.cs`)**
- Trick execution and combo tracking
- Processes spin/grab/flip inputs
- Manages repetition penalties and style bonuses

**SasquatchAI (`Sasquatch/SasquatchAI.cs`)**
- Chase AI with rubber-band mechanics
- NavMesh-based pathfinding
- Dynamic speed adjustment based on player distance

**TerrainGenerator (`Terrain/TerrainGenerator.cs`)**
- Infinite procedural terrain with chunking system
- Perlin noise-based height generation
- 2km load distance, 2.5km unload distance

**ShaderManager (`Rendering/ShaderManager.cs`)**
- Runtime shader property management
- Time-of-day visual updates
- Weather effect control (blizzard, frost)

### Engine & Dependencies

- **Engine:** Unity 2023.2
- **Render Pipeline:** Universal Render Pipeline (URP) 16.0.4
- **Input System:** Unity InputSystem 1.7.0
- **AI Navigation:** com.unity.ai.navigation 1.1.5
- **UI Text:** TextMeshPro 3.0.6
- **Animation:** Timeline 1.8.5
- **Language:** C# 9+ with namespace organization

### Physics Implementation

- Custom raycast-based carving (no Rigidbody wheels)
- CharacterController + slope normal snapping
- Terrain-following via raycast sampling
- Ragdoll physics for crashes using configurable joints

### Performance Optimizations

- Dynamic LOD for terrain chunks
- Object pooling for powerups and obstacles
- Chunk-based terrain streaming
- Shader property caching via PropertyToID
- 60 FPS target for WebGL

### Export Targets

| Platform | Notes |
|----------|-------|
| WebGL | Primary target, 60fps, HTML5/WebGL 2.0 |
| Windows | Standalone build |
| macOS | Planned |
| Linux | Planned |
| Steam | With achievements and leaderboards |
| itch.io | Free/Pay-What-You-Want |

### Input Configuration

Uses Unity's new Input System with `ShredsquatchControls.inputactions`:
- Gameplay control scheme
- Menu control scheme
- Supports keyboard/mouse, Xbox, PlayStation, and generic gamepads
- Configurable bindings

### Save System

- JSON-based progression save (local files)
- Tracks best distance, trick scores, unlocks
- Skin and trail unlock persistence
- Achievement state tracking

---

## Connected Repositories

Shredsquatch is part of a broader ecosystem of projects. Explore related repositories:

### NatLangChain Ecosystem
- [NatLangChain](https://github.com/kase1111-hash/NatLangChain) - Prose-first, intent-native blockchain protocol for natural language smart contracts
- [IntentLog](https://github.com/kase1111-hash/IntentLog) - Git for human reasoning, tracking "why" changes happen via prose commits
- [RRA-Module](https://github.com/kase1111-hash/RRA-Module) - Revenant Repo Agent for abandoned repository monetization
- [mediator-node](https://github.com/kase1111-hash/mediator-node) - LLM mediation layer for matching and negotiation
- [ILR-module](https://github.com/kase1111-hash/ILR-module) - IP & Licensing Reconciliation for dispute resolution
- [Finite-Intent-Executor](https://github.com/kase1111-hash/Finite-Intent-Executor) - Posthumous execution of predefined intent via smart contracts

### Agent-OS Ecosystem
- [Agent-OS](https://github.com/kase1111-hash/Agent-OS) - Natural-language native operating system for AI agents
- [synth-mind](https://github.com/kase1111-hash/synth-mind) - NLOS-based agent with psychological modules for emergent continuity
- [boundary-daemon-](https://github.com/kase1111-hash/boundary-daemon-) - Trust enforcement layer for Agent OS cognition boundaries
- [memory-vault](https://github.com/kase1111-hash/memory-vault) - Secure, owner-sovereign storage for cognitive artifacts
- [value-ledger](https://github.com/kase1111-hash/value-ledger) - Economic accounting layer for cognitive work
- [learning-contracts](https://github.com/kase1111-hash/learning-contracts) - Safety protocols for AI learning and data management

### Security & Games
- [Boundary-SIEM](https://github.com/kase1111-hash/Boundary-SIEM) - Security Information and Event Management for AI systems
- [Midnight-pulse](https://github.com/kase1111-hash/Midnight-pulse) - Procedurally generated night driving game
- [Long-Home](https://github.com/kase1111-hash/Long-Home) - Atmospheric indie narrative game in Godot

---

## Credits

- Inspired by SkiFree (Chris Pirih, 1991)
- Art/SFX: CC0 + custom
- Code: Open-source MIT license

---

*Shred far. Squatch hard.*
