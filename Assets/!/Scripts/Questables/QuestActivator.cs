using UnityEngine;

public class QuestActivator
{
    private Player player;
    private PlayerUI playerUI;
    private int[] questIds;
    private Npc npcRef;

    public QuestActivator(Player player, PlayerUI playerUI, int[] questIds, Npc npc)
    {
        this.player = player;
        this.playerUI = playerUI;
        this.questIds = questIds;
        this.npcRef = npc;
        Debug.Log("[QuestActivator] Bol vytvorený pre NPC: " + npcRef.NpcName);
    }

    private int CheckForNonActiveQuests()
    {
        QuestManager questManager = QuestManager.Instance;
        foreach (int id in questIds)
        {
            Quest quest = questManager.GetQuestById(id);
            if (quest != null && quest.Status == QuestStatus.Inactive)
            {
                Debug.Log($"[QuestActivator] Našiel som inaktívny quest s ID {id}");
                return id;
            }
        }
        Debug.Log("[QuestActivator] Žiadny inaktívny quest sa nenašiel, vraciam -1");
        return -1;
    }

    public void ActivateQuest()
    {
        QuestManager questManager = QuestManager.Instance;
        int questId = CheckForNonActiveQuests();
        Quest quest = questManager.GetQuestById(questId);
        if (quest != null && !questManager.IsQuestActive(questId) && !questManager.IsQuestCompleted(questId))
        {
            playerUI.DisplayQuestUnlock(quest.questName);
            player.PlayerManagment.journal.ChangeQuestStatus(quest, QuestStatus.Active);
            quest.fromNpc = npcRef;
            Debug.Log($"[QuestActivator] Quest s ID {questId} bol aktivovaný pre NPC {npcRef.NpcName}");
        }
        else
        {
            Debug.Log($"[QuestActivator] Quest s ID {questId} nebol aktivovaný.");
        }
    }
}
