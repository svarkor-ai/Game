using OpenTK.Mathematics;
using Djurspel.Core;

namespace Djurspel.Graphics;

/// <summary>
/// Interface för spritebatch-rendering.
/// Abstraherar batch-rendering av 2D-sprites med färger och transformationer.
/// </summary>
public interface ISpriteBatchRenderer : IDisposable
{
    /// <summary>
    /// Updates projection and view matrices. Call every frame when camera moves.
    /// </summary>
    void SetMatrices(Matrix4 projMatrix, Matrix4 viewMatrix);
    
    /// <summary>
    /// Begins a new sprite batch. All subsequent DrawQuad calls are batched until EndBatch().
    /// </summary>
    void BeginBatch();
    
    /// <summary>
    /// Ends the current batch and submits it to the GPU for rendering.
    /// </summary>
    void EndBatch();
    
    /// <summary>
    /// Draws a single quad in the current batch.
    /// </summary>
    /// <param name="position">World position (X, Y)</param>
    /// <param name="size">Quad dimensions (width, height)</param>
    /// <param name="color">Quad color with alpha (R, G, B, A)</param>
    void DrawQuad(Vector2 position, Vector2 size, Vector4 color);
    
    /// <summary>
    /// Sets the default color for subsequent DrawQuad calls.
    /// </summary>
    void SetColor(Vector4 color);
}
