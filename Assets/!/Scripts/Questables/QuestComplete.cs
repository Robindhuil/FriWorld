using UnityEngine;

public class QuestComplete
{
    private PlayerUI playerUI;
    private Npc npc;
    private string[] questIds;

    public QuestComplete(PlayerUI playerUI, Npc npc, string[] questIds)
    {
        this.playerUI = playerUI;
        this.npc = npc;
        this.questIds = questIds;
    }

    private string CheckForActiveQuests()
    {
        QuestManager questManager = QuestManager.Instance;
        foreach (string id in questIds)
        {
            if (questManager.GetQuestById(id).Status == QuestStatus.Active)
            {
                return id;
            }
        }
        return null;
    }

    private bool IsLocalQuest(string questId)
    {
        foreach (string id in questIds)
        {
            if (id == questId)
                return true;
        }
        return false;
    }

    public void CompleteQuest(Journal journal)
    {
        string questId = CheckForActiveQuests();
        if (questId != null)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questId);
            if (quest == null)
            {
                Debug.LogWarning($"[QuestComplete] Quest with ID {questId} not found.");
                return;
            }
            journal.ChangeQuestStatus(quest, QuestStatus.Completed);
            Debug.Log($"[QuestComplete] Quest {questId} completed.");
            playerUI.DisplayQuestComplete(quest.questName);

            if (IsLocalQuest(questId))
            {
                // Set quest completion state for dialogue system
                GameState.Instance.SetBool($"{questId}_Completed", true);
                Debug.Log($"[QuestComplete] Quest {questId} completion state set in GameState.");
                
                // Node-based dialogue system automatically handles progression via conditions
                Debug.Log($"[QuestComplete] NPC {npc.name} dialogue will update based on quest completion.");
            }
        }
    }
}
