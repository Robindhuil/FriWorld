using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BindKeyComponent : VisualElement
{
    private const string UxmlPath = "UI/BindKey";

    private Label actionLabel;
    private Label bindingLabel;

    public string ActionName
    {
        get => actionLabel?.text;
        set
        {
            if (actionLabel != null)
                actionLabel.text = value;
        }
    }

    public string CurrentKey
    {
        get => bindingLabel?.text;
        set
        {
            if (bindingLabel != null)
                bindingLabel.text = value;
        }
    }

    public BindKeyComponent()
    {
        var visualTree = Resources.Load<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"BindKey.uxml not found in Resources/{UxmlPath}");
            return;
        }

        visualTree.CloneTree(this);

        actionLabel = this.Q<Label>("ActionLabel");
        bindingLabel = this.Q<Label>("CurrentBindingLabel");

        if (actionLabel == null || bindingLabel == null)
        {
            Debug.LogError("BindKey: Could not find required labels inside UXML.");
            return;
        }

        var rebindButton = this.Q<Button>("RebindButton");
        if (rebindButton != null)
        {
            rebindButton.style.display = DisplayStyle.None;
        }
    }

    public void Cleanup()
    {
    }
}