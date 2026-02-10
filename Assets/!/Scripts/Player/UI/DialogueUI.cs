using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UIElements;

/// <summary>
/// Spravuje dialógové okno a interakciu s NPC.
/// </summary>
public class DialogueUI : BaseUi
{
    private UIDocument dialogueDocument;
    private VisualElement rootElement;
    private Button exitButton;
    private Label dialogueText;
    private Label npcNameText;
    private ListView optionsList;
    private List<string> currentOptions = new List<string>();
    private InputManager inputManager;
    private UIManager uiManager;
    private Npc currentNpc;
    private MonoBehaviour runner;
    private Coroutine _currentTypingCoroutine;


    public DialogueUI(UIDocument document, InputManager manager, UIManager uiManager, MonoBehaviour runner)
    {
        dialogueDocument = document;
        rootElement = dialogueDocument.rootVisualElement;
        rootElement.style.display = DisplayStyle.None;

        this.uiManager = uiManager;
        inputManager = manager;
        this.runner = runner;

        exitButton = rootElement.Q<Button>("CloseButton");
        dialogueText = rootElement.Q<Label>("NpcPart");
        npcNameText = rootElement.Q<Label>("NpcName");
        optionsList = rootElement.Q<ListView>("OptionsList");

        exitButton?.RegisterCallback<ClickEvent>(evt => CloseWindow());
        
        SetupListView();
        
        if (inputManager != null)
        {
            inputManager.dialogueUI.Exit.performed += ctx => CloseWindow();
        }
    }

    private void SetupListView()
    {
        if (optionsList == null) return;

        // Remove default ListView styling and center items
        optionsList.showBorder = false;
        optionsList.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
        optionsList.selectionType = SelectionType.None;
        
        // Center items vertically in the container
        optionsList.style.justifyContent = Justify.Center;
        optionsList.style.alignItems = Align.Center;
        optionsList.style.alignContent = Align.Center;
        
        optionsList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        
        // Set fixed item height to match button size
        optionsList.fixedItemHeight = 60;

        optionsList.makeItem = () =>
        {
            var button = new Button();
            button.AddToClassList("optionButton");
            button.style.height = 50;
            button.style.minHeight = 50;
            button.style.flexGrow = 0;
            button.style.flexShrink = 0;
            
            // Add hover effects
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.color = new Color(251f / 255f, 184f / 255f, 0f);
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.color = Color.white;
            });
            
            return button;
        };

        optionsList.bindItem = (element, index) =>
        {
            var button = element as Button;
            if (button != null && index < currentOptions.Count)
            {
                button.text = currentOptions[index];
                // Clear any existing callbacks
                var existingCallbacks = button.userData as System.Action;
                if (existingCallbacks != null)
                {
                    button.clicked -= existingCallbacks;
                }
                
                // Create a new callback that captures the current index
                System.Action callback = () => OnOptionSelected(index);
                button.clicked += callback;
                button.userData = callback;
            }
        };

        optionsList.unbindItem = (element, index) =>
        {
            var button = element as Button;
            if (button != null && button.userData is System.Action callback)
            {
                button.clicked -= callback;
                button.userData = null;
            }
        };

        optionsList.itemsSource = currentOptions;
    }

    public void UpdateDialogue(string message, string npcName)
    {
        if (npcNameText != null) npcNameText.text = $"- {npcName}";
        if (dialogueText != null && uiManager != null)
        {
            if (_currentTypingCoroutine != null)
            {
                runner.StopCoroutine(_currentTypingCoroutine);
                _currentTypingCoroutine = null;
            }

            _currentTypingCoroutine = runner.StartCoroutine(TypewriterEffect(message));
        }
    }

    private IEnumerator TypewriterEffect(string message)
    {
        dialogueText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.05f);
        }

        _currentTypingCoroutine = null;
    }

    public void UpdateOptionButtons(string[] options)
    {
        currentOptions.Clear();
        currentOptions.AddRange(options);
        
        if (optionsList != null)
        {
            optionsList.itemsSource = currentOptions;
            optionsList.Rebuild();
            optionsList.style.display = options.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void OnOptionSelected(int choiceIndex)
    {
        Debug.Log($"[DialogueUI] Klikol si na možnosť {choiceIndex}");
        currentNpc?.ContinueDialogue(choiceIndex);
    }

    public override void CloseWindow()
    {
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.None;
        }
        currentNpc?.StopDialogue();
        inputManager?.SwitchToOnFootActions();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public override void OpenWindow()
    {
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.Flex;
        }
        inputManager?.SwitchToDialogueUIActions();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void SetNpc(Npc newNpc)
    {
        currentNpc = newNpc;
        Debug.Log(newNpc != null ? $"[DialogueUI] NPC nastavené: {newNpc.name}" : "[DialogueUI] NPC je null");
    }
}
