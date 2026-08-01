using System.Collections.Generic;

namespace Djurspel.Gameplay;

/// <summary>Quest-system for uppdrag och belöningar.</summary>
public class QuestSystem
{
    public record Quest(string Title, string Description, int RequiredKills, int RewardGold, bool Completed);
    public record QuestProgress(string Title, int CurrentKills, int RequiredKills, bool Completed);

    private readonly List<Quest> _quests = new();
    private int _totalKills = 0;

    public QuestSystem()
    {
        // Add initial quests
        AddQuest(new Quest("Börja jakten", "Döda 3 fiender för att börja din resa.", 3, 50, false));
        AddQuest(new Quest("Erfaren jakare", "Döda 10 fiender.", 10, 100, false));
        AddQuest(new Quest("Mästare jakare", "Döda 25 fiender.", 25, 250, false));
    }

    public void AddQuest(Quest quest)
    {
        _quests.Add(quest);
    }

    public void TrackKill()
    {
        _totalKills++;
        
        // Check if any quests are completed
        foreach (var quest in _quests)
        {
            if (!quest.Completed && _totalKills >= quest.RequiredKills)
            {
                CompleteQuest(quest.Title);
            }
        }
    }

    public void CompleteQuest(string title)
    {
        foreach (var quest in _quests)
        {
            if (quest.Title == title && !quest.Completed)
            {
                _quests[_quests.IndexOf(quest)] = quest with { Completed = true };
                Console.Error.WriteLine($"[Quest] Completed: {quest.Title} - Got {quest.RewardGold} gold!");
                break;
            }
        }
    }

    public QuestProgress[] GetProgress()
    {
        var progresses = new List<QuestProgress>();
        foreach (var quest in _quests)
        {
            progresses.Add(new QuestProgress(
                quest.Title,
                Math.Min(_totalKills, quest.RequiredKills),
                quest.RequiredKills,
                quest.Completed
            ));
        }
        return progresses.ToArray();
    }

    public int TotalKills => _totalKills;
}
