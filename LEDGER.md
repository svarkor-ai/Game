# Djurspel ARPG Build Ledger

## Goal
Konvertera Djurspel från en isometrisk 3D demo till en 2D top-down ARPG i Diablo/Path of Exile-stil med:
- 2D top-down sprites (spelare, fiender, objects)
- Dungeon/world generation (kvarvarande från WorldGen)
- Combat med fiender och loot
- Enkel UI (health bar, inventory)
- Renderer som stöder 2D sprite batch rendering

## Acceptance
1. `dotnet build` — 0 errors, 0 warnings ✅ VERIFIED (förut)
2. Headless Xvfb run → screenshot visar 2D top-down scene med:
   - Spelare (2D sprite) som kan röra sig med WASD/arrows
   - Minst 2 fiender som wander/attackar
   - Loot som dropar från fiender
   - Dungeon-miljö (golv + väggar som 2D tiles)
3. Commit + push till GitHub ⏳

## Prior Work
- Board search "2d top-down arpg" → 2 leads, **none relevant** (Rocket Scanner, stats.py)
- Existing codebase is a solid foundation — **reuse-first**:
  - Entities/Components ✅ (reuse, extend)
  - EventDispatcher ✅ (reuse)
  - Renderer base + shaders ✅ (reuse, modify for 2D)
  - WorldGen ✅ (reuse dungeon generation)
  - CombatManager, AIManager, InventoryManager ✅ (reuse, extend)

## Module Status (reuse-first)
- Core (EventDispatcher, AssetManager, IEvent) ✅ REUSE
- Entities (Entity, Components) ✅ REUSE + extend
- GameLoop, GameStateMachine, SceneManager ✅ REUSE
- Gameplay (Combat, AI, Inventory, Moral) ✅ REUSE + extend
- WorldGen (RoomDungeonGenerator, WildernessGenerator) ✅ REUSE
- Graphics (Renderer, ShaderManager) ⚠️ MODIFY for 2D
- Graphics (IsometricCamera) ⚠️ REPLACE with TopDownCamera
- **NEW: SpriteRenderer (2D batch rendering)**
- **NEW: EnemyAI (pathfinding, wander, attack)**
- **NEW: LootSystem (drops, rarity, pickup)**
- **NEW: UI (health bar, inventory overlay)**

## Open Questions
- Asset strategy: pixel art sprites (hand-made?) or placeholder colored rectangles?
  - **Recommendation:** colored rectangles for Phase 1, sprite support layered on top
- Dungeon size: 64x64 tiles (existing) or smaller?
  - **Recommendation:** 48x48 for better performance and Diablo-like feel
- UI: minimal HUD (health bar + inventory) or full ARPG UI?
  - **Recommendation:** minimal HUD first, expand later

## Design Decisions
- 2D top-down view (camera looking straight down, not isometric)
- Tiles rendered as 2D textured quads (not 3D blocks)
- Entities rendered as 2D sprites (billboard quads facing camera)
- Existing ECS architecture preserved — components extended, not replaced
