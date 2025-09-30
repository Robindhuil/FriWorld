using UnityEngine;
using System.Collections.Generic;
using System;

public class QuestManager
{
    private static QuestManager instance;

    public static QuestManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new QuestManager();
            }
            return instance;
        }
    }

    private List<Quest> quests = new List<Quest>();
    private Dictionary<int, Transform> questPositions = new Dictionary<int, Transform>();
    private string questFilePath = "Quests/questList";
    public List<Quest> Quests => quests;
    public Dictionary<int, Transform> QuestPositions => questPositions;

    private QuestManager()
    {
        LoadQuests();
    }

    /// <summary>
    /// Načíta úlohy z definovaného súboru
    /// </summary>
    private void LoadQuests()
    {
        var questLoader = new QuestLoader(questFilePath);
        quests = questLoader.LoadQuests();
        Debug.Log($"[QuestLoader] Celkový počet načítaných úloh do QuestManager: {quests.Count}");
    }

    /// <summary>
    /// Nastaví pozíciu úlohy podľa ID.
    /// </summary>
    public void SetQuestPosition(int questId, Transform position)
    {
        if (!questPositions.ContainsKey(questId))
        {
            questPositions.Add(questId, position);
            //Debug.Log($"[QuestLoader] Pozícia úlohy {questId} nastavená: {position.position}");
        }
        else
        {
            Debug.LogWarning($"[QuestLoader] Úloha {questId} už má nastavenú pozíciu.");
        }
    }

    /// <summary>
    /// Získa pozíciu úlohy podľa ID.
    /// </summary>
    public Transform GetQuestPosition(int questId)
    {
        if (questPositions.TryGetValue(questId, out Transform position))
        {
            return position;
        }
        Debug.Log($"[QuestLoader] Pozícia úlohy pre ID {questId} nebola nájdená!");
        return null;
    }

    /// <summary>
    /// Získa úlohu podľa ID.
    /// </summary>
    public Quest GetQuestById(int id)
    {
        Quest quest = quests.Find(q => q.id == id);
        if (quest != null)
        {
            //Debug.Log($"[QuestLoader] Úloha nájdená: {quest.questName} - {quest.questObjective}");
        }
        else
        {
            Debug.LogWarning($"[QuestLoader] Úloha s ID: {id} nebola nájdená.");
        }
        return quest;
    }


    public bool IsQuestActive(int questId)
    {
        Quest quest = quests.Find(q => q.id == questId);
        if (quest != null && quest.Status == QuestStatus.Active)
        {
            return true;
        }
        return false;
    }

    internal bool IsQuestCompleted(int questId)
    {
        Quest quest = quests.Find(q => q.id == questId);
        if (quest != null && quest.Status == QuestStatus.Completed)
        {
            return true;
        }
        return false;
    }
}
