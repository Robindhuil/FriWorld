using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class JournalUI : BaseUi
{
    private UIDocument uiDocument;
    private VisualElement journalUI;
    private Journal journal;
    private GlobalButtonClickSound globalButtonClickSound;
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



    public JournalUI(UIDocument document, Journal j, GlobalButtonClickSound buttonClickSound)
    {
        uiDocument = document;
        journal = j;
        globalButtonClickSound = buttonClickSound;

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
        
        // Setup track button styling and hover
        if (trackQuestButton != null)
        {
            trackQuestButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                UpdateTrackQuestButtonHover();
            });
            trackQuestButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                UpdateTrackButton();
            });
        }

        // Setup filter buttons
        activeQuestsButton.clicked += () =>
        {
            globalButtonClickSound?.PlayClickSound();
            ShowActiveQuests();
        };
        completedQuestsButton.clicked += () =>
        {
            globalButtonClickSound?.PlayClickSound();
            ShowCompletedQuests();
        };
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
            button.style.marginBottom = 5;
            return button;
        };

        questListView.bindItem = (element, index) =>
        {
            var button = element as Button;
            var quest = allQuests[index];
            
            button.text = quest.questName;
            button.userData = quest;
            
            // Set initial color
            var initialColorSet = GetQuestColorSet(quest);
            button.style.backgroundColor = initialColorSet.backgroundColor;
            button.style.color = initialColorSet.textColor;
            
            // Add hover effects
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                var btn = evt.target as Button;
                var q = btn.userData as Quest;
                if (q != SelectedQuest)
                {
                    var hoverColorSet = GetQuestColorSet(q);
                    btn.style.backgroundColor = hoverColorSet.hoverBackgroundColor;
                    btn.style.color = hoverColorSet.hoverTextColor;
                }
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                var btn = evt.target as Button;
                var q = btn.userData as Quest;
                var colorSet = GetQuestColorSet(q);
                btn.style.backgroundColor = colorSet.backgroundColor;
                btn.style.color = colorSet.textColor;
            });
            
            // Handle click
            button.clicked += () =>
            {
                globalButtonClickSound?.PlayClickSound();
                ShowQuestDetails(quest, button);
            };
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

        // Untrack quest if it has been completed
        if (TrackedQuest != null && journal.CompletedQuests.Contains(TrackedQuest))
        {
            TrackedQuest = null;
            UpdateTrackButton();
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
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            activeQuestsButton.style.backgroundColor = primaryColorSet.backgroundColor;
            activeQuestsButton.style.color = primaryColorSet.textColor;
            
            var fadedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
            completedQuestsButton.style.backgroundColor = fadedColorSet.backgroundColor;
            completedQuestsButton.style.color = fadedColorSet.textColor;
        }
        else
        {
            var fadedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
            activeQuestsButton.style.backgroundColor = fadedColorSet.backgroundColor;
            activeQuestsButton.style.color = fadedColorSet.textColor;
            
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            completedQuestsButton.style.backgroundColor = successColorSet.backgroundColor;
            completedQuestsButton.style.color = successColorSet.textColor;
        }
    }

    private void AddFilterButtonHoverEffects(Button button)
    {
        button.RegisterCallback<MouseEnterEvent>(evt =>
        {
            var btn = evt.target as Button;
            if (btn == activeQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
                    btn.style.backgroundColor = hovering.hoverBackgroundColor;
                    btn.style.color = hovering.hoverTextColor;
                }
                else
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = hovering.hoverBackgroundColor;
                    btn.style.color = hovering.hoverTextColor;
                }
            }
            else if (btn == completedQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = hovering.hoverBackgroundColor;
                    btn.style.color = hovering.hoverTextColor;
                }
                else
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
                    btn.style.backgroundColor = hovering.hoverBackgroundColor;
                    btn.style.color = hovering.hoverTextColor;
                }
            }
        });
        
        button.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            var btn = evt.target as Button;
            // Restore based on current filter state
            if (btn == activeQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
                    btn.style.backgroundColor = normal.backgroundColor;
                    btn.style.color = normal.textColor;
                }
                else
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = normal.backgroundColor;
                    btn.style.color = normal.textColor;
                }
            }
            else if (btn == completedQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = normal.backgroundColor;
                    btn.style.color = normal.textColor;
                }
                else
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
                    btn.style.backgroundColor = normal.backgroundColor;
                    btn.style.color = normal.textColor;
                }
            }
        });
    }

    private void ShowQuestDetails(Quest quest, Button questButton)
    {
        if (selectedQuestElement != null)
        {
            var prevQuest = selectedQuestElement.userData as Quest;
            var prevColorSet = GetQuestColorSet(prevQuest);
            selectedQuestElement.style.backgroundColor = prevColorSet.backgroundColor;
            selectedQuestElement.style.color = prevColorSet.textColor;
        }

        selectedQuestElement = questButton;
        SelectedQuest = quest;

        RefreshQuestColors();

        var selectedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Secondary);
        selectedQuestElement.style.backgroundColor = selectedColorSet.backgroundColor;
        selectedQuestElement.style.color = selectedColorSet.textColor;

        questNameText.text = quest.questName;
        questObjectiveText.text = quest.questObjective;
        questInfoText.text = quest.questInfo;
        
        trackQuestButton.style.display = journal.ActiveQuests.Contains(quest) ? DisplayStyle.Flex : DisplayStyle.None;
        
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
            globalButtonClickSound?.PlayClickSound();
            ToggleTrackQuest();
            callback?.Invoke();
        };
    }

    private void UpdateTrackButton()
    {
        if (TrackedQuest == SelectedQuest)
        {
            trackQuestButton.text = "Prestať sledovať úlohu";
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            trackQuestButton.style.backgroundColor = successColorSet.backgroundColor;
            trackQuestButton.style.color = successColorSet.textColor;
        }
        else
        {
            trackQuestButton.text = "Sledovať úlohu";
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackQuestButton.style.backgroundColor = primaryColorSet.backgroundColor;
            trackQuestButton.style.color = primaryColorSet.textColor;
        }
    }

    private void UpdateTrackQuestButtonHover()
    {
        if (TrackedQuest == SelectedQuest)
        {
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            trackQuestButton.style.backgroundColor = successColorSet.hoverBackgroundColor;
            trackQuestButton.style.color = successColorSet.hoverTextColor;
        }
        else
        {
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackQuestButton.style.backgroundColor = primaryColorSet.hoverBackgroundColor;
            trackQuestButton.style.color = primaryColorSet.hoverTextColor;
        }
    }

    private void RefreshQuestColors()
    {
        questListView.Rebuild();
    }

    private ButtonColorScheme.ButtonColorSet GetQuestColorSet(Quest quest)
    {
        if (quest == null)
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Ghost);
        if (quest == SelectedQuest)
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Secondary);
        if (quest == TrackedQuest)
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
        if (journal.CompletedQuests.Contains(quest))
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Ghost);
        return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Ghost);
    }

}
