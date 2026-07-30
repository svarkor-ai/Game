# Djurspel Build Ledger

## Goal
Isometric 3D game med OpenTK 4 — spelare syns med sprite, rendering fungerar i headless Xvfb.

## Acceptance
1. Programmet startar utan krasch ✅ VERIFIED
2. Skärmen visar spelvärlden (tiles + player) — inte svart ❌ BLOCKED
3. Commit och push till GitHub ⏳

## Module Status
- Core/Entities ✅ VERIFIED
- Graphics/Renderer ✅ VERIFIED (no crashes, shaders compile)
- Graphics/GameWindow ✅ VERIFIED  
- Game/GameLoop ✅ VERIFIED
- **Rendering output ❌ BLACK SCREEN — under investigation**

## Open Questions
- Why is the screen black? Shaders have uView/uProjection uniforms but they're never set
- Camera and player at same position (0,0,0)
- Depth test enabled — could be hiding everything
