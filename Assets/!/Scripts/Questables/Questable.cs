using UnityEngine;

public class Questable : Interactable
{
    [Header("Quest Settings")]
    [Tooltip("ID úlohy priradený tomuto objektu.")]
    [SerializeField]
    protected int questId;
    [SerializeField]
    [Tooltip("NPC, ktorý je súčasťou úlohy.")]
    private PlayerUI playerUI;
    public Journal Journal { get; private set; }

    public bool CanBeInteracted { get; set; } = true;
    private UIManager uIManager;
    [Header("Quests to activate")]
    [SerializeField] private int[] questIds;
    private QuestActivator questActivator;
    private Npc npc;

    private void Awake()
    {
        if (IsValidQuest())
        {
            QuestManager.Instance.SetQuestPosition(questId, transform);
            Debug.Log($"[Questable] Quest position for ID {questId} set at {transform.position}.");
        }
        else
        {
            Debug.LogError($"[Questable] Invalid questId ({questId}) or missing transform.");
        }
    }

    private bool IsValidQuest()
    {
        return questId > 0 && transform != null;
    }

    protected override void Interact()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("[Questable] Player not found.");
            CanBeInteracted = false;
            return;
        }
        Journal = player.PlayerManagment.journal;
        if (Journal == null)
        {
            Debug.LogWarning("[Questable] Journal is null.");
            CanBeInteracted = false;
            return;
        }
        if (!Journal.IsQuestActive(questId))
        {
            Debug.Log("[Questable] Quest is not active.");
            CanBeInteracted = false;
            return;
        }
        CanBeInteracted = true;
    }

    protected virtual void CompleteQuest(Journal journal)
    {
        uIManager = FindFirstObjectByType<UIManager>();
        playerUI = uIManager.playerUI;
        Quest quest = QuestManager.Instance.GetQuestById(questId);
        if (quest == null)
        {
            Debug.LogWarning($"[Questable] Quest with ID {questId} not found.");
            return;
        }
        npc = quest.fromNpc;

        Player player = FindFirstObjectByType<Player>();
        questActivator = new QuestActivator(player, playerUI, questIds, npc);
        questActivator.ActivateQuest();

        journal.ChangeQuestStatus(quest, QuestStatus.Completed);
        Debug.Log($"[Questable] Quest {questId} completed.");
        playerUI.DisplayQuestComplete(quest.questName);

        Navigation nav = FindFirstObjectByType<Navigation>();

        if (nav.QuestDestination != null)
        {
            nav.ClearQuestPath();
            nav.DrawQuestLine = false;
        }
    }
}
