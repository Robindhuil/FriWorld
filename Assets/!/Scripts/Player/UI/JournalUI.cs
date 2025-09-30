using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class JournalUI : BaseUi
{
    private Canvas journalCanvas;
    private Journal journal;
    public bool IsMenuOn { get; set; } = false;
    private GameObject listPanel;
    private ScrollRect scrollRect;
    private TextMeshProUGUI questNameText;
    private TextMeshProUGUI questObjectiveText;
    private TextMeshProUGUI questInfoText;
    private GameObject selectedQuestObject;
    private Button trackQuestButton;
    public Quest SelectedQuest { get; set; }
    public Quest TrackedQuest { get; set; }

    private readonly Color defaultButtonColor = new Color(1f, 1f, 1f, 0f);
    private readonly Color trackedButtonColor = new Color(0f, 0.8f, 0.2f, 0.6f);
    private readonly Color selectedButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private GameObject buttonPrefab;

    public JournalUI(Canvas canvas, Journal j, TextMeshProUGUI questName, TextMeshProUGUI questObjective, TextMeshProUGUI questInfo,
    GameObject questListPanel, Button trackButton, GameObject buttonPrefab, MonoBehaviour runner)
    {
        journalCanvas = canvas;
        journalCanvas.gameObject.SetActive(false);
        journal = j;
        questNameText = questName;
        questObjectiveText = questObjective;
        questInfoText = questInfo;
        listPanel = questListPanel;
        this.buttonPrefab = buttonPrefab;

        scrollRect = questListPanel.GetComponent<ScrollRect>() ?? questListPanel.AddComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.content = listPanel.GetComponent<RectTransform>();

        trackQuestButton = trackButton;
        trackQuestButton.gameObject.SetActive(false);
        trackQuestButton.onClick.AddListener(ToggleTrackQuest);

    }

    public override void CloseWindow()
    {
        journalCanvas.gameObject.SetActive(false);
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        journalCanvas.gameObject.SetActive(true);
        IsMenuOn = true;
        ResetUIState();
        DisplayQuests();
    }

    private void ResetUIState()
    {
        selectedQuestObject = null;
        SelectedQuest = null;
        questNameText.text = string.Empty;
        questObjectiveText.text = string.Empty;
        questInfoText.text = string.Empty;
        trackQuestButton.gameObject.SetActive(false);
    }

    private void DisplayQuests()
    {
        ClearCanvas();
        DisplayQuestList("Aktívne úlohy", journal.ActiveQuests);
        DisplayQuestList("Splnené úlohy", journal.CompletedQuests);
    }

    private void DisplayQuestList(string title, List<Quest> quests)
    {
        if (quests.Count == 0) return;

        GameObject titleObject = new GameObject(title);
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI buttonText = buttonPrefab.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            titleText.font = buttonText.font;
        }

        titleObject.transform.SetParent(listPanel.transform, false);

        foreach (var quest in quests)
        {
            CreateButton(buttonPrefab, listPanel.transform, quest);
        }
    }

    private void CreateButton(GameObject prefab, Transform parent, Quest quest)
    {
        GameObject buttonObj = Object.Instantiate(prefab, parent);
        buttonObj.name = quest.questName;

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[JournalUI] Prefab button does not contain a Button component!");
            return;
        }

        button.onClick.AddListener(() => ShowQuestDetails(quest, buttonObj));

        Image image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            if (journal.CompletedQuests.Contains(quest))
            {
                if (quest == TrackedQuest)
                {
                    TrackedQuest = null;
                }
                image.color = Color.clear;
            }
            else
            {
                image.color = GetQuestColor(quest);
            }
        }

        TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = quest.questName ?? "Unnamed Quest";
        }

        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(-30f, 45f);
        }

        AddHoverEffects(buttonObj, image, quest);
    }

    private void ShowQuestDetails(Quest quest, GameObject questObject)
    {
        if (selectedQuestObject != null)
        {
            selectedQuestObject.GetComponent<Image>().color = GetQuestColor(journal.FindQuestByName(selectedQuestObject.name));
        }

        selectedQuestObject = questObject;
        SelectedQuest = quest;

        RefreshQuestColors();

        selectedQuestObject.GetComponent<Image>().color = selectedButtonColor;

        questNameText.text = quest.questName;
        questObjectiveText.text = quest.questObjective;
        questInfoText.text = quest.questInfo;
        if (journal.GetQuestTransform(SelectedQuest.id) == null)
        {
            trackQuestButton.gameObject.SetActive(false);
        }
        else
        {
            trackQuestButton.gameObject.SetActive(journal.ActiveQuests.Contains(quest));

        }
        UpdateTrackButton();
    }

    private void AddHoverEffects(GameObject buttonObj, Image bgImage, Quest quest)
    {
        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((_) => { bgImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); });
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener((_) => { bgImage.color = GetQuestColor(quest); });
        trigger.triggers.Add(pointerExit);
    }

    private void ToggleTrackQuest()
    {
        if (SelectedQuest == null) return;

        if (TrackedQuest == SelectedQuest)
        {
            TrackedQuest = null;
        }
        else
        {
            TrackedQuest = SelectedQuest;
        }
        RefreshQuestColors();
        UpdateTrackButton();
    }

    private void UpdateTrackButton()
    {
        if (TrackedQuest == SelectedQuest)
        {
            trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text = "Prestať sledovať úlohu";
            trackQuestButton.GetComponent<Image>().color = trackedButtonColor;
        }
        else
        {
            trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sledovať úlohu";
            trackQuestButton.GetComponent<Image>().color = Color.white;
        }
    }

    private void RefreshQuestColors()
    {
        foreach (Transform child in listPanel.transform)
        {
            var image = child.GetComponent<Image>();
            if (image != null)
            {
                var quest = journal.FindQuestByName(child.name);
                if (quest != null)
                {
                    image.color = GetQuestColor(quest);
                }
            }
        }
    }

    private Color GetQuestColor(Quest quest)
    {
        if (quest == TrackedQuest) return trackedButtonColor;
        if (quest == SelectedQuest) return selectedButtonColor;
        return defaultButtonColor;
    }

    private void ClearCanvas()
    {
        RectTransform contentRectTransform = listPanel.GetComponent<RectTransform>();
        contentRectTransform.anchoredPosition = Vector2.zero;
        contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, 0);

        foreach (Transform child in listPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

}
