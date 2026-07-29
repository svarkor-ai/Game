namespace Djurspel.Entities.Components;

public class DialogueComponent : IComponent
{
    public string DialogueFile { get; set; } = "";
    public int RelationshipScore { get; set; }
    public bool IsCompanion { get; set; }
}
