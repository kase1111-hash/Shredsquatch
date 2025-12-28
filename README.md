# Shredsquatch

*Hero Image*

A modern homage to the classic SkiFree, reimagined as a tense, infinite 3D first-person snowboarding descent where a relentless Sasquatch pursues you down procedurally generated peaks. Shred the powder, nail tricks, and survive the chase—distance is king.

## Overview

Shredsquatch is an addictive, endless snowboarding game built in Unity (WebGL/HTML5 playable). You carve down an ever-unfurling, procedurally generated mountain in first-person view, dodging obstacles, chaining tricks, and racking up distance while a hulking Sasquatch emerges from the mist to hunt you.

Unlike the top-down original, this is a full 3D experience with realistic physics, dynamic powder effects, and heart-pounding chase tension. It's not a clone—it's an evolution: snowboards replace skis, Sasquatch swaps in for the Yeti (with a nod via hidden Easter eggs), and rubber-banded pursuit ensures you're always on edge. Primary score is distance traveled (in kilometers), with a separate trick score for style points. Top speeds let you pull ahead, but one mistake and those glowing red eyes close in.

**Core Loop:** Accelerate → Steer & trick → Survive → Outrun or get squatched. Never-ending until caught. High-score chases fuel replayability.

**Playtime:** 2-10 minutes per run. Perfect for itch.io, browsers, or mobile (with gyro controls).

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
| Pause/Menu | Start/Options |

### Mobile Touch

| Action | Gesture |
|--------|---------|
| Steer Left/Right | Left Swipe/Drag |
| Accelerate (Tuck) | Right Lower Swipe Down |
| Brake/Slow | Right Upper Swipe Up |
| Jump | Tap Screen Center |
| Look Around | Gyro / Right Drag |
| Grab Tricks | On-Screen Buttons |
| Pause/Menu | Top-Right Icon |

**Trick Inputs:** In air, combine spins (Q/E or Mouse X) + grabs for multipliers (e.g., 540 Indy = x5).

**Pro Tip:** Tuck + perfect carves = top speed (~120 km/h). Strafe mid-air for style.

## Mechanics

### Snowboarding Physics

- **Gravity-Driven Descent:** Auto-forward on slopes; steeper = faster. Powder slows you (~20% drag).
- **Carving & Momentum:** Lean builds speed on turns; over-lean = edge catch → wipeout (tumble 50-100m, lose speed but recoverable).
- **Crashes:** Hit trees/rocks at speed = ragdoll spinout. Low-speed bumps just powder spray.

**Speed Curve:**

| State | Speed (km/h) | Notes |
|-------|--------------|-------|
| Cruise | 40-60 | Flat/default |
| Tuck | 80-120 | Hold duck; max outrun |
| Wipeout Recovery | 0-30 | Ragdoll → stand-up |

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
| <200m | -20% (tires) |

- **Catches you?** Game Over screen with slow-mo squash.
- **Visuals:** 3m tall, furry beast with snowboard? No—raw primal chase (bounds over powder, smashes trees).

## Scoring System

- **Primary: Distance** – Real-time km counter. High score = farthest run.
- **Trick Score** – Separate total/this-run counter.

| Trick Type | Base Points | Combo Multiplier |
|------------|-------------|------------------|
| Basic Jump | 100 | x1 |
| 180/360 Spin | 500/1500 | x2 (chain 3+) |
| Grab (Indy etc.) | +300 | x1.5 airtime |
| Full Combo (Spin + Grab + Land Clean) | 5000+ | x5 max |

**Total Run Score:** Distance x (1 + Tricks/10000) – Encourages style without sacrificing survival.

**Multipliers:**
- No-Crash Streak: x1.1 per km.
- Speed Avg >100km/h: x1.5 end bonus.
- Collect 10 Golds: x2 tricks.

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
- Avalanche: Falling rocks.
- Storm: Low vis, wind push.

## Technical Details (For Devs/Ports)

- **Engine:** Unity 2023.2 (URP for performance).
- **Assets:** ProBuilder terrain, custom shader for powder displacement.
- **Physics:** Custom raycast carver (no wheels—slope normal snapping).
- **Audio:** FMOD for adaptive chase music (builds tension).
- **Export:** WebGL (60fps target), Android/iOS (gyro), Steam.

## Installation & Play

1. Download from itch.io or GitHub Releases.
2. Run `index.html` in browser (Chrome/Firefox best).
3. No install—play instantly!

## Credits

- Inspired by SkiFree (Chris Pirih, 1991).
- Art/SFX: CC0 + custom.
- Code: Open-source MIT license.

---

*Shred far. Squatch hard.* 🏂👹
