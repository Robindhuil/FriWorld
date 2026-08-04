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
    private NotificationUI notificationUI;
    private TabUI tabUI;
    private StatsUI statsUI;
    private CodexUI codexUI;
    private IdeUI ideUI;
    private QuizUI quizUI;
    private InputManager inputManager;
    private Player player;

    [Header("UI Prefabs")]
    [SerializeField]
    private AudioMixer audioMixer;

    [SerializeField]
    private GlobalButtonClickSound globalButtonClickSound;

    [Header("UI Documents")]
    [SerializeField]
    private UIDocument playerUIDocument;

    [SerializeField]
    private UIDocument playerWindowsDocument;

    [SerializeField]
    private UIDocument dialogueUIDocument;

    [SerializeField]
    private UIDocument notificationUIDocument;

    [SerializeField]
    private UIDocument ideUIDocument;

    [SerializeField]
    private UIDocument quizUIDocument;

    [Header("Menu")]
    [SerializeField]
    private UniversalMenu universalMenu;

    void Awake()
    {
        notificationUI = new NotificationUI(notificationUIDocument, this);
        playerUI = new PlayerUI(playerUIDocument, this, notificationUI);
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
        InitializeIdeUI();
        InitializeQuizUI();

        // Ensure all UIs start closed and initialize input actions
        CloseAll();
        inputManager.SwitchToOnFootActions();
        OpenPlayerUI();

        if (inputManager != null)
        {
            inputManager.OnFoot.OpenJournal.performed += OnOpenJournal;
            inputManager.OnFoot.OpenManager.performed += OnOpenManager;
            inputManager.OnFoot.OpenNavigation.performed += OnOpenNavigation;
            inputManager.OnFoot.OpenStats.performed += OnOpenStats;
            inputManager.OnFoot.OpenCodex.performed += OnOpenCodex;
            inputManager.OnFoot.OpenMenu.performed += OnOpenMenu;
            inputManager.InUI.ToggleUI.performed += OnOpenMenu;
        }
    }

    public void OpenPlayerUI()
    {
        CloseAll();
        playerUI.OpenWindow();
        notificationUI.OpenWindow();
        tabUI.CloseWindow();
        inputManager.SwitchToOnFootActions();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public void ClosePlayerUI()
    {
        CloseAll();
        inputManager.OnFoot.Look.Disable();
        inputManager.OnFoot.Movement.Disable();
        tabUI.OpenWindow();
        notificationUI.OpenWindow();
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
        notificationUI.CloseWindow();
        if (ideUI != null)
        {
            ideUI.CloseWindow();
        }
        if (quizUI != null)
        {
            quizUI.CloseWindow();
        }
        if (universalMenu != null)
        {
            universalMenu.CloseMenu();
        }
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
        if (universalMenu == null)
            return;

        // Don't allow menu toggling if it's a MainMenu
        if (universalMenu.IsMainMenu)
        {
            Debug.LogWarning("MainMenu cannot be toggled during gameplay");
            return;
        }

        if (tabUI.IsMenuOn)
        {
            CloseAll();
            OpenPlayerUI();
        }
        else if (universalMenu.IsMenuOpen)
        {
            inputManager.SwitchToOnFootActions();
            universalMenu.CloseMenu();
            notificationUI.OpenWindow();
            OpenPlayerUI();
        }
        else
        {
            inputManager.SwitchToInUIActions();
            ClosePlayerUI();
            tabUI.CloseWindow();
            notificationUI.CloseWindow();
            universalMenu.OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (universalMenu == null)
            return;

        // Don't allow menu toggling if it's a MainMenu
        if (universalMenu.IsMainMenu)
            return;

        if (universalMenu.IsMenuOpen)
        {
            universalMenu.CloseMenu();
            notificationUI.OpenWindow();
            inputManager.SwitchToOnFootActions();
            OpenPlayerUI();
        }
        else
        {
            ClosePlayerUI();
            inputManager.SwitchToInUIActions();
            tabUI.CloseWindow();
            notificationUI.CloseWindow();
            universalMenu.OpenMenu();
        }
    }

    public void InitializeJournalUI()
    {
        Journal journal = player.PlayerManagment.journal;
        journalUI = new JournalUI(playerWindowsDocument, journal, globalButtonClickSound);
        journalUI.SetTrackButtonCallback(TrackQuest);
    }

    public void InitializeTabUI()
    {
        tabUI = new TabUI(playerWindowsDocument, globalButtonClickSound);

        // Connect button clicks to actions (order: Journal, Codex, Navigation, Stats)
        tabUI.Buttons[0].clicked += OpenJournal;
        tabUI.Buttons[1].clicked += OpenCodex;
        tabUI.Buttons[2].clicked += OpenNavigation;
        tabUI.Buttons[3].clicked += OpenStats;
    }

    public void InitializeNavigationUI()
    {
        navigationUI = new NavigationUI(playerWindowsDocument, globalButtonClickSound, this);
        navigationUI.TrackButton.clicked += TrackRoom;
    }

    public void InitializeCodexUI()
    {
        codexUI = new CodexUI(playerWindowsDocument, audioMixer, globalButtonClickSound);
    }

    public void InitializeStatsUI()
    {
        statsUI = new StatsUI(playerWindowsDocument, this);
    }

    public void InitializeIdeUI()
    {
        ideUI = new IdeUI(ideUIDocument, this);
    }

    public void InitializeQuizUI()
    {
        quizUI = new QuizUI(quizUIDocument, this);
    }

    public IdeUI GetIdeUI()
    {
        return ideUI;
    }

    public QuizUI GetQuizUI()
    {
        return quizUI;
    }

    public void OpenIde()
    {
        inputManager.SwitchToInUIActions();
        inputManager.InUI.Disable();

        CloseAll();
        ideUI.OpenWindow();
        notificationUI.OpenWindow();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        // Fix cursor offset by setting hotspot to top-left (0,0)
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void CloseIde()
    {
        if (ideUI != null && ideUI.IsMenuOn)
        {
            ideUI.CloseWindow();
            inputManager.InUI.Enable();
            inputManager.SwitchToOnFootActions();
            OpenPlayerUI();
        }
    }

    public void OpenQuiz()
    {
        inputManager.SwitchToInUIActions();
        inputManager.InUI.Disable();

        CloseAll();
        quizUI.OpenWindow();
        notificationUI.OpenWindow();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void CloseQuiz()
    {
        if (quizUI != null && quizUI.IsMenuOn)
        {
            quizUI.CloseWindow();
            inputManager.InUI.Enable();
            inputManager.SwitchToOnFootActions();
            OpenPlayerUI();
        }
    }

    public void OpenJournal()
    {
        CloseAll();
        journalUI.OpenWindow();
        notificationUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[0]);
    }

    public void OpenNavigation()
    {
        CloseAll();
        navigationUI.OpenWindow();
        notificationUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[2]);
    }

    public void OpenCodex()
    {
        CloseAll();
        codexUI.OpenWindow();
        notificationUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[1]);
    }

    public void OpenStats()
    {
        CloseAll();
        statsUI.OpenWindow();
        notificationUI.OpenWindow();
        tabUI.SetActiveButton(tabUI.Buttons[3]);
    }

    public void TrackQuest()
    {
        if (journalUI.SelectedQuest != null)
        {
            Navigation nav = GetComponent<Navigation>();
            Journal journal = player.PlayerManagment.journal;

            Transform questTransform = journal.GetQuestTransform(journalUI.SelectedQuest.Id);

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
                playerUI.DisplayQuest(
                    journalUI.TrackedQuest.QuestName,
                    journalUI.TrackedQuest.QuestObjective
                );
                if (questTransform == null)
                {
                    // No valid tracking target: show quest panel only
                    nav.ClearQuestPath();
                    nav.DrawQuestLine = false;
                    return;
                }
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

    private const float RoomArrivalDistance = 2f;

    // FPS label: refresh only a few times per second (averaged) instead of every frame.
    // Setting the UIElements label text every frame dirtied the panel -> repaint + ~1KB/frame GC.
    private const float FpsUpdateInterval = 0.5f;
    private float fpsTimer;
    private int fpsFrameCount;
    private float fpsDeltaSum;

    private void Update()
    {
        UpdateFpsCounter();
        CheckPointerLockReleased();

        if (navigationUI.TrackedRoom == null)
            return;

        Navigation nav = GetComponent<Navigation>();
        if (nav == null || !nav.DrawRoomLine || nav.RoomDestination == null)
            return;

        float distance = Vector3.Distance(transform.position, nav.RoomDestination.position);
        if (distance <= RoomArrivalDistance)
        {
            string arrivedRoomName = navigationUI.TrackedRoom?.Name ?? string.Empty;
            nav.ClearRoomPath();
            nav.DrawRoomLine = false;
            navigationUI.UntrackRoom();
            playerUI.DisplayRoomArrival(arrivedRoomName);
        }
    }

    // Web only. While the cursor is captured, Escape belongs to the browser,
    // not to us: it releases pointer lock and the key never reaches Unity, so
    // opening the menu used to take two presses. Treat losing the lock as the
    // menu request instead - one press gives back the cursor and opens the
    // menu, which needs that cursor anyway.
    //
    // Deliberately not enabled for standalone: whether Escape releases the lock
    // there is unverified, and alt-tabbing away would open the menu too, which
    // is a change the desktop build has not asked for.
#if UNITY_WEBGL
    private bool cursorWasLocked;
#endif

    private void CheckPointerLockReleased()
    {
#if UNITY_WEBGL
        bool locked = UnityEngine.Cursor.lockState == CursorLockMode.Locked;

        // Only a genuine locked -> unlocked transition counts. Reacting to the
        // unlocked state alone would pop the menu open on startup, when the
        // cursor has not been captured yet: browsers grant pointer lock only
        // after a click, however early the game asks for it.
        //
        // Look is enabled exactly while the player is walking around, and off
        // for every UI that unlocks the cursor on purpose (dialogue, panels,
        // IDE, quiz, the menu itself) - so it tells intentional releases apart
        // from ones the browser forced on us.
        if (cursorWasLocked && !locked && inputManager != null && inputManager.OnFoot.Look.enabled)
        {
            OpenMenu();
        }

        cursorWasLocked = locked;
#endif
    }


    private void UpdateFpsCounter()
    {
        fpsFrameCount++;
        fpsDeltaSum += Time.unscaledDeltaTime;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer < FpsUpdateInterval)
            return;

        // Average over the interval -> stable reading, and the label text changes only
        // ~2x/sec, so the UIElements panel repaints (and allocates) only then.
        int fps = fpsDeltaSum > 0f ? Mathf.RoundToInt(fpsFrameCount / fpsDeltaSum) : 0;
        playerUI.UpdateFps(fps);

        fpsTimer = 0f;
        fpsFrameCount = 0;
        fpsDeltaSum = 0f;
    }
}
