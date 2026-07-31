# PLAN BRIEF — Djurspel ARPG

## MÅL
Konvertera Djurspel till 2D top-down ARPG i Diablo/Path of Exile-stil.

## ACCEPTANS
- `dotnet build` — 0 errors ✅ (redan gjort)
- Headless Xvfb → screenshot visar 2D top-down scene med:
  - Spelare (2D) som kan röra sig (WASD)
  - 2 fiender med wander/attack-beteende
  - Loot som dropar från fiender
  - Dungeon-miljö (2D tiles: golv + väggar)

## BEFINTLIG KOD (Ska REANVÄNDAS)
- Djurspel.Core: EventDispatcher, AssetManager, Math2D
- Djurspel.Entities: Entity, EntityRegistry, Components (Transform, Health, Combat, Movement, AI, Render, Player, Loot, Dialogue, PlayerComponent)
- Djurspel.Game: GameLoop, GameStateMachine, SceneManager
- Djurspel.Gameplay: CombatManager, AIManager, InventoryManager, MoralManager, InputManager
- Djurspel.World: TileMap, TileData, WorldFactory, IWorld
- Djurspel.WorldGen: RoomDungeonGenerator, WildernessGenerator, SimplexNoise
- Djurspel.Graphics: Renderer (20k rader), ShaderManager, IsometricCamera

## VAD SOM MÅSTE ÄNDRAS
1. **IsometricCamera → TopDownCamera** (ortografisk vy rakt ner)
2. **Renderer.DrawTileMap** (3D-boxar → 2D-quads)
3. **Renderer.DrawEntity** (3D-cube → 2D-sprite med rotation)
4. **Nytt: SpriteBatchRenderer** för batcha 2D-sprites

## VAD SOM MÅSTE LÄGGAS TILL (Nya filer)
1. **TopDownCamera** — ortografisk vy, camera-follow på spelare
2. **SpriteBatchRenderer** — batcha 2D sprite draws för prestanda
3. **EnemyAI** — wander, chase, attack beteende med enkel pathfinding
4. **LootDropSystem** — loot dropar från döda fiender, pickup på proximity
5. **UIManager** — health bar, inventory overlay
6. **InputManager** — WASD/arrows movement, space för attack, E för pickup, I för inventory

## BETEENDENDE
- Spelaren rörlig med WASD/arrows
- Fiender wanderar runt, attackerar när de ser spelaren
- Loot dropar när fiender dör, plockas upp med E
- 2D top-down vy med camera som följer spelaren

## BEGRÄNSNINGAR
- C#, .NET 8.0, OpenTK 4, OpenGL 3.3 Core
- Headless Linux (Xvfb + llvmpipe) — inget GUI
- ECS-arkitektur bevaras
- Inga externa assets — färgade rektanglar OK för Phase 1
- Modulär: en funktion per fil

## INSTRUktioner
Ge en FILER-NIVÅ plan: varje modul, dess väg, och exakta gränssnitt. Ange vad som redan finns som vi kan använda. Skriv INTE kod. Avsluta med RESULTAT-block.
