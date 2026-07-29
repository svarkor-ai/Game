namespace Djurspel.Core;

/// <summary>
/// Event dispatched every fixed timestep to request AI updates.
/// </summary>
public record AIUpdateEvent(double DeltaTime) : IEvent;