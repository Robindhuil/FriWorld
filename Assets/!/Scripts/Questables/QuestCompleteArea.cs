using UnityEngine;

public class QuestCompleteArea : MonoBehaviour
{
    [Header("Quest Completion Settings")]
    [Tooltip("ID questov, ktoré sa majú dokončiť")]
    [SerializeField] private int[] questIds;

    [Header("Collider Settings")]
    [Tooltip("Referencia na collider komponent")]
    [SerializeField] private Collider triggerCollider;

    private void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("[QuestCompleteArea] QuestCompleteArea vyžaduje collider komponent", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && player.PlayerManagment != null && player.PlayerManagment.journal != null)
            {
                CompleteQuests(player.PlayerManagment.journal);
                Destroy(gameObject);
            }
        }
    }

    private void CompleteQuests(Journal journal)
    {
        foreach (int questId in questIds)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questId);
            if (quest != null && quest.Status == QuestStatus.Active)
            {
                journal.ChangeQuestStatus(quest, QuestStatus.Completed);
                Debug.Log($"[QuestCompleteArea] Quest {questId} bol dokončený cez QuestCompleteArea");

                UIManager uiManager = FindFirstObjectByType<UIManager>();
                if (uiManager != null && uiManager.playerUI != null)
                {
                    uiManager.playerUI.DisplayQuestComplete(quest.questName);
                }

                Navigation nav = FindFirstObjectByType<Navigation>();
                if (nav != null && nav.QuestDestination != null)
                {
                    nav.ClearQuestPath();
                    nav.DrawQuestLine = false;
                }
            }
        }
    }
}