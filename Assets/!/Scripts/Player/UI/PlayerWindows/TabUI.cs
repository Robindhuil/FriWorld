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
    private GlobalButtonClickSound globalButtonClickSound;
    public UnityEngine.UIElements.Button ActiveButton { get; private set; }
    public UnityEngine.UIElements.Button[] Buttons { get; set; }



    /// <summary>
    /// Konštruktor inicializuje UI prvky a nastavuje ich správanie.
    /// </summary>
    public TabUI(UIDocument sharedDocument, GlobalButtonClickSound buttonClickSound)
    {
        if (sharedDocument == null)
        {
            Debug.LogError("[TabUI] UIDocument is null!");
            return;
        }

        globalButtonClickSound = buttonClickSound;
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
        // Click sound
        button.RegisterCallback<ClickEvent>(evt => globalButtonClickSound?.PlayClickSound());
        
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
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            ActiveButton.style.backgroundColor = primaryColorSet.backgroundColor;
            ActiveButton.style.color = primaryColorSet.textColor;
        }
        ActiveButton = button;
        var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
        ActiveButton.style.backgroundColor = successColorSet.backgroundColor;
        ActiveButton.style.color = successColorSet.textColor;
    }

    /// <summary>
    /// Zmení farbu tlačidla pri najetí myši.
    /// </summary>
    private void OnHoverEnter(UnityEngine.UIElements.Button button)
    {
        if (button != ActiveButton)
        {
            var hoverColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            button.style.backgroundColor = hoverColorSet.hoverBackgroundColor;
            button.style.color = hoverColorSet.hoverTextColor;
        }
        else
        {
            var successHoverColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            button.style.backgroundColor = successHoverColorSet.hoverBackgroundColor;
            button.style.color = successHoverColorSet.hoverTextColor;
        }
    }

    /// <summary>
    /// Reset farby tlačidla pri odchode myši.
    /// </summary>
    private void OnHoverExit(UnityEngine.UIElements.Button button)
    {
        if (button != ActiveButton)
        {
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            button.style.backgroundColor = primaryColorSet.backgroundColor;
            button.style.color = primaryColorSet.textColor;
        }
        else
        {
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            button.style.backgroundColor = successColorSet.backgroundColor;
            button.style.color = successColorSet.textColor;
        }
    }

    /// <summary>
    /// Resets all button colors to their default state (or active state if active).
    /// </summary>
    private void ResetAllButtonColors()
    {
        var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
        var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
        
        foreach (var button in Buttons)
        {
            if (button != null)
            {
                if (button == ActiveButton)
                {
                    button.style.backgroundColor = successColorSet.backgroundColor;
                    button.style.color = successColorSet.textColor;
                }
                else
                {
                    button.style.backgroundColor = primaryColorSet.backgroundColor;
                    button.style.color = primaryColorSet.textColor;
                }
            }
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
        ResetAllButtonColors();
        IsMenuOn = true;
    }
}