# Shredsquatch

A modern homage to the classic SkiFree, reimagined as a tense, infinite 3D first-person snowboarding descent where a relentless Sasquatch pursues you down procedurally generated peaks. Shred the powder, nail tricks, and survive the chase—distance is king.

## Overview

Shredsquatch is an addictive, endless snowboarding game built in Unity (WebGL/HTML5 playable). You carve down an ever-unfurling, procedurally generated mountain in first-person view, dodging obstacles, chaining tricks, and racking up distance while a hulking Sasquatch emerges from the mist to hunt you.

Unlike the top-down original, this is a full 3D experience with realistic physics, dynamic powder effects, and heart-pounding chase tension. It's not a clone—it's an evolution: snowboards replace skis, Sasquatch swaps in for the Yeti (with a nod via hidden Easter eggs), and rubber-banded pursuit ensures you're always on edge. Primary score is distance traveled (in kilometers), with a separate trick score for style points. Top speeds let you pull ahead, but one mistake and those glowing red eyes close in.

**Core Loop:** Accelerate → Steer & trick → Survive → Outrun or get squatched. Never-ending until caught. High-score chases fuel replayability.

**Playtime:** 2-10 minutes per run. Perfect for itch.io and browsers.

### Homages to SkiFree:

- Distance-based monster spawn (~5km in).
- Classic obstacles (trees, rocks) with modern flair.
- "Get me off this mountain!" achievement unlock.
- Hidden Yeti skin for Sasquatch (unlock via 10km run).

## Features

- **Procedural Infinite World:** Seamless terrain generation using Perlin/Simplex noise for slopes, forests, jumps, and chutes. Chunks load 2km ahead, unload behind—zero loading screens.
- **3D First-Person Physics:** Arcade-style snowboarding with momentum, carve turns, powder drag, and crash recovery.
- **Dynamic Chase System:** Sasquatch rubber-bands to keep 200-800m behind at top speeds. Faster if you slow/crash; lags if you're flawless.
- **Trick System:** 20+ tricks with combos, multipliers, and a dedicated score/counter.
- **Visual/SFX Polish:** Volumetric fog, snow particles, dynamic lighting (dawn-to-dusk cycle), booming Sasquatch roars, and carve squeals.
- **Collectibles & Powerups:** Golden boards (trick multipliers), speed bursts, and "Yeti Repellent" (temp slow for beast).
- **UI/Meters:** Clean HUD—distance/speed, trick combo/counter/score, Sasquatch proximity bar (pulses red when close).
- **Achievements/Leaderboards:** Steam/itch.io integration for global distance/trick highs.
- **Accessibility:** Color-blind modes, control remapping, auto-camera smoothing.

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
| Grab Tricks | D-Pad |
| Spin Left/Right | Left/Right Bumpers (in air) |
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

**Ramp Bonuses:**

| Ramp Type | Height Boost | Speed Boost |
|-----------|--------------|-------------|
| Small Bump | +1m | None |
| Medium Ramp | +2m | +10 km/h |
| Large Kicker | +4m | +20 km/h |
| Half-pipe Lip | +3m | Maintains speed |

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

**Lighting Effects:**
- Shadows affect obstacle visibility (trees harder to see in shadow)
- Sasquatch eyes glow brighter in low light (easier to track)
- Snow sparkle intensity changes with sun angle
- Fog density increases at dawn/dusk transitions

**Night Mode (10km unlock):**
- Headlamp provides 40m cone of bright visibility
- Peripheral vision reduced to 20m
- Sasquatch eyes visible at 100m (glowing red dots)
- Stars and aurora borealis visual effects

### Procedural Terrain Generation

**Infinite Heightmap:** 1024x1024m chunks. Noise layers:
- Base slope (steep downhill bias).
- Forests (clustered trees via Poisson sampling).
- Jumps/Ramps (raised ridges + splats).
- Half-pipes/Chutes (valleys for speed runs).
- Cliffs (rare drops—jump or death).

**Variety Over Distance:**
- 0-2km: Tutorial slopes, sparse trees.
- 2-5km: Dense forests, jumps.
- 5km+: Extreme terrain, powder fields, ruins (ancient ski lifts homage).

**Seeding:** Daily global seed for leaderboards + player-custom seeds.

### Sasquatch Chase

- **Spawn:** 5km mark—roar SFX, fog parts, eyes glow in distance.
- **AI Behavior:**
  - Pathfinds via A* on terrain mesh.
  - Base speed: 90 km/h (foot bounds + unnatural stamina).
- **Rubber-Banding:** Distance target 400m avg.

| Player Dist Behind | Sasq Speed Mod |
|--------------------|----------------|
| >800m | +30% (bursts) |
| 200-800m | Base |
| <200m | -20% (tired) |

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

| Trick | Input | Points | Description |
|-------|-------|--------|-------------|
| Nose Grab | 1 / D-Up | 300 | Grab front of board |
| Indy Grab | 2 / D-Right | 300 | Grab toe edge, between bindings |
| Melon Grab | 3 / D-Left | 300 | Grab heel edge, between bindings |
| Stalefish | 4 / D-Down | 300 | Grab heel edge, behind back foot |

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

**Clean Land Bonus:** +500 points for landing within 15° of slope. Requires completing rotation before touchdown.

**Style Bonuses:**

| Condition | Bonus |
|-----------|-------|
| Spin + Grab same jump | x1.5 to that trick |
| Flip + Spin same jump | x2 to that trick |
| Flip + Spin + Grab | x3 to that trick |
| Late grab (last 0.3s of air) | +100 points |

## Scoring System

- **Primary: Distance** – Real-time km counter. High score = farthest run.
- **Trick Score** – Separate total/this-run counter.

| Trick Type | Base Points | Combo Multiplier |
|------------|-------------|------------------|
| Basic Jump (no trick) | 100 | x1 |
| Single Spin (180-360) | 500-1,500 | x2 (chain 3+) |
| Big Spin (540+) | 3,000-12,000 | x2 (chain 3+) |
| Grab | +300 (+hold bonus) | x1.5 airtime |
| Flip | 2,000-5,000 | x2 |
| Full Combo (Spin + Grab + Clean) | Sum + bonuses | x5 max |

**Total Run Score:** Distance x (1 + Tricks/10000) – Encourages style without sacrificing survival.

**Multipliers:**
- No-Crash Streak: x1.1 per km survived without crashing.
- Speed Avg >100km/h: x1.5 end-of-run bonus.
- Collect 10 Golds: x2 trick points for remainder of run.

## Powerups & Collectibles

Spawn procedurally (rarer post-5km):

- **Golden Board:** x2 tricks (10s).
- **Nitro Tuck:** +50km/h (5s).
- **Repellent Cloud:** Sasq slows 50% (15s).
- **Coins:** 50 trick pts each; magnet on combos.

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
- 5km: Sasq skins (Yeti homage).
- 10km: Night mode (headlamp shreds).
- 20km: Board trails (fire/rainbow).

**Endless Mode Variants (unlock via PB):**

| Mode | Unlock | Description |
|------|--------|-------------|
| **Avalanche** | 15km PB | Boulders roll down from above |
| **Storm** | 20km PB | Blizzard with wind and low visibility |

**Avalanche Mode Details:**
- Boulders spawn every 5-10 seconds after 2km
- Boulder speed: 60-80 km/h (slower than max player speed)
- Boulder size: 2-4m diameter
- Shadow warning appears 1.5 seconds before impact zone
- Direct hit = instant ragdoll + 100m tumble
- Boulders destroy trees on contact (creates clear paths)

**Storm Mode Details:**
- Visibility range: 50m (vs 200m normal)
- Wind gusts every 8-15 seconds
- Wind push: 10-20 km/h lateral force for 2-3 seconds
- Wind direction shown by snow particle angle
- Sasquatch visibility: 30m (harder to see coming)
- Audio cues amplified (roars echo longer)

## Technical Details (For Devs/Ports)

- **Engine:** Unity 2023.2 (URP for performance).
- **Assets:** ProBuilder terrain, custom shader for powder displacement.
- **Physics:** Custom raycast carver (no wheels—slope normal snapping).
- **Audio:** FMOD for adaptive chase music (builds tension).
- **Export:** WebGL (60fps target), Steam.

## Credits

- Inspired by SkiFree (Chris Pirih, 1991).
- Art/SFX: CC0 + custom.
- Code: Open-source MIT license.

---

*Shred far. Squatch hard.* 🏂👹
