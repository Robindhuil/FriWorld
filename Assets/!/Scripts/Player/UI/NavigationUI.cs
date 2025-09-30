using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

public class NavigationUI : BaseUi
{
    private readonly Canvas navigationCanvas;
    public bool IsMenuOn { get; private set; }

    private readonly GameObject sectorAPanel;
    private readonly GameObject sectorBPanel;
    private readonly GameObject sectorCPanel;
    private readonly Button trackRoomButton;
    private RawImage qrImage;

    public RoomData TrackedRoom { get; set; }
    public RoomData SelectedRoom { get; set; }

    private readonly Dictionary<RoomData, Button> roomButtons = new();
    private readonly Color defaultButtonColor = new(0, 0, 0, 0);
    private readonly Color trackedButtonColor = new(0, 0.8f, 0.2f, 0.6f);
    private readonly Color selectedButtonColor = new(0, 0.6f, 1f, 0.6f);
    private readonly Color defaultLevelButtonColor = new Color(0f, 0.6f, 1f, 1f);
    private readonly Color selectedLevelButtonColor = new Color(0, 0.8f, 0.2f, 0.6f);
    private TextMeshProUGUI department;
    private TextMeshProUGUI roomName;
    private TextMeshProUGUI originalName;
    private TextMeshProUGUI function;
    private TextMeshProUGUI profeList;
    private QRCodeGenerator qrCodeGenerator;
    private GameObject prefabButton;
    private MonoBehaviour runner;

    private GameObject LevelButtonA;
    private GameObject LevelButtonB;
    private GameObject LevelButtonC;

    private readonly Dictionary<GameObject, Dictionary<int, List<Button>>> roomButtonsByLevel = new();
    private readonly Dictionary<GameObject, Dictionary<string, Button>> levelButtonsBySector = new();
    private readonly Dictionary<GameObject, int> currentFilteredLevel = new();

    public NavigationUI(Canvas canvas, GameObject sectorA, GameObject sectorB, GameObject sectorC, Button trackButton,
                    TextMeshProUGUI department, TextMeshProUGUI roomName, TextMeshProUGUI originalName,
                    TextMeshProUGUI function, TextMeshProUGUI profeList, RawImage roomImage, GameObject prefabButton, GameObject list,
                    MonoBehaviour runner)
    {
        if (canvas == null || sectorA == null || sectorB == null || sectorC == null)
        {
            Debug.LogError("[NavigationUI] One of the input objects is null!");
            return;
        }

        this.runner = runner;
        navigationCanvas = canvas;
        navigationCanvas.gameObject.SetActive(false);

        sectorAPanel = sectorA;
        sectorBPanel = sectorB;
        sectorCPanel = sectorC;
        trackRoomButton = trackButton;
        trackRoomButton.gameObject.SetActive(false);

        this.department = department;
        this.roomName = roomName;
        this.originalName = originalName;
        this.function = function;
        this.profeList = profeList;
        this.qrImage = roomImage;
        this.prefabButton = prefabButton;

        LevelButtonA = list.transform.Find("SectorA").gameObject;
        LevelButtonB = list.transform.Find("SectorB").gameObject;
        LevelButtonC = list.transform.Find("SectorC").gameObject;

        GenerateRoomButtons();
        qrCodeGenerator = new QRCodeGenerator(qrImage);
    }

    private void GenerateRoomButtons()
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogError("[NavigationUI] RoomManager is not initialized!");
            return;
        }

        GenerateButtonsForSector(RoomManager.Instance.SectorA, sectorAPanel);
        GenerateButtonsForSector(RoomManager.Instance.SectorB, sectorBPanel);
        GenerateButtonsForSector(RoomManager.Instance.SectorC, sectorCPanel);
    }

    private void GenerateButtonsForSector(List<RoomData> sector, GameObject parentPanel)
    {
        if (sector == null || parentPanel == null || prefabButton == null)
        {
            Debug.LogError("[NavigationUI] Sector, parentPanel or prefabButton is null!");
            return;
        }

        roomButtonsByLevel[parentPanel] = new Dictionary<int, List<Button>>();
        currentFilteredLevel[parentPanel] = -1;

        runner.StartCoroutine(GenerateSectorButtonsCoroutine(sector, parentPanel));
    }

    private IEnumerator GenerateSectorButtonsCoroutine(List<RoomData> sector, GameObject parentPanel)
    {
        foreach (RoomData room in sector)
        {
            yield return CreateButton(prefabButton, parentPanel.transform, room, parentPanel);
        }
        SetupLevelFilters();
    }

    private IEnumerator CreateButton(GameObject prefab, Transform parent, RoomData room, GameObject sectorPanel)
    {
        yield return null;

        GameObject buttonObj = Object.Instantiate(prefab, parent);
        buttonObj.name = room.Name;

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[NavigationUI] Prefab button doesn't contain Button component!");
            yield break;
        }

        button.onClick.AddListener(() => SelectRoom(room));
        roomButtons[room] = button;

        SetupButtonAppearance(buttonObj, room, parent);

        int level = ParseFloorLevel(room.Name);
        if (!roomButtonsByLevel[sectorPanel].ContainsKey(level))
        {
            roomButtonsByLevel[sectorPanel][level] = new List<Button>();
        }
        roomButtonsByLevel[sectorPanel][level].Add(button);

        button.gameObject.SetActive(currentFilteredLevel[sectorPanel] == -1 || currentFilteredLevel[sectorPanel] == level);
    }

    private void SetupButtonAppearance(GameObject buttonObj, RoomData room, Transform parent)
    {
        Image image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = GetRoomColor(room);
        }

        TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = room.Name ?? "Unnamed Room";
        }

        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(-30f, 45f);

            int buttonIndex = parent.childCount - 1;
            float paddingTop = -15f;
            float paddingBot = 15f;
            float buttonSpacing = 5f;

            rectTransform.anchoredPosition = new Vector2(0, paddingTop - buttonIndex * (45f + buttonSpacing));

            RectTransform contentRectTransform = parent.GetComponent<RectTransform>();
            contentRectTransform.sizeDelta = new Vector2(
                contentRectTransform.sizeDelta.x,
                (45f + buttonSpacing) * parent.childCount + buttonSpacing + paddingBot
            );
        }

        AddHoverEffect(buttonObj, image, room);
    }

    private void SetupLevelFilters()
    {
        var sectorPanels = new Dictionary<GameObject, GameObject>()
    {
        { LevelButtonA, sectorAPanel },
        { LevelButtonB, sectorBPanel },
        { LevelButtonC, sectorCPanel }
    };

        foreach (var kvp in sectorPanels)
        {
            GameObject levelButtonPanel = kvp.Key;
            GameObject roomPanel = kvp.Value;

            Transform levelButtonsContainer = levelButtonPanel.transform.Find("LevelButtons");
            if (levelButtonsContainer == null)
            {
                Debug.LogError($"[NavigationUI] LevelButtons container not found in {levelButtonPanel.name}");
                continue;
            }

            levelButtonsBySector[roomPanel] = new Dictionary<string, Button>();

            foreach (Transform buttonTransform in levelButtonsContainer)
            {
                var button = buttonTransform.GetComponent<Button>();
                if (button != null)
                {
                    string levelName = button.name;
                    levelButtonsBySector[roomPanel][levelName] = button;

                    SetLevelButtonColor(button, false);

                    if (levelName == "AllLevels")
                    {
                        button.onClick.AddListener(() =>
                        {
                            ResetLevelButtonsColor(roomPanel);
                            SetLevelButtonColor(button, true);
                            currentFilteredLevel[roomPanel] = -1;
                            ApplyLevelFilter(roomPanel);
                        });
                    }
                    else if (int.TryParse(levelName.Replace("Level", ""), out int level))
                    {
                        button.onClick.AddListener(() =>
                        {
                            ResetLevelButtonsColor(roomPanel);
                            SetLevelButtonColor(button, true);
                            currentFilteredLevel[roomPanel] = level;
                            ApplyLevelFilter(roomPanel);
                        });
                    }
                }
            }
        }
    }

    private void ResetLevelButtonsColor(GameObject sectorPanel)
    {
        if (levelButtonsBySector.TryGetValue(sectorPanel, out var buttons))
        {
            foreach (var button in buttons.Values)
            {
                SetLevelButtonColor(button, false);
            }
        }
    }

    private void SetLevelButtonColor(Button button, bool isSelected)
    {
        var colors = button.colors;
        colors.normalColor = isSelected ? selectedLevelButtonColor : defaultLevelButtonColor;
        colors.selectedColor = isSelected ? selectedLevelButtonColor : defaultLevelButtonColor;
        colors.highlightedColor = isSelected ?
            Color.Lerp(selectedLevelButtonColor, Color.white, 0.2f) :
            Color.Lerp(defaultLevelButtonColor, Color.white, 0.2f);
        button.colors = colors;

        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
    }

    private void ApplyLevelFilter(GameObject sectorPanel)
    {
        if (!roomButtonsByLevel.ContainsKey(sectorPanel)) return;

        int targetLevel = currentFilteredLevel[sectorPanel];
        Transform contentTransform = sectorPanel.transform;
        float buttonHeight = 45f;
        float buttonSpacing = 5f;
        float currentYPosition = -15f;

        foreach (var levelPair in roomButtonsByLevel[sectorPanel])
        {
            foreach (var button in levelPair.Value)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0, currentYPosition);
                }
            }
        }

        foreach (var levelPair in roomButtonsByLevel[sectorPanel])
        {
            bool shouldShow = (targetLevel == -1) || (levelPair.Key == targetLevel);

            foreach (var button in levelPair.Value)
            {
                if (button != null && shouldShow)
                {
                    button.gameObject.SetActive(true);
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0, currentYPosition);
                    currentYPosition -= (buttonHeight + buttonSpacing);
                }
            }
        }

        RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
        float contentHeight = Mathf.Abs(currentYPosition) + 15f;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void AddHoverEffect(GameObject buttonObj, Image image, RoomData room)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entryEnter = new() { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((_) =>
        {
            if (room != SelectedRoom && room != TrackedRoom)
                image.color = new Color(1, 1, 1, 0.3f);
        });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new() { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((_) => image.color = GetRoomColor(room));
        trigger.triggers.Add(entryExit);
    }

    private int ParseFloorLevel(string roomName)
    {
        Match match = Regex.Match(roomName, @"\D+(\d)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int level) ? level : -1;
    }


    private Color GetRoomColor(RoomData room)
    {
        if (room == TrackedRoom) return trackedButtonColor;
        if (room == SelectedRoom) return selectedButtonColor;
        return defaultButtonColor;
    }

    public void SelectRoom(RoomData room)
    {
        if (room == null)
        {
            Debug.LogError("[NavigationUI] RoomData je null! Nie je možné vybrať miestnosť.");
            return;
        }

        SelectedRoom = room;
        trackRoomButton.gameObject.SetActive(true);

        RefreshRoomButtonColors();

        UpdateRoomInfo(room);

        UpdateRoomButtonColor();
    }

    public void UpdateRoomInfo(RoomData room)
    {
        if (department != null)
            department.text = room.Department;
        else
            Debug.LogError("[NavigationUI] department TextMeshProUGUI is not assigned!");

        if (roomName != null)
            roomName.text = room.Name;
        else
            Debug.LogError("[NavigationUI] roomName TextMeshProUGUI is not assigned!");

        if (originalName != null)
            originalName.text = $"{room.OriginalCode} - pôvodné označenie";
        else
            Debug.LogError("[NavigationUI] originalName TextMeshProUGUI is not assigned!");

        if (function != null)
            function.text = room.Function;
        else
            Debug.LogError("[NavigationUI] function TextMeshProUGUI is not assigned!");

        if (profeList != null)
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

                profeList.text = $"\n{string.Join("\n", professorsText)}";
            }


            else
            {
                profeList.text = "";
            }
        }
        else
        {
            Debug.LogError("[NavigationUI] profeList TextMeshProUGUI is not assigned!");
        }

        Debug.Log($"[NavigationUI] Vybraná miestnosť: {room.Name}");
        GenerateRoomQRCode();
    }

    private int GetLevelFromRoom(RoomData room)
    {
        if (room.Name.Length >= 3 && int.TryParse(room.Name.Substring(2, 1), out int level))
            return level;

        return -1;
    }



    public void TrackRoom(RoomData room)
    {
        if (room == null)
        {
            Debug.LogError("[NavigationUI] RoomData je null! Nie je možné sledovať miestnosť.");
            return;
        }

        TrackedRoom = room;
        RefreshRoomButtonColors();
        UpdateRoomButtonColor();
    }

    public void UntrackRoom()
    {
        TrackedRoom = null;
        RefreshRoomButtonColors();
        UpdateRoomButtonColor();
    }

    private void UpdateRoomButtonColor()
    {
        if (TrackedRoom != null && SelectedRoom == TrackedRoom)
        {
            trackRoomButton.GetComponentInChildren<TextMeshProUGUI>().text = "Prestať sledovať miestnosť";
            trackRoomButton.GetComponent<Image>().color = trackedButtonColor;

        }
        else
        {
            trackRoomButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sledovať miestnosť";
            trackRoomButton.GetComponent<Image>().color = Color.white;

        }
    }

    private void RefreshRoomButtonColors()
    {
        foreach (var (room, button) in roomButtons)
        {
            var image = button.GetComponent<Image>();
            image.color = GetRoomColor(room);
        }
    }

    public void ClearSelectedRoom()
    {
        if (SelectedRoom != null && roomButtons.TryGetValue(SelectedRoom, out Button button))
        {
            button.GetComponent<Image>().color = (SelectedRoom == TrackedRoom) ? trackedButtonColor : defaultButtonColor;
        }
        if (SelectedRoom != null)
        {
            SelectedRoom = null;
        }
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
        navigationCanvas?.gameObject.SetActive(false);
        ClearSelectedRoom();
        UpdateRoomButtonColor();
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        navigationCanvas?.gameObject.SetActive(true);
        IsMenuOn = true;

        foreach (var sector in levelButtonsBySector)
        {
            if (currentFilteredLevel.TryGetValue(sector.Key, out int level))
            {
                ApplyLevelFilter(sector.Key);
                ResetLevelButtonsColor(sector.Key);

                string buttonName = level == -1 ? "AllLevels" : $"Level{level}";
                if (sector.Value.TryGetValue(buttonName, out Button activeButton))
                {
                    SetLevelButtonColor(activeButton, true);
                }
            }
        }
    }
}