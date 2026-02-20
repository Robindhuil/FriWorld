using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]

public partial class NotificationComponent : VisualElement
{
    private const string UxmlPath = "UI/NotificationComponentTree";
    private Label notificationNameLabel;
    private Label notificationDescriptionLabel;

    public NotificationComponent()
    {
        var visualTree = Resources.Load<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"NotificationComponent.uxml not found in Resources/{UxmlPath}");
            return;
        }

        visualTree.CloneTree(this);

        notificationNameLabel = this.Q<Label>("NotificationNameLabel");
        notificationDescriptionLabel = this.Q<Label>("NotificationDescriptionLabel");

        if (notificationNameLabel == null || notificationDescriptionLabel == null)
        {
            Debug.LogError("NotificationComponent: Could not find required labels inside UXML.");
            return;
        }

        // Set initial position off-screen
        this.style.position = Position.Absolute;
        this.style.right = -400;
    }

    public void SetNotificationName(string name)
    {
        if (notificationNameLabel != null)
        {
            notificationNameLabel.text = name;
        }
    }

    public void SetNotificationDescription(string description)
    {
        if (notificationDescriptionLabel != null)
        {
            notificationDescriptionLabel.text = description;
        }
    }

    public void SetNotification(string name, string description)
    {
        SetNotificationName(name);
        SetNotificationDescription(description);
    }

    public void Animate(MonoBehaviour coroutineRunner, Action onComplete)
    {
        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(AnimateNotification(onComplete));
        }
    }

    private IEnumerator AnimateNotification(Action onComplete)
    {
        float duration = 0.5f;
        float elapsed = 0;
        float startX = -400;
        float endX = 0;

        // Slide in
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            this.style.right = Mathf.Lerp(startX, endX, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        this.style.right = endX;

        yield return new WaitForSeconds(5f);

        // Slide out
        elapsed = 0;
        startX = 0;
        endX = -400;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            this.style.right = Mathf.Lerp(startX, endX, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        this.style.right = endX;

        onComplete?.Invoke();
    }

}
