using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Trieda TabUI spravuje rozhranie so záložkami a ich interakcie.
/// </summary>
public class TabUI : BaseUi
{
    public bool IsMenuOn { get; private set; }
    private readonly VisualElement root;
    private readonly VisualElement backgroundBase;
    private readonly VisualElement tabsUIElement;
    public UnityEngine.UIElements.Button ActiveButton { get; private set; }
    public UnityEngine.UIElements.Button[] Buttons { get; set; }

    private static readonly Color ACTIVE_BUTTON_COLOR = new Color(0.984f, 0.721f, 0f);
    private static readonly Color HOVER_COLOR = Color.grey;
    private static readonly Color DEFAULT_COLOR = Color.clear;

    /// <summary>
    /// Konštruktor inicializuje UI prvky a nastavuje ich správanie.
    /// </summary>
    public TabUI(UIDocument sharedDocument)
    {
        if (sharedDocument == null)
        {
            Debug.LogError("[TabUI] UIDocument is null!");
            return;
        }

        root = sharedDocument.rootVisualElement;
        backgroundBase = root.Q<VisualElement>("BackgroundBase");
        tabsUIElement = root.Q<VisualElement>("TabsUI");

        if (tabsUIElement == null)
        {
            Debug.LogError("[TabUI] TabsUI element not found in UXML!");
            return;
        }

        // Get buttons from UXML (order matches old UI: Journal, Codex, Navigation, Stats)
        var journalButton = tabsUIElement.Q<UnityEngine.UIElements.Button>("Journal");
        var codexButton = tabsUIElement.Q<UnityEngine.UIElements.Button>("Codex");
        var navigationButton = tabsUIElement.Q<UnityEngine.UIElements.Button>("Navigation");
        var statsButton = tabsUIElement.Q<UnityEngine.UIElements.Button>("Stats");

        Buttons = new[] { journalButton, codexButton, navigationButton, statsButton };
        
        foreach (var button in Buttons)
        {
            if (button != null)
            {
                AddButtonListeners(button);
            }
        }

        CloseWindow();
    }

    /// <summary>
    /// Pridá event listenery pre kliknutie a hover efekty tlačidla.
    /// </summary>
    private void AddButtonListeners(UnityEngine.UIElements.Button button)
    {
        // Hover effects
        button.RegisterCallback<MouseEnterEvent>(evt => OnHoverEnter(button));
        button.RegisterCallback<MouseLeaveEvent>(evt => OnHoverExit(button));
    }

    /// <summary>
    /// Nastaví aktívne tlačidlo a zvýrazní ho.
    /// </summary>
    public void SetActiveButton(UnityEngine.UIElements.Button button)
    {
        if (ActiveButton != null)
        {
            ActiveButton.style.backgroundColor = DEFAULT_COLOR;
        }
        ActiveButton = button;
        ActiveButton.style.backgroundColor = ACTIVE_BUTTON_COLOR;
    }

    /// <summary>
    /// Zmení farbu tlačidla pri najetí myši.
    /// </summary>
    private void OnHoverEnter(UnityEngine.UIElements.Button button)
    {
        if (button != ActiveButton)
        {
            button.style.backgroundColor = HOVER_COLOR;
        }
    }

    /// <summary>
    /// Reset farby tlačidla pri odchode myši.
    /// </summary>
    private void OnHoverExit(UnityEngine.UIElements.Button button)
    {
        if (button != ActiveButton)
        {
            button.style.backgroundColor = DEFAULT_COLOR;
        }
    }

    /// <summary>
    /// Skryje tabu.
    /// </summary>
    public override void CloseWindow()
    {
        if (backgroundBase != null)
        {
            backgroundBase.style.display = DisplayStyle.None;
        }
        IsMenuOn = false;
    }

    /// <summary>
    /// Zobrazí tabu.
    /// </summary>
    public override void OpenWindow()
    {
        if (backgroundBase != null)
        {
            backgroundBase.style.display = DisplayStyle.Flex;
        }
        IsMenuOn = true;
    }
}