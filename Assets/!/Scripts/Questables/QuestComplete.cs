using UnityEngine;

public class QuestComplete
{
    private PlayerUI playerUI;
    private Npc npc;
    private int[] questIds;

    public QuestComplete(PlayerUI playerUI, Npc npc, int[] questIds)
    {
        this.playerUI = playerUI;
        this.npc = npc;
        this.questIds = questIds;
    }

    private int CheckForActiveQuests()
    {
        QuestManager questManager = QuestManager.Instance;
        foreach (int id in questIds)
        {
            if (questManager.GetQuestById(id).Status == QuestStatus.Active)
            {
                return id;
            }
        }
        return -1;
    }

    private bool IsLocalQuest(int questId)
    {
        foreach (int id in questIds)
        {
            if (id == questId)
                return true;
        }
        return false;
    }

    public void CompleteQuest(Journal journal)
    {
        int questId = CheckForActiveQuests();
        if (questId != -1)
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

                if (quest.fromNpc != null && quest.fromNpc != npc)
                {
                    quest.fromNpc.SwitchDialogueMode();
                    if (quest.fromNpc.QuestDialogueManager.CanIncreaseDialogueLevel())
                    {
                        quest.fromNpc.QuestDialogueManager.IncreaseDialogueLevel();
                    }
                    npc.SwitchDialogueMode();
                    Debug.Log($"[QuestComplete] NPC {quest.fromNpc.name} prešiel do casual módu.");
                }
                else
                {
                    npc.QuestDialogueManager.IncreaseDialogueLevel();
                    Debug.Log($"[QuestComplete] NPC {npc.name} prešiel do ďalšieho levelu quest dialógu.");
                }
            }
        }
    }
}
