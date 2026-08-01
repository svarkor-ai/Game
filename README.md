# 🎮 Djurspel 2D ARPG — PoC Konvertering

> **PoC**: Konvertera Djurspel 3D-firstperson till 2D top-down ARPG i Diablo/Path of Exile-stil.
> **Status**: Bygg klar ✅ | OpenGL-verifiering pågår

## Arkitektur

```
Djurspel.sln (8 projekt)
├── Djurspel.Core       — AssetManager, ECS-bas
├── Djurspel.Entities   — Entity + Components
├── Djurspel.World      — Världshanterare
├── Djurspel.WorldGen   — Procedural generering
├── Djurspel.Graphics   — OpenGL-rendering (llvmpipe/Xvfb)
├── Djurspel.Gameplay   — ARPG-mekanik, loot, fiender
├── Djurspel.Game       — Spelloop, scen
└── Djurspel.Program    — Entrépunkt
```

**Teknikstack**: C# 12 · .NET 8 · OpenTK 4.8 · OpenGL · ECS-arkitektur

## Bygg & Kör

### Förutsättningar
- .NET 8 SDK (`~/.dotnet/dotnet`)
- OpenGL-drivrutiner (llvmpipe för headless)
- Xvfb för headless rendering

### Snabbstart
```bash
# Bygg hela lösningen
dotnet build src/Djurspel.sln

# Minimal OpenGL-test
dotnet build minimal-test.csproj

# Kör med Xvfb (headless)
./run_and_screenshot.sh
```

## Acceptanskriterier (testbara)
- A1: `dotnet build` → 0 errors ✅
- A2: Headless Xvfb + OpenGL → 10 frames, OpenGL 4.5 Core (Mesa) ✅
- A3: ECS-arkitektur bevaras ✅
- A4: Inga externa assets — färgade rektanglar OK för Phase 1

### Verifiering
```bash
# Minimal OpenGL-test (headless-friendly, 10 frames)
xvfb-run --auto-servernum dotnet run --project minimal-test.csproj
# Output: "OpenGL version: 4.5 (Core Profile) Mesa ..." + 10 frames
```

## PoC-mål
- Spelare (blå rektangel) kan röra sig med WASD
- 2 fiender (röda rektanglar) wanderar och attackerar
- Loot (gula rektanglar) dropar från döda fiender
- Dungeon-miljö med 2D tiles (golv+väggar)
