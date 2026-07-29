namespace Djurspel.Entities.Components;

using Djurspel.Core;
using Djurspel.Entities;

/// <summary>
/// Render component — controls whether an entity is visible and its render order.
/// </summary>
public class RenderComponent : IComponent
{
    public bool Visible { get; set; } = true;
    public int RenderOrder { get; set; } = 0;
    public string? SpriteName { get; set; }
    public Vec3I? HighlightedTile { get; set; }

    public void Render()
    {
        // Stub — actual rendering is handled by the Graphics module
    }

    public void MarkAsSelected(bool selected)
    {
        // Stub — highlight selected entity
    }
}