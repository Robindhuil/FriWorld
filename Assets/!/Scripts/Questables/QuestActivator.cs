using UnityEngine;

public class QuestActivator
{
    private Player player;
    private PlayerUI playerUI;
    private string[] questIds;
    private Npc npcRef;

    public QuestActivator(Player player, PlayerUI playerUI, string[] questIds, Npc npc)
    {
        this.player = player;
        this.playerUI = playerUI;
        this.questIds = questIds;
        this.npcRef = npc;
        Debug.Log("[QuestActivator] Bol vytvorený pre NPC: " + npcRef.NpcName);
    }

    private string CheckForNonActiveQuests()
    {
        QuestManager questManager = QuestManager.Instance;
        foreach (string id in questIds)
        {
            Quest quest = questManager.GetQuestById(id);
            if (quest != null && quest.Status == QuestStatus.Inactive)
            {
                Debug.Log($"[QuestActivator] Našiel som inaktívny quest s ID {id}");
                return id;
            }
        }
        Debug.Log("[QuestActivator] Žiadny inaktívny quest sa nenašiel, vraciam null");
        return null;
    }

    public void ActivateQuest()
    {
        string questId = CheckForNonActiveQuests();
        if (!TryActivateQuest(player, playerUI, npcRef, questId))
        {
            Debug.Log($"[QuestActivator] Quest s ID {questId} nebol aktivovaný.");
        }
    }

    public static bool TryActivateQuest(Player player, PlayerUI playerUI, Npc npcRef, string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            Debug.LogWarning("[QuestActivator] Neplatné questId, aktivácia zrušená.");
            return false;
        }

        QuestManager questManager = QuestManager.Instance;
        Quest quest = questManager.GetQuestById(questId);
        if (quest == null)
        {
            Debug.LogWarning($"[QuestActivator] Quest s ID {questId} nebol nájdený.");
            return false;
        }

        if (questManager.IsQuestActive(questId) || questManager.IsQuestCompleted(questId))
        {
            Debug.Log($"[QuestActivator] Quest {questId} je už aktívny alebo dokončený.");
            return false;
        }

        quest.fromNpc = npcRef;
        playerUI?.DisplayQuestUnlock(quest.questName);
        player?.PlayerManagment?.journal?.ChangeQuestStatus(quest, QuestStatus.Active);

        // Set quest started state for dialogue system
        GameState.Instance.SetBool($"{questId}_Started", true);
        Debug.Log($"[QuestActivator] Quest {questId} started state set in GameState.");

        Debug.Log($"[QuestActivator] Quest s ID {questId} bol aktivovaný pre NPC {npcRef?.NpcName}");
        return true;
    }
}
