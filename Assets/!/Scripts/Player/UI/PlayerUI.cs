using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class PlayerUI : BaseUi
{
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI questName;
    private TextMeshProUGUI questInfo;

    private Canvas playerCanvas;
    private Transform notificationPanel;
    private Queue<GameObject> notificationQueue = new Queue<GameObject>();
    private MonoBehaviour coroutineRunner;
    private GameObject questPanel;
    private float notificationSpacing = 90f;

    public PlayerUI(Canvas canvas, MonoBehaviour runner)
    {
        playerCanvas = canvas;
        coroutineRunner = runner;
        promptText = playerCanvas.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(b => b.name == "PromptText");
        questName = playerCanvas.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(b => b.name == "QuestName");
        questInfo = playerCanvas.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(b => b.name == "QuestInfo");
        notificationPanel = playerCanvas.GetComponentsInChildren<Transform>().FirstOrDefault(b => b.name == "NotificationPanel");
        questPanel = playerCanvas.transform.Find("Background/Quest")?.gameObject;

        questPanel.SetActive(false);

        if (notificationPanel == null)
        {
            Debug.LogError("[PlayerUI] NotificationPanel not found in PlayerUI. Make sure it exists in the canvas.");
        }
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
        questPanel.SetActive(true);
        UpdateQuestInfo(questName, questInfo);
    }

    public void DisplayQuestComplete(string questName)
    {
        HideQuestInfo();
        AddNotification("Úloha dokončená", questName);
    }

    public void HideQuestInfo()
    {
        questPanel.SetActive(false);
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
            Debug.LogError("[PlayerUI] NotificationPanel is null. Cannot display notification.");
            return;
        }

        GameObject notification = CreateNotification(title, message);
        notification.transform.SetParent(notificationPanel.transform, false);

        notificationQueue.Enqueue(notification);
        UpdateNotificationPositions();

        coroutineRunner.StartCoroutine(AnimateNotification(notification));
    }

    private GameObject CreateNotification(string title, string message)
    {
        GameObject notification = new GameObject("Notification");
        RectTransform rectTransform = notification.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 80);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.localPosition = new Vector3(0, -notificationQueue.Count * notificationSpacing, 0);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(notification.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.65f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(notification.transform, false);
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "<color=green><b>" + title + "</b></color>";
        titleText.fontSize = 22;
        titleText.font = promptText.font;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0, 1);
        titleRect.offsetMin = new Vector2(10, -30);
        titleRect.offsetMax = new Vector2(-10, 0);

        GameObject messageObject = new GameObject("Message");
        messageObject.transform.SetParent(notification.transform, false);
        TextMeshProUGUI messageText = messageObject.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.fontSize = 18;
        messageText.font = promptText.font;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform messageRect = messageText.rectTransform;
        messageRect.anchorMin = new Vector2(0, 0);
        messageRect.anchorMax = new Vector2(1, 0.8f);
        messageRect.pivot = new Vector2(0, 1);
        messageRect.offsetMin = new Vector2(10, 10);
        messageRect.offsetMax = new Vector2(-10, -10);

        return notification;
    }

    private IEnumerator AnimateNotification(GameObject notification)
    {
        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        float duration = 0.5f;
        float elapsed = 0;
        Vector3 start = new Vector3(400, rectTransform.localPosition.y, 0);
        Vector3 end = new Vector3(0, rectTransform.localPosition.y, 0);

        while (elapsed < duration)
        {
            rectTransform.localPosition = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.localPosition = end;

        yield return new WaitForSeconds(5f);

        elapsed = 0;
        start = rectTransform.localPosition;
        end = new Vector3(400, rectTransform.localPosition.y, 0);

        while (elapsed < duration)
        {
            rectTransform.localPosition = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.localPosition = end;

        notificationQueue.Dequeue();
        GameObject.Destroy(notification);
        UpdateNotificationPositions();
    }

    private void UpdateNotificationPositions()
    {
        int index = 0;
        foreach (var notification in notificationQueue)
        {
            if (notification != null)
            {
                RectTransform rectTransform = notification.GetComponent<RectTransform>();
                rectTransform.localPosition = new Vector3(0, -index * notificationSpacing, 0);
                index++;
            }
        }
    }

    public override void OpenWindow()
    {
        playerCanvas.gameObject.SetActive(true);
    }

    public override void CloseWindow()
    {
        playerCanvas.gameObject.SetActive(false);
    }
}
