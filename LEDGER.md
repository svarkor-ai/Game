# Djurspel Build Ledger

## Goal
Headless-screenshot med färgat innehåll (inte svart)

## MC Job
- **Job 105**: `Djurspel Headless Rendering — GL Error 1282 Felsökning`
  - Status: `running` (skapad 2026-08-01)
  - Purpose: Fixa GL_INVALID_OPERATION (1282) som förhindrar rendering

## Known State
### Fixed
- [x] NaN i `TopDownCamera.GetViewMatrix` → `view.M11 = -1.00`
- [x] Index-overflow i `SpriteBatchRenderer` → `uint`-indices, `MaxIndices=98304`
- [x] `SpriteBatchRenderer.SetMatrices()` metod tillagd

### Blocked
- [ ] **GL_INVALID_OPERATION (1282)** före `DrawElements` i `SpriteBatchRenderer.EndBatch()`
  - Artemis dispatchad: `job-1785625336-17319` (2026-08-01 ~22:30)

### Screenshots
Alla screenshot är SVARTA (1 unik färg, ~1280x720px, 2.7MB)
- `/tmp/djurspel_v4.png`, `/tmp/djurspel_v5.png`, `/tmp/djurspel_debug.png` etc.

## Next Steps
- Vänta på Artemis resultat (job-1785625336-17319)
- Integrera fix
- Bygg + testa headless-screenshot
- Close job 105 + request-done-gate