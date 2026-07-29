using System.Collections.Generic;

namespace Djurspel.Entities;

public class EntityDefinition
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "Entity";
    public Dictionary<string, object> ComponentData { get; } = new();
}
