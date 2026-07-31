# DESIGN BRIEF — Djurspel ARPG: 2D Top-Down Conversion

## GOAL
Konvertera Djurspel från en isometrisk 3D demo till en 2D top-down ARPG i Diablo/Path of Exile-stil.

## ACCEPTANCE
- Programmet bygger och körs i headless Xvfb
- Screenshot visar 2D top-down scene med:
  - 2D-spelare som kan röra sig (WASD/arrows)
  - Minst 2 fiender med wander/attack-beteende
  - Loot som dropar från fiender
  - Dungeon-miljö (2D tiles: golv + väggar)

## EXISTING CODEBASE (REUSE-FIRST — DO NOT REPLACE)
Alla dessa finns redan och ska **extendas**, inte skrivas om:

1. **Djurspel.Core** — EventDispatcher, AssetManager, Math2D, MoralAlignment
2. **Djurspel.Entities** — Entity, EntityRegistry, Component system (Transform, Health, Combat, Movement, AI, Render, Player, Loot, Dialogue, PlayerComponent)
3. **Djurspel.Game** — GameLoop, GameStateMachine, SceneManager
4. **Djurspel.Gameplay** — CombatManager, AIManager, InventoryManager, MoralManager, InputManager
5. **Djurspel.World** — TileMap, TileData, WorldFactory, IWorld interfaces
6. **Djurspel.WorldGen** — RoomDungeonGenerator, WildernessGenerator, SimplexNoise

## WHAT MUST CHANGE
- **IsometricCamera** → **TopDownCamera** (ortografisk vy rakt ner)
- **Renderer** — nuvarande renderer ritar 3D-block och en enkel sprite. Behöver:
  - 2D tile rendering (texturplattor istället för 3D-boxar)
  - 2D sprite rendering med rotation (entities vänds mot rörelseriktning)
  - Sprite batch rendering för prestanda
- **Tile rendering** — nuvarande DrawTileMap ritar 3D-boxar. Byt till 2D-quads.
- **Entity rendering** — nuvarande DrawEntity ritar kub fallback om ingen sprite finns. Byt till 2D top-down sprites.

## WHAT MUST BE ADDED (NEW FILES)
- **TopDownCamera** — ortografisk vy, camera-follow på spelare
- **SpriteBatchRenderer** — batcha 2D sprite draws för prestanda
- **EnemyAI** — wander, chase, attack beteende med enkel pathfinding
- **LootDropSystem** — loot dropar från döda fiender, pickup på proximity
- **UIManager** — health bar, inventory overlay (enkel 2D rendering)
- **InputManager** — WASD/arrows movement, space för attack, E för pickup, I för inventory

## CONSTRAINTS
- C#, .NET 8.0, OpenTK 4, OpenGL 3.3 Core
- Headless Linux (Xvfb + llvmpipe) — inget GUI
- Existing ECS architecture preserved
- No external asset dependencies — colored placeholders OK for Phase 1
- Modular: one concern per file

## DESIGN QUESTIONS TO ANSWER
1. How should the camera follow the player? (smooth, locked to tile?)
2. What's the exact interface between Renderer and World for 2D tiles?
3. How should entity sprite rotation work (face movement direction?)
4. What's the loot drop/pickup flow — events or polling?
5. How should the UI overlay integrate with the renderer?

## INSTRUCTIONS
Produce a FILE-LEVEL design: each module, its single concern, its path, and the exact interface it exposes (function signatures / route shapes — not prose). Name the data that crosses each boundary. State the trade-offs you REJECTED and why. Name what already exists that this extends rather than replaces. Do NOT write code. End with the RESULT block.
