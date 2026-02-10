using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SelectionComponent : VisualElement
{
    private const string UxmlPath = "UI/SelectionComponent";

    private Button leftButton;
    private Button rightButton;
    private Label descriptionLabel;
    private Label nameLabel;

    private string _nameText = "";
    [UxmlAttribute("name-text")]
    public string NameText
    {
        get => _nameText;
        set
        {
            _nameText = value;
            UpdateNameLabel();
        }
    }

    private List<SelectionOption> options = new();
    private int currentIndex = 0;

    public event Action<SelectionOption> OnSelectionChanged;

    [Serializable]
    public struct SelectionOption
    {
        public string displayName;
        public string value;
        public object data;
    }

    public SelectionComponent()
    {
        var visualTree = Resources.Load<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"SelectionComponent.uxml not found in Resources/{UxmlPath}");
            return;
        }

        visualTree.CloneTree(this);

        leftButton = this.Q<Button>("LeftButton");
        rightButton = this.Q<Button>("RightButton");
        descriptionLabel = this.Q<Label>("DescriptionLabel");
        nameLabel = this.Q<Label>("NameLabel");

        UpdateNameLabel();

        var globalSound = GameObject.FindFirstObjectByType<GlobalButtonClickSound>();

        leftButton?.RegisterCallback<ClickEvent>(_ =>
        {
            LeftButtonClicked();
            globalSound.PlayClickSound();
        });
        rightButton?.RegisterCallback<ClickEvent>(_ =>
        {
            RightButtonClicked();
            globalSound.PlayClickSound();
        });

        UpdateDisplay();
    }

    private void UpdateNameLabel()
    {
        if (nameLabel != null)
            nameLabel.text = _nameText;
    }

    public void Initialize(List<SelectionOption> newOptions, int startIndex = 0)
    {
        if (newOptions == null || newOptions.Count == 0) return;
        options = newOptions;
        currentIndex = Mathf.Clamp(startIndex, 0, options.Count - 1);
        UpdateDisplay();
    }

    private void LeftButtonClicked()
    {
        if (options.Count <= 1) return;
        currentIndex = (currentIndex - 1 + options.Count) % options.Count;
        UpdateDisplay();
        OnSelectionChanged?.Invoke(GetCurrentOption());
    }

    private void RightButtonClicked()
    {
        if (options.Count <= 1) return;
        currentIndex = (currentIndex + 1) % options.Count;
        UpdateDisplay();
        OnSelectionChanged?.Invoke(GetCurrentOption());
    }

    private void UpdateDisplay()
    {
        if (descriptionLabel == null) return;

        descriptionLabel.text = options.Count > 0
            ? options[currentIndex].displayName
            : "No options";

        bool hasMultiple = options.Count > 1;
        leftButton?.SetEnabled(hasMultiple);
        rightButton?.SetEnabled(hasMultiple);
    }

    public SelectionOption GetCurrentOption() =>
        options.Count > 0 ? options[currentIndex] : default;

    public string GetCurrentValue() =>
        options.Count > 0 ? options[currentIndex].value : "";

    public void SetNameText(string text)
    {
        NameText = text;
    }

    public void SetCurrentValue(string value)
    {
        if (options == null || options.Count == 0) return;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].value == value)
            {
                currentIndex = i;
                UpdateDisplay();
                return;
            }
        }

        currentIndex = 0;
        UpdateDisplay();
    }
}