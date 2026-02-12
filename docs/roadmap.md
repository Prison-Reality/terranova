# Terranova – Roadmap

> Last updated: February 2026 | Based on GDD v0.8
> Status: Pre-Production

---

## Overview

Terranova is developed in milestone-based increments. Each milestone has a clear goal, defined acceptance criteria, and a "Definition of Done" that someone other than the developer can verify. We move to the next milestone only when the current one is done.

### Key Principle: One Facet First

The GDD describes a Goldberg polyhedron planet with 20 biome types, 29 epochs, and 100+ individual settlers. **We build none of that first.** We build one flat terrain, one biome, one building, and prove it works. Then we add complexity layer by layer.

---

## MS1: Technical Foundation 🔴 NOT STARTED

**Goal:** Prove that voxel terrain rendering and basic interaction work in Unity.

**Why this first:** Everything else (economy, settlers, research) builds on top of terrain. If the voxel system doesn't perform well on iPad-class hardware, the entire project concept needs rethinking. This is the highest-risk technical question.

### Acceptance Criteria

- [ ] Voxel terrain generates and renders (single flat facet, Grassland biome)
- [ ] Minimum 64×64 blocks surface area, terrain height variation (hills)
- [ ] Multiple voxel types visible (grass, stone, dirt, water, sand)
- [ ] Chunk-based rendering (16×16×256 as per GDD spec)
- [ ] RTS camera: pan (WASD/arrows), zoom (scroll), rotate (middle mouse)
- [ ] One building type (Campfire) can be placed on terrain via mouse click
- [ ] Terrain-aware placement: building snaps to surface, rejects invalid positions (water, steep slope)
- [ ] Basic UI: resource counter (Wood: 0, Stone: 0) – static numbers, no gathering yet
- [ ] Stable 60 FPS in Editor on development machine

### Not In Scope

Settlers, economy, research, touch input, audio, save/load, multiple biomes, Goldberg polyhedron.

### Key Technical Decisions Required

- [ ] Unity version: 2022 LTS or Unity 6?
- [ ] Voxel mesh generation approach (Greedy meshing? Marching cubes? Simple per-face?)
- [ ] Art style direction: realistic-voxel or stylized? (GDD open question #5)

### Dependencies

None – this is the starting point.

### Estimated Complexity: L (Large)

Voxel rendering is the core technical risk. Chunk mesh generation, LOD, and memory management need to work before anything else makes sense.

### Definition of Done

A person can open the Unity scene, move the camera around a voxel terrain with hills and a water area, and place a campfire on valid ground. Performance is smooth. It doesn't need to look good.

---

## MS2: Living World 🔴 NOT STARTED

**Goal:** Prove that settlers can exist, move, and interact with the world.

**Why this second:** The "Wuselfaktor" (watching settlers go about their lives) is Design Pillar #1. If settlers moving around and gathering resources doesn't feel satisfying, the game won't work – regardless of how many epochs we add.

### Acceptance Criteria

- [ ] Settlers spawn at Campfire (starting: 5 settlers, as per GDD)
- [ ] Settler AI: idle, walk to target, gather resource, return to storage
- [ ] Pathfinding on voxel terrain (A* or NavMesh adapted for voxels)
- [ ] Two gatherable resources: Wood (from trees) and Stone (from rocks)
- [ ] Trees and rocks as voxel objects on terrain that can be gathered (depleted → respawn)
- [ ] Resource counter in UI updates when settlers deliver resources
- [ ] 3–5 building types from GDD Epoch I.1:
  - Campfire (center, gathering point) – 5 Wood
  - Woodcutter's Hut (produces wood) – 10 Wood, 5 Stone
  - Hunter's Hut (produces food) – 8 Wood
  - Simple Hut (housing for 2 settlers) – 15 Wood, 5 Stone
- [ ] Building construction: costs resources, takes time, settlers do the building work
- [ ] Basic settler needs: Hunger (settlers need food or they slow down/die)
- [ ] Game speed: Pause (0x), Normal (1x), Fast (2x), Very Fast (3x)
- [ ] Settlers are visually distinguishable (even if just colored cubes for now)

### Not In Scope

Research/discoveries, epoch transitions, terraforming, touch input, settler lifecycle (birth/death/aging), knowledge system, multiple biomes.

### Dependencies

- MS1 complete (terrain, camera, building placement)

### Key Technical Decisions Required

- [ ] Settler AI approach: behavior tree, utility AI, or state machine?
- [ ] Pathfinding: Unity NavMesh on voxel terrain, or custom A*?

### Estimated Complexity: XL

Settler AI + pathfinding on voxel terrain + resource gathering + building construction is a lot of interconnected systems. This is likely the longest milestone.

### Definition of Done

A person can watch 5 settlers gather wood and stone, build a hut, and hunt for food – without any direct commands. Settlers that don't eat eventually die. The player can place buildings and adjust game speed. It feels like a tiny living world.

---

## MS3: Discovery 🔴 NOT STARTED

**Goal:** Prove that the probabilistic research system works and feels good.

**Why this third:** The research system is Terranova's unique selling point – and its biggest design risk. The probability distributions need playtesting. Better to test early with a small set of discoveries than to build 29 epochs worth of content on an unproven system.

### Acceptance Criteria

- [ ] Discovery system implemented: biome-driven + activity-driven discoveries
- [ ] 5–8 discoveries from Epoch I.1 (GDD research.md):
  - Flint (biome-driven: mountains/volcanic + stone gathering)
  - Resin & Glue (biome-driven: forest + wood gathering)
  - Friction Fire (activity-driven: lots of wood work + forest biome)
  - Spark Fire (activity-driven: stone work + mountains biome)
  - Lightning Fire (spontaneous event)
  - Improved Stone Tools (activity-driven: lots of stone work experience)
  - Primitive Cord (activity-driven: plant fiber gathering)
  - Animal Traps (activity-driven: hunting experience)
- [ ] Discovery notification UI (event popup: "Your settlers discovered Fire!")
- [ ] Discoveries unlock new capabilities (e.g., Fire → Campfire can cook food → new building "Cooking Fire")
- [ ] Bad luck protection: guaranteed discovery after X activity cycles
- [ ] Discovery probabilities influenced by biome + activity + repetition (as per GDD)
- [ ] Resource dependency tree: new discoveries open new resource branches
- [ ] ScriptableObject-based discovery definitions (easy to add more later)

### Not In Scope

Epoch transitions, cave paintings/knowledge multipliers, multiple biomes, touch input, settler lifecycle beyond basic needs.

### Dependencies

- MS2 complete (settlers + gathering + buildings)

### Estimated Complexity: L

The system itself isn't huge, but **balancing** it is. Plan for multiple tuning iterations.

### Definition of Done

A person plays for 15–20 minutes and discovers Fire through one of the available paths. The discovery feels earned but not frustrating. Different playthroughs produce different discovery orders. The "no tech tree" concept is validated.

---

## MS4: Vertical Slice 🔴 NOT STARTED

**Goal:** One complete, polished slice of the game that proves the concept is fun.

**Why this fourth:** This is the "would I keep playing?" test. Everything before this was proving individual systems. Now they need to work together as a cohesive experience.

### Acceptance Criteria

- [ ] Two biomes playable (e.g., Grassland + Forest or Grassland + Mountains)
- [ ] Different biomes produce different discovery paths (as described in GDD research.md)
- [ ] Epoch I.1 fully complete: all discoveries, all buildings, all resource chains
- [ ] Epoch transition I.1 → I.2 functional (threshold of discoveries triggers transition)
- [ ] Epoch I.2 partially playable (at least: leather working, cold protection, tents)
- [ ] Terraforming basics: Remove (dig) and Transform (earth → path) operations
- [ ] Terrain affects build costs (flat = cheap, slope = expensive, as per GDD terraforming.md)
- [ ] Build cost preview shows terraforming overhead when placing buildings
- [ ] Settler lifecycle: aging (child → adult → old), death by age
- [ ] Knowledge inheritance: parents pass skills to children
- [ ] Touch input: basic camera controls (pan, zoom) on iPad
- [ ] Exploration: settlers can walk beyond visible area, fog of war / sight lines
- [ ] Full Epoch I.1 UI: resource panel, build menu, settler info on tap, epoch progress indicator

### Not In Scope

Goldberg polyhedron, multiplayer, GenAI events, save/load, combat, audio integration (placeholder sounds OK), Eras II–IV.

### Dependencies

- MS3 complete (discovery system working)
- Sound assets from ElevenLabs production available (for integration in MS5)

### Estimated Complexity: XL

Many systems coming together + first iPad build + first epoch transition.

### Definition of Done

Someone who has never seen the project can play on an iPad for 30 minutes, understand what's happening, progress from I.1 to I.2, and say "I want to keep playing." This is the **green light / kill decision** for further development.

---

## MS5: Playable Demo 🔴 NOT STARTED

**Goal:** A polished, shareable build that represents the game's vision.

### Acceptance Criteria

- [ ] 3+ biomes with visual and strategic distinction
- [ ] Epochs I.1 through I.4 fully playable
- [ ] Full touch controls (Gesture Lexicon v0.4 implemented)
- [ ] Audio integration: biome ambience, resource SFX, building SFX, UI sounds
- [ ] Save/Load system
- [ ] Main menu, settings
- [ ] Basic tutorial / onboarding (non-intrusive)
- [ ] Performance: stable 30+ FPS on iPad M4
- [ ] Polished UI following the Layered UI concept (GDD Section 3.2)

### Dependencies

- MS4 complete
- Audio assets produced and QA'd

### Estimated Complexity: XXL

Polish is always more work than expected.

### Definition of Done

Could be published as a free demo on TestFlight or itch.io. A stranger can download it, understand it, enjoy it, and tell you what they think.

---

## Future Milestones (not planned in detail)

| Milestone | Concept |
|-----------|---------|
| MS6: Goldberg Planet | Multi-facet world, Mercator projection at edges, planetary view |
| MS7: Full Era I | All 10 epochs of Early History playable |
| MS8: Era II | Antiquity & Pre-Modern, active research buildings, writing, metallurgy |
| MS9: GenAI Events | Event bus + GenAI service integration + validation |
| MS10+: Eras III–IV | Modern + Speculative Future epochs |

---

## System Dependency Map

```
MS1: Terrain ─────────────────────────────────────────────────►
MS1: Camera ──────────────────────────────────────────────────►
MS1: Building Placement ──┬───────────────────────────────────►
                          │
MS2: Settler AI ──────────┤
MS2: Pathfinding ─────────┤
MS2: Resource Gathering ──┼───────────────────────────────────►
MS2: Building Construction┤
MS2: Basic Needs ─────────┘
                          │
MS3: Discovery System ────┤
MS3: Resource Tree ───────┼───────────────────────────────────►
MS3: Event Notifications ─┘
                          │
MS4: Multiple Biomes ─────┤
MS4: Epoch Transitions ───┤
MS4: Terraforming ────────┼───────────────────────────────────►
MS4: Settler Lifecycle ───┤
MS4: Touch Input ─────────┤
MS4: Exploration/Fog ─────┘
                          │
MS5: Audio ───────────────┤
MS5: Save/Load ───────────┤
MS5: Full UI ─────────────┼──────────────────────────────────►
MS5: Tutorial ────────────┤
MS5: Polish ──────────────┘
```

---

## Principles

1. **Playable over perfect.** Every milestone ends with something you can play.
2. **Vertical before horizontal.** One complete system beats five half-finished ones.
3. **Cut scope, not quality.** If a milestone takes too long, remove features – don't ship broken ones.
4. **The Wuselfaktor test.** After MS2, every build should be enjoyable to just *watch*. If settlers moving around isn't satisfying, fix that before adding more systems.
5. **iPad reality check.** Test on actual iPad hardware at least once per milestone from MS4 onward.
