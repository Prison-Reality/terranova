# Terranova – Project Briefing for AI Developer Agent

> This file is the primary context for Claude Code working on Terranova.
> **Read this file completely at the start of every session.**
> Last updated: 2026-02-23 | Current version: v0.5.8 | Milestone: MS4

---

## What is Terranova?

Terranova is a **real-time strategy/economy simulation** for tablets (iPad, M4+) where the player guides a civilization through 29 epochs – from stone tool culture to a speculative post-biological future – on a procedurally generated voxel planet.

**Elevator pitch:** Empire Earth's epoch system + Minecraft's voxel terrain + Anno/Settlers' economic chains + RimWorld's emergent storytelling.

### Design Pillars (in priority order)

1. **Building Fascination ("Wuselfaktor")** – Individual settlers autonomously work, trade, and live. The player watches, optimizes, and guides indirectly. The joy of seeing a civilization grow from a campfire to a metropolis.
2. **Strategic Depth Through Terrain** – The voxel world is not backdrop but strategy. Where you build, which biome you settle, how you shape terrain – all have real consequences on build costs and research direction.
3. **Epoch Progression** – Advancing through epochs must feel like a civilizational leap. New possibilities, new aesthetics, new strategies. Even the way research works evolves over time.

### What Terranova is NOT

- Not a combat-focused RTS (combat is emergent from self-defense/hunting, no dedicated system)
- Not a city builder (individual settlers, not abstract population)
- Not turn-based (real-time with pause)
- Not multiplayer (singleplayer first, MP as future expansion)

---

## ⚠️ Build & Deployment Pipeline

**READ THIS FIRST. This is the most important section for avoiding build errors.**

| Step | Who | Tool |
|------|-----|------|
| Code changes | Claude Code | Git push to `claude/*` branch (e.g. `claude/v0.5.3-fixes`) |
| Merge to main | Producer | `git checkout main && git pull && git merge claude/[branch] && git push` |
| Build iOS project | Producer | Unity → File → Build Settings → iOS |
| Deploy to iPad | Producer | Xcode → Run on device |

### CRITICAL constraints for ALL code:

- **The game runs on iPad, NOT in Unity Editor.** `UNITY_EDITOR` is NOT defined in the target build.
- **`Resources.Load()` only works for assets inside a `Resources/` folder.** The Explorer Stoneage asset pack is NOT in a Resources folder.
- **All asset references MUST be serialized** via ScriptableObject with direct prefab references, or via Addressables. Never rely on `Resources.Load()` for third-party assets.
- **Never use editor-only APIs.** Code inside `#if UNITY_EDITOR` blocks will NOT run on the device.
- **Render Pipeline is URP** (Universal Render Pipeline). Use URP-compatible shaders only. Use `Shader.Find("Universal Render Pipeline/Lit")` and set `"_BaseColor"` (not `"_Color"`).
- **Apple Developer Team ID:** L9NZX2W46H
- **Primary test device:** iPad (M4 processor)
- **Touch input:** All UI elements must have minimum 44×44px touch targets.

### Git Workflow

- `main` is protected – Claude Code cannot push directly to main (403)
- Always push to a `claude/*` branch (e.g. `claude/v0.5.3-terrain-fix`)
- Producer merges locally: `git checkout main && git pull && git merge claude/[branch] && git push`
- After push, always state the version number (e.g. "Bump to v0.5.3")

---

## Technical Stack

| Aspect | Decision |
|--------|----------|
| Engine | Unity 6 |
| Language | C# |
| Render Pipeline | URP (Universal Render Pipeline) |
| Primary Platform | iPad (M4 processor or higher) |
| Future Platforms | Meta Quest 3 (MR multiplayer – far future) |
| Input | Touch primary, Apple Pencil optional |
| Networking | Singleplayer only (for now) |

---

## Asset Pack: EXPLORER – Stone Age

The project uses a purchased asset pack from Unity Asset Store: **"EXPLORER – Stone Age"** (522 prefabs).

**Location:** `Assets/EXPLORER - Stone Age/`

### Loading Assets

**DO NOT use `Resources.Load()` for these assets.** They are not in a Resources folder. Instead:

1. Use a **`PrefabDatabase` ScriptableObject** that holds direct references to all needed prefabs
2. The PrefabDatabase is assigned in the scene/inspector by the producer
3. At runtime, access prefabs through `PrefabDatabase.Instance.GetPrefab("Tree_Pine_01")`
4. Unity serializes these references into the build automatically

### Asset Categories (522 prefabs total)

| Folder | Count | Content |
|--------|-------|---------|
| Particles/ | 12 | Fire, butterflies, fog, fireflies, sun shafts |
| Prefabs/Avatars/ | 10 | Male/female characters (V1-V4), dead bodies |
| Prefabs/Buildings/ | 47 | Huts, tents, camps, towers, workshops |
| Prefabs/Pottery/ | 25 | Clay cups, dishes, vases, food bowls |
| Prefabs/Props/ | 139 | Campfire, fences, bones, tools, carts, bridges |
| Prefabs/Rocks/ | 121 | Cliffs, caves, boulders, rock formations, canyons |
| Prefabs/Tools/ | 26 | Axes, clubs, spears, shields, workbench |
| Prefabs/Vegetation/Agricultural/ | 13 | Farming plants (later epochs) |
| Prefabs/Vegetation/Plants/ | 55 | Bushes, ferns, flowers, mushrooms |
| Prefabs/Vegetation/Trees/ | 70 | Pine trees, deciduous trees, logs, stumps |

### Asset Usage Rules

- **GATHERABLE resources** (disappear when collected): Tree_Log, Twigs, Rock_Small, Mushroom, Bone, Animal_Carcass
- **DECORATION** (permanent, not interactive): Tree, Pine_Tree, Rock_Large, Rock_Medium, Cliff, Bush, Fern, Flower, Tree_Trunk
- **SHELTERS** (permanent, tappable, show info panel): Cave_Entrance, Canyon_Overpass, Rock_Cluster
- **NOT in Epoch I.1**: Furnace, Agricultural_Plant, Palisade, Stone_Mill, Cart, Wheel, Pier, Boat, Tower_1B+, Flag_Pole, Spike_Trap

See `docs/asset-mapping-explorer-stoneage.md` for full mapping of prefabs to game elements per biome.

---

## Current State of Development

### Completed Milestones

| Milestone | Status | Content |
|-----------|--------|---------|
| MS1: Technical Foundation | ✅ Done | Voxel terrain, camera, building placement |
| MS2: Living World | ✅ Done | Settler AI, resource gathering, buildings |
| MS3: Discovery | ✅ Done | Research system, discoveries, Epoch I.1 core |

### Current Milestone: MS4 – Vertical Slice (v0.5.x)

**Goal:** A visually appealing, playable version of Epoch I.1 with professional assets, order system, and terrain variety.

**Current version:** v0.5.8

#### Implemented Features (MS4)

| Feature | Status | Notes |
|---------|--------|-------|
| F1: Terrain with 3 biomes (Forest, Mountains, Coast) | ✅ Working | v0.5.8: Real terrain textures from EXPLORER pack |
| F2: Extended material system (28 materials) | ✅ Working | |
| F3: Tool system with quality Q1-Q5 | ✅ Working | |
| F4: Food system, hunger/thirst mechanics | ✅ Working | Settlers drink reliably |
| F5: Shelters (campfire as night shelter) | ✅ Working | Natural shelters (caves etc.) planned for terrain refactoring |
| F6: Settler traits, names, XP system | ✅ Working | 5 traits, 26 names, XP categories |
| F7: Order Grammar & Klappbuch UI | ⚡ In Progress | 3-column picker works, orders execute, bugs parked |
| F8: Extended Discovery System | ⏳ Planned | Phases A-D, failure-driven discovery |
| F9: Wildlife & Events | ⏳ Planned | Animals, random events |
| F10: Seasons | ✅ Done (v0.5.7) | Sun arc 52°N, seasonal day length, ground/tree/bush tinting, snow/leaves particles, gameplay modifiers |
| F11: Organic Terraforming | ⏳ Planned | Part of terrain refactoring |
| F12: Tribal Chronicle | ⏳ Planned | Narrative system |

#### Active Work: Visual Overhaul with Asset Pack

The game is transitioning from procedurally generated primitive shapes to professional prefabs from the Explorer Stoneage pack. The PrefabDatabase ScriptableObject approach is being used to load assets correctly in iOS builds.

#### Parked Bugs (fix in a later Polish sprint)

See `docs/ms4-bug-backlog.md` for the full list. Key items:
- Klappbuch UI: cancel button too small, some layout issues
- "Sheltered" status shown during daytime
- Settler info panel shows internal state names instead of order text
- Settlers restless at campfire at night
- Tribe death → new settlers not verified

### Future Milestones

| Milestone | Content |
|-----------|---------|
| MS4 (continued) | Complete visual overhaul, F8-F12 features |
| Terrain Refactoring | Fog of War, trampled paths, natural shelters, water visuals, terrain deformation, tribe death persistence |
| MS5: Demo | Polish, full UI, sound, tutorial, save/load |

---

## Core Systems Overview

### 1. World Geometry: Goldberg Polyhedron

The planet is a **Goldberg polyhedron** – a body of pentagons and hexagons approximating a sphere. Each facet is internally flat and contains a chunk-based voxel terrain.

> **For current milestone:** Single flat facet. Polyhedron integration is a later milestone.

### 2. Voxel System

- Block size: 1×1×1 meter
- Chunk size: 16×16×256 blocks
- View distance: 12 chunks each direction (192m)
- Terrain height: 0–256 blocks (sea level at block 64)

### 3. Biomes (3 implemented, 20 planned)

Currently implemented: **Forest, Mountains, Coast** (selectable in main menu).

Full biome list (future): Grassland, Forest, Desert, Tundra, Mountains, Ocean, Coast, Rainforest, Steppe, Volcanic, Savanna, Swamp/Moor, Taiga, Mangroves, High Plateau, River Valley, Coral Reef, Glacier, Karst, Fjord.

### 4. Epochs (29 in 4 Eras)

| Era | Epochs | Theme |
|-----|--------|-------|
| I: Early History | 10 (I.1–I.10) | Stone tools → Weaving. No active research – discoveries are emergent. |
| II: Antiquity & Pre-Modern | 8 (II.1–II.8) | Wheel → Printing press. Active research from II.3. |
| III: Industry & Modern | 8 (III.1–III.8) | Steam → AI/Robotics. Systematic science. |
| IV: Speculative Future | 3 (IV.1–IV.3) | AGI → Post-Biological. Not yet designed. |

**Currently implementing:** Epoch I.1 (Deep Stone Age)

### 5. Research & Discoveries

**There is NO tech tree.** Discoveries happen through observation, imitation, trial-and-error.

- **Activity-driven:** Settlers working with materials triggers discoveries
- **Biome-driven:** Environment determines what can be discovered
- **Discovery UI:** Modal overlay pauses game, shows discoverer name + reason + unlocks
- **"Bad luck protection":** Guaranteed discovery after X activity cycles

### 6. Order Grammar (Klappbuch UI)

Player gives orders via a 3-column picker ("Klappbuch"):
- **Column 1 – WHO:** All, Next Free, or specific settler by name
- **Column 2 – DOES:** Predicate (Gather, Explore, Avoid, + locked: Build, Hunt, Cook, Smoke, Craft, Fell, Dig)
- **Column 3 – WHAT/WHERE:** Context-dependent objects and locations

Result line shows the assembled sentence: "Kael gathers Berries at the stream"

See `docs/order-grammar-behaviors.md` for full predicate behavior definitions.

### 7. Population System

- 5 settlers per game start
- Individual names (26-name pool), traits (Curious/Cautious/Skilled/Robust/Enduring), XP categories
- Needs: thirst (priority 1), hunger, warmth/shelter at night
- Needs always override player orders
- Settlers carry 1-3 items, deliver to campfire stockpile

### 8. Day/Night Cycle

- Smooth sun arc (360° directional light rotation)
- Night: settlers walk to campfire, stay sheltered
- Dawn: settlers resume tasks
- Speed controls: Pause / 1x / 3x / 20x

### 9. Resources & Economy

Resources form a **dependency tree**. Root = gatherable resources (stones, sticks, berries, herbs, water – biome-dependent). Everything else requires combinations of: Knowledge (discovery), Tools, Buildings, Specialized workers, Other resources.

Goods are physically transported by individual settlers. Paths, distances, and storage capacities are strategically relevant.

### 10. Touch Controls

- **Camera:** 1-finger drag (pan), 2-finger pinch (zoom)
- **Selection:** Tap object → info panel
- **Orders:** Long-tap on ground → Klappbuch with "Here" pre-filled. Tap settler → info panel → "Give Order". "Orders" button → Klappbuch empty.
- **All interactive UI elements: minimum 44×44px touch target**

---

## Architecture Principles

### Code Quality & Maintenance

- **Simplicity over cleverness.** The producer is a C# beginner. Code must be readable, well-commented, and easy to understand.
- **One class, one responsibility.** Keep scripts focused and small (<200 lines ideally).
- **Use comments generously.** Explain the "why", not just the "what". Write all comments and code in English.
- **No premature optimization.** Make it work → make it right → make it fast.
- **Document decisions.** When choosing between approaches, add a comment explaining why.

### Regular Code Hygiene (Every 3-5 Commits)

After completing a feature or bugfix round, run a code review pass:
1. **Remove unused variables and imports** – no dead code
2. **Add missing comments** – every public method needs a summary comment
3. **Check for missing null checks** – especially on prefab loads and GetComponent calls
4. **Verify naming conventions** – PascalCase for methods/properties, camelCase for locals, _camelCase for private fields
5. **Remove debug logs** – clean up temporary Debug.Log statements (keep only permanent error logs)
6. **Check for TODO/FIXME** – resolve or document why they remain

The producer will periodically request: `"Check the complete codebase for unused variables, missing comments, missing references and other flaws and refactor it."` This is a standard maintenance task, not a criticism.

### Unity-Specific

- **Use ScriptableObjects for data definitions** (BiomeDefinition, EpochDefinition, ResourceDefinition, BuildingDefinition, DiscoveryDefinition, PrefabDatabase)
- **Prefer composition over inheritance.** Use Unity's component system as intended.
- **Use namespaces.** All code under `Terranova.*`:
  - `Terranova.Core` – Game loop, epoch manager, event bus, save/load
  - `Terranova.World` – Goldberg polyhedron, facet management
  - `Terranova.Terrain` – Terrain generation, biome features, decoration
  - `Terranova.Economy` – Resources, production chains, storage, transport
  - `Terranova.Buildings` – Building placement, construction
  - `Terranova.Population` – Settler AI, needs, lifecycle, knowledge
  - `Terranova.Research` – Discovery system, probabilities, epoch transitions
  - `Terranova.Terraforming` – Remove/add/transform operations
  - `Terranova.Camera` – RTS camera, zoom levels, touch input
  - `Terranova.Input` – Gesture state machine, touch handling
  - `Terranova.UI` – HUD, panels, Klappbuch, resource display
  - `Terranova.Audio` – Sound management (not yet implemented)
  - `Terranova.Events` – Event bus, discoveries, weather
- **Follow Unity naming conventions:**
  - PascalCase for classes, methods, properties
  - camelCase for local variables and parameters
  - _camelCase for private fields
  - UPPER_SNAKE_CASE for constants

### Project Structure

```
Assets/
├── EXPLORER - Stone Age/        ← Purchased asset pack (DO NOT MODIFY files inside)
│   ├── Materials/
│   ├── Meshes/
│   ├── Particles/
│   ├── Prefabs/
│   │   ├── Avatars/
│   │   ├── Buildings/
│   │   ├── Pottery/
│   │   ├── Props/
│   │   ├── Rocks/
│   │   ├── Tools/
│   │   └── Vegetation/
│   └── Textures/
├── Terranova/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Terrain/
│   │   ├── Economy/
│   │   ├── Buildings/
│   │   ├── Population/
│   │   ├── Research/
│   │   ├── Camera/
│   │   ├── Input/
│   │   ├── UI/
│   │   └── Events/
│   ├── Data/                    ← ScriptableObjects (PrefabDatabase, BiomeDefinitions, etc.)
│   ├── Prefabs/                 ← Game-specific prefabs (NOT asset pack prefabs)
│   ├── Materials/               ← Game-specific materials
│   └── Scenes/
└── Plugins/
```

### Key Architectural Patterns

- **Event Bus** for system communication (settler discovered something → research system → UI notification → epoch check)
- **ScriptableObject-driven definitions** make balancing possible without code changes
- **PrefabDatabase ScriptableObject** for all asset pack references (serialized, works in iOS builds)
- **Separation: Simulation vs. Presentation.** The simulation should work without rendering.

---

## Development Workflow

### What Claude Code Does vs. What the Human Does

| Claude Code (terminal) | Human (Producer) |
|------------------------|---------------------|
| Write and edit C# scripts | Merge branches, build in Unity, deploy via Xcode |
| Create ScriptableObject code | Configure ScriptableObject instances in Inspector |
| Debug compilation errors | Test on iPad and report behavior + screenshots |
| Write unit tests | Run tests and report results |
| Refactor and clean up code | Decide what to build next |
| List available prefabs/assets | Visually verify asset appearance in Unity |

### What Claude Code Should NOT Try To Do

- **Do not use `Resources.Load()` for Explorer Stoneage assets.** Use PrefabDatabase references.
- **Do not rely on `UNITY_EDITOR` being defined.** The build runs on iOS.
- **Do not reference specific Unity Editor operations** like "click on GameObject → Add Component".
- **Do not assume scene contents.** Ask the human what's in the scene if you need to know.
- **Do not generate Unity meta files.** Unity creates these automatically.
- **Do not modify ProjectSettings/ files directly** unless asked.
- **Do not create procedural geometry** (cubes, spheres, cylinders) for game objects. Use prefabs from the asset pack instead.

---

## How to Work With Me

The human producer is learning C# and Unity. When writing code:

1. **Always explain what you're building** before writing code.
2. **Keep changes focused.** One feature or fix per session.
3. **If you see multiple valid approaches**, present them briefly with trade-offs.
4. **If something is risky or experimental**, flag it clearly.
5. **Write code that teaches.** The producer learns from reading your output.
6. **Reference the design documents.** If a design decision is unclear, ask rather than assume.
7. **Flag scope creep.** If implementing something "right" would take significantly longer, suggest the simpler version.
8. **Always bump the version** in the format v0.X.Y and state it when pushing.
9. **Never break existing functionality.** If your changes touch existing systems, verify they still work.
10. **Test against iOS build mentally.** Ask yourself: "Will this work without UNITY_EDITOR and without Resources.Load for third-party assets?"

---

## Reference Documents

| Document | Content |
|----------|---------|
| `CLAUDE.md` | This file – project briefing (read first every session) |
| `docs/gdd-terranova.md` | Main Game Design Document |
| `docs/deep-epoch-i1-design-v02.md` | Detailed Epoch I.1 design |
| `docs/order-grammar-behaviors.md` | Order predicate behaviors (living document) |
| `docs/asset-mapping-explorer-stoneage.md` | Prefab → game element mapping per biome |
| `docs/ms4-bug-backlog.md` | Parked bugs for later Polish sprint |
| `docs/epochs.md` | All 29 epochs, transition mechanics |
| `docs/biomes.md` | 20 biome types, distribution rules |
| `docs/research.md` | Research system, discoveries per epoch |
| `docs/terranova-gesture-lexicon-v04.md` | Touch control specification |
