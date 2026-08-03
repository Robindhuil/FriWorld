using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NotificationUI : BaseUi
{
    private UIDocument notificationUIDocument;
    private VisualElement rootVisualElement;
    private VisualElement notificationPanel;
    private Queue<VisualElement> notificationQueue = new Queue<VisualElement>();
    private MonoBehaviour coroutineRunner;
    private float notificationSpacing = 110f;

    public NotificationUI(UIDocument uiDocument, MonoBehaviour runner)
    {
        notificationUIDocument = uiDocument;
        coroutineRunner = runner;

        // Ensure the GameObject is active so the visual tree is available
        if (!notificationUIDocument.gameObject.activeSelf)
        {
            notificationUIDocument.gameObject.SetActive(true);
        }

        rootVisualElement = notificationUIDocument.rootVisualElement;
        notificationPanel = rootVisualElement.Q<VisualElement>("NotificationPanel");

        if (notificationPanel == null)
        {
            Debug.LogError(
                "[NotificationUI] NotificationPanel not found in NotificationUI. Make sure it exists in the UXML."
            );
        }

        // Make UI not block clicks - pass through to UI elements beneath
        rootVisualElement.pickingMode = PickingMode.Ignore;
        if (notificationPanel != null)
        {
            notificationPanel.pickingMode = PickingMode.Ignore;
        }

        // Always visible
        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void AddNotification(string title, string message)
    {
        AddNotification(title, message, UnityEngine.Color.white);
    }

    public void AddNotification(string title, string message, UnityEngine.Color titleColor)
    {
        if (notificationPanel == null)
        {
            Debug.LogError(
                "[NotificationUI] NotificationPanel is null. Cannot display notification."
            );
            return;
        }

        NotificationComponent notification = CreateNotification(title, message, titleColor);
        notificationPanel.Add(notification);

        notificationQueue.Enqueue(notification);
        UpdateNotificationPositions();

        notification.Animate(coroutineRunner, () => OnNotificationComplete(notification));
    }

    private NotificationComponent CreateNotification(
        string title,
        string message,
        UnityEngine.Color titleColor
    )
    {
        NotificationComponent notification = new NotificationComponent();
        notification.SetNotification(title, message);
        notification.SetTitleColor(titleColor);
        notification.style.top = notificationQueue.Count * notificationSpacing;
        notification.pickingMode = PickingMode.Ignore;
        return notification;
    }

    private void OnNotificationComplete(VisualElement notification)
    {
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
