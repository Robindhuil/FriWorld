using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Npc : MonoBehaviour
{
    public StateMachine stateMachine;
    public NavMeshAgent Agent { get; set; }
    private PlayerUI playerUI;
    private DialogueUI dialogueUI;
    private QuestManager questManager;

    public NodeDialogueManager QuestDialogueManager { get; private set; }
    public NodeDialogueManager CurrentDialogueManager { get; private set; }

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

    [Header("Complete quest of this npc")]
    [SerializeField] private string[] questIds;
    private QuestComplete questComplete;
    private float rotationSpeed = 5f;

    private Player player;
    public Animator Animator { get; set; }


    void Start()
    {
        Animator = GetComponentInChildren<Animator>();
        InitializeComponents();

        // Check if GameState exists
        if (GameState.Instance == null)
        {
            Debug.LogError("[NPC] GameState.Instance is null! Please add a GameState component to a GameObject in your scene.");
        }

        QuestDialogueManager = new NodeDialogueManager();

        if (canCommunicate)
        {
            QuestDialogueManager.LoadDialogue(dialogueFileQuest, GameState.Instance);
        }

        CurrentDialogueManager = QuestDialogueManager;

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

    public void StartDialogue()
    {
        if (!canCommunicate || isInDialogue)
            return;

        questComplete.CompleteQuest(player.PlayerManagment.journal);
        isInDialogue = true;
        StopAgent();
        FacePlayer();
        stateMachine.ChangeState(new DialogueState());
        
        if (CurrentDialogueManager.StartDialogue(out string npcText, out string[] playerChoices))
        {
            playerUI?.CloseWindow();
            dialogueUI?.SetNpc(this);
            dialogueUI?.OpenWindow();
            dialogueUI?.UpdateDialogue(npcText, NpcName);
            dialogueUI?.UpdateOptionButtons(GetValidChoicesForUi());
            ProcessCurrentNodeEntryEffects();
        }
        else
        {
            Debug.LogWarning($"[NPC] Failed to start dialogue for {NpcName}");
            StopDialogue();
        }
    }

    public void ContinueDialogue(int choiceIndex)
    {
        Debug.Log($"[NPC] Pokračujem v dialógu. Index voľby: {choiceIndex}");
        
        // Get the current node before selecting choice to check for quest effects
        var currentNode = CurrentDialogueManager.CurrentNode;
        
        if (currentNode == null)
        {
            Debug.LogError("[NPC] Current node is NULL before SelectChoice!");
            return;
        }
        
        Debug.Log($"[NPC] Current node ID: {currentNode.Id}, Type: {currentNode.Type}");
        
        if (GameState.Instance == null)
        {
            Debug.LogError("[NPC] GameState.Instance is NULL in ContinueDialogue!");
            return;
        }
        
        var validChoices = currentNode.GetValidChoices(GameState.Instance);
        Debug.Log($"[NPC] Node has {validChoices.Count} valid choices, selecting index {choiceIndex}");
        
        if (choiceIndex < 0 || choiceIndex >= validChoices.Count)
        {
            Debug.LogError($"[NPC] Invalid choice index {choiceIndex}, valid range is 0-{validChoices.Count - 1}");
            return;
        }
        
        var selectedChoice = validChoices[choiceIndex];
        Debug.Log($"[NPC] Selected choice: '{selectedChoice.Text}' -> NextNode: {selectedChoice.NextNodeId}");
        Debug.Log($"[NPC] Choice has {selectedChoice.Effects?.Count ?? 0} effects");
        
        // Process quest effects BEFORE SelectChoice (which might end the dialogue)
        ProcessChoiceEffects(selectedChoice);
        
        if (CurrentDialogueManager.SelectChoice(choiceIndex, out string npcLine, out string[] playerChoices))
        {
            Debug.Log($"[NPC] Nová replika: {npcLine}");
            
            dialogueUI.UpdateDialogue(npcLine, NpcName);
            dialogueUI.UpdateOptionButtons(GetValidChoicesForUi());
            ProcessCurrentNodeEntryEffects();
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

        isInDialogue = false;
        Agent.updateRotation = true;
        playerUI?.OpenWindow();

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

    /// <summary>
    /// Processes effects from a dialogue choice, particularly quest-related effects
    /// </summary>
    private void ProcessChoiceEffects(DialogueChoice choice)
    {
        if (choice == null)
        {
            Debug.LogError("[NPC] ProcessChoiceEffects: choice is NULL!");
            return;
        }
        
        if (choice.Effects == null || choice.Effects.Count == 0)
        {
            Debug.Log($"[NPC] Choice '{choice.Text}' has no effects");
            return;
        }

        Debug.Log($"[NPC] Processing {choice.Effects.Count} effects from choice '{choice.Text}'");
        
        if (GameState.Instance == null)
        {
            Debug.LogError("[NPC] GameState.Instance is NULL in ProcessChoiceEffects!");
            return;
        }
        
        ProcessEntryEffects(choice.Effects);

        foreach (var effect in choice.Effects)
        {
            Debug.Log($"[NPC] Effect - Type: {effect.Type}, Key: '{effect.Key}', Value: {effect.Value}");
            
            if (effect.Type == EffectType.StartQuest)
            {
                string key = effect.Key;
                if (key.StartsWith("quest_"))
                {
                    Debug.Log($"[NPC] Quest ID from effect key '{effect.Key}': {key}");
                    Debug.Log($"[NPC] Calling ActivateQuest({key})...");
                    ActivateQuest(key);
                }
                else
                {
                    Debug.LogWarning($"[NPC] Unknown StartQuest key prefix: '{key}'");
                }
            }
            else
            {
                Debug.Log($"[NPC] Effect type {effect.Type} is not StartQuest, skipping quest activation");
            }
        }
    }

    private void ProcessCurrentNodeEntryEffects()
    {
        var node = CurrentDialogueManager?.CurrentNode;
        if (node == null)
        {
            return;
        }

        ProcessEntryEffects(node.Effects);
    }

    private void ProcessEntryEffects(List<Effect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return;
        }

        foreach (var effect in effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.Key))
            {
                continue;
            }

            if (effect.Key.StartsWith("entry_"))
            {
                Debug.Log($"[NPC] Entry effect detected: '{effect.Key}', unlocking entry.");
                EntryActivator.ActivateEntryById(effect.Key, playerUI);
            }
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


    private void ActivateQuest(string questId)
    {
        Debug.Log($"[NPC] ActivateQuest called with ID: {questId}");
        if (!QuestActivator.TryActivateQuest(player, playerUI, this, questId))
        {
            Debug.Log($"[NPC] Quest {questId} is already active or completed");
        }
    }

    private List<DialogueChoice> GetValidChoicesForUi()
    {
        if (CurrentDialogueManager?.CurrentNode == null || GameState.Instance == null)
        {
            return new List<DialogueChoice>();
        }

        return CurrentDialogueManager.CurrentNode.GetValidChoices(GameState.Instance);
    }

}