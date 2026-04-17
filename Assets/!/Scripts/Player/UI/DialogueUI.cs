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
    private List<bool> currentOptionStartsQuest = new List<bool>();
    private InputManager inputManager;
    private UIManager uiManager;
    private Npc currentNpc;
    private MonoBehaviour runner;
    private Coroutine _currentTypingCoroutine;
    private readonly Color questOptionColor = new Color32(4, 131, 178, 255);


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
            inputManager.DialogueUI.Exit.performed += ctx => CloseWindow();
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
            return button;
        };

        optionsList.bindItem = (element, index) =>
        {
            var button = element as Button;
            if (button != null && index < currentOptions.Count)
            {
                button.text = currentOptions[index];

                var existingData = button.userData as OptionButtonData;
                if (existingData != null)
                {
                    if (existingData.ClickCallback != null)
                    {
                        button.clicked -= existingData.ClickCallback;
                    }

                    if (existingData.EnterCallback != null)
                    {
                        button.UnregisterCallback(existingData.EnterCallback);
                    }

                    if (existingData.LeaveCallback != null)
                    {
                        button.UnregisterCallback(existingData.LeaveCallback);
                    }
                }

                bool startsQuest = index < currentOptionStartsQuest.Count && currentOptionStartsQuest[index];
                var baseColor = startsQuest ? questOptionColor : Color.white;
                button.style.color = baseColor;

                var data = new OptionButtonData
                {
                    BaseColor = baseColor
                };

                data.ClickCallback = () => OnOptionSelected(index);
                data.EnterCallback = evt =>
                {
                    button.style.color = new Color(251f / 255f, 184f / 255f, 0f);
                };
                data.LeaveCallback = evt =>
                {
                    button.style.color = data.BaseColor;
                };

                button.clicked += data.ClickCallback;
                button.RegisterCallback(data.EnterCallback);
                button.RegisterCallback(data.LeaveCallback);
                button.userData = data;
            }
        };

        optionsList.unbindItem = (element, index) =>
        {
            var button = element as Button;
            if (button != null && button.userData is OptionButtonData data)
            {
                if (data.ClickCallback != null)
                {
                    button.clicked -= data.ClickCallback;
                }

                if (data.EnterCallback != null)
                {
                    button.UnregisterCallback(data.EnterCallback);
                }

                if (data.LeaveCallback != null)
                {
                    button.UnregisterCallback(data.LeaveCallback);
                }

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

    public void UpdateOptionButtons(List<DialogueChoice> choices)
    {
        currentOptions.Clear();
        currentOptionStartsQuest.Clear();

        if (choices != null)
        {
            foreach (var choice in choices)
            {
                currentOptions.Add(choice?.Text ?? string.Empty);
                currentOptionStartsQuest.Add(ChoiceIsOffer(choice));
            }
        }
        
        if (optionsList != null)
        {
            optionsList.itemsSource = currentOptions;
            optionsList.Rebuild();
            optionsList.style.display = currentOptions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private bool ChoiceIsOffer(DialogueChoice choice)
    {
        if (choice == null || string.IsNullOrEmpty(choice.NextNodeId))
        {
            return false;
        }

        return choice.NextNodeId.IndexOf("offer", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private class OptionButtonData
    {
        public System.Action ClickCallback;
        public EventCallback<MouseEnterEvent> EnterCallback;
        public EventCallback<MouseLeaveEvent> LeaveCallback;
        public Color BaseColor;
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
