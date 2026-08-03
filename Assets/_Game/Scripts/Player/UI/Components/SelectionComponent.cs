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
    private Button resetButton;
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
    private int defaultIndex = 0;

    public event Action<SelectionOption> OnSelectionChanged;
    public event Action OnResetToDefault;

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
        resetButton = this.Q<Button>("ResetToDefaultButton");
        descriptionLabel = this.Q<Label>("DescriptionLabel");
        nameLabel = this.Q<Label>("NameLabel");

        // Apply USS classes for styling
        if (leftButton != null)
            leftButton.AddToClassList("rollNextButton");
        if (rightButton != null)
            rightButton.AddToClassList("rollNextButton");
        if (resetButton != null)
            resetButton.AddToClassList("resetToDefaultButton");

        // Add hover effects for left/right buttons (inline styles override USS, so we handle in C#)
        leftButton?.RegisterCallback<MouseEnterEvent>(evt => 
        {
            leftButton.style.color = new Color(94f/255f, 94f/255f, 94f/255f);
        });
        leftButton?.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            leftButton.style.color = Color.white;
        });
        
        rightButton?.RegisterCallback<MouseEnterEvent>(evt => 
        {
            rightButton.style.color = new Color(94f/255f, 94f/255f, 94f/255f);
        });
        rightButton?.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            rightButton.style.color = Color.white;
        });

        UpdateNameLabel();

        var globalSound = GameObject.FindFirstObjectByType<GlobalButtonClickSound>();

        leftButton?.RegisterCallback<ClickEvent>(_ =>
        {
            LeftButtonClicked();
            globalSound?.PlayClickSound();
        });
        rightButton?.RegisterCallback<ClickEvent>(_ =>
        {
            RightButtonClicked();
            globalSound?.PlayClickSound();
        });
        resetButton?.RegisterCallback<ClickEvent>(_ =>
        {
            ResetToDefault();
            globalSound?.PlayClickSound();
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
        defaultIndex = currentIndex;
        UpdateDisplay();
    }

    public void Initialize(List<SelectionOption> newOptions, int startIndex, int defaultIndexValue)
    {
        if (newOptions == null || newOptions.Count == 0) return;
        options = newOptions;
        currentIndex = Mathf.Clamp(startIndex, 0, options.Count - 1);
        defaultIndex = Mathf.Clamp(defaultIndexValue, 0, options.Count - 1);
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

    public void SetCurrentValueWithoutNotify(string value)
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

    public int GetCurrentIndex() => currentIndex;

    public void SetCurrentIndex(int index)
    {
        if (options == null || options.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, options.Count - 1);
        UpdateDisplay();
        OnSelectionChanged?.Invoke(GetCurrentOption());
    }

    public void SetCurrentIndexWithoutNotify(int index)
    {
        if (options == null || options.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, options.Count - 1);
        UpdateDisplay();
    }

    public object GetCurrentData() =>
        options.Count > 0 ? options[currentIndex].data : null;

    public void SetDefaultIndex(int index)
    {
        defaultIndex = Mathf.Clamp(index, 0, options.Count - 1);
    }

    public int GetDefaultIndex() => defaultIndex;

    private void ResetToDefault()
    {
        if (currentIndex == defaultIndex) return;
        
        currentIndex = defaultIndex;
        UpdateDisplay();
        OnResetToDefault?.Invoke();
        OnSelectionChanged?.Invoke(GetCurrentOption());
    }

    public void SetTooltipText(string text)
    {
        tooltip = text;
    }

    public int GetOptionCount() => options.Count;
}