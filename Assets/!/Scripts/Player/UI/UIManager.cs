using System.Collections;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PlayerUI playerUI;
    public DialogueUI dialogueUI;
    private JournalUI journalUI;
    public NavigationUI navigationUI;
    private TabUI tabUI;
    private StatsUI statsUI;
    private CodexUI codexUI;
    private InputManager inputManager;
    private Player player;
    [Header("UI Prefabs")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private AudioMixer audioMixer;
    [Header("UI Canvases")]
    [SerializeField] private Canvas playerCanvas;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Canvas journalCanvas;
    [SerializeField] private Canvas navigationCanvas;
    [SerializeField] private Canvas tabCanvas;
    [SerializeField] private Canvas codexCanvas;
    [SerializeField] private Canvas statsCanvas;
    [SerializeField] private Canvas menuCanvas;



    void Awake()
    {
        playerUI = new PlayerUI(playerCanvas, this);
        inputManager = GetComponent<InputManager>();
        dialogueUI = new DialogueUI(dialogueCanvas, inputManager, this, buttonPrefab, this);
    }

    void Start()
    {
        player = GetComponent<Player>();
        InitializeJournalUI();
        InitializeTabUI();
        InitializeNavigationUI();
        InitializeCodexUI();
        InitializeStatsUI();
        menuCanvas.gameObject.SetActive(false);

        if (inputManager != null)
        {
            inputManager.onFoot.OpenJournal.performed += OnOpenJournal;
            inputManager.onFoot.OpenManager.performed += OnOpenManager;
            inputManager.onFoot.OpenNavigation.performed += OnOpenNavigation;
            inputManager.onFoot.OpenStats.performed += OnOpenStats;
            inputManager.onFoot.OpenCodex.performed += OnOpenCodex;
            inputManager.onFoot.OpenMenu.performed += OnOpenMenu;


        }
    }

    public void OpenPlayerUI()
    {
        CloseAll();
        playerUI.OpenWindow();
        tabUI.CloseWindow();
        inputManager.onFoot.Look.Enable();
        inputManager.onFoot.Movement.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ClosePlayerUI()
    {
        CloseAll();
        inputManager.onFoot.Look.Disable();
        inputManager.onFoot.Movement.Disable();
        tabUI.OpenWindow();
        playerUI.CloseWindow();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }

    public void CloseAll()
    {
        playerUI.CloseWindow();
        journalUI.CloseWindow();
        navigationUI.CloseWindow();
        codexUI.CloseWindow();
        statsUI.CloseWindow();
        menuCanvas.gameObject.SetActive(false);
    }

    private void OnOpenJournal(InputAction.CallbackContext context)
    {
        if (journalUI.IsMenuOn)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            journalUI.OpenWindow();
            tabUI.SetActiveButton(tabUI.Buttons[0]);
        }
    }


    private void OnOpenManager(InputAction.CallbackContext context)
    {
        if (tabUI.IsMenuOn)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            journalUI.OpenWindow();
            tabUI.SetActiveButton(tabUI.Buttons[0]);
        }
    }

    private void OnOpenNavigation(InputAction.CallbackContext context)
    {
        if (navigationUI.IsMenuOn)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            navigationUI.OpenWindow();
            tabUI.SetActiveButton(tabUI.Buttons[2]);
        }
    }

    private void OnOpenStats(InputAction.CallbackContext context)
    {
        if (statsUI.IsMenuOn)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            statsUI.OpenWindow();
            tabUI.SetActiveButton(tabUI.Buttons[3]);
        }
    }

    private void OnOpenCodex(InputAction.CallbackContext context)
    {
        if (codexUI.IsMenuOn)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            codexUI.OpenWindow();
            tabUI.SetActiveButton(tabUI.Buttons[1]);
        }
    }


    private void OnOpenMenu(InputAction.CallbackContext context)
    {
        if (tabCanvas.gameObject.activeSelf)
        {
            CloseAll();
            OpenPlayerUI();
        }
        else if (menuCanvas.gameObject.activeSelf)
        {
            OpenPlayerUI();

        }
        else
        {
            ClosePlayerUI();
            tabUI.CloseWindow();
            menuCanvas.gameObject.SetActive(true);
            menuCanvas.gameObject.transform.Find("Menu").GetComponent<MainMenu>().ShowMusicPanel();
        }
    }

    public void OpenMenu()
    {
        if (menuCanvas.gameObject.activeSelf)
        {
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            tabUI.CloseWindow();
            menuCanvas.gameObject.SetActive(true);
            menuCanvas.gameObject.transform.Find("Menu").GetComponent<MainMenu>().ShowMusicPanel();
        }
    }

    public void InitializeJournalUI()
    {

        TextMeshProUGUI questName = journalCanvas.transform.Find("Background/QuestInfo/QuestName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI questObjective = journalCanvas.transform.Find("Background/QuestInfo/QuestObjective")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI questInfo = journalCanvas.transform.Find("Background/QuestInfo/Info")?.GetComponent<TextMeshProUGUI>();
        GameObject questList = journalCanvas.transform.Find("Background/QuestList/List/Scroll/Viewport/Content")?.gameObject;
        Button trackButton = journalCanvas.transform.Find("Background/QuestInfo/TrackButton")?.GetComponentInChildren<Button>();


        Journal journal = player.PlayerManagment.journal;
        journalUI = new JournalUI(journalCanvas, journal, questName, questObjective, questInfo, questList, trackButton, buttonPrefab, this);
        trackButton.onClick.AddListener(TrackQuest);
    }

    public void InitializeTabUI()
    {
        Button journalButton = tabCanvas.transform.Find("Background/Journal")?.GetComponentInChildren<Button>();
        Button codexButton = tabCanvas.transform.Find("Background/Codex")?.GetComponentInChildren<Button>();
        Button navigationButton = tabCanvas.transform.Find("Background/Navigation")?.GetComponentInChildren<Button>();
        Button statsButton = tabCanvas.transform.Find("Background/Stats")?.GetComponentInChildren<Button>();

        tabUI = new TabUI(tabCanvas, journalButton, codexButton, navigationButton, statsButton);
        journalButton.onClick.AddListener(OpenJournal);
        navigationButton.onClick.AddListener(OpenNavigation);
        codexButton.onClick.AddListener(OpenCodex);
        statsButton.onClick.AddListener(OpenStats);


    }

    public void InitializeNavigationUI()
    {
        GameObject sectorA = navigationCanvas.transform.Find("Background/RoomList/List/SectorA/Scroll/Viewport/Content")?.gameObject;
        GameObject sectorB = navigationCanvas.transform.Find("Background/RoomList/List/SectorB/Scroll/Viewport/Content")?.gameObject;
        GameObject sectorC = navigationCanvas.transform.Find("Background/RoomList/List/SectorC/Scroll/Viewport/Content")?.gameObject;
        GameObject list = navigationCanvas.transform.Find("Background/RoomList/List")?.gameObject;
        Button trackButton = navigationCanvas.transform.Find("Background/RoomList/TrackRoom")?.GetComponentInChildren<Button>();
        TextMeshProUGUI department = navigationCanvas.transform.Find("Background/RoomInfo/Info/Top/Left/Department")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI roomName = navigationCanvas.transform.Find("Background/RoomInfo/Info/Mid/NameBackground/Name")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI originalName = navigationCanvas.transform.Find("Background/RoomInfo/Info/Mid/Original")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI function = navigationCanvas.transform.Find("Background/RoomInfo/Info/Mid/Function")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI profeList = navigationCanvas.transform.Find("Background/RoomInfo/Info/Bot/List")?.GetComponent<TextMeshProUGUI>();
        RawImage qrImage = navigationCanvas.transform.Find("Background/RoomInfo/Info/Bot/QR").GetComponent<RawImage>();

        navigationUI = new NavigationUI(navigationCanvas, sectorA, sectorB, sectorC, trackButton,
        department, roomName, originalName, function, profeList, qrImage,
         buttonPrefab, list, this);

        trackButton.onClick.AddListener(TrackRoom);
    }

    public void InitializeCodexUI()
    {
        GameObject categoryList = codexCanvas.transform.Find("Background/Category/List/Scroll/Viewport/Content")?.gameObject;
        GameObject subList = codexCanvas.transform.Find("Background/SubCategory/SubList/Scroll/Viewport/Content")?.gameObject;
        TextMeshProUGUI infoName = codexCanvas.transform.Find("Background/Info/InfoList/Name")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI infoDesc = codexCanvas.transform.Find("Background/Info/InfoList/InfoBackground/Info/Scroll/Viewport/Content").GetComponent<TextMeshProUGUI>();
        RawImage photo = codexCanvas.transform.Find("Background/Info/InfoList/InfoBackground/PhotoBackground/Photo")?.GetComponent<RawImage>();

        codexUI = new CodexUI(codexCanvas, categoryList, subList, infoName, infoDesc, photo, buttonPrefab, this, audioMixer);
    }
    public void InitializeStatsUI()
    {
        TextMeshProUGUI questCount = statsCanvas.transform.Find("Background/Panel1/QuestCount")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI secretCount = statsCanvas.transform.Find("Background/Panel1/SecretCount")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI mistakeCount = statsCanvas.transform.Find("Background/Panel1/MistakeCount")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI walkCount = statsCanvas.transform.Find("Background/Panel1/WalkCount")?.GetComponent<TextMeshProUGUI>();

        statsUI = new StatsUI(statsCanvas, questCount, secretCount, mistakeCount, walkCount, this);
    }

    public void OpenJournal()
    {
        CloseAll();
        journalUI.OpenWindow();
    }

    public void OpenNavigation()
    {
        CloseAll();
        navigationUI.OpenWindow();
    }

    public void OpenCodex()
    {
        CloseAll();
        codexUI.OpenWindow();
    }
    public void OpenStats()
    {
        CloseAll();
        statsUI.OpenWindow();
    }


    public void TrackQuest()
    {
        if (journalUI.SelectedQuest != null)
        {
            Navigation nav = GetComponent<Navigation>();
            Journal journal = player.PlayerManagment.journal;

            if (journal.GetQuestTransform(journalUI.SelectedQuest.id) == null)
            {
                return;
            }
            Transform questTransform = journal.GetQuestTransform(journalUI.SelectedQuest.id);

            if (nav.QuestDestination == questTransform)
            {
                playerUI.HideQuestInfo();
                nav.ClearQuestPath();
                nav.DrawQuestLine = false;
            }
            else if (questTransform != null)
            {
                playerUI.DisplayQuest(journalUI.TrackedQuest.questName, journalUI.TrackedQuest.questObjective);
                nav.QuestDestination = questTransform;
                nav.DrawQuestLine = true;
            }

        }
    }

    public void TrackRoom()
    {
        if (navigationUI.SelectedRoom != null)
        {
            Navigation nav = GetComponent<Navigation>();
            Transform roomTransform = navigationUI.SelectedRoom.RoomTransform;

            if (nav.RoomDestination == roomTransform)
            {
                nav.ClearRoomPath();
                nav.DrawRoomLine = false;
                navigationUI.UntrackRoom();
            }
            else if (roomTransform != null)
            {
                nav.RoomDestination = roomTransform;
                navigationUI.TrackRoom(navigationUI.SelectedRoom);
                nav.DrawRoomLine = true;
            }

        }
    }

}
