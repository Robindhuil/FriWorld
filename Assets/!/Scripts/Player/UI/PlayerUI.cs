using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUI : BaseUi
{
    private Label promptText;
    private Label questName;
    private Label questInfo;

    private UIDocument playerUIDocument;
    private VisualElement rootVisualElement;
    private NotificationUI notificationUI;
    private MonoBehaviour coroutineRunner;
    private VisualElement questPanel;

    public PlayerUI(UIDocument uiDocument, MonoBehaviour runner, NotificationUI notificationUI)
    {
        playerUIDocument = uiDocument;
        coroutineRunner = runner;
        this.notificationUI = notificationUI;

        // Ensure the GameObject is active so the visual tree is available
        if (!playerUIDocument.gameObject.activeSelf)
        {
            playerUIDocument.gameObject.SetActive(true);
        }

        rootVisualElement = playerUIDocument.rootVisualElement;

        promptText = rootVisualElement.Q<Label>("PromptText");
        questName = rootVisualElement.Q<Label>("QuestName");
        questInfo = rootVisualElement.Q<Label>("QuestDescription");

        if (questName == null || questInfo == null)
        {
            Debug.LogError(
                "[PlayerUI] Could not find QuestName or QuestDescription labels in the UXML."
            );
        }

        questPanel = rootVisualElement.Q<VisualElement>("QuestPanel");

        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.None;
        }

        // Start visible by default
        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }

    public void UpdateQuestInfo(string questName, string questInfo)
    {
        this.questName.text = questName;
        this.questInfo.text = questInfo;
    }

    public void DisplayQuest(string questName, string questInfo)
    {
        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.Flex;
            UpdateQuestInfo(questName, questInfo);
        }
    }

    public void DisplayQuestComplete(string questName)
    {
        HideQuestInfo();
        if (notificationUI != null)
        {
            notificationUI.AddNotification(
                "Úloha dokončená",
                questName,
                ButtonColorScheme.Instance.GetBackgroundColor(ButtonColorScheme.ButtonType.Success)
            );
        }
    }

    public void HideQuestInfo()
    {
        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.None;
        }
    }

    public void DisplayRoomArrival(string roomName)
    {
        if (notificationUI != null)
        {
            notificationUI.AddNotification(
                "Cieľová destinácia",
                roomName,
                ButtonColorScheme.Instance.GetBackgroundColor(ButtonColorScheme.ButtonType.Secondary)
            );
        }
    }

    public void DisplayCodexUnlock(string entryName)
    {
        if (notificationUI != null)
        {
            notificationUI.AddNotification(
                "Codex odomknutý",
                entryName,
                ButtonColorScheme.Instance.GetBackgroundColor(ButtonColorScheme.ButtonType.Faded)
            );
        }
    }

    public void DisplayQuestUnlock(string questName)
    {
        if (notificationUI != null)
        {
            notificationUI.AddNotification(
                "Nová úloha",
                questName,
                ButtonColorScheme.Instance.GetBackgroundColor(ButtonColorScheme.ButtonType.Primary)
            );
        }
    }

    public void DisplaySecretUnlock(string name)
    {
        if (notificationUI != null)
        {
            notificationUI.AddNotification(
                "Našiel si secret!",
                name,
                ButtonColorScheme.Instance.GetBackgroundColor(ButtonColorScheme.ButtonType.Danger)
            );
        }
    }

    public override void OpenWindow()
    {
        if (rootVisualElement != null)
        {
            rootVisualElement.style.display = DisplayStyle.Flex;
        }
    }

    public override void CloseWindow()
    {
        if (rootVisualElement != null)
        {
            rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
