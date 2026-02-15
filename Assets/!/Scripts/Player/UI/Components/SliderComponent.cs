using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SliderComponent : VisualElement
{
    private const string UxmlPath = "UI/SliderComponent";

    private Label nameLabel;
    private Label sliderLevel;
    private Slider slider;
    private Button resetButton;

    private float defaultValue = 50f;
    private bool useWholeNumbers = false;
    private string levelFormat = "{0:0}%";

    public event Action<float> OnValueChanged;
    public event Action OnResetToDefault;

    public SliderComponent()
    {
        var visualTree = Resources.Load<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"SliderComponent.uxml not found in Resources/{UxmlPath}");
            return;
        }

        visualTree.CloneTree(this);

        nameLabel = this.Q<Label>("NameLabel");
        sliderLevel = this.Q<Label>("SliderLevel");
        slider = this.Q<Slider>();
        resetButton = this.Q<Button>("ResetToDefaultButton");

        // Apply USS class for styling
        if (resetButton != null)
            resetButton.AddToClassList("resetToDefaultButton");

        if (slider != null)
        {
            slider.label = "";
            slider.RegisterValueChangedCallback(OnSliderValueChanged);
        }

        var globalSound = GameObject.FindFirstObjectByType<GlobalButtonClickSound>();

        if (resetButton != null)
        {
            resetButton.RegisterCallback<ClickEvent>(_ =>
            {
                OnResetButtonClicked();
                globalSound?.PlayClickSound();
            });
        }

        UpdateLevelDisplay();
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }


    public string NameText
    {
        get => nameLabel?.text ?? "";
        set
        {
            if (nameLabel != null)
                nameLabel.text = value;
        }
    }

    public float Value
    {
        get => slider?.value ?? 0f;
        set
        {
            if (slider != null)
            {
                slider.value = value;
                UpdateLevelDisplay();
            }
        }
    }

    public float MinValue
    {
        get => slider?.lowValue ?? 0f;
        set
        {
            if (slider != null)
                slider.lowValue = value;
        }
    }

    public float MaxValue
    {
        get => slider?.highValue ?? 100f;
        set
        {
            if (slider != null)
                slider.highValue = value;
        }
    }

    public float DefaultValue
    {
        get => defaultValue;
        set => defaultValue = value;
    }

    public bool UseWholeNumbers
    {
        get => useWholeNumbers;
        set
        {
            useWholeNumbers = value;
            if (slider != null && value)
            {
                slider.value = Mathf.Round(slider.value);
            }
            UpdateLevelDisplay();
        }
    }

    public bool ShowResetButton
    {
        get => resetButton?.style.display == DisplayStyle.Flex;
        set
        {
            if (resetButton != null)
                resetButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public string TooltipText
    {
        get => tooltip;
        set => tooltip = value;
    }


    private void OnSliderValueChanged(ChangeEvent<float> evt)
    {
        float value = evt.newValue;
        
        if (useWholeNumbers)
        {
            value = Mathf.Round(value);
            if (value != evt.newValue)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        UpdateLevelDisplay();
        OnValueChanged?.Invoke(value);
    }

    private void OnResetButtonClicked()
    {
        Value = defaultValue;
        OnResetToDefault?.Invoke();
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        if (slider != null)
        {
            slider.UnregisterValueChangedCallback(OnSliderValueChanged);
        }

        UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private void UpdateLevelDisplay()
    {
        if (sliderLevel != null && slider != null)
        {
            float displayValue = useWholeNumbers ? Mathf.Round(slider.value) : slider.value;
            sliderLevel.text = string.Format(levelFormat, displayValue);
        }
    }

    public void SetLevelFormat(string format)
    {
        levelFormat = format;
        UpdateLevelDisplay();
    }

    public void SetValueWithoutNotify(float value)
    {
        if (slider != null)
        {
            float finalValue = useWholeNumbers ? Mathf.Round(value) : value;
            slider.SetValueWithoutNotify(finalValue);
            UpdateLevelDisplay();
        }
    }

    public void Initialize(string name, float min, float max, float defaultVal, bool wholeNumbers = false)
    {
        NameText = name;
        MinValue = min;
        MaxValue = max;
        DefaultValue = defaultVal;
        UseWholeNumbers = wholeNumbers;
        Value = defaultVal;
    }

    public new void SetEnabled(bool enabled)
    {
        if (slider != null)
            slider.SetEnabled(enabled);
        if (resetButton != null)
            resetButton.SetEnabled(enabled);
    }
}