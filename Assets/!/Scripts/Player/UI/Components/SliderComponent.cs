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

    public event Action<float> OnValueChanged;

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

        if (slider != null)
        {
            slider.label = "";
            slider.RegisterValueChangedCallback(OnSliderValueChanged);
        }

        UpdateLevelDisplay();
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


    private void OnSliderValueChanged(ChangeEvent<float> evt)
    {
        UpdateLevelDisplay();
        OnValueChanged?.Invoke(evt.newValue);
    }

    private void UpdateLevelDisplay()
    {
        if (sliderLevel != null && slider != null)
        {
            sliderLevel.text = $"{slider.value:0}%";
        }
    }

    public void SetLevelFormat(string format)
    {
        if (sliderLevel != null && slider != null)
        {
            sliderLevel.text = string.Format(format, slider.value);
        }
    }

    public void SetValueWithoutNotify(float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
            UpdateLevelDisplay();
        }
    }
}