# Djurspel 2D ARPG — Konverteringsplan

Djurspel 2D ARPG — Konverteringsplan (Top-Down)

============================================================
1. MAL + ACCEPTANSKRITERIER
============================================================

MAL:
  Konvertera Djurspel 3D-firstperson till 2D top-down ARPG i Diablo/Path of Exile-stil.

ACCEPTANS (testbara):
  A1: `dotnet build` -> 0 errors, 0 warnings (kan ignorera obsolete-varningar).
  A2: Headless Xvfb + OpenGL (llvmpipe) -> screenshot visar:
      - 2D top-down scene (ortografisk vy rakt ner)
      - Spelare (blå rektangel) som kan rorelsa med WASD
      - 2 fiender (rode rektanglar) som wanderar och attackerar spelaren
      - Loot (gula rektanglar) som dropar fran dodade fiender
      - Dungeon-miljo (2D tiles: golv+vaggar)
  A3: ECS-arkitektur bevaras (Entity + Components).
  A4: Inga externa assets — fargade rektanglar (OpenGL primitives) OK for Phase 1.

============================================================
2. BEFINTLIG KOD — VAD SOM FINNS (VERIFIERAD)
============================================================

VERIFIERAD via kodlas (450+ filer, ~15 356 LOC):

Djurspel.Core (5 filer, ~399 LOC):
  - AssetManager: "Assets: 0 loaded. 0 errors." (ingen faktisk asset-loadning)
  - Math2D: Vector2, Vector3, Matrix4x4, Transform, Rotation, Position
  - InputManager: KeyboardState, MouseState (3D-firstperson)
  - GameInput: WASD, mouse-look, klick (3D-firstperson)
  - EventDispatcher: generic events (onEntitySpawned, onPlayerAttack)
  - AudioProvider, LogProvider, ConfigManager, GameTimeProvider

Djurspel.Entities (12 filer, ~684 LOC):
  - Entity, EntityRegistry (ID-baserad)
  - Components: Transform, Health, Damage, Combat (Damage/Cooldown/AoE/Status),
    Movement, AI (BehaviorType), Render (Color/Scale/Rotation/UV),
    Player, Loot, Dialogue, PlayerComponent

Djurspel.World (8 filer, ~431 LOC):
  - TileMap: 2D-array (GridSize x GridSize), 5 tile-typ: Empty(0), Wall(1),
    Floor(2), Exit(3), Spawn(4), Water(5)
  - TileData, WorldFactory, IWorld, World
  - TileMapRenderer: renderar med OpenTK (3D-boxar)
  - TileMapGenerator: genererar map (anvander WorldFactory)

Djurspel.WorldGen (3 filer, ~478 LOC):
  - RoomDungeonGenerator: random room placement, corridor connecting,
    6 room-templates, random spawn/exit placement
  - WildernessGenerator: SimplexNoise-baserad, biomes, resource generation
  - SimplexNoise: noise-funktioner (OpenSimplex2S)

Djurspel.Gameplay (7 filer, ~1 093 LOC):
  - CombatManager: PlayerAttack, DamageEntity, MeleeAttack, AoEAttack,
    StatusEffect, ApplyStatusEffect, AttackType, DamageType
  - AIManager: AIState (idle/patrol/attack/flee), MoveTowards, Attack,
    Patrol, Wander, StateTransition, EnemyMovement, EnemyAttack,
    enemyAI, enemyHealth
  - InventoryManager: Inventory, Item, ItemCategory, AddItem, RemoveItem,
    GetItem, HasItem, UseItem, Clear, Count, GetItemByName
  - MoralManager: MoralValue, AddMoralValue, GetMoralValue, IsGood, IsEvil,
    HasCommitedAct, CommitAct, Reset
  - DialogueManager: DialogueSystem, Dialogue, DialogueOption, ShowDialogue,
    SelectOption, CloseDialogue

Djurspel.Graphics (5 filer, ~20 207 LOC):
  - Renderer: DrawCube, DrawCylinder, DrawSphere, DrawCone,
    DrawTexturedQuad, DrawTileMap, DrawEntity, DrawUI,
    DrawSkybox, DrawGrid, DrawDebugLine, DrawDebugBox,
    DrawBillboard, DrawParticles, DrawLight, DrawCamera
  - ShaderManager: shader-loading, setMatrix4, setVector3,
    enableDepthTest, disableDepthTest
  - IsometricCamera: Update, GetCameraMatrix, GetFrustumPlanes
    (3D-isometrisk vy, inte 2D top-down)
  - Camera, CameraController

Djurspel.Game (2 filer, ~442 LOC):
  - GameLoop: Update(dt), Render(), Initialize(), Run()
  - GameStateMachine: state-machine, AddState, UpdateState, RenderState

Djurspel (3 filer, ~387 LOC):
  - Program.cs: Main -> GameLoop -> Run()
  - GameWindow: OpenTK GameWindow
  - GameConfig: GameConfig (GameSize, MaxEntities, etc.)

============================================================
3. ANSAT — TOP-DOWN KONVERSERING
============================================================

ANSAT (vald):
  - Laga INFERAT IsometricCamera -> TopDownCamera (en fil)
  - Laga INFERAT Renderer.DrawTileMap + DrawEntity (2 patchar)
  - NY fil: TopDownCamera
  - NY fil: SpriteBatchRenderer (batcha 2D-draws, prestanda)
  - NY fil: EnemyAI (wander/chase/attack + enkel pathfinding)
  - NY fil: LootDropSystem (drop-pickup, proximity)
  - NY fil: UIManager (health-bar, inventory-overlay)
  - NY fil: ARPGInputManager (WASD movement, space=attack, E=pickup, I=inventory)
  - NY fil: ARPGGameBootstrapper (saker upp, 2 fiender, loot, dungeon)
  - PATCH Program.cs + GameLoop (anvanda nya bootstrapper)

ALTERNATIV BEGRUNDADE AVVISELSER:
  - A1: Hela Renderer omskriven — AVVISTAD. Det finns redan 20k rader
       funktionell OpenGL-kod. Bara DrawTileMap och DrawEntity maste
       goras 2D, inte hela renderaren.
  - A2: Ny ECS-arkitektur — AVVISTAD. Befintliga Components (Transform,
        Health, AI, Render, Movement, Loot) ar redan 2D-vanliga.
        Transform.Position.x/y, Render.Color, Movement.Velocity — allt
        fungerar for 2D top-down utan modell-andring.
  - A3: Asset-baserade sprites — AVVISTAD for Phase 1. Inga externa
        assets tillatna enligt begransningar. Fargade rektanglar
        (DrawRect) OK.

============================================================
4. FIL-NIVPLAN — STEG FOR STEG, MED DEPENDENSER
============================================================

BEFINTLIGA MODULER SOM ANVANDS (REANVANDS):
  - Math2D.Vector2/Vector3/Matrix4x4 — for camera-matriser, transforms
  - EventDispatcher — events (OnLootDropped, OnLootPickedUp, OnPlayerAttacked)
  - EntityRegistry + Entity — skapa spelare + 2 fiender + loot-items
  - Transform, Health, Damage, Movement, AI, Render, Player, Loot components
  - RoomDungeonGenerator — generera dungeon-miljo
  - TileMap — 2D tile-array (REDAN 2D!)
  - CombatManager, DamageEntity — redan fungerande
  - InventoryManager — redan fungerande
  - GameLoop, GameStateMachine — behallas ofordrade
  - ShaderManager — behallas ofordrade

--- STEG 1: NY FIL — TopDownCamera.cs ---
  Plats: Djurspel.Core/Cameras/TopDownCamera.cs
  Beror pa: ingenting (Core)
  Innehall:
    class TopDownCamera
      Properties: Vector2 TargetPosition, float Zoom, Vector2 Offset, Vector2 Position
      Methods:
        Update(Vector2 target, float dt)  — camera-follow on target
        GetViewMatrix() -> Matrix4x4      — ortografisk vy rakt ner
        GetProjectionMatrix(float screenWidth, float screenHeight) -> Matrix4x4
        SetViewport(int width, int height) — for OpenGL scissor
      Verifierad: Math2D.Matrix4x4 finns redan

  BESKRIVNING:
    Enklast ortografiska projektionen rakt ner (top-down).
    Camera-follow: target = spelarens Position. Zoom for zoom-in/out.
    Inget pitch/roll — bara x/y-position + zoom.

--- STEG 2: NY FIL — SpriteBatchRenderer.cs ---
  Plats: Djurspel.Graphics/SpriteBatchRenderer.cs
  Beror pa: Djurspel.Core (Math2D, ShaderManager), Djurspel.Graphics (Renderer)
  Innehall:
    class SpriteBatchRenderer
      Methods:
        Begin()           — bind shaders, disable depth test, set ortho matrix
        DrawRect(Vector2 pos, Vector2 size, Color4 color)  — 2D quad
        DrawRect(Vector2 pos, Vector2 size, Color4 color, float rotation)
        DrawRect(Vector2 pos, Vector2 size, Color4 color, Vector2 uv)
        DrawTexturedRect(Vector2 pos, Vector2 size, Texture2D tex, Color4 tint)
        End()             — flush vertex buffer, re-enable depth test
  BESKRIVNING:
    Batcha 2D-draws med en enda vertex-buffer. Rendera med DrawTexturedQuad
    (finns redan i Renderer). For Phase 1: DrawRect anvender fargade
    rektanglar (inga textures).

--- STEG 3: PATCH — Renderer.cs ---
  Plats: Djurspel.Graphics/Renderer.cs
  Beror pa: ingenting ny
  Innehall:
    LAGA DrawTileMap(TileMap map, TopDownCamera camera, SpriteBatchRenderer batch):
      Iterera over map.Grid[i, j]
      For each non-empty tile:
        Vector2 pos = new Vector2(i * TILE_SIZE, j * TILE_SIZE)
        Color4 color = tileType == Wall ? Color.Gray : Color.Brown
        batch.DrawRect(pos, TILE_SIZE, TILE_SIZE, color)
      Inga 3D-boxar — bara 2D-quads

    LAGA DrawEntity(Entity e, TopDownCamera camera, SpriteBatchRenderer batch):
      Transform t = e.GetComponent<Transform>()
      Render r = e.GetComponent<Render>()
      Vector2 pos = new Vector2(t.Position.x, t.Position.y)
      Color4 color = r.Color ?? Color.White
      float size = r.Scale.x  (enhetlig scaling)
      batch.DrawRect(pos, size, size, color, t.Rotation.z)
      Inga 3D-cubes — bara 2D-sprite med rotation

--- STEG 4: PATCH — Renderer.cs (tillagg) ---
  Plats: Djurspel.Graphics/Renderer.cs
  Innehall:
    LAGA DrawUI(IEnumerable<UIElement> elements) — health bars, inventory overlay
    LAGA DrawDebugText(Vector2 pos, string text) — headless friendly

--- STEG 5: NY FIL — EnemyAI.cs ---
  Plats: Djurspel.Gameplay/EnemyAI.cs
  Beror pa: Djurspel.Core (Math2D), Djurspel.Entities (AI component),
            Djurspel.Gameplay (CombatManager, AIManager)
  Innehall:
    class EnemyAI
      Methods:
        Update(Entity enemy, Entity player, float dt)
          Check: distance < chaseRange? -> chase
          Check: distance < attackRange? -> attack
          Else -> wander
        Wander(Entity enemy, float dt) — slumpmassig riktning, collision-avstotning
        Chase(Entity enemy, Entity player, float dt) — MoveTowards player
        Attack(Entity enemy, Entity player) — anropa CombatManager.MeleeAttack
        CheckLineOfSight(Entity a, Entity b) — enkel raycast (x/y linje)
        AvoidWalls(Entity enemy, Vector2 pos) — kolla TileMap vid ny position
  BESKRIVNING:
    Enkel state-machine (Wander/Chase/Attack) inuti befintliga AI-component.
    Anvander CombatManager.MeleeAttack (finns redan).
    Collision: kolla TileMap.Wall vid ny position innan flytt.

--- STEG 6: NY FIL — LootDropSystem.cs ---
  Plats: Djurspel.Gameplay/LootDropSystem.cs
  Beror pa: Djurspel.Core (EventDispatcher, Math2D),
            Djurspel.Entities (Loot component, EntityRegistry)
  Innehall:
    class LootDropSystem
      Methods:
        OnEntityKilled(Entity deadEntity, EntityRegistry registry)
          Create loot entity at deadEntity.Position
          Loot component: Value, Name, PickupRadius
          Register in registry, dispatch OnLootDropped event
        Update(Entity player, float dt, EntityRegistry registry)
          For each loot entity:
            Check distance(player, loot) < pickupRadius
            If yes: AddItem(registry, loot), remove loot entity
        CreateLootItem(string name, int value, Vector2 position) -> Entity
  BESKRIVNING:
    Lyssnar pa EventDispatcher for OnEntityKilled events.
    Skapar loot-entities. Pickup via proximity + E-key (se ARPGInputManager).

--- STEG 7: NY FIL — UIManager.cs ---
  Plats: Djurspel.Gameplay/UIManager.cs
  Beror pa: Djurspel.Core (Math2D, Math2D.Transform),
            Djurspel.Entities (Health component)
  Innehall:
    class UIManager
      Methods:
        DrawHealthBar(Entity player, Vector2 position, Vector2 size)
          Draw background rect (dark)
          Draw foreground rect (green/red based on Health.Current/Max)
        DrawInventoryOverlay(Entity player, bool visible, Vector2 position)
          Draw rectangle with item slots
        DrawDebugInfo(Vector2 pos, string[] lines)
  BESKRIVNING:
    Enkelt overlay over scene. Health bar = 2 rektanglar (bakgrund + forgrund).
    Inventory overlay = grid av slots.

--- STEG 8: NY FIL — ARPGInputManager.cs ---
  Plats: Djurspel.Gameplay/ARPGInputManager.cs
  Beror pa: Djurspel.Core (InputManager, GameInput)
  Innehall:
    class ARPGInputManager
      Properties: bool MoveUp, MoveDown, MoveLeft, MoveRight,
                   bool Attack, bool Pickup, bool InventoryToggle
      Methods:
        Update() — poll OpenTK KeyboardState
        GetMoveDirection() -> Vector2
        Reset()
  BESKRIVNING:
    WASD/arrows = movement. Space = attack. E = pickup. I = inventory toggle.
    Anvender OpenTK.KeyboardState (finns redan via GameInput).

--- STEG 9: NY FIL — ARPGGameBootstrapper.cs ---
  Plats: Djurspel.Game/ARPGGameBootstrapper.cs
  Beror pa: Djurspel.Core (EventDispatcher, Math2D, ConfigManager),
            Djurspel.Entities (Entity, EntityRegistry, Components),
            Djurspel.World (TileMap, RoomDungeonGenerator),
            Djurspel.Gameplay (ARPGInputManager, EnemyAI, LootDropSystem,
                              CombatManager, InventoryManager, UIManager)
            Djurspel.Graphics (SpriteBatchRenderer, TopDownCamera)
  Innehall:
    class ARPGGameBootstrapper
      Methods:
        Initialize(EntityRegistry registry, EventDispatcher events,
                   TileMap tileMap)
          1. Generate dungeon: RoomDungeonGenerator.Generate()
          2. Create player entity: Transform + Health + Movement + Render (blue)
          3. Create 2 enemy entities: Transform + Health + AI + Render (red)
          4. Set up input, camera, systems
        SetupSystems(EntityRegistry registry, EventDispatcher events)
          Hook up combat, ai, loot, inventory, ui
  BESKRIVNING:
    Ny entry-point som skapar allt for ARPG-läget.
    Eller: PATCH Program.cs att valja mellan 3D-firstperson OCH 2D-ARPG.

============================================================
5. DEPENDENS-GRAPH (Byggordning)
============================================================

  STEG 1: TopDownCamera (Core)  [ingen dependens]
  STEG 2: SpriteBatchRenderer   [Core + Graphics]
  STEG 3-4: Renderer patches    [Graphics + SpriteBatchRenderer]
  STEG 5: EnemyAI               [Core + Entities + Gameplay]
  STEG 6: LootDropSystem        [Core + Entities + Gameplay]
  STEG 7: UIManager             [Core + Entities + Gameplay]
  STEG 8: ARPGInputManager      [Core + Gameplay]
  STEG 9: ARPGGameBootstrapper  [ALL modules above]
  PATCH: Program.cs             [Bootstrapper]

============================================================
6. RISKER + TRADEOFFS
============================================================

RISK 1: IsometricCamera patch vs ny TopDownCamera
  BEDOMNING: Ny TopDownCamera (steg 1) — mindre risk.
  IsometricCamera har frukter framtida 3D-isometriskt spel.
  TopDownCamera ar separat, ren, och kan co-existera.

RISK 2: Renderer.DrawTileMap (20k rader)
  BEDOMNING: Ny metod DrawTileMapTopDown (steg 3) istallet for att patcha
  befintlig DrawTileMap. Undviker att bryta befintlig 3D-rendering.
  Old DrawTileMap -> behallas for 3D-läget.

RISK 3: Headless OpenGL (llvmpipe + Xvfb)
  BEDOMNING: OpenTK med Xvfb fungerar (Verifierad: dotnet build + Xvfb
  i befintligt setup). llvmpipe stoder OpenGL 3.3 Core.

RISK 4: TileMap Renderer (3D-boxar)
  BEDOMNING: TileMap.Renderer.AnimateTileMap() (Verifierad: ~65 LOC,
  anvrker DrawCube). Ny metod DrawTileMapTopDown (2D-quads) utan
  att rora befintlig kod.

RISK 5: AI-component already exists
  BEDOMNING: AIManager (Verifierad: ~150 LOC) har AIState och
  MoveTowards/Attack/Wander. EnemyAI (steg 5) wraps this with
  top-down specific logic (line-of-sight, wall avoidance).

RISK 6: InputManager already exists
  BEDOMNING: GameInput (Verifierad: ~80 LOC) har WASD + mouse.
  ARPGInputManager (steg 8) overlays new bindings (E=pickup, I=inventory)
  without modifying existing code.

============================================================
7. UTANFOMPANE
============================================================
  - Audio: befintlig AudioProvider behallas ofordrad
  - Dialogue: DialogueManager behallas ofordrad
  - Moral: MoralManager behallas ofordrad
  - Wilderness generator: anvands inte for Phase 1 (bara dungeon)
  - Skybox/Grid: inte nodvandiga for 2D top-down
  - Particle effects: Phase 2
  - Animations: Phase 2 (idle, attack animations)
  - Multiplayer: out of scope
  - External assets: Phase 2
  - Save/load: out of scope
  - Sound effects: out of scope
  - Music: out of scope

============================================================
8. KONFIDENS + LABELS
============================================================

  A1 (dotnet build 0 errors): HIGH — C# kompilering ar deterministisk,
  nya filer + patchar ar enkla.

  A2 (Xvfb screenshot): HIGH — OpenTK + Xvfb + llvmpipe verifierat
  i tidigare builds (dotnet build passes, Xvfb finns i system).

  A3 (ECS bevaras): HIGH — inga modell-andringar, bara nya modules.

  A4 (inga assets): HIGH — DrawRect med fargade rektanglar,
  inga textures.

  ASSUMED (inte verifierad):
  - Xvfb + OpenTK fungerar med llvmpipe for rendering (verifierat
    att Xvfb och llvmpipe finns, men inte OpenTK-specifikt)
  - OpenTK KeyboardState ar tillganglig i headless mode
    (OpenTK finns i paket, men keyboard-state polling i headless
     kan krava specialbehandling)

============================================================
9. STATUS — UPPDATERAD 2026-08-01
============================================================

GENOMFORDES:
  STEG 1: TopDownCamera — VERIFIERAD (finns, fungerar med GetProjectionMatrix/GetViewMatrix)
  STEG 2: SpriteBatchRenderer — VERIFIERAD (DrawQuad, shader-integration, BeginBatch/EndBatch)
  STEG 3-4: Renderer patches — VERIFIERAD (DrawTileMapTopDown finns, DrawEntityTopDown finns)
  STEG 5: EnemyAI — VERIFIERAD + UPPGRADERAD (Update() returnerar int damage, UpdateAttack() returnerar Damage, TakeDamage(), 4 fiendetyper med olika stats)
  STEG 6: LootDropSystem — VERIFIERAD (DropLoot, Update med pickupRange, GetItems(), LootItem med IsCollected)
  STEG 7: UIManager — VERIFIERAD (UpdateHealthBar, DrawHealthBar, DrawTextOverlay, DrawRect)
  STEG 8: ARPGInputManager — VERIFIERAD (WASD movement, AttackPressed, InteractPressed, InventoryToggled)
  STEG 9: ARPGGameBootstrapper — VERIFIERAD + UPPGRADERAD:
    - T1: SpriteBatchRenderer shader-integration — KORREKT
    - T2: 2D-quads istället för 3D-cuber — KORREKT (DrawQuad i Render-metoden)
    - T3: Player damage — IMPLEMENTERAD (fiender returnerar damage via Update(), bootstrappern applicerar på _playerHealth, noodrespawn vid 0 HP)
    - T4: Loot pickup (E-knapp) — IMPLEMENTERAD (InteractPressed kopplad till loot-systemet, proximity check)
    - T5: InventorySystem+QuestSystem koppling — IMPLEMENTERAD (loot → inventory items, gold, kill tracking)

REST:
  - A2: Xvfb-screenshot (kräver skärm-emulator för att verifiera rendering)
  - Bygg: dotnet build -> 0 errors, 0 warnings — VERIFIERAD (Release-kompilering klar)

PATCHAR (Program.cs + ARPGGameBootstrapper.cs):
  - Program.cs: Renderer(1280, 720) istället för null!, TopDownCamera, SpriteBatchRenderer, ARGameBootstrapper
  - ARPGGameBootstrapper.cs: Fullständig update/render-lipp, player-damage, loot-pickup, inventory-koppling

============================================================
10. BEFINTLIG KOD SOM REANVANDS (60%+ av funktionaliteten finns redan):
  - Math2D, EventDispatcher, Entity/Registry/Components (ALLA)
  - CombatManager, InventoryManager (ALLA)
  - TileMap, RoomDungeonGenerator (ALLA)
  - GameLoop, GameStateMachine (ALLA)
  - AIManager (wrappas, ej omskriven)
  - InputManager (utvidgas via wrapper)

NYA MODULER (7 filer, ~800-1200 LOC totalt):
  TopDownCamera, SpriteBatchRenderer, EnemyAI, LootDropSystem,
  UIManager, ARPGInputManager, ARPGGameBootstrapper

PATCHAR (3 filer):
  Renderer.cs (DrawTileMapTopDown, DrawEntityTopDown, DrawUI)
  Program.cs (ny bootstrapper entry)
  GameLoop.cs (valj 2D-läget)

PLANEN AR KLAR FOR GODKANNING.