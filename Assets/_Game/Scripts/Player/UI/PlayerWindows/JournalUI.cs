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
            
            button.text = quest.QuestName;
            button.userData = quest;
            
            // Set initial color
            var initialColorSet = GetQuestColorSet(quest);
            button.style.backgroundColor = initialColorSet.BackgroundColor;
            button.style.color = initialColorSet.TextColor;
            
            // Add hover effects
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                var btn = evt.target as Button;
                var q = btn.userData as Quest;
                if (q != SelectedQuest)
                {
                    var hoverColorSet = GetQuestColorSet(q);
                    btn.style.backgroundColor = hoverColorSet.HoverBackgroundColor;
                    btn.style.color = hoverColorSet.HoverTextColor;
                }
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                var btn = evt.target as Button;
                var q = btn.userData as Quest;
                var colorSet = GetQuestColorSet(q);
                btn.style.backgroundColor = colorSet.BackgroundColor;
                btn.style.color = colorSet.TextColor;
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
            activeQuestsButton.style.backgroundColor = primaryColorSet.BackgroundColor;
            activeQuestsButton.style.color = primaryColorSet.TextColor;
            
            var fadedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
            completedQuestsButton.style.backgroundColor = fadedColorSet.BackgroundColor;
            completedQuestsButton.style.color = fadedColorSet.TextColor;
        }
        else
        {
            var fadedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
            activeQuestsButton.style.backgroundColor = fadedColorSet.BackgroundColor;
            activeQuestsButton.style.color = fadedColorSet.TextColor;
            
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            completedQuestsButton.style.backgroundColor = successColorSet.BackgroundColor;
            completedQuestsButton.style.color = successColorSet.TextColor;
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
                    btn.style.backgroundColor = hovering.HoverBackgroundColor;
                    btn.style.color = hovering.HoverTextColor;
                }
                else
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = hovering.HoverBackgroundColor;
                    btn.style.color = hovering.HoverTextColor;
                }
            }
            else if (btn == completedQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = hovering.HoverBackgroundColor;
                    btn.style.color = hovering.HoverTextColor;
                }
                else
                {
                    var hovering = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
                    btn.style.backgroundColor = hovering.HoverBackgroundColor;
                    btn.style.color = hovering.HoverTextColor;
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
                    btn.style.backgroundColor = normal.BackgroundColor;
                    btn.style.color = normal.TextColor;
                }
                else
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = normal.BackgroundColor;
                    btn.style.color = normal.TextColor;
                }
            }
            else if (btn == completedQuestsButton)
            {
                if (showingActiveQuests)
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Faded);
                    btn.style.backgroundColor = normal.BackgroundColor;
                    btn.style.color = normal.TextColor;
                }
                else
                {
                    var normal = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
                    btn.style.backgroundColor = normal.BackgroundColor;
                    btn.style.color = normal.TextColor;
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
            selectedQuestElement.style.backgroundColor = prevColorSet.BackgroundColor;
            selectedQuestElement.style.color = prevColorSet.TextColor;
        }

        selectedQuestElement = questButton;
        SelectedQuest = quest;

        RefreshQuestColors();

        var selectedColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Secondary);
        selectedQuestElement.style.backgroundColor = selectedColorSet.BackgroundColor;
        selectedQuestElement.style.color = selectedColorSet.TextColor;

        questNameText.text = quest.QuestName;
        questObjectiveText.text = quest.QuestObjective;
        questInfoText.text = quest.QuestInfo;
        
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
            trackQuestButton.style.backgroundColor = successColorSet.BackgroundColor;
            trackQuestButton.style.color = successColorSet.TextColor;
        }
        else
        {
            trackQuestButton.text = "Sledovať úlohu";
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackQuestButton.style.backgroundColor = primaryColorSet.BackgroundColor;
            trackQuestButton.style.color = primaryColorSet.TextColor;
        }
    }

    private void UpdateTrackQuestButtonHover()
    {
        if (TrackedQuest == SelectedQuest)
        {
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            trackQuestButton.style.backgroundColor = successColorSet.HoverBackgroundColor;
            trackQuestButton.style.color = successColorSet.HoverTextColor;
        }
        else
        {
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackQuestButton.style.backgroundColor = primaryColorSet.HoverBackgroundColor;
            trackQuestButton.style.color = primaryColorSet.HoverTextColor;
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
