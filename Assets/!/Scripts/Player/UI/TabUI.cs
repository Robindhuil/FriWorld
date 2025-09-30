using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Trieda TabUI spravuje rozhranie so záložkami a ich interakcie.
/// </summary>
public class TabUI : BaseUi
{
    public bool IsMenuOn { get; private set; }
    private Canvas tabCanvas;
    public Button ActiveButton { get; private set; }
    public Button[] Buttons { get; set; }

    private static readonly Color ACTIVE_BUTTON_COLOR = new Color(0.984f, 0.721f, 0f);
    private static readonly Color HOVER_COLOR = Color.grey;
    private static readonly Color DEFAULT_COLOR = Color.clear;

    /// <summary>
    /// Konštruktor inicializuje UI prvky a nastavuje ich správanie.
    /// </summary>
    public TabUI(Canvas canvas, Button journalButton, Button codexButton, Button navigationButton, Button statsButton)
    {
        tabCanvas = canvas;
        tabCanvas.gameObject.SetActive(false);

        Buttons = new[] { journalButton, codexButton, navigationButton, statsButton };
        foreach (var button in Buttons)
        {
            AddButtonListeners(button);
        }
    }

    /// <summary>
    /// Pridá event listenery pre kliknutie a hover efekty tlačidla.
    /// </summary>
    private void AddButtonListeners(Button button)
    {
        button.onClick.AddListener(() => SetActiveButton(button));
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        AddEventTrigger(trigger, EventTriggerType.PointerEnter, (eventData) => OnHoverEnter(button));
        AddEventTrigger(trigger, EventTriggerType.PointerExit, (eventData) => OnHoverExit(button));
    }

    /// <summary>
    /// Pomocná metóda na pridanie eventov do EventTriggeru.
    /// </summary>
    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(action));
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// Nastaví aktívne tlačidlo a zvýrazní ho.
    /// </summary>
    public void SetActiveButton(Button button)
    {
        if (ActiveButton != null)
        {
            ActiveButton.image.color = DEFAULT_COLOR;
        }
        ActiveButton = button;
        ActiveButton.image.color = ACTIVE_BUTTON_COLOR;
    }

    /// <summary>
    /// Zmení farbu tlačidla pri najetí myši.
    /// </summary>
    private void OnHoverEnter(Button button)
    {
        if (button != ActiveButton)
        {
            button.image.color = HOVER_COLOR;
        }
    }

    /// <summary>
    /// Reset farby tlačidla pri odchode myši.
    /// </summary>
    private void OnHoverExit(Button button)
    {
        if (button != ActiveButton)
        {
            button.image.color = DEFAULT_COLOR;
        }
    }

    /// <summary>
    /// Skryje tabu.
    /// </summary>
    public override void CloseWindow()
    {
        tabCanvas.gameObject.SetActive(false);
        IsMenuOn = false;
    }

    /// <summary>
    /// Zobrazí tabu.
    /// </summary>
    public override void OpenWindow()
    {
        tabCanvas.gameObject.SetActive(true);
        IsMenuOn = true;
    }
}