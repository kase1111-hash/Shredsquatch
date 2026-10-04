# Shredsquatch

[![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue.svg)](CHANGELOG.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-2023.2-black.svg)](https://unity.com/)

A 3D first-person snowboarding game and SkiFree spiritual successor—an infinite runner snowboard experience where a relentless Sasquatch pursues you down procedurally generated peaks. This winter sports game combines the nostalgia of the retro ski game with modern 3D graphics and a thrilling yeti chase mechanic. If you're looking for games like SkiFree or a snowboard endless runner, shred the powder, nail tricks, and survive the chase—distance is king.

## Overview

Shredsquatch is an addictive snowboarding infinite runner built in Unity (WebGL/HTML5 playable). You carve down an ever-unfurling, procedurally generated mountain in first-person view, dodging obstacles, chaining tricks, and racking up distance while a hulking Sasquatch emerges from the mist to hunt you.

Unlike the top-down original, this is a full 3D experience with arcade physics, dynamic powder effects, and heart-pounding chase tension. Snowboards replace skis, Sasquatch swaps in for the Yeti (with a nod via hidden Easter eggs), and rubber-banded pursuit ensures you're always on edge. Primary score is distance traveled (in kilometers), with a separate trick score for style points. Top speeds let you pull ahead, but one mistake and those glowing red eyes close in.

**Core Loop:** Accelerate → Steer & trick → Survive → Outrun or get squatched. Never-ending until caught. High-score chases fuel replayability.

**Playtime:** 2-10 minutes per run. Perfect for itch.io and browsers.

## Current Status (v0.1.0-alpha)

**What works:**
- Snowboard physics with slope acceleration, carving, tuck, braking, powder drag
- 14 trick types (6 spins, 4 grabs, 4 flips) with combo multipliers and repetition penalties
- Procedural infinite terrain with seeded generation and chunk streaming
- Terrain features: rollers, halfpipe chutes, cornice drops, and park lines of kickers and boxes
- Sasquatch chase AI with rubber-band distance tracking
- Rail grinding with 7 rail types, 3 park boxes and balance mechanics
- HUD with distance/speed, trick and grind popups, combo counter, Sasquatch proximity bar
- 17 achievements and local leaderboards
- Crash/ragdoll system with recovery and invincibility frames
- Screen shake and controller haptic feedback
- Error recovery system with automatic reset
- 9 custom URP shaders
- Powerups (Golden Board, Nitro, Repellent) and coin collectibles

**What doesn't work yet:**
- Avalanche Mode (selectable in menu but plays identically to Standard — no boulders)
- Storm Mode (selectable in menu but plays identically to Standard — no wind)
- Platform leaderboard integration (Steam/itch.io — currently local-only)
- Object pooling (terrain uses Instantiate/Destroy — planned optimization)
- Moon phase cycling (night mode exists but moon is static)
- Dynamic LOD for terrain chunks
- Cabin A-frame and log ramps (the ramp types exist, but nothing places them in the world)

### Homages to SkiFree

- Distance-based monster spawn (~5km in)
- Classic obstacles (trees, rocks) with modern flair
- "Get me off this mountain!" achievement unlock
- Hidden Yeti skin for Sasquatch (unlock via 10km run)

## Features

- **Procedural Infinite World:** Seamless 256 m terrain chunks (load 512 m, unload 768 m) with rollers, halfpipe chutes, cornice drops and park lines—zero loading screens.
- **3D First-Person Physics:** Arcade-style snowboarding with momentum, carve turns, powder drag, and crash recovery.
- **Dynamic Chase System:** Sasquatch rubber-bands to keep 200-800m behind at top speeds. Faster if you slow/crash; lags if you're flawless.
- **Trick System:** 14 base tricks (6 spins, 4 grabs, 4 flips) with 50+ combinations, multipliers, and a dedicated score/counter.
- **Rail Grinding:** 7 rail types including fallen pines, fences, log piles, metal barriers, cabin ridges, pipes, and chairlift cables, plus Fun, Flat and Down park boxes.
- **Visual/SFX Polish:** 9 custom URP shaders, snow particles, dynamic lighting (dawn-to-dusk cycle), and Sasquatch roars.
- **Collectibles & Powerups:** Golden boards (trick multipliers), speed bursts, and "Yeti Repellent" (temp slow for beast).
- **UI/Meters:** Clean HUD—distance/speed, trick combo/counter/score, Sasquatch proximity bar (green >600m, yellow 300-600m, red <300m, pulsing red <150m).
- **Achievements/Leaderboards:** 17 achievements with local leaderboard tracking. Platform integration (Steam, itch.io) planned.

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

- **Jump Height:** Base 2m from flat ground. Ramps add 1-6.5m depending on ramp type (see table below).
- **Charge Jump:** Hold jump to charge (max 1.5s). Full charge = +50% height/distance.
- **Late Release:** Releasing jump up to 0.12s after leaving the ground still jumps.
- **Airtime Windows:**
  - 0-0.5s: No tricks possible (too short)
  - 0.5-1.5s: Basic tricks (single spin or grab)
  - 1.5-3.0s: Combo tricks (spin + grab)
  - 3.0s+: Full combo potential (multiple spins + grab)
- **Landing:** Must be within 30° of slope angle for clean land. Steeper = stumble. Perpendicular = crash.

**Auto-Launch:** Park kickers and cornice drops pop you at the lip without a button press, as high as an uncharged jump off that ramp type. Hold jump through the lip to add your charge (up to +50%). The pop needs at least 6 m/s (about 22 km/h) and a heading within 30° of the kicker (45° for drops); slower or more sideways riders roll off the lip. Chute walls never auto-launch: press jump on the upper wall for the Half-pipe Lip bonus.

**Ramp Types:**

| Ramp Type | Height Boost | Speed Boost | Where | Notes |
|-----------|--------------|-------------|-------|-------|
| Small Bump | +1m | None | Small park kicker (0-5km) | An ollie off a park box also counts |
| Medium Ramp | +2m | +10 km/h | Medium park kicker (2km+) | Packed snow kickers |
| Large Kicker | +4m | +20 km/h | Large park kicker (5km+) | Biggest air of the park kickers |
| Half-pipe Lip | +3m | Maintains speed | Chute walls, press jump | Found in chutes |
| Cabin A-Frame | +3m | +15 km/h | Planned, not yet in the world | Abandoned cabin roofs, grindable peak |
| Cliff Jump | +6.5m | +25 km/h | Cornice drops (2km+) | Auto-launches off the knuckle |
| Log Ramp | +2m | +5 km/h | Planned, not yet in the world | Fallen trees angled upward |

**Park Kickers:**
- Snow wedges with flared 30° sides, so the only wall is the back face below the lip
- Small: 4m wide, 16° deck, lip about 1.8m above the snow
- Medium: 5m wide, 20° deck, lip about 3m above the snow
- Large: 6m wide, 24° deck, lip about 4.5m above the snow
- An orange stripe marks each lip; the landing zone beyond is kept clear of trees, rocks, rails and coins

**Cabin A-Frames (planned):**
- Spawn in 3km+ zone near ruins
- Approach from downhill side to launch off roof peak
- Can grind the roof ridge (see Rails section)
- Miss the roof = crash through window (full ragdoll)

**Cornice Drops:**
- Snow tables shaped into the slope from 2km: a 72m run-up (100m from 5km), a 10m table, a knuckle at the lip, then a steep landing
- 5m tall (7m from 5km), full height across 24m and shouldering off over 22m either side
- Each 224x320m cell has a 35% chance of holding one (50% from 5km), unless a chute runs close by
- Auto-launch as a Cliff Jump (+6.5m, +25 km/h) when you cross the lip heading within 45° of straight downhill; hold jump through the lip to add your charge
- Orange marker poles stand either side of the lip, and the run-up and landing are kept clear of trees, rocks, rails and coins

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
- Grind + grab: +300 bonus (grab while grinding), once per grind
- Perfect dismount (ollie in last 0.5m): +200 bonus

**Park Boxes:**

Park lines carry three box types. They have solid decks you ride on top of, rather than rails you jump onto:

| Box | Length | Points/sec | Spawn Zone | Notes |
|-----|--------|------------|------------|-------|
| Fun Box | 18m | 400 | 0-5km | Ramp up, 12m flat deck, ramp down |
| Flat Box | 16.5m | 450 | 2km+ | Ramp up onto a 14m flat deck |
| Down Box | 18m | 500 | 2km+ | Ramp up onto a 14m deck that drops 0.8m more than the slope |

- **Entry:** Ride straight on from the lead-in ramp; no jump needed. The slide starts once you are on the deck heading within 50° of the box.
- **Heading Lock:** Within 1.3m of the box's centre line your heading follows the box and you are pulled back to the middle, so A/D only works the balance meter.
- **Balance:** A/D balances as on rails, but at half the rail sensitivity, so boxes are more forgiving.
- **Clear Bonus:** +300 for riding at least 90% of the deck.
- **Ollie Bonus:** +200 for an ollie off the last 20% of the deck. The ollie counts as a Small Bump ramp launch, so air tricks off it score.
- **Grab Bonus:** One +300 grab bonus per grind.
- **Exit:** The slide ends 0.1s after you leave the deck, and slides shorter than 1.5m don't count. Losing your balance forfeits the slide's points.
- Box slides and rail grinds show a HUD popup with the points scored.

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

**Moonlight (Night Phase) — *Planned*:**
Moon phase cycling is not yet implemented. Planned mechanics:
- Full moon provides 40m ambient visibility (bluish tint)
- Moon phases cycle daily: Full → Half → New → Half → Full
- New moon nights = near-total darkness (20m visibility without headlamp)

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

**Infinite Heightmap:** 256x256m chunks: a noise base on a steady 0.15 grade (0.15m drop per metre, about 8.5°). Terrain features are added on top as pure world-space height terms (`TerrainFeatures`), so chunk seams line up exactly. They fade in from 320m, leaving the spawn area untouched:
- **Rollers:** 60m-wavelength whoops across the fall line (±1m, up to ±1.6m from 5km), on about a third of the slope
- **Chutes:** Meandering halfpipe channels in lanes every 288m (55% of lanes, along about half their length), 32m wide and 4.5-6.5m deep, with walls no steeper than 34°
- **Drops:** Cornice tables from 2km (see Cornice Drops above)

**Park Lines:** Kickers and boxes laid down the fall line (`ParkFeatureBuilder`), planned per chunk and deterministic for a given seed and chunk coordinate. Every chunk below the spawn row has 1/2/3 line slots (Tutorial/Forest/Extreme), each filled 70% of the time; the first line straight ahead of the spawn (a small kicker, then a fun box, from about 0.2km) always appears where the terrain allows. Lines steer clear of rollers, chutes and drops. Trees, rocks, rails and coins are kept out of chutes, drops and park landing zones.

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

### Avalanche Mode (15km unlock) — *Planned*
Selectable in menu after unlocking, but currently plays identically to Standard Mode. Planned unique mechanics:
- Boulders spawning every 5-10 seconds after 2km
- Shadow warning before impact
- Boulders destroying trees on contact

### Storm Mode (20km unlock) — *Planned*
Selectable in menu after unlocking, but currently plays identically to Standard Mode. Planned unique mechanics:
- Reduced visibility (50m vs 200m)
- Wind gusts with lateral push
- Amplified audio cues

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
| Avalanche Survivor | Survive 5km in Avalanche Mode *(requires Avalanche Mode implementation)* |
| Storm Chaser | Survive 5km in Storm Mode *(requires Storm Mode implementation)* |
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
│   ├── Tricks/            # TrickController, TrickData, RailGrindController, GrindSurface
│   ├── Terrain/           # TerrainGenerator, TerrainChunk, TerrainFeatures, ParkFeatureBuilder, NoiseGenerator
│   ├── Sasquatch/         # SasquatchAI, SasquatchSkin
│   ├── Powerups/          # PowerupBase, individual powerups, spawner
│   ├── UI/                # HUD, menus, game over, tutorials
│   ├── Audio/             # AudioManager, placeholder generator
│   ├── Progression/       # AchievementManager, LeaderboardManager
│   ├── Configuration/     # PrefabRegistry, audio/visual configs
│   ├── Procedural/        # ProceduralAssetFactory, mesh generation
│   ├── Rendering/         # ShaderManager
│   └── Editor/            # Editor tools, validators, scene wiring
├── Shaders/               # 9 custom URP shaders
├── Prefabs/               # Procedural placeholder prefabs (generated at runtime via ProceduralAssetFactory)
├── Scenes/                # GameScene.unity
├── Audio/                 # Music/ and SFX/ subdirectories
├── Materials/             # 20+ materials
├── Input/                 # Input action mappings
└── Settings/              # Project configuration
```

### Assembly Definitions

| Assembly | Purpose |
|----------|---------|
| Shredsquatch | All runtime code (Core, Player, Tricks, Terrain, Sasquatch, Powerups, UI, Audio namespaces) |
| Shredsquatch.Editor | Editor-only tools and utilities |
| Shredsquatch.Tests.PlayMode | Play mode tests |
| Shredsquatch.Tests.Editor | Editor tests |

Runtime code is intentionally a single assembly: the gameplay systems are
tightly interconnected (e.g. GameFeedback is called from Player, Tricks, and
Powerups while itself referencing Player types), so per-feature assemblies
would create circular references. Namespaces still separate the modules.

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
- Perlin noise-based height generation plus world-space terrain features (`TerrainFeatures`)
- Park lines of kickers and boxes built in code per chunk (`ParkFeatureBuilder`)
- 512 m load distance, 768 m unload distance (circular, with hysteresis)

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

- Chunk-based terrain streaming (load 512 m around the rider, unload beyond 768 m)
- Shader property caching via PropertyToID
- 60 FPS target for WebGL
- Performance monitoring with quality scaling (PerformanceMonitor.cs)

### Export Targets

| Platform | Status |
|----------|--------|
| WebGL | Primary target, 60fps, HTML5/WebGL 2.0 |
| Windows | Standalone build supported |
| macOS | Planned |
| Linux | Planned |
| Steam | Planned (requires platform leaderboard integration) |
| itch.io | Planned (Free/Pay-What-You-Want) |

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

## Credits

- Inspired by SkiFree (Chris Pirih, 1991)
- Art/SFX: CC0 + custom
- Code: Open-source MIT license

---

*Shred far. Squatch hard.*
