using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spravuje questy v hre a ich stavy.
/// </summary>
public class Journal
{
    private QuestManager questManager;

    public Dictionary<int, Transform> QuestPositions { get; private set; }

    public List<Quest> ActiveQuests { get; private set; }
    public List<Quest> InactiveQuests { get; private set; }
    public List<Quest> CompletedQuests { get; private set; }

    /// <summary>
    /// Inicializuje nový žurnál a načíta pozície questov.
    /// </summary>
    public Journal()
    {
        questManager = QuestManager.Instance;
        QuestPositions = new Dictionary<int, Transform>(questManager.QuestPositions);
        ActiveQuests = new List<Quest>();
        InactiveQuests = new List<Quest>();
        CompletedQuests = new List<Quest>();
    }

    /// <summary>
    /// Načíta všetky questy a nastaví ich ako neaktívne.
    /// </summary>
    public void LoadQuests()
    {
        foreach (var quest in questManager.Quests)
        {
            quest.SetInactive();
            InactiveQuests.Add(quest);
        }

        foreach (var kvp in questManager.QuestPositions)
        {
            QuestPositions[kvp.Key] = kvp.Value;
            Debug.Log($"[Journal] Journal added QuestPosition {kvp.Key} at {kvp.Value.position}");
        }

        Debug.Log($"[Journal] Loaded Quest Positions: {QuestPositions.Count}");
    }

    /// <summary>
    /// Vráti počet všetkých questov (aktívne, neaktívne a dokončené).
    /// </summary>
    public int GetAllQuestsCount()
    {
        return ActiveQuests.Count + InactiveQuests.Count + CompletedQuests.Count;
    }

    public int GetCompletedQuestsCount()
    {
        return CompletedQuests.Count;
    }

    /// <summary>
    /// Zmení stav questu a aktualizuje jeho zoznam.
    /// </summary>
    public void ChangeQuestStatus(Quest quest, QuestStatus newStatus)
    {
        RemoveQuestFromCurrentStatus(quest);

        quest.Status = newStatus;

        AddQuestToNewStatus(quest);

    }

    /// <summary>
    /// Skontroluje, či je quest aktívny.
    /// </summary>
    public bool IsQuestActive(int questId)
    {
        return ActiveQuests.Exists(quest => quest.id == questId);
    }

    /// <summary>
    /// Získa transformáciu questu podľa jeho ID.
    /// </summary>
    public Transform GetQuestTransform(int questId)
    {
        return questManager.GetQuestPosition(questId);
    }

    /// <summary>
    /// Nájde quest podľa jeho mena.
    /// </summary>
    public Quest FindQuestByName(string questName)
    {
        foreach (var quest in ActiveQuests)
        {
            if (quest.questName == questName)
            {
                return quest;
            }
        }
        foreach (var quest in InactiveQuests)
        {
            if (quest.questName == questName)
            {
                return quest;
            }
        }
        foreach (var quest in CompletedQuests)
        {
            if (quest.questName == questName)
            {
                return quest;
            }
        }
        return null;
    }

    /// <summary>
    /// Pomocná metóda na odstránenie questu z aktuálneho stavu.
    /// </summary>
    private void RemoveQuestFromCurrentStatus(Quest quest)
    {
        switch (quest.Status)
        {
            case QuestStatus.Active:
                ActiveQuests.Remove(quest);
                break;
            case QuestStatus.Inactive:
                InactiveQuests.Remove(quest);
                break;
            case QuestStatus.Completed:
                CompletedQuests.Remove(quest);
                break;
        }
    }

    /// <summary>
    /// Pomocná metóda na pridanie questu do nového stavu.
    /// </summary>
    private void AddQuestToNewStatus(Quest quest)
    {
        switch (quest.Status)
        {
            case QuestStatus.Active:
                ActiveQuests.Add(quest);
                break;
            case QuestStatus.Inactive:
                InactiveQuests.Add(quest);
                break;
            case QuestStatus.Completed:
                CompletedQuests.Add(quest);
                break;
        }
    }

    /// <summary>
    /// Loguje všetky questy do konzoly.
    /// </summary>
    private void LogAllQuests()
    {
        LogQuestList("Active Quests:", ActiveQuests);
        LogQuestList("Inactive Quests:", InactiveQuests);
        LogQuestList("Completed Quests:", CompletedQuests);
    }

    /// <summary>
    /// Loguje konkrétny zoznam questov.
    /// </summary>
    private void LogQuestList(string title, List<Quest> quests)
    {
        Debug.Log(title);
        foreach (var quest in quests)
        {
            Debug.Log($"[Journal] {quest.id}: {quest.questName}");
            if (QuestPositions.TryGetValue(quest.id, out Transform questTransform))
            {
                if (questTransform != null)
                {
                    Debug.Log($"[Journal] Position: {questTransform.position}");
                }
            }
        }
    }
}
