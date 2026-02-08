using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class JournalUI : BaseUi
{
    private UIDocument uiDocument;
    private VisualElement journalUI;
    private Journal journal;
    public bool IsMenuOn { get; set; } = false;
    private ListView questListView;
    private Label questNameText;
    private Label questObjectiveText;
    private Label questInfoText;
    private VisualElement selectedQuestElement;
    private Button trackQuestButton;
    private Button activeQuestsButton;
    private Button completedQuestsButton;
    public Quest SelectedQuest { get; set; }
    public Quest TrackedQuest { get; set; }

    private List<Quest> allQuests = new List<Quest>();
    private bool showingActiveQuests = true;

    private readonly Color defaultButtonColor = new Color(1f, 1f, 1f, 0f);
    private readonly Color trackedButtonColor = new Color(0f, 0.8f, 0.2f, 0.6f);
    private readonly Color selectedButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private readonly Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    private readonly Color completedButtonColor = Color.clear;
    
    private readonly Color activeFilterColor = new Color(251f/255f, 184f/255f, 0f/255f, 1f);
    private readonly Color completedFilterColor = new Color(18f/255f, 168f/255f, 0f/255f, 1f);
    private readonly Color inactiveFilterColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public JournalUI(UIDocument document, Journal j)
    {
        uiDocument = document;
        journal = j;

        var root = uiDocument.rootVisualElement;
        journalUI = root.Q<VisualElement>("JournalUI");
        
        // Get UI elements
        questListView = journalUI.Q<ListView>();
        questNameText = journalUI.Q<Label>("QuestName");
        questObjectiveText = journalUI.Q<Label>("QuestObjective");
        questInfoText = journalUI.Q<Label>("Info");
        trackQuestButton = journalUI.Q<Button>("TrackQuestButton");
        activeQuestsButton = journalUI.Q<Button>("ActiveQuestsButton");
        completedQuestsButton = journalUI.Q<Button>("CompletedQuestsButton");

        // Hide journal UI initially
        journalUI.style.display = DisplayStyle.None;

        // Setup track button - the callback will be added externally
        trackQuestButton.style.display = DisplayStyle.None;

        // Setup filter buttons
        activeQuestsButton.clicked += ShowActiveQuests;
        completedQuestsButton.clicked += ShowCompletedQuests;
        AddFilterButtonHoverEffects(activeQuestsButton);
        AddFilterButtonHoverEffects(completedQuestsButton);
        UpdateFilterButtons();

        // Setup ListView
        SetupListView();
    }

    public override void CloseWindow()
    {
        journalUI.style.display = DisplayStyle.None;
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        journalUI.style.display = DisplayStyle.Flex;
        IsMenuOn = true;
        ResetUIState();
        DisplayQuests();
    }

    private void ResetUIState()
    {
        selectedQuestElement = null;
        SelectedQuest = null;
        questNameText.text = string.Empty;
        questObjectiveText.text = string.Empty;
        questInfoText.text = string.Empty;
        trackQuestButton.style.display = DisplayStyle.None;
    }

    private void SetupListView()
    {
        questListView.makeItem = () => 
        {
            var button = new Button();
            button.AddToClassList("questListButton");
            return button;
        };

        questListView.bindItem = (element, index) =>
        {
            var button = element as Button;
            var quest = allQuests[index];
            
            button.text = quest.questName;
            button.userData = quest;
            
            // Set initial color
            button.style.backgroundColor = GetQuestColor(quest);
            
            // Add hover effects
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                var btn = evt.target as Button;
                if (btn.userData as Quest != SelectedQuest)
                {
                    btn.style.backgroundColor = hoverColor;
                }
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                var btn = evt.target as Button;
                var q = btn.userData as Quest;
                btn.style.backgroundColor = GetQuestColor(q);
            });
            
            // Handle click
            button.clicked += () => ShowQuestDetails(quest, button);
        };

        questListView.selectionType = SelectionType.Single;
        questListView.fixedItemHeight = 45;
    }

    private void DisplayQuests()
    {
        allQuests.Clear();
        
        if (showingActiveQuests)
        {
            // Add active quests
            allQuests.AddRange(journal.ActiveQuests);
        }
        else
        {
            // Add completed quests
            allQuests.AddRange(journal.CompletedQuests);
        }
        
        questListView.itemsSource = allQuests;
        questListView.Rebuild();
    }

    private void ShowActiveQuests()
    {
        showingActiveQuests = true;
        UpdateFilterButtons();
        ResetUIState();
        DisplayQuests();
    }

    private void ShowCompletedQuests()
    {
        showingActiveQuests = false;
        UpdateFilterButtons();
        ResetUIState();
        DisplayQuests();
    }

    private void UpdateFilterButtons()
    {
        if (showingActiveQuests)
        {
            activeQuestsButton.style.backgroundColor = activeFilterColor;
            completedQuestsButton.style.backgroundColor = inactiveFilterColor;
        }
        else
        {
            activeQuestsButton.style.backgroundColor = inactiveFilterColor;
            completedQuestsButton.style.backgroundColor = completedFilterColor;
        }
    }

    private void AddFilterButtonHoverEffects(Button button)
    {
        Color originalColor = Color.clear;
        
        button.RegisterCallback<MouseEnterEvent>(evt =>
        {
            var btn = evt.target as Button;
            originalColor = btn.style.backgroundColor.value;
            
            // Brighten the color on hover
            Color hoverColor = originalColor * 1.2f;
            hoverColor.a = originalColor.a;
            btn.style.backgroundColor = hoverColor;
        });
        
        button.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            var btn = evt.target as Button;
            // Restore based on current filter state
            if (btn == activeQuestsButton)
            {
                btn.style.backgroundColor = showingActiveQuests ? activeFilterColor : inactiveFilterColor;
            }
            else if (btn == completedQuestsButton)
            {
                btn.style.backgroundColor = showingActiveQuests ? inactiveFilterColor : completedFilterColor;
            }
        });
    }

    private void ShowQuestDetails(Quest quest, Button questButton)
    {
        if (selectedQuestElement != null)
        {
            var prevQuest = selectedQuestElement.userData as Quest;
            selectedQuestElement.style.backgroundColor = GetQuestColor(prevQuest);
        }

        selectedQuestElement = questButton;
        SelectedQuest = quest;

        RefreshQuestColors();

        selectedQuestElement.style.backgroundColor = selectedButtonColor;

        questNameText.text = quest.questName;
        questObjectiveText.text = quest.questObjective;
        questInfoText.text = quest.questInfo;
        
        if (journal.GetQuestTransform(SelectedQuest.id) == null)
        {
            trackQuestButton.style.display = DisplayStyle.None;
        }
        else
        {
            trackQuestButton.style.display = journal.ActiveQuests.Contains(quest) ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        UpdateTrackButton();
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

    public void SetTrackButtonCallback(System.Action callback)
    {
        trackQuestButton.clicked += () =>
        {
            ToggleTrackQuest();
            callback?.Invoke();
        };
    }

    private void UpdateTrackButton()
    {
        if (TrackedQuest == SelectedQuest)
        {
            trackQuestButton.text = "Prestať sledovať úlohu";
            trackQuestButton.style.backgroundColor = trackedButtonColor;
        }
        else
        {
            trackQuestButton.text = "Sledovať úlohu";
            trackQuestButton.style.backgroundColor = Color.white;
        }
    }

    private void RefreshQuestColors()
    {
        questListView.Rebuild();
    }

    private Color GetQuestColor(Quest quest)
    {
        if (quest == null) return defaultButtonColor;
        if (journal.CompletedQuests.Contains(quest)) return completedButtonColor;
        if (quest == TrackedQuest) return trackedButtonColor;
        if (quest == SelectedQuest) return selectedButtonColor;
        return defaultButtonColor;
    }

}
