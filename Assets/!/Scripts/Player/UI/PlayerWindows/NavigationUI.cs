using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;

public class NavigationUI : BaseUi
{
    private readonly VisualElement root;
    private readonly VisualElement navigationUIElement;
    private GlobalButtonClickSound globalButtonClickSound;
    public bool IsMenuOn { get; private set; }

    private readonly ListView sectorAListView;
    private readonly ListView sectorBListView;
    private readonly ListView sectorCListView;
    private readonly UnityEngine.UIElements.Button trackRoomButton;
    private readonly UnityEngine.UIElements.Image qrImage;

    public UnityEngine.UIElements.Button TrackButton => trackRoomButton;

    public RoomData TrackedRoom { get; set; }
    public RoomData SelectedRoom { get; set; }


    
    private Label departmentLabel;
    private Label roomNameLabel;
    private Label originalNameLabel;
    private Label functionLabel;
    private Label staffListLabel;
    private QRCodeGenerator qrCodeGenerator;

    private VisualElement sectorAListBase;
    private VisualElement sectorBListBase;
    private VisualElement sectorCListBase;

    private readonly Dictionary<VisualElement, Dictionary<int, List<RoomData>>> roomsByLevel = new();
    private readonly Dictionary<VisualElement, Dictionary<string, UnityEngine.UIElements.Button>> levelButtonsBySector = new();
    private readonly Dictionary<VisualElement, int> currentFilteredLevel = new();

    private List<RoomData> sectorAData;
    private List<RoomData> sectorBData;
    private List<RoomData> sectorCData;

    public NavigationUI(UIDocument sharedDocument, GlobalButtonClickSound buttonClickSound, MonoBehaviour runner)
    {
        if (sharedDocument == null)
        {
            Debug.LogError("[NavigationUI] UIDocument is null!");
            return;
        }

        globalButtonClickSound = buttonClickSound;

        root = sharedDocument.rootVisualElement;
        navigationUIElement = root.Q<VisualElement>("NavigationUI");
        
        if (navigationUIElement == null)
        {
            Debug.LogError("[NavigationUI] NavigationUI element not found in UXML!");
            return;
        }

        // Get the three sector list bases (they're all named SectorAListBase in the UXML)
        var sectorListBases = root.Query<VisualElement>("SectorAListBase").ToList();
        if (sectorListBases.Count >= 3)
        {
            sectorAListBase = sectorListBases[0];
            sectorBListBase = sectorListBases[1];
            sectorCListBase = sectorListBases[2];
        }
        else
        {
            Debug.LogError("[NavigationUI] Not enough SectorAListBase elements found!");
            return;
        }

        // Get ListViews
        sectorAListView = sectorAListBase?.Q<ListView>("ListView");
        sectorBListView = sectorBListBase?.Q<ListView>("ListView");
        sectorCListView = sectorCListBase?.Q<ListView>("ListView");

        // Get track button
        trackRoomButton = root.Q<UnityEngine.UIElements.Button>("TrackButton");
        trackRoomButton.style.display = DisplayStyle.None;
        
        // Setup track button styling and hover
        if (trackRoomButton != null)
        {
            trackRoomButton.clicked += () =>
            {
                globalButtonClickSound?.PlayClickSound();
            };
            trackRoomButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                UpdateTrackButtonHoverAppearance();
            });
            trackRoomButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                UpdateTrackButtonAppearance();
            });
        }

        // Get room info labels
        departmentLabel = root.Q<Label>("DepartmentName");
        roomNameLabel = root.Q<Label>("RoomName");
        originalNameLabel = root.Q<Label>("OriginalName");
        functionLabel = root.Q<Label>("Function");
        staffListLabel = root.Q<Label>("StaffList");
        qrImage = root.Q<UnityEngine.UIElements.Image>("QR");

        InitializeRoomData();
        SetupListViews();
        SetupLevelFilters();
        
        qrCodeGenerator = new QRCodeGenerator(qrImage, runner);
        
        CloseWindow();
    }

    private void InitializeRoomData()
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogError("[NavigationUI] RoomManager is not initialized!");
            return;
        }

        sectorAData = RoomManager.Instance.SectorA ?? new List<RoomData>();
        sectorBData = RoomManager.Instance.SectorB ?? new List<RoomData>();
        sectorCData = RoomManager.Instance.SectorC ?? new List<RoomData>();

        // Sort room data alphabetically/numerically
        sectorAData.Sort((a, b) => NaturalSort(a.Name, b.Name));
        sectorBData.Sort((a, b) => NaturalSort(a.Name, b.Name));
        sectorCData.Sort((a, b) => NaturalSort(a.Name, b.Name));

        // Initialize dictionaries
        roomsByLevel[sectorAListBase] = new Dictionary<int, List<RoomData>>();
        roomsByLevel[sectorBListBase] = new Dictionary<int, List<RoomData>>();
        roomsByLevel[sectorCListBase] = new Dictionary<int, List<RoomData>>();

        currentFilteredLevel[sectorAListBase] = -1;
        currentFilteredLevel[sectorBListBase] = -1;
        currentFilteredLevel[sectorCListBase] = -1;

        // Organize rooms by level
        OrganizeRoomsByLevel(sectorAData, sectorAListBase);
        OrganizeRoomsByLevel(sectorBData, sectorBListBase);
        OrganizeRoomsByLevel(sectorCData, sectorCListBase);
    }

    private void OrganizeRoomsByLevel(List<RoomData> rooms, VisualElement sectorBase)
    {
        foreach (var room in rooms)
        {
            int level = ParseFloorLevel(room.Name);
            if (!roomsByLevel[sectorBase].ContainsKey(level))
            {
                roomsByLevel[sectorBase][level] = new List<RoomData>();
            }
            roomsByLevel[sectorBase][level].Add(room);
        }
    }

    private void SetupListViews()
    {
        SetupListView(sectorAListView, sectorAData);
        SetupListView(sectorBListView, sectorBData);
        SetupListView(sectorCListView, sectorCData);
    }

    private void SetupListView(ListView listView, List<RoomData> roomData)
    {
        if (listView == null) return;

        listView.itemsSource = roomData;
        listView.makeItem = () =>
        {
            var button = new UnityEngine.UIElements.Button();
            button.AddToClassList("roomListButton");
            
            // Register click handler once - it will read room from userData
            button.clicked += () =>
            {
                globalButtonClickSound?.PlayClickSound();
                if (button.userData is RoomData room)
                {
                    SelectRoom(room);
                }
            };
            
            // Manual hover handling for ListView buttons
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                var btn = evt.currentTarget as UnityEngine.UIElements.Button;
                if (btn != null && btn.userData is RoomData room)
                {
                    var colorSet = GetRoomColorSet(room);
                    btn.style.backgroundColor = colorSet.HoverBackgroundColor;
                    btn.style.color = colorSet.HoverTextColor;
                }
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                var btn = evt.currentTarget as UnityEngine.UIElements.Button;
                if (btn != null && btn.userData is RoomData room)
                {
                    // Restore the room-specific color
                    UpdateRoomElementColor(btn, room);
                }
            });
            
            return button;
        };

        listView.bindItem = (element, index) =>
        {
            var button = element as UnityEngine.UIElements.Button;
            var currentList = listView.itemsSource as List<RoomData>;
            
            if (currentList == null || index >= currentList.Count)
            {
                Debug.LogWarning($"[NavigationUI] Invalid index {index} for list size {currentList?.Count ?? 0}");
                return;
            }
            
            var room = currentList[index];
            button.text = room.Name;
            
            // Store room data in userData for the click handler
            button.userData = room;
            
            // Update color based on current room state
            UpdateRoomElementColor(button, room);
        };
        
        listView.unbindItem = (element, index) =>
        {
            var button = element as UnityEngine.UIElements.Button;
            button.userData = null;
        };

        listView.fixedItemHeight = 50;
        listView.selectionType = SelectionType.Single;
    }

    private void SetupLevelFilters()
    {
        SetupLevelFiltersForSector(sectorAListBase, sectorAListView, sectorAData);
        SetupLevelFiltersForSector(sectorBListBase, sectorBListView, sectorBData);
        SetupLevelFiltersForSector(sectorCListBase, sectorCListView, sectorCData);
    }

    private void SetupLevelFiltersForSector(VisualElement sectorBase, ListView listView, List<RoomData> roomData)
    {
        if (sectorBase == null) return;

        var levelButtonsContainer = sectorBase.Q<VisualElement>("LevelButtons");
        if (levelButtonsContainer == null)
        {
            Debug.LogError($"[NavigationUI] LevelButtons container not found!");
            return;
        }

        levelButtonsBySector[sectorBase] = new Dictionary<string, UnityEngine.UIElements.Button>();

        var levelButtons = levelButtonsContainer.Query<UnityEngine.UIElements.Button>().ToList();
        foreach (var button in levelButtons)
        {
            string buttonName = button.name;
            levelButtonsBySector[sectorBase][buttonName] = button;

            SetLevelButtonColor(button, false);

            // Parse level from button name (Level0, Level1, etc.)
            if (buttonName.StartsWith("Level") && int.TryParse(buttonName.Replace("Level", ""), out int level))
            {
                // Capture button and level in local variables to avoid closure issues
                var currentButton = button;
                var currentLevel = level;
                
                // Add hover effects
                currentButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    bool isSelected = currentFilteredLevel[sectorBase] == currentLevel;
                    var colorScheme = isSelected ? ButtonColorScheme.ButtonType.Success : ButtonColorScheme.ButtonType.Secondary;
                    var colorSet = ButtonColorScheme.Instance.GetColorSet(colorScheme);
                    currentButton.style.backgroundColor = colorSet.HoverBackgroundColor;
                    currentButton.style.color = colorSet.HoverTextColor;
                });
                
                currentButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    SetLevelButtonColor(currentButton, currentFilteredLevel[sectorBase] == currentLevel);
                });
                
                currentButton.clicked += () =>
                {
                    globalButtonClickSound?.PlayClickSound();
                    // Toggle behavior: clicking the active button shows all levels
                    if (currentFilteredLevel[sectorBase] == currentLevel)
                    {
                        // Show all levels
                        ResetLevelButtonsColor(sectorBase);
                        currentFilteredLevel[sectorBase] = -1;
                        ApplyLevelFilter(sectorBase, listView, roomData);
                    }
                    else
                    {
                        // Filter by selected level
                        ResetLevelButtonsColor(sectorBase);
                        SetLevelButtonColor(currentButton, true);
                        currentFilteredLevel[sectorBase] = currentLevel;
                        ApplyLevelFilter(sectorBase, listView, roomData);
                    }
                };
            }
        }

        // Apply initial filter (all levels)
        ApplyLevelFilter(sectorBase, listView, roomData);
    }

    private void ResetLevelButtonsColor(VisualElement sectorBase)
    {
        if (levelButtonsBySector.TryGetValue(sectorBase, out var buttons))
        {
            foreach (var button in buttons.Values)
            {
                SetLevelButtonColor(button, false);
            }
        }
    }

    private void SetLevelButtonColor(UnityEngine.UIElements.Button button, bool isSelected)
    {
        var buttonType = isSelected ? ButtonColorScheme.ButtonType.Success : ButtonColorScheme.ButtonType.Secondary;
        var colorSet = ButtonColorScheme.Instance.GetColorSet(buttonType);
        button.style.backgroundColor = colorSet.BackgroundColor;
        button.style.color = colorSet.TextColor;
    }

    private void ApplyLevelFilter(VisualElement sectorBase, ListView listView, List<RoomData> allRooms)
    {
        if (!currentFilteredLevel.ContainsKey(sectorBase)) return;

        int targetLevel = currentFilteredLevel[sectorBase];
        
        List<RoomData> filteredRooms;
        if (targetLevel == -1)
        {
            // Show all rooms
            filteredRooms = allRooms;
        }
        else
        {
            // Filter by level
            filteredRooms = allRooms.Where(room => ParseFloorLevel(room.Name) == targetLevel).ToList();
        }

        // Update itemsSource and rebuild
        listView.itemsSource = filteredRooms;
        listView.RefreshItems();
    }

    private int ParseFloorLevel(string roomName)
    {
        Match match = Regex.Match(roomName, @"\D+(\d)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int level) ? level : -1;
    }

    /// <summary>
    /// Natural alphanumeric sort comparison for strings containing both letters and numbers.
    /// Examples: "Room 1", "Room 2", "Room 10" (not "Room 1", "Room 10", "Room 2")
    /// </summary>
    private int NaturalSort(string a, string b)
    {
        var aSegments = Regex.Split(a, @"(\d+)");
        var bSegments = Regex.Split(b, @"(\d+)");

        for (int i = 0; i < Mathf.Min(aSegments.Length, bSegments.Length); i++)
        {
            // Check if segment is numeric
            if (int.TryParse(aSegments[i], out int aNum) && int.TryParse(bSegments[i], out int bNum))
            {
                int numCompare = aNum.CompareTo(bNum);
                if (numCompare != 0) return numCompare;
            }
            else
            {
                // String comparison
                int strCompare = string.Compare(aSegments[i], bSegments[i], System.StringComparison.CurrentCulture);
                if (strCompare != 0) return strCompare;
            }
        }

        return aSegments.Length.CompareTo(bSegments.Length);
    }

    private ButtonColorScheme.ButtonColorSet GetRoomColorSet(RoomData room)
    {
        if (room == TrackedRoom)
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
        if (room == SelectedRoom)
            return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Secondary);
        return ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Ghost);
    }

    private void UpdateRoomElementColor(VisualElement element, RoomData room)
    {
        var colorSet = GetRoomColorSet(room);
        element.style.backgroundColor = colorSet.BackgroundColor;
        element.style.color = colorSet.TextColor;
    }

    public void SelectRoom(RoomData room)
    {
        if (room == null)
        {
            Debug.LogError("[NavigationUI] RoomData je null! Nie je možné vybrať miestnosť.");
            return;
        }

        SelectedRoom = room;
        trackRoomButton.style.display = DisplayStyle.Flex;

        RefreshAllRoomColors();
        UpdateRoomInfo(room);
        UpdateTrackButtonAppearance();
    }

    public void UpdateRoomInfo(RoomData room)
    {
        if (departmentLabel != null)
            departmentLabel.text = room.Department;

        if (roomNameLabel != null)
            roomNameLabel.text = room.Name;

        if (originalNameLabel != null)
            originalNameLabel.text = $"{room.OriginalCode} - pôvodné označenie";

        if (functionLabel != null)
            functionLabel.text = room.Function;

        if (staffListLabel != null)
        {
            if (room.Professors.Count > 0)
            {
                var professorsText = room.Professors.Select(professor =>
                {
                    int index = professor.IndexOf('(');
                    if (index > 0)
                    {
                        string name = professor.Substring(0, index).Trim();
                        string faculty = professor.Substring(index).Trim();
                        return $"{name}\n<size=80%>{faculty}</size>";
                    }
                    return professor;
                });

                staffListLabel.text = $"\n{string.Join("\n", professorsText)}";
            }
            else
            {
                staffListLabel.text = "";
            }
        }

        Debug.Log($"[NavigationUI] Vybraná miestnosť: {room.Name}");
        GenerateRoomQRCode();
    }

    public void TrackRoom(RoomData room)
    {
        if (room == null)
        {
            Debug.LogError("[NavigationUI] RoomData je null! Nie je možné sledovať miestnosť.");
            return;
        }

        TrackedRoom = room;
        RefreshAllRoomColors();
        UpdateTrackButtonAppearance();
    }

    public void UntrackRoom()
    {
        TrackedRoom = null;
        RefreshAllRoomColors();
        UpdateTrackButtonAppearance();
    }

    private void UpdateTrackButtonAppearance()
    {
        if (TrackedRoom != null && SelectedRoom == TrackedRoom)
        {
            trackRoomButton.text = "Prestať sledovať miestnosť";
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            trackRoomButton.style.backgroundColor = successColorSet.BackgroundColor;
            trackRoomButton.style.color = successColorSet.TextColor;
        }
        else
        {
            trackRoomButton.text = "Sledovať miestnosť";
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackRoomButton.style.backgroundColor = primaryColorSet.BackgroundColor;
            trackRoomButton.style.color = primaryColorSet.TextColor;
        }
    }

    private void UpdateTrackButtonHoverAppearance()
    {
        if (TrackedRoom != null && SelectedRoom == TrackedRoom)
        {
            var successColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Success);
            trackRoomButton.style.backgroundColor = successColorSet.HoverBackgroundColor;
            trackRoomButton.style.color = successColorSet.HoverTextColor;
        }
        else
        {
            var primaryColorSet = ButtonColorScheme.Instance.GetColorSet(ButtonColorScheme.ButtonType.Primary);
            trackRoomButton.style.backgroundColor = primaryColorSet.HoverBackgroundColor;
            trackRoomButton.style.color = primaryColorSet.HoverTextColor;
        }
    }

    private void RefreshAllRoomColors()
    {
        // Refresh all ListViews to update colors
        sectorAListView?.RefreshItems();
        sectorBListView?.RefreshItems();
        sectorCListView?.RefreshItems();
    }

    public void ClearSelectedRoom()
    {
        SelectedRoom = null;
        RefreshAllRoomColors();
    }

    public void GenerateRoomQRCode()
    {
        if (SelectedRoom != null)
        {
            qrCodeGenerator.GenerateQRCode(SelectedRoom.URL);
        }
    }

    public override void CloseWindow()
    {
        navigationUIElement.style.display = DisplayStyle.None;
        ClearSelectedRoom();
        UpdateTrackButtonAppearance();
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        navigationUIElement.style.display = DisplayStyle.Flex;
        IsMenuOn = true;

        // Refresh filters
        foreach (var sector in levelButtonsBySector)
        {
            if (currentFilteredLevel.TryGetValue(sector.Key, out int level))
            {
                ResetLevelButtonsColor(sector.Key);

                string buttonName = $"Level{level}";
                if (sector.Value.TryGetValue(buttonName, out UnityEngine.UIElements.Button activeButton))
                {
                    SetLevelButtonColor(activeButton, true);
                }
            }
        }
    }
}