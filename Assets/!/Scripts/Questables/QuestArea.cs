using UnityEngine;

public class QuestArea : MonoBehaviour
{
    [Header("Quest Activation Settings")]
    [Tooltip("ID questov, ktoré sa majú aktivovať")]
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
            Debug.LogError("[QuestArea] QuestArea vyžaduje collider komponent", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && player.PlayerManagment != null && player.PlayerManagment.journal != null)
            {
                ActivateQuests(player.PlayerManagment.journal);
                Destroy(gameObject);
            }
        }
    }

    private void ActivateQuests(Journal journal)
    {
        foreach (int questId in questIds)
        {
            Quest quest = QuestManager.Instance.GetQuestById(questId);
            if (quest != null && quest.Status == QuestStatus.Inactive)
            {
                journal.ChangeQuestStatus(quest, QuestStatus.Active);
                Debug.Log($"[QuestArea] Quest {questId} bol aktivovaný cez QuestArea");

                UIManager uiManager = FindFirstObjectByType<UIManager>();
                if (uiManager != null && uiManager.playerUI != null)
                {
                    uiManager.playerUI.DisplayQuestUnlock(quest.questName);
                }
            }
        }
    }
}