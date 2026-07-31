# Djurspel ARPG — Build Instructions

## GOAL
Konvertera Djurspel från isometrisk 3D till 2D top-down ARPG i Diablo/Path of Exile-stil.

## ACCEPTANS
- `dotnet build` → 0 errors
- Xvfb screenshot visar 2D top-down scene med:
  - Spelare (blå rektangel) rörlig med WASD
  - 2 fiender (roda rektanglar) som wanderar/attackar
  - Loot (gula rektanglar) som dropar och plockas upp med E
  - Dungeon-miljö (2D tiles: golv + väggar)
  - UI overlay (health bar, inventory)

## BEFINTLIG KOD (ÅTERANVÄND)
Se PLAN.md för full lista. 60%+ funktionalitet finns redan.

## VAD SOM BYGGES
Se PLAN.md för detaljerad fil-plan med 9 steg.

## BUILD ORDER (DEPENDENSER)
1. TopDownCamera.cs (Core) — ingen dependens
2. SpriteBatchRenderer.cs (Graphics) — Core + Graphics
3. Renderer patches (Graphics) — SpriteBatchRenderer
4. Renderer patches (Graphics) — SpriteBatchRenderer
5. EnemyAI.cs (Gameplay) — Core + Entities + Gameplay
6. LootDropSystem.cs (Gameplay) — Core + Entities + Gameplay
7. UIManager.cs (Gameplay) — Core + Entities + Gameplay
8. ARPGInputManager.cs (Gameplay) — Core + Gameplay
9. ARPGGameBootstrapper.cs (Game) + PATCH Program.cs + GameLoop

## IMPLEMENTATION NOTES
- Använd OpenTK 4, OpenGL 3.3 Core
- Headless: Xvfb + llvmpipe
- ECS: Transform, Health, Damage, Combat, Movement, AI, Render, Player, Loot
- Phaser 1: färgade rektanglar (inga externa assets)
- En funktion per fil, modulär kod
- Skriv verifierad kod — testa med dotnet build

## PATH
/workspace/src/

## BRANCH
main (github.com/svarkor-ai/Game.git)
