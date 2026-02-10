using System.Collections;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

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
    [SerializeField] private AudioMixer audioMixer;
    [Header("UI Documents")]
    [SerializeField] private UIDocument playerUIDocument;
    [SerializeField] private UIDocument playerWindowsDocument;
    [SerializeField] private UIDocument dialogueUIDocument;
    [Header("UI Canvases")]
    [SerializeField] private Canvas menuCanvas;



    void Awake()
    {
        playerUI = new PlayerUI(playerUIDocument, this);
        inputManager = GetComponent<InputManager>();
        dialogueUI = new DialogueUI(dialogueUIDocument, inputManager, this, this);
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
        
        // Ensure all UIs start closed
        CloseAll();
        OpenPlayerUI();

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
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public void ClosePlayerUI()
    {
        CloseAll();
        inputManager.onFoot.Look.Disable();
        inputManager.onFoot.Movement.Disable();
        tabUI.OpenWindow();
        playerUI.CloseWindow();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

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
        if (tabUI.IsMenuOn)
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
        Journal journal = player.PlayerManagment.journal;
        journalUI = new JournalUI(playerWindowsDocument, journal);
        journalUI.SetTrackButtonCallback(TrackQuest);
    }

    public void InitializeTabUI()
    {
        tabUI = new TabUI(playerWindowsDocument);
        
        // Connect button clicks to actions (order: Journal, Codex, Navigation, Stats)
        tabUI.Buttons[0].clicked += OpenJournal;
        tabUI.Buttons[1].clicked += OpenCodex;
        tabUI.Buttons[2].clicked += OpenNavigation;
        tabUI.Buttons[3].clicked += OpenStats;
    }

    public void InitializeNavigationUI()
    {
        navigationUI = new NavigationUI(playerWindowsDocument, this);
        navigationUI.TrackButton.clicked += TrackRoom;
    }

    public void InitializeCodexUI()
    {
        codexUI = new CodexUI(playerWindowsDocument, this, audioMixer);
    }
    public void InitializeStatsUI()
    {
        statsUI = new StatsUI(playerWindowsDocument, this);
    }

    public void OpenJournal()
    {
        CloseAll();
        journalUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[0]);
    }

    public void OpenNavigation()
    {
        CloseAll();
        navigationUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[2]);
    }

    public void OpenCodex()
    {
        CloseAll();
        codexUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[1]);
    }
    public void OpenStats()
    {
        CloseAll();
        statsUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[3]);
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

            if (journalUI.TrackedQuest == null)
            {
                // Untracking the quest
                playerUI.HideQuestInfo();
                nav.ClearQuestPath();
                nav.DrawQuestLine = false;
            }
            else if (journalUI.TrackedQuest == journalUI.SelectedQuest)
            {
                // Tracking the quest
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
