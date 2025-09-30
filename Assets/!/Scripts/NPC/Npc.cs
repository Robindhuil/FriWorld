using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum DialogueMode { Quest, Casual }

public class Npc : MonoBehaviour
{
    public StateMachine stateMachine;
    public NavMeshAgent Agent { get; set; }
    private PlayerUI playerUI;
    private DialogueUI dialogueUI;
    private QuestManager questManager;

    public DialogueManager QuestDialogueManager { get; private set; }
    public DialogueManager CasualDialogueManager { get; private set; }
    public DialogueManager CurrentDialogueManager { get; private set; }

    public bool isInDialogue = false;

    [Header("NPC Information")]
    [SerializeField] private string npcName;
    public string NpcName => npcName;

    [Header("NPC Movement")]
    [SerializeField] public bool canMove = false;
    [SerializeField] public bool randomMovement = false;

    public PathWay path;
    public int nextWaypointIndex;

    [Header("NPC Communication")]
    [SerializeField] public bool canCommunicate = false;
    [SerializeField] private TextAsset dialogueFileQuest;
    [SerializeField] private TextAsset dialogueFileCasual;

    [Header("Default Dialogue Settings")]
    [SerializeField] private DialogueMode defaultDialogueMode = DialogueMode.Quest;

    [Header("Complete quest of this npc")]
    [SerializeField] private int[] questIds;
    private QuestComplete questComplete;
    private float rotationSpeed = 5f;

    private Player player;
    public Animator Animator { get; set; }


    void Start()
    {
        Animator = GetComponentInChildren<Animator>();
        InitializeComponents();

        QuestDialogueManager = new DialogueManager();
        CasualDialogueManager = new DialogueManager();

        if (canCommunicate)
        {
            QuestDialogueManager.LoadDialogue(dialogueFileQuest);
            CasualDialogueManager.LoadDialogue(dialogueFileCasual);
        }

        CurrentDialogueManager = (defaultDialogueMode == DialogueMode.Quest) ?
                                    QuestDialogueManager : CasualDialogueManager;

        if (canMove)
        {
            stateMachine.ChangeState(new WonderState());
        }

        questComplete = new QuestComplete(playerUI, this, questIds);
    }

    private void InitializeComponents()
    {
        stateMachine = GetComponent<StateMachine>();
        Agent = GetComponent<NavMeshAgent>();
        stateMachine.Initialise();

        player = FindFirstObjectByType<Player>();
        UIManager uIManager = FindFirstObjectByType<UIManager>();

        playerUI = uIManager.playerUI;
        dialogueUI = uIManager.dialogueUI;
        questManager = QuestManager.Instance;
    }

    /// <summary>
    /// Prepína aktuálny režim dialógu medzi Quest a Casual.
    /// </summary>
    public void SwitchDialogueMode()
    {
        if (CurrentDialogueManager == QuestDialogueManager)
        {
            CurrentDialogueManager = CasualDialogueManager;
            CasualDialogueManager.ResetDialogue();
        }
        else
        {
            CurrentDialogueManager = QuestDialogueManager;
            QuestDialogueManager.ResetDialogue();
        }
    }

    public void StartDialogue()
    {
        if (!canCommunicate || isInDialogue)
            return;

        questComplete.CompleteQuest(player.PlayerManagment.journal);
        isInDialogue = true;
        CurrentDialogueManager.ResetDialogue();
        StopAgent();
        FacePlayer();
        stateMachine.ChangeState(new DialogueState());
        UpdateUIForDialogue();
    }

    public void ContinueDialogue(int choiceIndex)
    {
        Debug.Log($"[NPC] Pokračujem v dialógu. Index voľby: {choiceIndex}");
        if (CurrentDialogueManager.ContinueDialogue(choiceIndex, out string npcLine, out string[] playerChoices))
        {
            Debug.Log($"[NPC] Nová replika: {npcLine}");
            dialogueUI.UpdateDialogue(npcLine, NpcName);
            dialogueUI.UpdateOptionButtons(playerChoices);
        }
        else
        {
            Debug.LogWarning("[NPC] Dialóg skončil alebo neexistuje ďalšia replika.");
            StopDialogue();
            dialogueUI?.CloseWindow();
        }
    }

    public void StopDialogue()
    {
        if (!isInDialogue)
            return;

        CodexEntry npcEntry = Codex.Instance.Entries.FirstOrDefault(e => e.name == NpcName);
        if (npcEntry != null && !Codex.Instance.IsEntryUnlocked(npcEntry))
        {
            Codex.Instance.UnlockEntry(npcEntry);
            playerUI?.DisplayCodexUnlock(NpcName);
            Debug.Log($"[NPC] Odomkol sa NPC entry: {NpcName}");
        }

        isInDialogue = false;
        Agent.updateRotation = true;
        playerUI?.OpenWindow();

        Debug.Log($"[NPC] Aktivujem quest ID: {CurrentDialogueManager.LastValidQuestId}");
        ActivateQuest(CurrentDialogueManager.LastValidQuestId);
        Invoke(nameof(ResumeStateAfterDialogue), 1f);
    }

    private void StopAgent()
    {
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
    }

    private void FacePlayer()
    {
        Agent.updateRotation = false;

        Transform playerTransform = FindFirstObjectByType<PlayerInteract>().transform;
        Vector3 direction = (playerTransform.position - transform.position).normalized;

        Vector3 lookDirection = new Vector3(direction.x, 0, direction.z);

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void Update()
    {
        if (isInDialogue)
        {
            FacePlayer();
        }
    }

    private void UpdateUIForDialogue()
    {
        playerUI?.CloseWindow();
        dialogueUI?.SetNpc(this);
        dialogueUI?.OpenWindow();
        var currentDialogue = CurrentDialogueManager.GetCurrentDialogue();
        if (currentDialogue != null)
        {
            dialogueUI?.UpdateDialogue(currentDialogue.text, NpcName);
            dialogueUI?.UpdateOptionButtons(currentDialogue.choices?.Select(c => c.text).ToArray() ?? new string[0]);
        }
    }

    private void ResumeStateAfterDialogue()
    {
        if (canMove)
        {
            Agent.isStopped = false;
            stateMachine.ChangeState(new WonderState { waypointIndex = nextWaypointIndex });
        }
        else
        {
            stateMachine.ChangeState(new IdleState());
        }
    }


    private void ActivateQuest(int questId)
    {
        var quest = questManager.GetQuestById(questId);
        if (quest != null && !questManager.IsQuestActive(questId) && !questManager.IsQuestCompleted(questId))
        {
            quest.fromNpc = this;
            playerUI?.DisplayQuestUnlock(quest.questName);
            player.PlayerManagment.journal.ChangeQuestStatus(quest, QuestStatus.Active);
        }

        EntryActivator entryActivator = GetComponent<EntryActivator>();
        if (entryActivator != null)
        {
            entryActivator.ActivateEntryForQuest(questId);
        }
    }

}