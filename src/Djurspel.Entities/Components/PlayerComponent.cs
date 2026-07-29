namespace Djurspel.Entities.Components;

public enum MoralAlignment { Compassionate = 0, Neutral = 1, Ruthless = 2 }

public class PlayerComponent : IComponent
{
    public MoralAlignment Alignment { get; set; }
    public int Gold { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
}
