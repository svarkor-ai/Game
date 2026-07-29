using Djurspel.Core;

namespace Djurspel.Gameplay;

/// <summary>
/// Concrete implementation of IMoralManager — tracks moral decisions and alignment.
/// </summary>
public class MoralManager : IMoralManager
{
    private readonly IEventDispatcher _dispatcher;
    private readonly List<MoralDecision> _decisions = new();
    private int _compassionateScore;
    private int _ruthlessScore;

    public MoralManager(IEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void RecordDecision(int decisionId, MoralAlignment choice, int? companionAffected)
    {
        switch (choice)
        {
            case MoralAlignment.Compassionate:
                _compassionateScore++;
                break;
            case MoralAlignment.Ruthless:
                _ruthlessScore++;
                break;
        }

        _decisions.Add(new MoralDecision(decisionId, choice, companionAffected));
    }

    public MoralScore GetScore()
    {
        return new MoralScore
        {
            Compassionate = _compassionateScore,
            Ruthless = _ruthlessScore
        };
    }

    public bool TriggersBetrayal(int decisionId, int companionId)
    {
        var decision = _decisions.FirstOrDefault(d => d.Id == decisionId);
        if (decision == null) return false;

        // Ruthless decisions can trigger betrayal
        return decision.Choice == MoralAlignment.Ruthless;
    }

    public void Dispose()
    {
        _decisions.Clear();
    }

    private record MoralDecision(int Id, MoralAlignment Choice, int? Companion);
}
