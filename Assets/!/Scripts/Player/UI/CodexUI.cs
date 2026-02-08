using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using System.Linq;
using System.Collections.Generic;

public class CodexUI : BaseUi
{
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement codexUIElement;
    private ListView categoriesList;
    private ListView subCategoriesList;
    private ScrollView infoScrollView;
    private Label entryNameLabel;
    private Image pictureElement;
    public bool IsMenuOn { get; set; }

    private Button lastCategoryButton;
    private Button lastSubButton;
    private MonoBehaviour runner;
    private VideoPlayerUI videoPlayerUI;

    public CodexUI(UIDocument document, MonoBehaviour runner, AudioMixer mixer)
    {
        this.uiDocument = document;
        this.runner = runner;
        
        rootElement = uiDocument.rootVisualElement;
        codexUIElement = rootElement.Q<VisualElement>("CodexUI");
        categoriesList = rootElement.Q<ListView>("CategoriesList");
        subCategoriesList = rootElement.Q<ListView>("SubCategoriesList");
        infoScrollView = rootElement.Q<ScrollView>("InfoList");
        entryNameLabel = rootElement.Q<Label>("EntryName");
        pictureElement = rootElement.Q<Image>("Picture");
        
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.None;
        }

        if (Codex.Instance == null)
        {
            Debug.LogError("[CodexUI] Codex.Instance je null!");
            return;
        }

        // Initialize video player UI
        videoPlayerUI = new VideoPlayerUI(document, mixer);

        SetupCategoriesList();
        SetupSubCategoriesList();

        Codex.Instance.OnCodexUpdated += RefreshUI;
        RefreshCategoryButtons();
    }

    private void SetupCategoriesList()
    {
        if (categoriesList == null) return;

        categoriesList.fixedItemHeight = 70; // 60px height + 5px top margin + 5px bottom margin
        categoriesList.makeItem = () => CreateListButton();
        categoriesList.bindItem = (element, index) => BindCategoryItem(element, index);
        categoriesList.selectionChanged += OnCategorySelected;
        categoriesList.itemsSource = new List<string>();
    }

    private void SetupSubCategoriesList()
    {
        if (subCategoriesList == null) return;

        subCategoriesList.fixedItemHeight = 70; // 60px height + 5px top margin + 5px bottom margin
        subCategoriesList.makeItem = () => CreateListButton();
        subCategoriesList.bindItem = (element, index) => BindSubCategoryItem(element, index);
        subCategoriesList.selectionChanged += OnSubCategorySelected;
        subCategoriesList.itemsSource = new List<CodexEntry>();
    }

    private VisualElement CreateListButton()
    {
        var button = new Button();
        button.AddToClassList("codexButton");
        
        button.RegisterCallback<MouseEnterEvent>(evt => OnButtonHoverEnter(evt.target as Button));
        button.RegisterCallback<MouseLeaveEvent>(evt => OnButtonHoverExit(evt.target as Button));
        
        return button;
    }


    private void BindCategoryItem(VisualElement element, int index)
    {
        if (element is Button button && categoriesList.itemsSource is List<string> categories)
        {
            if (index < categories.Count)
            {
                button.text = categories[index];
                button.userData = categories[index];
                
                // Clear previous click handlers
                button.clicked -= null;
                button.clicked += () => 
                {
                    categoriesList.selectedIndex = index;
                    ShowSubList(categories[index]);
                };
            }
        }
    }

    private void BindSubCategoryItem(VisualElement element, int index)
    {
        if (element is Button button && subCategoriesList.itemsSource is List<CodexEntry> entries)
        {
            if (index < entries.Count)
            {
                button.text = RemoveTitles(entries[index].name);
                button.userData = entries[index];
                
                // Clear previous click handlers
                button.clicked -= null;
                button.clicked += () => 
                {
                    subCategoriesList.selectedIndex = index;
                    ShowEntryDetails(entries[index]);
                };
            }
        }
    }

    private void OnCategorySelected(IEnumerable<object> selectedItems)
    {
        var selectedCategory = selectedItems.FirstOrDefault() as string;
        if (!string.IsNullOrEmpty(selectedCategory))
        {
            ShowSubList(selectedCategory);
        }
    }

    private void OnSubCategorySelected(IEnumerable<object> selectedItems)
    {
        var selectedEntry = selectedItems.FirstOrDefault() as CodexEntry;
        if (selectedEntry != null)
        {
            ShowEntryDetails(selectedEntry);
        }
    }

    private void OnButtonHoverEnter(Button button)
    {
        if (button != null && button != lastCategoryButton && button != lastSubButton)
        {
            button.style.backgroundColor = new Color(1, 1, 1, 0.7f);
        }
    }

    private void OnButtonHoverExit(Button button)
    {
        if (button != null && button != lastCategoryButton && button != lastSubButton)
        {
            button.style.backgroundColor = new Color(0, 0, 0, 0);
        }
    }

    private void RefreshUI()
    {
        RefreshCategoryButtons();
        ResetAll();
    }

    private void RefreshCategoryButtons()
    {
        var uniqueCategories = Codex.Instance.GetUnlockedCategories().Distinct().ToList();
        if (categoriesList != null)
        {
            categoriesList.itemsSource = uniqueCategories;
            categoriesList.Rebuild();
        }
    }
    private void ShowSubList(string category)
    {
        var uniqueEntries = Codex.Instance.GetUnlockedEntries(category)
            .GroupBy(e => e.name)
            .Select(g => g.First())
            .ToList();

        if (subCategoriesList != null)
        {
            subCategoriesList.itemsSource = uniqueEntries;
            subCategoriesList.Rebuild();
        }
    }

    private void ShowEntryDetails(CodexEntry entry)
    {
        // Set entry name in the dedicated label
        if (entryNameLabel != null)
        {
            entryNameLabel.text = !string.IsNullOrEmpty(entry.name) ? entry.name : "";
        }
        
        // Clear and populate ScrollView with description
        if (infoScrollView != null)
        {
            infoScrollView.Clear();
            
            if (!string.IsNullOrEmpty(entry.description))
            {
                var descLabel = new Label(entry.description);
                descLabel.style.color = Color.white;
                descLabel.style.fontSize = 20;
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.paddingTop = 10;
                descLabel.style.paddingBottom = 10;
                descLabel.style.paddingLeft = 10;
                descLabel.style.paddingRight = 10;
                infoScrollView.Add(descLabel);
            }
        }

        // Handle image
        if (pictureElement != null)
        {
            pictureElement.style.backgroundImage = null;
            pictureElement.UnregisterCallback<ClickEvent>(OnPictureClicked);

            if (entry.category == "Programovanie" && !string.IsNullOrEmpty(entry.video))
            {
                // Register click handler for video
                pictureElement.RegisterCallback<ClickEvent>(evt => videoPlayerUI?.PlayVideo(entry.video));

                if (!string.IsNullOrEmpty(entry.photo))
                {
                    Texture2D texture = Resources.Load<Texture2D>(entry.photo);
                    if (texture != null)
                    {
                        pictureElement.style.backgroundImage = new StyleBackground(texture);
                    }
                    else
                    {
                        Debug.Log($"[CodexUI] Obrázok {entry.photo} sa nenašiel v Resources!");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(entry.photo))
            {
                Texture2D texture = Resources.Load<Texture2D>(entry.photo);
                if (texture != null)
                {
                    pictureElement.style.backgroundImage = new StyleBackground(texture);
                }
                else
                {
                    Debug.LogError($"[CodexUI] Obrázok {entry.photo} sa nenašiel v Resources!");
                }
            }
        }
    }

    private void OnPictureClicked(ClickEvent evt)
    {
        // This will be set dynamically in ShowEntryDetails if needed
    }

    private void ResetAll()
    {
        if (entryNameLabel != null)
        {
            entryNameLabel.text = "";
        }
        
        if (infoScrollView != null)
        {
            infoScrollView.Clear();
        }

        if (pictureElement != null)
        {
            pictureElement.style.backgroundImage = null;
        }

        if (subCategoriesList != null)
        {
            subCategoriesList.itemsSource = new List<CodexEntry>();
            subCategoriesList.Rebuild();
        }

        if (categoriesList != null)
        {
            categoriesList.ClearSelection();
        }

        if (subCategoriesList != null)
        {
            subCategoriesList.ClearSelection();
        }

        lastCategoryButton = null;
        lastSubButton = null;
    }

    public override void CloseWindow()
    {
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.None;
        }
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.Flex;
        }
        IsMenuOn = true;
        ResetAll();
    }

    private string RemoveTitles(string fullName)
    {
        string[] titles = { "doc.", "Ing.", "Mgr.", "PhD.", "RNDr.", "Bc.", "MUDr." };
        var nameParts = fullName.Split(' ').ToList();
        nameParts.RemoveAll(part => titles.Contains(part));
        for (int i = 0; i < nameParts.Count; i++)
        {
            nameParts[i] = nameParts[i].Replace(",", "");
        }
        return string.Join(" ", nameParts);
    }
}
