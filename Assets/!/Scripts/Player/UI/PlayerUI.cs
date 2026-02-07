using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class PlayerUI : BaseUi
{
    private Label promptText;
    private Label questName;
    private Label questInfo;

    private UIDocument playerUIDocument;
    private VisualElement rootVisualElement;
    private VisualElement notificationPanel;
    private Queue<VisualElement> notificationQueue = new Queue<VisualElement>();
    private MonoBehaviour coroutineRunner;
    private VisualElement questPanel;
    private float notificationSpacing = 90f;

    public PlayerUI(UIDocument uiDocument, MonoBehaviour runner)
    {
        playerUIDocument = uiDocument;
        coroutineRunner = runner;
        
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
            Debug.LogError("[PlayerUI] Could not find QuestName or QuestDescription labels in the UXML.");
        }
        
        notificationPanel = rootVisualElement.Q<VisualElement>("ToastPanel");
        questPanel = rootVisualElement.Q<VisualElement>("QuestPanel");

        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.None;
        }

        if (notificationPanel == null)
        {
            Debug.LogError("[PlayerUI] ToastPanel not found in PlayerUI. Make sure it exists in the UXML.");
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
        AddNotification("Úloha dokončená", questName);
    }

    public void HideQuestInfo()
    {
        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.None;
        }
    }

    public void DisplayCodexUnlock(string entryName)
    {
        AddNotification("Codex odomknutý", entryName);
    }

    public void DisplayQuestUnlock(string questName)
    {
        AddNotification("Nová úloha", questName);
    }

    public void DisplaySecretUnlock(string name)
    {
        AddNotification("Našiel si secret!", name);
    }

    public void AddNotification(string title, string message)
    {
        if (notificationPanel == null)
        {
            Debug.LogError("[PlayerUI] ToastPanel is null. Cannot display notification.");
            return;
        }

        VisualElement notification = CreateNotification(title, message);
        notificationPanel.Add(notification);

        notificationQueue.Enqueue(notification);
        UpdateNotificationPositions();

        coroutineRunner.StartCoroutine(AnimateNotification(notification));
    }

    private VisualElement CreateNotification(string title, string message)
    {
        VisualElement notification = new VisualElement();
        notification.name = "Notification";
        notification.style.width = 300;
        notification.style.height = 80;
        notification.style.position = Position.Absolute;
        notification.style.top = notificationQueue.Count * notificationSpacing;
        notification.style.right = -400; // Start off-screen

        // Background
        VisualElement background = new VisualElement();
        background.name = "Background";
        background.style.backgroundColor = new Color(0, 0, 0, 125f / 255f);
        background.style.borderBottomLeftRadius = 50;
        background.style.position = Position.Absolute;
        background.style.width = Length.Percent(100);
        background.style.height = Length.Percent(100);
        notification.Add(background);

        // Title
        Label titleLabel = new Label(title);
        titleLabel.name = "Title";
        titleLabel.style.fontSize = 22;
        titleLabel.style.color = Color.green;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = promptText.style.unityFontDefinition;
        titleLabel.style.paddingRight = 20;
        titleLabel.style.position = Position.Absolute;
        titleLabel.style.top = 5;
        titleLabel.style.left = 10;
        titleLabel.style.right = 10;
        notification.Add(titleLabel);

        // Message
        Label messageLabel = new Label(message);
        messageLabel.name = "Message";
        messageLabel.style.fontSize = 18;
        messageLabel.style.color = Color.white;
        messageLabel.style.unityFontDefinition = promptText.style.unityFontDefinition;
        messageLabel.style.paddingRight = 20;
        messageLabel.style.position = Position.Absolute;
        messageLabel.style.top = 30;
        messageLabel.style.left = 10;
        messageLabel.style.right = 10;
        messageLabel.style.bottom = 10;
        notification.Add(messageLabel);

        return notification;
    }

    private IEnumerator AnimateNotification(VisualElement notification)
    {
        float duration = 0.5f;
        float elapsed = 0;
        float startX = -400;
        float endX = 0;

        // Slide in
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            notification.style.right = Mathf.Lerp(startX, endX, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        notification.style.right = endX;

        yield return new WaitForSeconds(5f);

        // Slide out
        elapsed = 0;
        startX = 0;
        endX = -400;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            notification.style.right = Mathf.Lerp(startX, endX, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        notification.style.right = endX;

        notificationQueue.Dequeue();
        notificationPanel.Remove(notification);
        UpdateNotificationPositions();
    }

    private void UpdateNotificationPositions()
    {
        int index = 0;
        foreach (var notification in notificationQueue)
        {
            if (notification != null)
            {
                notification.style.top = index * notificationSpacing;
                index++;
            }
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
