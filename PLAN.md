# Djurspel — Arkitekturplan

> **Hack-and-slash RPG**, isometrisk 2.5D, Diablo/Path of Exile-stil.
> Setting: Framtid där högintelligenta djur ersatt människor. Spelaren är sista människobarnet.
> Språk: C# (.NET 8+) med OpenTK (OpenGL 4.x). Inga färdiga motorer.

---

## 0. Skala-principer (tvärstående)

| Princip | Regel |
|---------|-------|
| **Modullös** | Varje modul = egen namespace, ingen beroendescikel. Riktning: `Program → Game → Core/Entities/Graphics/World/Gameplay/WorldGen/UI` — inget back-edge. |
| **Händelse-drivet** | Alla subsystem kommunicerar via `EventDispatcher` (pub/sub, intill noll hårdkodade referenser). |
| **Data-oriented** | POCO-datatyper, inga arvhierarkier för gameplay. `Entity` = samling `IComponent` + `int`-ID. |
| **Asset pipeline** | Central `AssetManager<T>` i `Djurspel.Core`. Load/shader/cache i en fil. |
| **Skriptspråk** | Entities definitioner i JSON/YAML. En `EntityFactory` i `Djurspel.Entities` deserierar och bygger dem. |

**Modulberoendegraf (enriktad, ingen cirkel):**

```
Program
 └─ Game
     ├─ Djurspel.Core          ← (ingen dependency)
     ├─ Djurspel.Entities      ← Core
     ├─ Djurspel.Graphics      ← Core
     ├─ Djurspel.World         ← Core, Entities, Graphics
     ├─ Djurspel.Gameplay      ← Core, Entities, World, Graphics
     ├─ Djurspel.WorldGen      ← Core, Entities, World, Graphics
     ├─ Djurspel.UI            ← Core, Entities, Graphics
     └─ Djurspel.Program       ← alla ovan (bootstrapper)
```

---

## 1. MODULSTRUKTUR

```
djurspel/
├── src/
│   ├── Djurspel.Core/                # Händelsessystem, datatyper, asset pipeline
│   │   └── Djurspel.Core.csproj
│   ├── Djurspel.Entities/            # Entity-ID, Component-POCO, EntityFactory
│   │   └── Djurspel.Entities.csproj
│   ├── Djurspel.Graphics/            # Renderer, Shader, Camera, Primitive meshes
│   │   └── Djurspel.Graphics.csproj
│   ├── Djurspel.World/               # Isometrisk värld, tilemap, camera
│   │   └── Djurspel.World.csproj
│   ├── Djurspel.Gameplay/            # Spellogik, combat, AI, inventory
│   │   └── Djurspel.Gameplay.csproj
│   ├── Djurspel.WorldGen/            # Procedural generation (post-prototyp)
│   │   └── Djurspel.WorldGen.csproj
│   ├── Djurspel.UI/                  # HUD, menysystem (post-prototyp)
│   │   └── Djurspel.UI.csproj
│   ├── Djurspel.Game/                # Game state machine, scene management
│   │   └── Djurspel.Game.csproj
│   └── Djurspel.Program/             # Entrépunkt, bootstrapper
│       └── Djurspel.Program.csproj
├── assets/                           # OBJ, TGA, JSON, YAML filer
│   ├── meshes/
│   ├── textures/
│   ├── entities/
│   └── levels/
└── PLAN.md
```

---

## 2. VARJE MODULENS PUBLIC API

### 2.1 `Djurspel.Core` — Hjärtat

**Namespace:** `Djurspel.Core`

```csharp
// ===== EventDispatcher.cs — Pub/Sub-eventsystem =====
namespace Djurspel.Core;

public enum EventPriority { Low = 0, Normal = 1, High = 2, Critical = 3 }

/// <summary>
/// Global event dispatcher. Alla subsystem lyssnar via subscribe/unsubscribe.
/// Inga hårdkodade referenser mellan subsystem — allt går via events.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>Subscribe to events of type T.</summary>
    IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal)
        where T : IEvent;

    /// <summary>Subscribe once — auto-unsubscribes after first fire.</summary>
    IDisposable SubscribeOnce<T>(Action<T> handler)
        where T : IEvent;

    /// <summary>Dispatch an event to all subscribers (synchronous, fire-and-forget).</summary>
    void Dispatch<T>(T evt) where T : IEvent;

    /// <summary>
    /// Dispatch to a specific target entity. Only entities that subscribed
    /// for targetType receive it.
    /// </summary>
    void DispatchForEntity<T>(int targetEntityId, T evt) where T : IEvent;
}

/// <summary>
/// Marker interface for all events. Guarantees type safety.
/// </summary>
public interface IEvent { }

/// <summary>
/// Base class with timestamp and optional source entity ID.
/// Subclass this for events that relate to an entity.
/// </summary>
public abstract class EntityEvent : IEvent
{
    public DateTime Timestamp { get; }
    public int SourceEntityId { get; }

    protected EntityEvent(int sourceEntityId)
    {
        Timestamp = DateTime.UtcNow;
        SourceEntityId = sourceEntityId;
    }
}

/// <summary>
/// Root implementation for events not tied to a specific entity.
/// </summary>
public sealed class GameEvent : IEvent
{
    public DateTime Timestamp { get; }
    public GameEvent() { Timestamp = DateTime.UtcNow; }
}
```

**Data flow:**
```
[Gameplay] → IEventDispatcher.Dispatch(new EnemyKilled(enemyId, lootId))
              │
              ├─> [UI]         → update kill counter
              ├─> [Core]       → log to file
              └─> [World]      → drop loot entity at position
```

```csharp
// ===== AssetManager.cs — Central asset loader/cache =====
namespace Djurspel.Core;

public enum AssetType { Mesh, Texture, Shader, EntityDef, LevelDef }

/// <summary>
/// Handles loading, caching, and unloading of assets.
/// Thread-safe for reads; all writes go through this singleton.
/// </summary>
public interface IAssetManager
{
    /// <summary>Load an asset by path. Returns cached instance if already loaded.</summary>
    T Load<T>(string path) where T : notnull;

    /// <summary>Check if an asset is in cache.</summary>
    bool Contains<T>(string path);

    /// <summary>Remove an asset from cache. If refcount=0, dispose native resources.</summary>
    void Unload<T>(string path);

    /// <summary>Unload all assets of a given type. Called on scene change.</summary>
    void UnloadAll<T>();

    /// <summary>Unload every cached asset. Called on shutdown.</summary>
    void ClearAll();
}

/// <summary>
/// Lightweight handle returned by Load. Supports reference counting.
/// </summary>
public readonly struct AssetHandle<T> where T : notnull
{
    public string Path { get; }
    public T Resource { get; }
    public int RefCount { get; }
}
```

**Internal types (not exported, loaded into cache):**
```csharp
// Internal: what the manager actually stores
public class MeshAsset
{
    public int VaoId { get; set; }
    public int ElementCount { get; set; }
    public string Name { get; set; } = "";
    // vertices, normals, UVs, indices in arrays
    public float[] Vertices { get; set; }
    public float[] Normals { get; set; }
    public float[] Uv { get; set; }
    public int[] Indices { get; set; }
}

public class TextureAsset
{
    public int GlHandle { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TextureFormat Format { get; set; }
}

public class ShaderProgram
{
    public int GlProgramId { get; set; }
    public Dictionary<string, int> UniformLocations { get; } = new();
}
```

```csharp
// ===== Math2D.cs — Vektortyper =====
namespace Djurspel.Core;

public readonly struct Vec2
{
    public float X { get; }
    public float Y { get; }
    public Vec2(float x, float y) { X = x; Y = y; }
    public static Vec2 Zero => new(0, 0);
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    public float Length => MathF.Sqrt(X * X + Y * Y);
    public Vec2 Normalized => Length > 0 ? this * (1f / Length) : Vec2.Zero;
}

public readonly struct Vec3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
    public static Vec3 Zero => new(0, 0, 0);
    public static Vec3 Up => new(0, 1, 0);
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    public static float Dot(Vec3 a, Vec3 b) => a.X*b.X + a.Y*b.Y + a.Z*b.Z;
    public static Vec3 Cross(Vec3 a, Vec3 b) =>
        new(a.Y*b.Z - a.Z*b.Y, a.Z*b.X - a.X*b.Z, a.X*b.Y - a.Y*b.X);
}

public readonly struct Vec2I
{
    public int X { get; }
    public int Y { get; }
    public Vec2I(int x, int y) { X = x; Y = y; }
}

public readonly struct Vec3I
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public Vec3I(int x, int y, int z) { X = x; Y = y; Z = z; }
}
```

---

### 2.2 `Djurspel.Entities` — Entity + Component

**Namespace:** `Djurspel.Entities`

```csharp
namespace Djurspel.Entities;

/// <summary>
/// Component marker — all gameplay data is stored in plain POCO components.
/// An Entity is just a bag of components keyed by type.
/// NO inheritance hierarchy for gameplay logic.
/// </summary>
public interface IComponent { }

/// <summary>
/// Core entity. Identified by a monotonically increasing int.
/// Holds components in a Dictionary<Type, IComponent>.
/// Fires events when components are added/removed.
/// </summary>
public class Entity : IDisposable
{
    public int Id { get; }
    public string Name { get; set; } = "";
    public bool IsAlive { get; private set; } = true;

    /// <summary>Add a component. Fires ComponentAdded event.</summary>
    public void AddComponent<T>(T component) where T : IComponent;

    /// <summary>Get a component by type. Returns null if not present.</summary>
    public T? GetComponent<T>() where T : IComponent;

    /// <summary>Remove a component. Fires ComponentRemoved event.</summary>
    public void RemoveComponent<T>() where T : IComponent;

    /// <summary>Get all components of a given type.</summary>
    public IEnumerable<IComponent> GetComponents<T>();

    /// <summary>Mark entity as dead. Scheduled for cleanup this frame.</summary>
    public void Die();

    public void Dispose();
}

/// <summary>
/// Entity manager — central registry of all entities.
/// Manages lifecycle, IDs, queries by component type.
/// </summary>
public interface IEntityRegistry
{
    /// <summary>Create a new entity. Assigns next available ID.</summary>
    Entity Create();

    /// <summary>Create an entity from a JSON definition. EntityFactory uses this.</summary>
    Entity CreateFromDefinition(EntityDefinition def);

    /// <summary>Get an entity by ID. Null if dead or not found.</summary>
    Entity? Get(int id);

    /// <summary>Get all living entities that have the given component type.</summary>
    IEnumerable<Entity> Query<T>() where T : IComponent;

    /// <summary>
    /// Get all living entity IDs. Called each frame for rendering/iteration.
    /// </summary>
    IEnumerable<int> GetAllLivingIds();

    /// <summary>
    /// Process pending deaths from this frame. Returns list of IDs that were removed.
    /// Call once per fixed-timestep update.
    /// </summary>
    List<int> ProcessDeaths();
}

/// <summary>
/// Component definitions for entity JSON serialization.
/// Each component type has a corresponding JSON key.
/// </summary>
public class EntityDefinition
{
    public string Type { get; set; } = "";      // "player", "wolf_enemy", "treasure"
    public string Name { get; set; } = "";
    public Dictionary<string, object> ComponentData { get; } = new();
    // ComponentData maps component names (e.g., "transform", "health", "combat")
    // to their JSON-serializable property values.
}
```

**Entity data flow:**
```
JSON/YAML file
    │
    │ (EntityFactory.Deserializer)
    ▼
EntityDefinition ──→ IEntityRegistry.CreateFromDefinition()
    │
    │ (creates Entity, adds components from ComponentData)
    ▼
Entity { Id: 42, Name: "AlphaWolf" }
    ├── TransformComponent { Position = (5,0,10), Scale = (1,1,1) }
    ├── HealthComponent { Current = 100, Max = 100 }
    ├── CombatComponent { AttackDamage = 15, AttackSpeed = 1.0 }
    └── AIComponent { Behavior = "patrol", TargetId = null }
```

### 2.2.1 Komponenter (Component POCO-data)

**Namespace:** `Djurspel.Entities.Components`

```csharp
namespace Djurspel.Entities.Components;

/// <summary>Position, rotation, scale in the world. 3D float coords.</summary>
public class TransformComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }    // Y = "up" in world space; in isometric,
                                     // screen Y maps to world (tileX + tileY) direction
    public float Z { get; set; }    // Z = height (floor levels, jumping)
    public float Scale { get; set; } = 1.0f;
    public float RotationY { get; set; } = 0f;
}

/// <summary>Current and maximum hit points.</summary>
public class HealthComponent : IComponent
{
    public float Current { get; set; }
    public float Max { get; set; }
    public bool IsDead => Current <= 0f;
}

/// <summary>Attack damage, cooldown, range, target type.</summary>
public class CombatComponent : IComponent
{
    public float AttackDamage { get; set; }
    public float AttackCooldown { get; set; }    // seconds between attacks
    public float AttackRange { get; set; }       // world units
    public float AttackSpeed { get; set; }       // attacks per second
    public string WeaponType { get; set; } = "melee";
}

/// <summary>AI behavior tree definition for NPCs.</summary>
public class AIComponent : IComponent
{
    public AIBehavior Behavior { get; set; }
    public int? TargetEntityId { get; set; }
    public Vec3I PatrolPoint { get; set; }
}

public enum AIBehavior { Idle, Patrol, Chase, Attack, Flee, Ally }

/// <summary>Movement speed and current velocity.</summary>
public class MovementComponent : IComponent
{
    public float Speed { get; set; } = 3.0f;       // world units per second
    public float CurrentSpeed { get; set; }
    public bool IsMoving { get; set; }
    public Vec3 TargetPosition { get; set; }
}

/// <summary>Visual sprite/model reference for rendering.</summary>
public class RenderComponent : IComponent
{
    public string MeshAssetPath { get; set; } = "";
    public string TextureAssetPath { get; set; } = "";
    public Vector3 Color { get; set; } = new(1, 1, 1);
    public bool Visible { get; set; } = true;
}

/// <summary>Player-specific: moral alignment, inventory slots.</summary>
public class PlayerComponent : IComponent
{
    public MoralAlignment Alignment { get; set; } = MoralAlignment.Neutral;
    public int Gold { get; set; } = 0;
    public int Experience { get; set; } = 0;
    public int Level { get; set; } = 1;
}

public enum MoralAlignment { Compassionate = 0, Neutral = 1, Ruthless = 2 }

/// <summary>Drop loot on death.</summary>
public class LootComponent : IComponent
{
    public string[] LootTable { get; set; } = Array.Empty<string>();
    public float DropChance { get; set; } = 1.0f;
}

/// <summary>Dialogue and companion state.</summary>
public class DialogueComponent : IComponent
{
    public string DialogueFile { get; set; } = "";
    public int RelationshipScore { get; set; } = 0;
    public bool IsCompanion { get; set; }
}
```

---

### 2.3 `Djurspel.Graphics` — Renderer, Shaders, Camera

**Namespace:** `Djurspel.Graphics`

```csharp
namespace Djurspel.Graphics;

/// <summary>
/// Main renderer interface. Receives draw calls from World/Gameplay
/// and issues OpenGL commands.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>Set viewport dimensions (called on window resize).</summary>
    void SetViewport(int width, int height);

    /// <summary>Clear the screen with given color. Called once per frame.</summary>
    void Clear(Color background);

    /// <summary>Draw a single mesh at the given isometric screen position.</summary>
    void DrawMesh(
        MeshAsset mesh,
        TextureAsset? texture,
        Vec2 screenPos,
        float scale,
        Vector3 color,
        float rotation);

    /// <summary>Draw a flat tile (ground/wall) at isometric screen position.</summary>
    void DrawTile(
        TileMesh tileMesh,
        Vec2 screenPos,
        float heightOffset,
        Color tintColor);

    /// <summary>Draw a line (for debug, paths, etc.).</summary>
    void DrawLine(Vec2 from, Vec2 to, Color color, float thickness);

    /// <summary>Begin/End a batch for efficient state grouping.</summary>
    void BeginBatch(RenderBatchType type);
    void EndBatch();
}

public enum RenderBatchType { World, UI, Debug }

public readonly struct Color
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }
    public Color(float r, float g, float b, float a = 1.0f)
    { R = r; G = g; B = b; A = a; }
    public static Color White => new(1, 1, 1);
    public static Color Black => new(0, 0, 0);
    public static Color Red => new(1, 0, 0);
}

/// <summary>
/// Shader manager. Creates, caches, and binds GLSL shader programs.
/// </summary>
public interface IShaderManager : IDisposable
{
    /// <summary>Load a shader program from vertex + fragment source files.</summary>
    ShaderProgram Load(string vertexPath, string fragmentPath, string? geometryPath = null);

    /// <summary>Get a cached shader by name.</summary>
    ShaderProgram? Get(string name);

    /// <summary>Activate a shader program for rendering.</summary>
    void Bind(ShaderProgram shader);

    /// <summary>Set a float uniform.</summary>
    void SetUniform(string name, float value);

    /// <summary>Set a vec3 uniform.</summary>
    void SetUniformVec3(string name, Vector3 value);

    /// <summary>Set a vec4 uniform.</summary>
    void SetUniformVec4(string name, Color value);

    /// <summary>Set a mat4 uniform (projection * view).</summary>
    void SetUniformMat4(string name, float[] matrix);

    /// <summary>Set a sampler2D uniform (texture unit index).</summary>
    void SetUniformInt(string name, int value);
}

/// <summary>
/// Camera for isometric view. Handles tile-to-screen and screen-to-tile transforms.
/// </summary>
public interface ICamera
{
    /// <summary>Position of the camera center in world tile coordinates.</summary>
    Vec3 Position { get; set; }

    /// <summary>Zoom level (1.0 = default). Range: 0.5 to 3.0.</summary>
    float Zoom { get; set; }

    /// <summary>Convert world tile position to screen pixel position.</summary>
    Vec2 WorldToScreen(Vec3 worldPos);

    /// <summary>Convert screen pixel position to world tile position (Z=0 plane).</summary>
    Vec3 ScreenToWorld(Vec2 screenPos);

    /// <summary>
    /// Convert a tile-grid coordinate to isometric screen coordinate.
    /// tileX = right-down, tileY = left-down, tileZ = up.
    /// </summary>
    Vec2 TileToScreen(Vec3I tile, float tilePixelWidth = 64f, float tilePixelHeight = 32f);

    /// <summary>Invert: screen pixel → tile-grid coordinate.</summary>
    Vec3I ScreenToTile(Vec2 screen, float tilePixelWidth = 64f, float tilePixelHeight = 32f);

    /// <summary>
    /// Return screen-space draw order for a list of world positions.
    /// Entities lower on screen draw first (back-to-front sort).
    /// </summary>
    IEnumerable<Vec3I> GetDepthSortedOrder(IEnumerable<Vec3I> entities);

    /// <summary>Smoothly follow a target entity's position.</summary>
    void FollowEntity(Entity target, float smoothingFactor = 5f, float dt = 0.016f);
}
```

**Shader details (sektion 6):**

```csharp
// ===== Primitive mesh factory =====
namespace Djurspel.Graphics;

public interface IPrimitiveMeshFactory
{
    MeshAsset CreateCube();
    MeshAsset CreateSphere(int segments = 16);
    MeshAsset CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16);
    MeshAsset CreatePlane(float width = 1.0f, float depth = 1.0f);
    MeshAsset CreateIsometricTile(float width, float depth, float height);
}

// ===== Window/OpenGL bootstrap =====
namespace Djurspel.Graphics;

public interface IGameWindow : IDisposable
{
    int Width { get; }
    int Height { get; }
    bool IsOpen { get; }
    bool ShouldClose { get; }

    /// <summary>Swap buffers. Call at end of each frame.</summary>
    void SwapBuffers();

    /// <summary>Request window close.</summary>
    void Close();

    /// <summary>Set window title.</summary>
    void SetTitle(string title);

    /// <summary>Process OS events (must call every frame).</summary>
    void ProcessEvents();

    /// <summary>Mouse position in pixels.</summary>
    Vec2 MousePosition { get; }

    /// <summary>Is a mouse button down?</summary>
    bool IsMouseButtonPressed(int button);

    /// <summary>Is a keyboard key down?</summary>
    bool IsKeyDown(int key);
}
```

---

### 2.4 `Djurspel.World` — Isometrisk tilevärld + camera

**Namespace:** `Djurspel.World`

```csharp
namespace Djurspel.World;

/// <summary>
/// Tile type enumeration. Each tile has a type that determines
/// its mesh, texture, and collision behavior.
/// </summary>
public enum TileType
{
    Ground = 0,
    Wall = 1,
    Floor = 2,
    Door = 3,
    Water = 4,
    Stairs = 5,
    Void = 6    // out-of-bounds
}

/// <summary>
/// Collision mask for a tile. Bit flags.
/// </summary>
public enum TileCollision
{
    None = 0,
    Walkable = 1,
    Solid = 2,
    Water = 4,
    Interactable = 8
}

/// <summary>
/// A single tile in the isometric grid.
/// Pure POCO data — no logic, no references to other systems.
/// </summary>
public struct TileData
{
    public TileType Type;
    public TileCollision Collision;
    public string? MeshPath;
    public string? TexturePath;
    public Color TintColor;
    public float HeightOffset;    // how high above ground plane
}

/// <summary>
/// Isometric world. A 2D array of TileData (tile grid).
/// Z-layer support: multiple layers stacked vertically (floors, ceilings).
/// </summary>
public interface IWorld
{
    /// <summary>Width in tiles.</summary>
    int Width { get; }

    /// <summary>Height in tiles.</summary>
    int Height { get; }

    /// <summary>Number of Z-layers (floors).</summary>
    int Layers { get; }

    /// <summary>Get the tile at grid position (X, Y, Z-layer). Void if out of bounds.</summary>
    TileData GetTile(int x, int y, int z = 0);

    /// <summary>Set a tile. Fires TileChanged event.</summary>
    void SetTile(int x, int y, int z, TileData tile);

    /// <summary>Check if a world position is walkable (no solid tile).</summary>
    bool IsWalkable(Vec3I position);

    /// <summary>
    /// Check if a position collides with any solid tile.
    /// Used by movement and pathfinding.
    /// </summary>
    bool CollidesWithSolid(Vec3I position);

    /// <summary>Get the camera for this world.</summary>
    ICamera Camera { get; }

    /// <summary>
    /// Get the list of drawable tile regions for the current camera view.
    /// Called once per frame. Returns screen-space bounds for frustum culling.
    /// </summary>
    IEnumerable<TileDrawRegion> GetVisibleTiles();
}

/// <summary>
/// A region of tiles that should be drawn together. Used for batching.
/// </summary>
public struct TileDrawRegion
{
    public Vec2I Origin;    // top-left of region in tile grid
    public Vec2I Size;      // width and height in tiles
    public int Layer;       // Z-layer
}

/// <summary>
/// World factory — creates worlds from level definition files.
/// Used during prototyping and WorldGen module.
/// </summary>
public interface IWorldFactory
{
    IWorld CreateFromJson(string levelJsonPath);
    IWorld CreateFromPrimitive(int width, int height, int layers, TileType defaultType);
}
```

**Data flow — rendering pipeline:**
```
World (tile grid)
    │
    │ IWorld.GetVisibleTiles() → frustum cull
    ▼
TileDrawRegion[] (visible tiles)
    │
    │ Sort by depth (tileX + tileY) for isometric painter's algorithm
    ▼
For each tile:
    AssetManager<MeshAsset> tileMesh = Load(tile.MeshPath)
    IRenderer.DrawTile(tileMesh, TileToScreen(tile), tile.HeightOffset, tile.TintColor)
```

---

### 2.5 `Djurspel.Gameplay` — Spellogik, combat, events

**Namespace:** `Djurspel.Gameplay`

```csharp
namespace Djurspel.Gameplay;

/// <summary>
/// Handles combat logic: attacks, damage, cooldowns, kills.
/// Listens to player input events and emits combat result events.
/// </summary>
public interface ICombatManager
{
    /// <summary>Register a player attack. Returns CombatResult.</summary>
    CombatResult Attack(int attackerEntityId, int? targetEntityId, Vec3 attackerPos);

    /// <summary>Process cooldowns for all entities. Called every fixed timestep.</summary>
    void Update(float dt);

    /// <summary>Register an AI attack from an enemy. Returns CombatResult.</summary>
    CombatResult AiAttack(int attackerEntityId, int targetEntityId);
}

public struct CombatResult
{
    public int TargetId { get; set; }
    public float DamageDealt { get; set; }
    public bool Hit { get; set; }
    public bool Killed { get; set; }
}

/// <summary>
/// Handles AI behavior updates for entities with AIComponent.
/// Listens to AIUpdate tick events, emits AIAction events.
/// </summary>
public interface IAIManager
{
    /// <summary>Update all AI entities. Called every fixed timestep.</summary>
    void Update(float dt);

    /// <summary>Set target for an AI entity (e.g., when aggroed).</summary>
    void SetTarget(int aiEntityId, int targetEntityId);

    /// <summary>Remove AI entity from management (dead or departed).</summary>
    void Remove(int aiEntityId);
}

/// <summary>
/// Manages player input → actions → events.
/// Translates keyboard/mouse into gameplay commands.
/// </summary>
public interface IInputManager
{
    /// <summary>Move player toward screen position.</summary>
    void QueueMove(Vec2 screenTarget);

    /// <summary>Player attacks at screen position (raycast to world).</summary>
    void QueueAttack(Vec2 screenTarget);

    /// <summary>Player uses an ability/skill.</summary>
    void QueueAbility(int abilityId);

    /// <summary>Player makes a moral choice (Compassionate/Neutral/Ruthless).</summary>
    void QueueMoralChoice(MoralAlignment choice, int companionId);

    /// <summary>Reset all queued actions. Called after processing this frame.</summary>
    void ClearQueues();
}

/// <summary>
/// Manages the moral choice system.
/// Tracks decisions and their consequences on companion relationships.
/// </summary>
public interface IMoralManager
{
    /// <summary>Record a moral decision and its consequences.</summary>
    void RecordDecision(int decisionId, MoralAlignment choice, int? companionAffected);

    /// <summary>Get current player alignment score breakdown.</summary>
    MoralScore GetScore();

    /// <summary>Check if a decision triggers a companion betrayal event.</summary>
    bool TriggersBetrayal(int decisionId, int companionId);
}

public struct MoralScore
{
    public int Compassionate { get; set; }
    public int Ruthless { get; set; }
    public MoralAlignment Dominant =>
        Compassionate > Ruthless ? MoralAlignment.Compassionate :
        Ruthless > Compassionate ? MoralAlignment.Ruthless : MoralAlignment.Neutral;
}

/// <summary>
/// Manages inventory (post-prototyp, stubbed for now).
/// </summary>
public interface IInventoryManager
{
    void AddItem(string itemId, int quantity);
    void RemoveItem(string itemId, int quantity);
    IEnumerable<InventorySlot> GetItems();
}

public struct InventorySlot
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
    public string? EquippedToEntityId { get; set; }  // null = in inventory
}
```

**Event-driven combat data flow:**
```
[IInputManager] → QueueAttack(screenPos)
    │
    │ ICombatManager.Attack(attackerId, targetId, attackerWorldPos)
    ▼
CombatResult { DamageDealt: 15, Killed: false }
    │
    │ IEventDispatcher.Dispatch(new DamageDealtEvent { TargetId=5, Amount=15 })
    ├─> [HealthComponent] → Current -= 15
    ├─> [UI] → Show damage number floating text
    │
    │ If Killed:
    │   IEventDispatcher.Dispatch(new EnemyKilledEvent { TargetId=5, KillerId=1 })
    │   ├─> [World] → Drop loot at target position
    │   ├─> [IMoralManager] → Check for companion betrayal opportunities
    │   └─> [PlayerComponent] → Add experience + gold
    │
    │ If target.IsDead:
    │   IEventDispatcher.Dispatch(new EntityDiedEvent { EntityId=5 })
    │   ├─> [AIManager] → Remove from AI management
    │   └─> [EntityRegistry] → Schedule for cleanup
```

---

### 2.6 `Djurspel.WorldGen` — Procedural generation (post-prototyp)

**Namespace:** `Djurspel.WorldGen`

```csharp
namespace Djurspel.WorldGen;

/// <summary>
/// Procedural world generator. Post-prototyp feature.
/// Creates level layouts, places enemies, loot, and environmental features.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>Generate a dungeon floor with rooms, corridors, and spawns.</summary>
    GeneratedLevel GenerateDungeon(int width, int height, int floor, Random? rng = null);

    /// <summary>Generate an outdoor wilderness area.</summary>
    GeneratedLevel GenerateWilderness(int width, int height, Random? rng = null);
}

public struct GeneratedLevel
{
    public TileData[,] Tiles;         // width x height
    public EntityDefinition[] EnemySpawns;
    public EntityDefinition[] LootSpawns;
    public Vec3I PlayerStart;
    public string Name { get; set; } = "";
}

/// <summary>
/// Room-based dungeon generator.
/// Places rectangular rooms and connects them with corridors.
/// </summary>
public interface IRoomDungeonGenerator
{
    GeneratedLevel Generate(
        int minWidth, int minHeight,
        int maxWidth, int maxHeight,
        int minRooms, int maxRooms,
        int floor);
}
```

---

### 2.7 `Djurspel.UI` — HUD + Menysystem (post-prototyp)

**Namespace:** `Djurspel.UI`

```csharp
namespace Djurspel.UI;

/// <summary>
/// HUD manager. Displays health bar, minimap, inventory slots,
//  moral alignment meter, and combat feedback (damage numbers, etc.).
/// </summary>
public interface IHudManager
{
    /// <summary>Update HUD with current game state. Called every frame.</summary>
    void Update(float dt, int playerEntityId);

    /// <summary>Draw HUD elements. Called during render pass.</summary>
    void Draw(IRenderer renderer);

    /// <summary>Show a floating damage number at screen position.</summary>
    void ShowDamageNumber(Vec2 screenPos, float damage, bool isCrit);

    /// <summary>Show a message (quest update, companion betrayal, etc.).</summary>
    void ShowMessage(string text, float duration = 3f);
}

/// <summary>
/// Menu system for main menu, pause menu, game over screen.
/// </summary>
public interface IMenuManager
{
    /// <summary>Show the main menu.</summary>
    void ShowMainMenu();

    /// <summary>Show the pause menu (in-game).</summary>
    void ShowPauseMenu();

    /// <summary>Show game over / victory screen.</summary>
    void ShowGameOver(bool victory, string reason);

    /// <summary>Process menu input and return state transition request.</summary>
    MenuTransition HandleInput(float dt);
}

public struct MenuTransition
{
    public MenuAction Action { get; set; }
    public string? NextScene { get; set; }
}

public enum MenuAction { None, StartGame, Resume, Quit, Restart }
```

---

### 2.8 `Djurspel.Game` — State machine + scene management

**Namespace:** `Djurspel.Game`

```csharp
namespace Djurspel.Game;

/// <summary>
/// Central game state machine. Manages transitions between Menu, Game, Pause, GameOver.
/// Owns the game loop (fixed timestep, scene management).
/// </summary>
public interface IGameStateMachine : IDisposable
{
    /// <summary>Current game state.</summary>
    GameState CurrentState { get; }

    /// <summary>Start the game loop. Call once from Program.Main.</summary>
    void Run();

    /// <summary>Request a state transition. Thread-safe.</summary>
    void TransitionTo(GameState newState, object? payload = null);

    /// <summary>Toggle pause (Game ↔ Pause).</summary>
    void TogglePause();
}

public enum GameState { Menu, Game, Pause, GameOver }

/// <summary>
/// Scene manager. Loads/unloads scenes (levels) and manages scene-specific systems.
/// </summary>
public interface ISceneManager
{
    /// <summary>Load a scene (level) by definition file.</summary>
    void LoadScene(string scenePath);

    /// <summary>Unload the current scene and all its entities.</summary>
    void UnloadScene();

    /// <summary>Get the current scene's world.</summary>
    IWorld? World { get; }

    /// <summary>Get the current scene's player entity.</summary>
    Entity? PlayerEntity { get; }
}

/// <summary>
/// Game loop controller. Manages fixed timestep, interpolation, and frame pacing.
/// </summary>
public interface IGameLoop : IDisposable
{
    /// <summary>Set fixed timestep (default: 1/60).</summary>
    void SetFixedTimestep(double fps);

    /// <summary>Register update callbacks. Called in order each fixed timestep.</summary>
    void RegisterUpdate(Action<double> update, string name);

    /// <summary>Register render callback. Called every rendered frame.</summary>
    void RegisterRender(Action<double> render, string name);

    /// <summary>Start/stop the game loop.</summary>
    void Start();
    void Stop();
}
```

**Game loop architecture:**
```
┌─────────────────────────────────────────────────┐
│                 Game Loop                       │
│                                                 │
│  Fixed Timestep (60 Hz)                         │
│  ┌──────────┐   ┌──────────┐   ┌───────────┐   │
│  │  Input   │──→│  Update  │──→│  Physics  │──→│  (fixed dt = 16.67ms)
│  │  Poll    │   │  (AI,    │   │  (collis. │   │
│  │          │   │  combat,  │   │  movement)│   │
│  │          │   │  events)  │   │           │   │
│  └──────────┘   └──────────┘   └───────────┘   │
│                                                 │
│  Render (vsync)                                 │
│  ┌──────────┐   ┌──────────┐   ┌───────────┐   │
│  │  World   │──→│  Entities│──→│    UI     │   │
│  │  Tiles   │   │  Entities│   │  Overlay  │   │  (variable, capped at monitor refresh)
│  └──────────┘   └──────────┘   └───────────┘   │
└─────────────────────────────────────────────────┘
```

**State machine transitions:**
```
            [Start]
               │
               ▼
          ┌───────┐     ┌───────┐
          │  Menu │────→│ Game  │
          └───────┘     └──┬──┬─┘
               ↑           │  │  TogglePause (Esc)
               │           ▼  ▼
          [Quit]        ┌──────┐
                        │ Pause│
                        └──┬───┘
                           │
                    Resume │   GameOver (player dies)
                           ▼
                        ┌─────────┐
                        │ GameOver│────→ [Menu] or [Restart]
                        └─────────┘
```

---

### 2.9 `Djurspel.Program` — Entrépunkt

**Namespace:** `Djurspel.Program`

```csharp
namespace Djurspel.Program;

/// <summary>
/// Application entry point. Bootstraps all subsystems in dependency order.
/// Single responsibility: wire up the DI graph and start the game loop.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // 1. Bootstrap Core
        var eventDispatcher = new EventDispatcher();
        var assetManager = new AssetManager();

        // 2. Bootstrap Graphics (needs OpenGL context)
        var window = GameWindow.Create(1280, 720, "Djurspel");
        var renderer = new Renderer(window);
        var shaderManager = new ShaderManager();
        var camera = new IsometricCamera();
        var primitiveFactory = new PrimitiveMeshFactory(assetManager, shaderManager);

        // 3. Bootstrap Entities
        var entityRegistry = new EntityRegistry(eventDispatcher);

        // 4. Bootstrap World
        var worldFactory = new WorldFactory(assetManager, eventDispatcher);
        var world = worldFactory.CreateFromPrimitive(64, 64, 3, TileType.Ground);

        // 5. Bootstrap Gameplay
        var inputManager = new InputManager(window, eventDispatcher);
        var combatManager = new CombatManager(entityRegistry, eventDispatcher);
        var aiManager = new AIManager(entityRegistry, world, eventDispatcher);
        var moralManager = new MoralManager(eventDispatcher);

        // 6. Bootstrap Game layer
        var sceneManager = new SceneManager(world, entityRegistry, assetManager, eventDispatcher);
        var gameStateMachine = new GameStateMachine(
            window, renderer, shaderManager, camera,
            entityRegistry, world, inputManager, combatManager,
            aiManager, moralManager, sceneManager, eventDispatcher);

        // 7. Run
        gameStateMachine.Run();
    }
}
```

---

## 3. DATA-STRUKTURER — Detaljerad översikt

### 3.1 BaseEntity (Entity + Components)

```
Entity (ID: int, Name: string)
  ├── TransformComponent { X, Y, Z, Scale, RotationY }
  ├── HealthComponent { Current, Max }
  ├── CombatComponent { AttackDamage, AttackCooldown, AttackRange, WeaponType }
  ├── MovementComponent { Speed, CurrentSpeed, IsMoving, TargetPosition }
  ├── RenderComponent { MeshAssetPath, TextureAssetPath, Color, Visible }
  ├── AIComponent { Behavior, TargetEntityId, PatrolPoint }
  ├── PlayerComponent { Alignment, Gold, Experience, Level }
  ├── LootComponent { LootTable[], DropChance }
  └── DialogueComponent { DialogueFile, RelationshipScore, IsCompanion }
```

**Regel: Inget entity har alla komponenter. Varje entity har bara de den behöver.**
- Player: `Transform + Health + Combat + Movement + Render + Player`
- Enemy Wolf: `Transform + Health + Combat + Movement + Render + AI + Loot`
- Treasure Chest: `Transform + Render + Loot`
- NPC Companion: `Transform + Health + Combat + Movement + Render + AI + Dialogue`

### 3.2 Entity Serialization (JSON-format)

```json
{
    "Type": "wolf_enemy",
    "Name": "Alpha Wolf",
    "Components": {
        "Transform": {
            "X": 5.0,
            "Y": 0.0,
            "Z": 10.0,
            "Scale": 1.0,
            "RotationY": 0.0
        },
        "Health": {
            "Current": 100.0,
            "Max": 100.0
        },
        "Combat": {
            "AttackDamage": 15.0,
            "AttackCooldown": 1.5,
            "AttackRange": 2.5,
            "AttackSpeed": 0.67,
            "WeaponType": "melee"
        },
        "Movement": {
            "Speed": 2.5,
            "CurrentSpeed": 0.0,
            "IsMoving": false,
            "TargetPosition": {"X": 0, "Y": 0, "Z": 0}
        },
        "Render": {
            "MeshAssetPath": "assets/meshes/wolf.obj",
            "TextureAssetPath": "assets/textures/wolf.tga",
            "Color": [1.0, 0.8, 0.6],
            "Visible": true
        },
        "AI": {
            "Behavior": "patrol",
            "TargetEntityId": null,
            "PatrolPoint": {"X": 5, "Y": 0, "Z": 10}
        },
        "Loot": {
            "LootTable": ["wolf_pelt", "bone_shard"],
            "DropChance": 0.75
        }
    }
}
```

Deserialiseringsflöde:
```
JSON → EntityDefinition (Dictionary<string, object> per component)
  → EntityFactory.CreateFromDefinition()
    → Entity.AddComponent<T>() for each component in ComponentData
  → EntityRegistry.Add(entity)
```

---

## 4. EVENT SYSTEM — Alla event-typer

**Alla events är POCO-structs eller -klasser som implementerar `IEvent`.**
**Alla events som relaterar till en entity arvsar `EntityEvent` (med `SourceEntityId`).**

### 4.1 EventDispatcher API (upprepning för referens)

```csharp
public interface IEventDispatcher
{
    IDisposable Subscribe<T>(Action<T> handler, EventPriority priority) where T : IEvent;
    IDisposable SubscribeOnce<T>(Action<T> handler) where T : IEvent;
    void Dispatch<T>(T evt) where T : IEvent;
    void DispatchForEntity<T>(int targetEntityId, T evt) where T : IEvent;
}
```

**Implementeringsdetaljer:**
- Intern lagring: `Dictionary<Type, List<Subscription>>` där `Subscription` har handler + priority.
- Vid Dispatch: sortera subscribers by priority (High→Critical first), then fire each handler synchronously.
- `IDisposable` returneras för att unsubscribe. `Dispose()` tar bort handler från listan.
- `DispatchForEntity` filtrerar subscribers som har angett `targetType` vid subscribe.

### 4.2 Alla Event-typer

```csharp
// ===== Entity lifecycle events =====
public sealed class EntitySpawned : EntityEvent
{
    public EntitySpawned(int entityId) : base(entityId) { }
}

public sealed class EntityDied : EntityEvent
{
    public int KillerId { get; }
    public EntityDied(int entityId, int killerId) : base(entityId) => KillerId = killerId;
}

public sealed class ComponentAdded<T> : EntityEvent where T : IComponent
{
    public T Component { get; }
    public ComponentAdded(int entityId, T component) : base(entityId) => Component = component;
}

public sealed class ComponentRemoved<T> : EntityEvent where T : IComponent
{
    public ComponentRemoved(int entityId) : base(entityId) { }
}

// ===== Combat events =====
public sealed class DamageDealt : EntityEvent
{
    public float Amount { get; }
    public bool IsCritical { get; }
    public DamageDealt(int targetId, float amount, bool isCritical) : base(targetId)
    { Amount = amount; IsCritical = isCritical; }
}

public sealed class EnemyKilled : EntityEvent
{
    public int KillerId { get; }
    public Vec3I Position { get; }
    public EnemyKilled(int enemyId, int killerId, Vec3I position) : base(enemyId)
    { KillerId = killerId; Position = position; }
}

public sealed class CombatStarted : GameEvent
{
    public int PlayerId { get; }
    public int[] EnemyIds { get; }
    public CombatStarted(int playerId, int[] enemyIds) : base()
    { PlayerId = playerId; EnemyIds = enemyIds; }
}

public sealed class CombatEnded : GameEvent
{
    public bool Victory { get; }
    public int PlayerId { get; }
    public CombatEnded(int playerId, bool victory) : base()
    { PlayerId = playerId; Victory = victory; }
}

// ===== Moral choice events =====
public sealed class MoralChoiceMade : EntityEvent
{
    public MoralAlignment Choice { get; }
    public int DecisionId { get; }
    public MoralChoiceMade(int playerId, int decisionId, MoralAlignment choice) : base(playerId)
    { Choice = choice; DecisionId = decisionId; }
}

public sealed class CompanionBetrayal : EntityEvent
{
    public int CompanionId { get; }
    public string Reason { get; }
    public CompanionBetrayal(int companionId, string reason) : base(companionId) => Reason = reason;
}

public sealed class CompanionBonded : EntityEvent
{
    public int CompanionId { get; }
    public MoralAlignment BondType { get; }
    public CompanionBonded(int companionId, MoralAlignment bond) : base(companionId) => BondType = bond;
}

// ===== World events =====
public sealed class TileChanged : GameEvent
{
    public int X { get; }
    public int Y { get; }
    public int Layer { get; }
    public TileType NewType { get; }
    public TileChanged(int x, int y, int layer, TileType newType) : base()
    { X = x; Y = y; Layer = layer; NewType = newType; }
}

public sealed class PlayerMoved : EntityEvent
{
    public Vec3I NewPosition { get; }
    public PlayerMoved(int playerId, Vec3I newPos) : base(playerId) => NewPosition = newPos;
}

// ===== Loot events =====
public sealed class LootDropped : EntityEvent
{
    public int EntityId { get; }
    public string[] ItemIds { get; }
    public Vec3I Position { get; }
    public LootDropped(int lootId, string[] itemIds, Vec3I pos) : base(lootId)
    { ItemIds = itemIds; Position = pos; }
}

public sealed class LootPickedUp : EntityEvent
{
    public int PlayerId { get; }
    public string ItemId { get; }
    public int Quantity { get; }
    public LootPickedUp(int playerId, string itemId, int quantity) : base(playerId)
    { ItemId = itemId; Quantity = quantity; }
}

// ===== Level/scene events =====
public sealed class LevelLoaded : GameEvent
{
    public string LevelName { get; }
    public Vec3I PlayerSpawn { get; }
    public LevelLoaded(string name, Vec3I spawn) : base()
    { LevelName = name; PlayerSpawn = spawn; }
}

public sealed class LevelChanged : GameEvent
{
    public string FromLevel { get; }
    public string ToLevel { get; }
    public LevelChanged(string from, string to) : base()
    { FromLevel = from; ToLevel = to; }
}

// ===== Game state events =====
public sealed class GameStateChanged : GameEvent
{
    public GameState From { get; }
    public GameState To { get; }
    public GameStateChanged(GameState from, GameState to) : base()
    { From = from; To = to; }
}

// ===== Debug/infrastructure events =====
public sealed class AssetLoaded : GameEvent
{
    public AssetType Type { get; }
    public string Path { get; }
    public AssetLoaded(AssetType type, string path) : base()
    { Type = type; Path = path; }
}

public sealed class ShaderCompiled : GameEvent
{
    public string ShaderName { get; }
    public ShaderCompiled(string name) : base() => ShaderName = name;
}
```

**Event subscription mappning (vilket subsystem lyssnar på vad):**

| Event | Lyssnare |
|-------|---------|
| `EntitySpawned` | `RenderComponent` — initierar rendering |
| `EntityDied` | `AIManager`, `EntityRegistry`, `IMoralManager` |
| `DamageDealt` | `HealthComponent`, `UI` (floating numbers) |
| `EnemyKilled` | `World` (loot drop), `PlayerComponent` (XP/gold) |
| `CombatStarted` | `AIManager` (aggro alla fiender), `UI` (combat UI) |
| `CombatEnded` | `SceneManager` (next level / game over) |
| `MoralChoiceMade` | `IMoralManager`, `DialogueComponent` |
| `CompanionBetrayal` | `AIManager` (target = player), `UI` (narrative event) |
| `CompanionBonded` | `AIComponent` (behavior → Ally) |
| `TileChanged` | `Renderer` (re-render affected region) |
| `PlayerMoved` | `World` (update path), `Camera` (follow) |
| `LootDropped` | `World` (spawn loot entity) |
| `LootPickedUp` | `IInventoryManager`, `PlayerComponent` |
| `LevelLoaded` | `Camera` (reset position), `Renderer` |
| `StateChanged` | `IMenuManager`, `IInputManager` |

---

## 5. ISOMETRISK MATEMATIK

### 5.1 Koordinatsystem

```
        World Space (isometrisk projection)

        Z (up)
        │
        │
        ┌──────┐
       ╱│     ╱│
      ┌──────┘ │
      │   Y    ├────→ tileY (left-down on screen)
      │        │
      └────────┘
      │
      └───────→ tileX (right-down on screen)

Tile coordinate: (tileX, tileY, tileZ)
  tileX = position along right-down axis (screen X increases)
  tileY = position along left-down axis (screen X decreases)
  tileZ = height (screen Y increases)
```

### 5.2 Tile-to-Screen (world → pixels)

**Formel:**
```
screenX = (tileX - tileY) * tilePixelWidth / 2.0  +  screenOffsetX
screenY = (tileX + tileY) * tilePixelHeight / 2.0  -  tileZ * tilePixelHeight  +  screenOffsetY
```

**Implementation:**
```csharp
// In IsometricCamera (Djurspel.Graphics)
public Vec2 TileToScreen(Vec3I tile, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
{
    float isoX = (tile.X - tile.Y) * (tilePixelWidth / 2.0f);
    float isoY = (tile.X + tile.Y) * (tilePixelHeight / 2.0f) - tile.Z * tilePixelHeight;

    // Apply camera offset (center of screen)
    float screenOffsetX = _viewportWidth / 2f;
    float screenOffsetY = _viewportHeight / 2f;

    return new Vec2(
        isoX + screenOffsetX,
        isoY + screenOffsetY
    );
}
```

**Exempel (tile = (3, 2, 0), tileSize = 64×32):**
```
screenX = (3 - 2) * 32 + viewportX = 32 + viewportX
screenY = (3 + 2) * 16 + viewportY = 80 + viewportY
```

### 5.3 Screen-to-Tile (pixels → world)

**Invertera formeln:**
```
tileX = (screenX / tilePixelWidth) + (screenY / tilePixelHeight) - viewportOffset
tileY = (screenY / tilePixelHeight) - (screenX / tilePixelWidth) + viewportOffset
tileZ = 0   // default ground plane; for Z, use ray-casting into tile height
```

**Implementation:**
```csharp
// In IsometricCamera
public Vec3I ScreenToTile(Vec2 screen, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
{
    // Remove camera center offset
    float isoX = screen.X - _viewportWidth / 2f;
    float isoY = screen.Y - _viewportHeight / 2f;

    // Inverse isometric projection
    float tileX = (isoX / (tilePixelWidth / 2f) + isoY / (tilePixelHeight / 2f)) / 2f;
    float tileY = (isoY / (tilePixelHeight / 2f) - isoX / (tilePixelWidth / 2f)) / 2f;

    return new Vec3I(
        MathF.Round(tileX),
        MathF.Round(tileY),
        0  // default to ground layer
    );
}
```

### 5.4 Depth-sorting (rendereringsordning)

**Problemet:** I isometrisk vy måste objekt längre bort (högare screenY) ritas först.

**Lösning — Painter's Algorithm baserat på `tileX + tileY`:**

```csharp
// In IsometricCamera
public IEnumerable<Vec3I> GetDepthSortedOrder(IEnumerable<Vec3I> entities)
{
    return entities
        .OrderBy(e => e.X + e.Y)   // lower sum = further back = draw first
        .ThenBy(e => e.Z);          // higher Z = closer on screen = draw later
}
```

**Ritningsordning per frame:**
```
For each tile in world:
    depth = tile.X + tile.Y + tile.Z
    Add to draw list with depth value

Sort by depth ascending (furthest first)

For each entry in sorted list:
    screenPos = TileToScreen(entry)
    DrawMesh/DrawTile at screenPos
```

### 5.5 Camera Follow Logic

```csharp
// In IsometricCamera
public void FollowEntity(Entity target, float smoothing = 5f, float dt = 0.016f)
{
    if (target == null) return;

    var transform = target.GetComponent<TransformComponent>();
    if (transform == null) return;

    // Target: center the camera on the entity
    Vec3 targetPos = new Vec3(transform.X, transform.Y, transform.Z);

    // Smooth interpolation (lerp)
    float lerpFactor = 1f - MathF.Pow(1f - smoothing * dt, 1);
    _position.X += (targetPos.X - _position.X) * lerpFactor;
    _position.Y += (targetPos.Y - _position.Y) * lerpFactor;
    _position.Z += (targetPos.Z - _position.Z) * lerpFactor;
}
```

**Camera clamping (håll inom världens gränser):**
```csharp
public void ClampToWorld(int worldWidth, int worldHeight, int worldLayers)
{
    _position.X = Math.Clamp(_position.X, 0, worldWidth - 1);
    _position.Y = Math.Clamp(_position.Y, 0, worldHeight - 1);
    _position.Z = Math.Clamp(_position.Z, 0, worldLayers - 1);
}
```

---

## 6. SHADER STRATEGI

### 6.1 Antal shaders

**Totalt: 3 shaders** (enkelhet för prototyp, utökas senare):

| # | Namn | Ansvar |
|---|------|--------|
| 1 | `WorldShader` | Isometrisk belysning för tiles, terrain, walls |
| 2 | `EntityShader` | 3D-modeller (spelare, fiender, NPC:er) med belysning |
| 3 | `OverlayShader` | HUD-overlay, UI-element, sprite-rendering (ortogonal, ingen belysning) |

### 6.2 WorldShader — isometrisk belysning

**Vertex shader (`world.vert`):**
```glsl
#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;

uniform mat4 uProjection;      // 4x4 isometric projection matrix
uniform mat4 uView;            // 4x4 view (camera) matrix
uniform mat4 uModel;           // 4x4 model (position, scale, rotation)

uniform vec3 uLightDir;        // Directional light direction (normalized)
uniform vec3 uLightColor;      // RGB of directional light
uniform vec3 uAmbientColor;    // Ambient/occlusion light
uniform float uAmbientStrength; // 0.0–1.0

out vec3 vNormal;
out vec2 vUV;
out float vLightIntensity;
out vec3 vWorldPos;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vWorldPos = worldPos.xyz;

    // Transform normal to world space
    mat3 normalMat = mat3(transpose(inverse(uModel)));
    vec3 norm = normalize(normalMat * aNormal);

    // Isometric directional lighting
    float diff = max(dot(norm, uLightDir), 0.0);
    vLightIntensity = uAmbientStrength + (1.0 - uAmbientStrength) * diff;

    vNormal = norm;
    vUV = aUV;

    gl_Position = uProjection * uView * worldPos;
}
```

**Fragment shader (`world.frag`):**
```glsl
#version 430 core

in vec3 vNormal;
in vec2 vUV;
in float vLightIntensity;
in vec3 vWorldPos;

uniform sampler2D uTexture;    // Tile texture (optional, could be -1 for no texture)
uniform vec4 uTintColor;       // Tile color tint
uniform int uHasTexture;       // 1 = use texture, 0 = use tint only

out vec4 FragColor;

void main()
{
    vec4 texColor = vec4(uTintColor.rgb, uTintColor.a);
    if (uHasTexture == 1)
    {
        texColor = texture(uTexture, vUV) * vec4(uTintColor.rgb, uTintColor.a);
    }

    // Apply lighting
    vec3 litColor = texColor.rgb * vLightIntensity;

    // Simple shadow at tile edges (distance from center)
    float edgeDist = min(min(vUV.x, 1.0 - vUV.x), min(vUV.y, 1.0 - vUV.y));
    float edgeShadow = smoothstep(0.0, 0.1, edgeDist);
    litColor *= (0.7 + 0.3 * edgeShadow);

    FragColor = vec4(litColor, texColor.a);
}
```

**WorldShader uniforms:**

| Uniform | Type | Beskrivning |
|---------|------|-------------|
| `uProjection` | mat4 | Isometrisk projektmatris |
| `uView` | mat4 | Vy-matris (camera) |
| `uModel` | mat4 | Model-matris (position/scale/rotation per tile) |
| `uLightDir` | vec3 | Riktning för directionell ljus (t.ex. `normalize(vec3(1.0, 1.0, -0.5))`) |
| `uLightColor` | vec3 | Ljusfärg (t.ex. `vec3(1.0, 0.95, 0.85)` = varmt vitt) |
| `uAmbientColor` | vec3 | Ambient ljus (t.ex. `vec3(0.3, 0.3, 0.35)`) |
| `uAmbientStrength` | float | Ambient-styrka (0.3 = mörk skugga, 1.0 = fullt ljus) |
| `uTexture` | sampler2D | Texture (optional) |
| `uTintColor` | vec4 | Tile-tint färg |
| `uHasTexture` | int | 1 = texture on, 0 = tint only |

### 6.3 EntityShader — 3D-enheter

**Vertex shader (`entity.vert`):**
```glsl
#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;

uniform mat4 uProjection;
uniform mat4 uView;
uniform mat4 uModel;           // Entity model matrix (position + scale + Y-rotation)

uniform vec3 uLightDir;
uniform vec3 uLightColor;
uniform vec3 uAmbientColor;
uniform float uAmbientStrength;

// Entity-specific uniforms
uniform vec3 uEntityColor;
uniform float uEmissive;       // 0.0–1.0, glow effect
uniform float uDamageFlash;    // 1.0 when hit, fades to 0

out vec3 vNormal;
out vec2 vUV;
out float vLightIntensity;
out vec3 vEntityColor;
out float vEmissive;
out float vDamageFlash;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    mat3 normalMat = mat3(transpose(inverse(uModel)));
    vec3 norm = normalize(normalMat * aNormal);

    float diff = max(dot(norm, uLightDir), 0.0);
    vLightIntensity = uAmbientStrength + (1.0 - uAmbientStrength) * diff;

    vNormal = norm;
    vUV = aUV;
    vEntityColor = uEntityColor;
    vEmissive = uEmissive;
    vDamageFlash = uDamageFlash;

    gl_Position = uProjection * uView * worldPos;
}
```

**Fragment shader (`entity.frag`):**
```glsl
#version 430 core

in vec3 vNormal;
in vec2 vUV;
in float vLightIntensity;
in vec3 vEntityColor;
in float vEmissive;
in float vDamageFlash;

uniform sampler2D uTexture;
uniform int uHasTexture;

out vec4 FragColor;

void main()
{
    vec4 baseColor = vec4(vEntityColor, 1.0);
    if (uHasTexture == 1)
    {
        baseColor = texture(uTexture, vUV) * vec4(vEntityColor, 1.0);
    }

    // Damage flash (white tint when hit)
    vec3 flashTint = mix(baseColor.rgb, vec3(1.0), vDamageFlash * 0.8);

    // Apply lighting
    vec3 litColor = flashTint * vLightIntensity;

    // Add emissive glow
    litColor += vEntityColor * vEmissive * 0.3;

    FragColor = vec4(litColor, 1.0);
}
```

**EntityShader uniforms:**

| Uniform | Type | Beskrivning |
|---------|------|-------------|
| `uProjection` | mat4 | Projektmatris (samma som world) |
| `uView` | mat4 | Vy-matris (samma som world) |
| `uModel` | mat4 | Entity model matrix |
| `uLightDir` | vec3 | Directional light |
| `uLightColor` | vec3 | Light color |
| `uAmbientColor` | vec3 | Ambient color |
| `uAmbientStrength` | float | Ambient strength |
| `uEntityColor` | vec3 | Entity base color |
| `uEmissive` | float | Glow intensity (0–1) |
| `uDamageFlash` | float | Flash intensity (fades each frame) |
| `uTexture` | sampler2D | Texture (optional) |
| `uHasTexture` | int | 1 = on, 0 = off |

### 6.4 OverlayShader — HUD/UI

**Vertex shader (`overlay.vert`):**
```glsl
#version 430 core

layout(location = 0) in vec2 aPosition;     // Screen-space quad vertices
layout(location = 1) in vec2 aUV;

out vec2 vUV;

uniform vec4 uColor;      // Tint color with alpha
uniform vec2 uOffset;     // Screen pixel offset
uniform vec2 uSize;       // Screen pixel size

void main()
{
    vUV = aUV;
    gl_Position = vec4(aPosition * uSize + uOffset, 0.0, 1.0);
}
```

**Fragment shader (`overlay.frag`):**
```glsl
#version 430 core

in vec2 vUV;
uniform vec4 uColor;
uniform sampler2D uTexture;
uniform int uHasTexture;

out vec4 FragColor;

void main()
{
    vec4 texColor = uColor;
    if (uHasTexture == 1)
        texColor = texture(uTexture, vUV) * uColor;
    FragColor = texColor;
}
```

**OverlayShader uniforms:**

| Uniform | Type | Beskrivning |
|---------|------|-------------|
| `uColor` | vec4 | Tint färg + alpha |
| `uOffset` | vec2 | Screen pixel position (X, Y) |
| `uSize` | vec2 | Quad size (width, height) i pixlar |
| `uTexture` | sampler2D | Texture (optional, för HUD sprites) |
| `uHasTexture` | int | 1 = on, 0 = off |

### 6.5 Projektmatris (isometrisk)

```csharp
// In IsometricCamera — generates the projection matrix
public float[] GetProjectionMatrix(float tilePixelWidth, float tilePixelHeight)
{
    // Isometric perspective: parallel projection (no perspective divide)
    // This gives the classic 2.5D isometric look
    float aspect = _viewportWidth / (float)_viewportHeight;
    float fov = MathF.PI / 3.0f; // 60 degree FOV
    float zNear = 0.1f;
    float zFar = 1000f;

    float f = 1.0f / MathF.Tan(fov / 2.0f);
    float[] projection = new float[16];

    // Perspective projection matrix (column-major for OpenGL)
    projection[0]  = f / aspect;
    projection[5]  = f;
    projection[10] = (zFar + zNear) / (zNear - zFar);
    projection[11] = -1.0f;
    projection[14] = (2.0f * zFar * zNear) / (zNear - zFar);
    projection[15] = 0.0f;

    return projection;
}
```

### 6.6 Renderorder per frame

```
Frame start
    │
    ▼
1. Clear screen with background color
    │
    ▼
2. Bind WorldShader
    │
    ├── Set projection, view, light uniforms
    │
    ├── For each tile (sorted by depth):
    │   ├── Set model matrix (position + scale)
    │   ├── Set tile tint color
    │   ├── Bind tile texture (if any)
    │   └── Draw tile mesh (Indexed draw call)
    │
    ▼
3. Bind EntityShader
    │
    ├── Set projection, view, light uniforms (same as world)
    │
    ├── For each entity (sorted by depth):
    │   ├── Set model matrix (entity position + scale + Y rotation)
    │   ├── Set entity color, emissive, damage flash
    │   ├── Bind entity mesh + texture
    │   └── Draw entity mesh
    │
    ▼
4. Bind OverlayShader
    │
    ├── For each HUD element:
    │   ├── Set color, offset, size
    │   ├── Bind HUD texture (if any)
    │   └── Draw quad
    │
    ▼
5. Swap buffers
```

---

## 7. ASSET PIPELINE

### 7.1 Filtyper

| Asset Type | Filformat | Beskrivning |
|------------|-----------|-------------|
| **Mesh** | `.obj` (Wavefront OBJ) | Enkel triangulär mesh med normals + UVs |
| **Texture** | `.tga` (Truevision TGA) | Rasterbild, okomprimerad (enkel parsing), RGB(A) |
| **Entity definition** | `.json` | JSON-format entity definition (sektion 3.2) |
| **Level definition** | `.json` | JSON-format level tilemap + spawns |
| **Shader source** | `.vert` / `.frag` / `.geom` | GLSL shader källfiler |

### 7.2 AssetManager API

```csharp
// Sektion 2.1 — återanvändning för referens:
// public interface IAssetManager { Load<T>, Contains<T>, Unload<T>, UnloadAll<T>, ClearAll(); }
```

**Implementation (intern detalj):**
```csharp
// AssetManager.cs — intern cache-struktur
public class AssetManager : IAssetManager
{
    // Internal: Dictionary<AssetType, Dictionary<path, AssetEntry>>
    private readonly Dictionary<Type, Dictionary<string, AssetEntry>> _cache
        = new Dictionary<Type, Dictionary<string, AssetEntry>>();

    private readonly object _lock = new();

    public T Load<T>(string path) where T : notnull
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_cache.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<string, AssetEntry>();
                _cache[type] = dict;
            }

            if (dict.TryGetValue(path, out var entry))
            {
                entry.RefCount++;
                return (T)entry.Resource;
            }

            // Load from disk
            var resource = LoadFromFile<T>(path);
            dict[path] = new AssetEntry(resource);

            // Fire event
            _dispatcher.Dispatch(new AssetLoaded(GetAssetType<T>(), path));

            return resource;
        }
    }

    public bool Contains<T>(string path) =>
        _cache.TryGetValue(typeof(T), out var dict) && dict.ContainsKey(path);

    public void Unload<T>(string path)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(typeof(T), out var dict)
                && dict.TryGetValue(path, out var entry))
            {
                entry.RefCount--;
                if (entry.RefCount <= 0)
                {
                    dict.Remove(path);
                    DisposeResource<T>(entry.Resource);
                }
            }
        }
    }

    // ... UnloadAll, ClearAll similar pattern
}
```

**Asset loading pipeline:**
```
Load request: AssetManager.Load<MeshAsset>("assets/meshes/wolf.obj")
    │
    ▼
Check cache (dictionary lookup)
    │
    ├── Found → Increment refcount, return
    │
    └── Not found → LoadFromFile<T>(path)
        │
        ├── .obj → Parse OBJ → build vertex arrays → create GL VAO → return MeshAsset
        ├── .tga → Read TGA header → read pixel data → create GL texture → return TextureAsset
        ├── .json → Read JSON → deserialize to EntityDefinition/LevelDef → return
        └── .vert/.frag → Read source → compile GL shader → return ShaderProgram
```

### 7.3 Primitive Meshes

```csharp
// In IPrimitiveMeshFactory (sektion 2.3)
public interface IPrimitiveMeshFactory
{
    MeshAsset CreateCube();
    MeshAsset CreateSphere(int segments = 16);
    MeshAsset CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16);
    MeshAsset CreatePlane(float width = 1.0f, float depth = 1.0f);
    MeshAsset CreateIsometricTile(float width, float depth, float height);
}

// Implementering (intern detalj)
public class PrimitiveMeshFactory : IPrimitiveMeshFactory
{
    // CreateCube(): 8 vertices, 12 triangles (4 faces × 2 tris), normals per face
    // Vertices: [±0.5, ±0.5, ±0.5] arranged as 4 faces
    // Normals: face-normal per vertex (flat shading)

    // CreateSphere(): UV-sphere parameterization
    // u ∈ [0, π], v ∈ [0, 2π]
    // x = sin(u)cos(v), y = cos(u), z = sin(u)sin(v)
    // segments controls radial segments (16 = default, 32 = high quality)

    // CreateCylinder(): Two caps + side surface
    // Side: parameterize by angle + height
    // Top/bottom: triangles from center to edge

    // CreatePlane(): 2 triangles, 4 vertices
    // Quad from (-w/2, 0, -d/2) to (w/2, 0, d/2)
    // Normal = (0, 1, 0) for ground tiles

    // CreateIsometricTile(): Custom box mesh with proper isometric UVs
    // Top face: 4 vertices, 2 triangles
    // Side faces: 2 visible faces (right and left in isometric)
    // Bottom face: hidden (cull)
    // UVs: tiled (0–1) for texture mapping
}
```

**Primitive mesh data flow:**
```
Game bootstraps
    │
    ▼
PrimitiveMeshFactory.CreateCube()
    │ → Generates vertices/indices in memory
    │ → Calls OpenGL to create VAO + element buffer
    │ → Returns MeshAsset { VaoId, ElementCount, Vertices[], Normals[], Indices[] }
    │
    ▼
Cached in AssetManager
    │
    ▼
Available for: Entity meshes (characters = composed of primitives),
               UI elements, debug visuals, placeholder rendering
```

---

## 8. GAME LOOP

### 8.1 Fixed Timestep (60 Hz)

```csharp
// In GameLoop
public const double FixedTimestep = 1.0 / 60.0;  // 16.667ms per step
public const double MaxFrameTime = 1.0 / 30.0;    // Don't spiral on lag (30fps cap)

// Game loop structure:
double accumulator = 0;
double lastTime = GetTimestamp();

while (gameState != GameState.Shutdown)
{
    double currentTime = GetTimestamp();
    double frameTime = Math.Min(currentTime - lastTime, MaxFrameTime);
    lastTime = currentTime;
    accumulator += frameTime;

    // Process fixed-timestep updates
    while (accumulator >= FixedTimestep)
    {
        PollInput();
        Update(FixedTimestep);    // AI, combat, physics, events
        ProcessDeaths();           // Entity cleanup
        accumulator -= FixedTimestep;
    }

    // Render at whatever framerate (interpolate for smooth visuals)
    Render(accumulator / FixedTimestep);  // interpolation factor
    SwapBuffers();
    ProcessEvents();                       // OS-level events (window resize, close)
}
```

### 8.2 State Machine

```csharp
// GameState machine transitions:
public enum GameState { Menu, Game, Pause, GameOver }

// Transition rules:
// Menu → Game:      "Start Game" button or Enter key
// Game → Pause:     Escape key or Pause key
// Pause → Game:     Escape key or "Resume" button
// Game → GameOver:  Player health reaches 0
// GameOver → Menu:  "Main Menu" button or timeout
// GameOver → Game:  "Restart" button (reload current scene)
```

**State-specific behavior:**

| State | Update loop | Render | Input handling |
|-------|------------|--------|---------------|
| **Menu** | Menu UI update | Menu drawing | Button selection, Start |
| **Game** | Full update (AI, combat, physics, events) | World + Entities + UI overlay | Player movement, attack, ability, pause |
| **Pause** | Menu UI update (overlay) | World (dimmed) + Menu overlay | Resume, Quit to menu |
| **GameOver** | GameOver screen update | Victory/defeat screen | Restart, Main Menu, Quit |

### 8.3 Scene Management

```csharp
// SceneManager handles loading/unloading levels
public class SceneManager : ISceneManager
{
    private IWorld? _currentWorld;
    private Entity? _playerEntity;

    public void LoadScene(string scenePath)
    {
        // 1. Unload previous
        UnloadScene();

        // 2. Create new world
        _currentWorld = _worldFactory.CreateFromJson(scenePath);

        // 3. Get player spawn position from level definition
        var levelDef = ParseLevelDef(scenePath);
        Vec3I spawn = levelDef.PlayerStart;

        // 4. Create player entity at spawn
        var playerDef = LoadPlayerDefinition();
        var playerDefData = playerDef.ComponentData["Transform"];
        playerDefData["X"] = spawn.X;
        playerDefData["Y"] = spawn.Y;
        playerDefData["Z"] = spawn.Z;
        _playerEntity = _entityRegistry.CreateFromDefinition(playerDef);

        // 5. Dispatch LevelLoaded event
        _dispatcher.Dispatch(new LevelLoaded(levelDef.Name, spawn));

        // 6. Camera follows player
        _camera.FollowEntity(_playerEntity);
    }

    public void UnloadScene()
    {
        if (_currentWorld != null)
        {
            _currentWorld = null;
            _playerEntity = null;
            _entityRegistry.ProcessDeaths();
            _assetManager.UnloadAll<MeshAsset>();
            _assetManager.UnloadAll<TextureAsset>();
        }
    }
}
```

---

## 9. PROTYP-SCOPE

### 9.1 Med i prototypen

Dessa moduler och funktioner **måste** finnas för att prototypen ska vara spelbar:

| Modul | Funktioner | Prioritet |
|-------|-----------|-----------|
| **Djurspel.Core** | EventDispatcher, AssetManager, Vec2/Vec3 types | **P0 — blocking** |
| **Djurspel.Graphics** | Isometric camera, 3 shaders (world/entity/overlay), primitive meshes, game window | **P0** |
| **Djurspel.World** | Tile grid (isometric), tile-to-screen, depth sorting, collision detection | **P0** |
| **Djurspel.Entities** | Entity (ID + components), IEntityRegistry, Transform/Health/Combat/Movement/Render components | **P0** |
| **Djurspel.Gameplay** | Player input → movement, Combat (attack/damage/death), AI (basic chase/attack) | **P0** |
| **Djurspel.Game** | Game loop (fixed 60Hz), state machine (Menu → Game → Pause → GameOver), scene loading | **P0** |
| **Djurspel.Program** | Bootstrap wiring, entry point | **P0** |

**Prototypens innehåll:**
```
Prototyp-spel:
├── 1 isometrisk level (64×64 tiles, 1 layer, ground + walls)
├── Spelare:
│   ├── Kan röra sig (WASD → tile movement)
│   ├── Kan attackera (mus-klick → nearest enemy in range)
│   ├── Har health bar (HUD overlay)
│   └── Kan dö
├── 3–5 fiender (vargar):
│   ├── Patrollerar eller chase:ar spelare
│   ├── Angriper när i range
│   ├── Har health
│   └── Droppar loot vid död
├── 1 treasure chest med loot
├── HUD:
│   ├── Spelare health bar
│   ├── Damage numbers (floating text)
│   └── Game over screen
└── Game over → Restart eller Main Menu
```

### 9.2 Väckande i prototypen (post-prototyp)

Dessa moduler och funktioner **väntar** tills efter prototypen:

| Modul | Funktioner | Prioritet |
|-------|-----------|-----------|
| **Djurspel.WorldGen** | Procedural generation, room-based dungeons, wilderness | **P3 — efter prototyp** |
| **Djurspel.UI** | Full HUD (inventory, skill bar, minimap, moral meter), menu system | **P2 — efter combat** |
| (ej modul) | Inventory system (full implementation) | **P2** |
| (ej modul) | Companion system (bond, betrayal, dialogue) | **P2** |
| (ej modul) | Advanced AI (behavior trees, flocking, tactics) | **P3** |
| (ej modul) | Moral choice system (Mass Effect-style) | **P2** |
| (ej modul) | Skill/tree system | **P3** |
| (ej modul) | Multi-level / dungeon crawler progression | **P3** |

### 9.3 Prototypens data-flöde (simplified)

```
┌─────────────────────────────────────────────────────────────┐
│                       Djurspel.Program                       │
│                                                              │
│  Bootstraps:                                                 │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌──────────────┐  │
│  │  Core   │→│ Graphics │→│ Entities│→│    World       │  │
│  └─────────┘  └──────────┘  └─────────┘  └──────────────┘  │
│       │                                            │        │
│       ▼                                            ▼        │
│  ┌─────────────────────┐       ┌──────────────────────────┐ │
│  │   Gameplay          │←─────→│         Game             │ │
│  │  ┌─────────────┐    │       │  ┌──────────────────┐   │ │
│  │  │ InputManager│    │       │  │ GameStateMachine │   │ │
│  │  └─────────────┘    │       │  └──────────────────┘   │ │
│  │  ┌─────────────┐    │       │  ┌──────────────────┐   │ │
│  │  │CombatManager│    │       │  │   Game Loop       │   │ │
│  │  └─────────────┘    │       │  │ (Fixed 60Hz)     │   │ │
│  │  ┌─────────────┐    │       │  └──────────────────┘   │ │
│  │  │   AIManager │    │       └──────────────────────────┘ │
│  │  └─────────────┘    │              ▲                     │
│  └─────────────────────┘              │                     │
│       │                              │                     │
│       ▼                              │                     │
│  ┌──────────────────────────────────┐│                     │
│  │      IEventDispatcher (all subsystems)│                │
│  └───────────────────────────────────┘│                    │
│                                        │                   │
└────────────────────────────────────────┼───────────────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    ▼                    ▼
              World Tiles           Entity Meshes          UI Overlay
              (isometric)           (3D models)            (HUD)
```

---

## B. IMPLEMENTERINGSORDNING

Rekommenderad sekvens för den som implementerar:

1. **Core** — Vec2/Vec3, EventDispatcher, AssetManager, primitive math
2. **Graphics** — GameWindow (OpenTK bootstrap), ShaderManager, Camera (iso math), Renderer, PrimitiveMeshFactory
3. **Entities** — Entity (components), EntityRegistry, all Component POCOs
4. **World** — TileData, IWorld (grid), Tile-to-Screen, Depth Sort, WorldFactory
5. **Gameplay** — InputManager, CombatManager, AIManager (basic), moral stubs
6. **Game** — GameStateMachine, GameLoop (fixed timestep), SceneManager
7. **Program** — Bootstrap wiring, Main entry point
8. **Testa prototypen** — Spela 5 minuter, fixa bugs

---

## C. ÖVRIGA DETALJER

### C.1 OpenTK/OpenGL Bootstrap (referens)

```csharp
// GameWindow.Create — OpenTK bootstrap:
public static IGameWindow Create(int width, int height, string title)
{
    // OpenTK 4.x bootstrap:
    var gameWindow = new GameWindow(
        width,
        height,
        new GameWindowSettings { NativeWindowSettings = { ApiVersion = new Version(4, 3) } }
    );

    gameWindow.Title = title;
    gameWindow.Resize += (s, e) => { /* update viewport */ };
    gameWindow.Closing += (s, e) => { /* set shouldClose */ };

    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    GL.Viewport(0, 0, width, height);

    return new GameWindowAdapter(gameWindow);
}
```

### C.2 Filstruktur (fullständig)

```
djurspel/
├── src/
│   ├── Djurspel.Core/
│   │   ├── Djurspel.Core.csproj
│   │   ├── IEvent.cs
│   │   ├── EventDispatcher.cs
│   │   ├── IAssetManager.cs
│   │   ├── AssetManager.cs
│   │   └── Math2D.cs        (Vec2, Vec3, Vec2I, Vec3I, Color, Vector3)
│   ├── Djurspel.Entities/
│   │   ├── Djurspel.Entities.csproj
│   │   ├── IComponent.cs
│   │   ├── Entity.cs
│   │   ├── IEntityRegistry.cs
│   │   ├── EntityDefinition.cs
│   │   └── Components/
│   │       ├── TransformComponent.cs
│   │       ├── HealthComponent.cs
│   │       ├── CombatComponent.cs
│   │       ├── MovementComponent.cs
│   │       ├── RenderComponent.cs
│   │       ├── AIComponent.cs
│   │       ├── PlayerComponent.cs
│   │       ├── LootComponent.cs
│   │       └── DialogueComponent.cs
│   ├── Djurspel.Graphics/
│   │   ├── Djurspel.Graphics.csproj
│   │   ├── IRenderer.cs
│   │   ├── Renderer.cs
│   │   ├── IShaderManager.cs
│   │   ├── ShaderManager.cs
│   │   ├── ICamera.cs
│   │   ├── IsometricCamera.cs
│   │   ├── IGameWindow.cs
│   │   ├── GameWindow.cs
│   │   ├── IPrimitiveMeshFactory.cs
│   │   ├── PrimitiveMeshFactory.cs
│   │   ├── Shaders/
│   │   │   ├── world.vert
│   │   │   ├── world.frag
│   │   │   ├── entity.vert
│   │   │   ├── entity.frag
│   │   │   ├── overlay.vert
│   │   │   └── overlay.frag
│   │   └── Types.cs          (Color, RenderBatchType, TileMesh)
│   ├── Djurspel.World/
│   │   ├── Djurspel.World.csproj
│   │   ├── IWorld.cs
│   │   ├── World.cs
│   │   ├── TileData.cs
│   │   ├── IWorldFactory.cs
│   │   ├── WorldFactory.cs
│   │   └── Types.cs          (TileType, TileCollision, TileDrawRegion)
│   ├── Djurspel.Gameplay/
│   │   ├── Djurspel.Gameplay.csproj
│   │   ├── ICombatManager.cs
│   │   ├── CombatManager.cs
│   │   ├── IAIManager.cs
│   │   ├── AIManager.cs
│   │   ├── IInputManager.cs
│   │   ├── InputManager.cs
│   │   ├── IMoralManager.cs
│   │   ├── MoralManager.cs
│   │   ├── IInventoryManager.cs
│   │   └── Types.cs          (CombatResult, MoralScore, InventorySlot)
│   ├── Djurspel.WorldGen/
│   │   ├── Djurspel.WorldGen.csproj
│   │   ├── IWorldGenerator.cs
│   │   └── Types.cs          (GeneratedLevel)
│   ├── Djurspel.UI/
│   │   ├── Djurspel.UI.csproj
│   │   ├── IHudManager.cs
│   │   ├── IMenuManager.cs
│   │   └── Types.cs          (MenuTransition, MenuAction)
│   ├── Djurspel.Game/
│   │   ├── Djurspel.Game.csproj
│   │   ├── IGameStateMachine.cs
│   │   ├── GameStateMachine.cs
│   │   ├── ISceneManager.cs
│   │   ├── SceneManager.cs
│   │   ├── IGameLoop.cs
│   │   ├── GameLoop.cs
│   │   └── Types.cs          (GameState)
│   └── Djurspel.Program/
│       ├── Djurspel.Program.csproj
│       └── Program.cs        (Main entry point)
├── assets/
│   ├── meshes/
│   │   ├── cube.obj
│   │   ├── sphere.obj
│   │   ├── cylinder.obj
│   │   ├── plane.obj
│   │   └── isometric_tile.obj
│   ├── textures/
│   │   ├── ground.tga
│   │   ├── wall.tga
│   │   └── player.tga
│   ├── entities/
│   │   ├── player.json
│   │   ├── wolf_enemy.json
│   │   ├── npc_companion.json
│   │   └── treasure.json
│   └── levels/
│       └── prototype_floor1.json
└── PLAN.md
```

---

## D. BEGREPPSORD (Gloslista)

| Term | Beskrivning |
|------|------------|
| **Tile** | En cell i det isometriska rutnätet (64×32 pixlar) |
| **Tile-to-Screen** | Koordinattransformation: (tileX, tileY, tileZ) → (screenX, screenY) |
| **Depth Sort** | Rendereringsordning baserad på tileX + tileY + tileZ |
| **Painter's Algorithm** | Rita längst bort först, närmast sist |
| **Component** | Ren datatyp (POCO) som beskriver en aspekt av en entity |
| **Entity** | Samling av komponenter med ett unikt ID |
| **Event** | Händelse-meddelande som dispatchas till alla lyssnare |
| **Asset** | Laddad fil (mesh, texture, shader, entity definition) i minnet |
| **Fixed Timestep** | Uppdatering med fast FPS (60 Hz) oavsett render-fps |
| **State Machine** | Tillståndsmaskin för game flow (Menu → Game → Pause → GameOver) |
| **Isometric** | 2.5D-projektion där x och y-axlar visas i 30-graders vinkel |
| **Moral Alignment** | Spelarens moraliska profil (Compassionate / Neutral / Ruthless) |

---

*Planen är komplett. Implementeraren börjar med Core → Graphics → Entities → World → Gameplay → Game → Program.*
