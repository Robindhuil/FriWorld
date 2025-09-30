using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Spravuje dialógové okno a interakciu s NPC.
/// </summary>
public class DialogueUI : BaseUi
{
    private Canvas dialogueCanvas;
    private Button exitButton;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI npcNameText;
    private Transform optionsContainer;
    private List<Button> optionButtons = new List<Button>();
    private InputManager inputManager;
    private UIManager uiManager;
    private Npc currentNpc;
    private GameObject buttonPrefab;
    private MonoBehaviour runner;
    private Coroutine _currentTypingCoroutine;


    public DialogueUI(Canvas canvas, InputManager manager, UIManager uiManager, GameObject buttonPrefab, MonoBehaviour runner)
    {
        dialogueCanvas = canvas;
        dialogueCanvas.gameObject.SetActive(false);

        this.uiManager = uiManager;
        inputManager = manager;
        this.buttonPrefab = buttonPrefab;
        this.runner = runner;

        exitButton = FindButton("Exit");
        dialogueText = FindText("DialogueText");
        npcNameText = FindText("NpcName");
        optionsContainer = FindOptionsContainer("OptionsContainer");

        exitButton?.onClick.AddListener(CloseWindow);
        if (inputManager != null)
        {
            inputManager.dialogueUI.Exit.performed += ctx => CloseWindow();
        }
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
        ClearOptionButtons();
        for (int i = 0; i < options.Length; i++)
        {
            CreateOptionButton(options[i], i);
        }
    }

    private void CreateOptionButton(string optionText, int index)
    {
        if (buttonPrefab == null)
        {
            Debug.LogError("[DialogueUI] Button Prefab is not assigned!");
            return;
        }

        GameObject buttonObj = GameObject.Instantiate(buttonPrefab, optionsContainer);
        buttonObj.name = $"OptionButton_{index}";

        Button newButton = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text = optionText;
        }

        newButton.onClick.AddListener(() => OnOptionSelected(index));
        optionButtons.Add(newButton);

        AddHoverEffect(newButton);
    }


    private void AddHoverEffect(Button button)
    {
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnPointerEnter(button); });

        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnPointerExit(button); });

        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    private void OnPointerEnter(Button button)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = new Color(251f / 255f, 184f / 255f, 0f);
        }
    }

    private void OnPointerExit(Button button)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = Color.white;
        }
    }

    private void ClearOptionButtons()
    {
        foreach (var button in optionButtons)
        {
            GameObject.Destroy(button.gameObject);
        }
        optionButtons.Clear();
    }

    private void OnOptionSelected(int choiceIndex)
    {
        Debug.Log($"[DialogueUI] Klikol si na možnosť {choiceIndex}");
        currentNpc?.ContinueDialogue(choiceIndex);
    }

    public override void CloseWindow()
    {
        dialogueCanvas.gameObject.SetActive(false);
        currentNpc?.StopDialogue();
        inputManager?.SwitchToOnFootActions();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OpenWindow()
    {
        dialogueCanvas.gameObject.SetActive(true);
        inputManager?.SwitchToDialogueUIActions();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetNpc(Npc newNpc)
    {
        currentNpc = newNpc;
        Debug.Log(newNpc != null ? $"[DialogueUI] NPC nastavené: {newNpc.name}" : "[DialogueUI] NPC je null");
    }

    private Button FindButton(string buttonName)
    {
        return dialogueCanvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == buttonName);
    }

    private TextMeshProUGUI FindText(string textName)
    {
        return dialogueCanvas.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == textName);
    }

    private Transform FindOptionsContainer(string containerName)
    {
        return dialogueCanvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == containerName);
    }
}
