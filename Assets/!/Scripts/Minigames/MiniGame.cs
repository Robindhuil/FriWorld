using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MiniGame : Questable
{
    [Header("Šablóna vstupného kódu")]
    [SerializeField] private TextAsset inputTextAsset;

    [Header("Checker pre túto minihru")]
    [SerializeField] private QuestChecker checker;
    private QuestChecker runtimeChecker;

    [Header("Prefab pre Canvas UI")]
    [SerializeField] private GameObject canvasPrefab;

    private GameObject canvasInstance;
    private JavaCodeExecutor executor;

    private TMP_InputField inputField;
    private TextMeshProUGUI outputText;
    private Button runButton;
    private Button exitButton;
    private Button minimizeButton;
    private CodeInput codeInput;

    private InputManager inputManager;
    private bool isCompleted = false;
    private bool isInitialized = false;

    private string savedInputText = "";
    private string savedOutputText = "";

    void Start()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
            Debug.LogWarning($"[MiniGame] [{name}] InputManager nebol nájdený.");
    }

    protected override void Interact()
    {
        base.Interact();
        if (CanBeInteracted)
        {
            OpenWindow();
        }
    }

    public void OpenWindow()
    {
        if (isCompleted) return;

        InitializeCanvas();

        if (canvasInstance == null || inputField == null || outputText == null || executor == null || executor.codeInput == null || executor.codeInput.inputField == null)
        {
            Debug.LogError($"[MiniGame] [{name}] UI nie je správne inicializované, canvas alebo input je NULL!");
            return;
        }

        canvasInstance.SetActive(true);

        if (!string.IsNullOrEmpty(savedInputText))
        {
            inputField.text = savedInputText;
            outputText.text = savedOutputText;
        }
        else
        {
            ResetState();
        }

        codeInput?.FocusInputField();

        inputManager?.onFoot.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseWindow()
    {
        savedInputText = "";
        savedOutputText = "";

        DestroyCanvas();
        inputManager?.onFoot.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        AudioSource audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            if (audioSource.clip != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
            }
        }

        if (isCompleted)
        {
            base.CompleteQuest(Journal);
            Destroy(this);
        }
    }

    public void MinimizeWindow()
    {
        if (canvasInstance != null)
        {
            savedInputText = inputField.text;
            savedOutputText = outputText.text;

            canvasInstance.SetActive(false);
            inputManager?.onFoot.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (isCompleted)
        {
            base.CompleteQuest(Journal);
            Destroy(this);
        }
    }

    private void InitializeCanvas()
    {
        if (canvasPrefab == null)
        {
            Debug.LogError($"[MiniGame] [{name}] canvasPrefab nie je priradený!");
            return;
        }

        if (canvasInstance != null) return;

        canvasInstance = Instantiate(canvasPrefab, transform);
        Transform root = canvasInstance.transform;

        inputField = root.Find("MainBody/Main/ScrollView/Viewport/Content/InputHolder/InputField")?.GetComponent<TMP_InputField>();
        outputText = root.Find("Terminal/Body/Terminal/Scroll View/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        runButton = root.Find("TopPanel/UpperLowerPanel/Icons/ExecuteButton")?.GetComponent<Button>();
        exitButton = root.Find("TopPanel/UpperPanel/ExitButton")?.GetComponent<Button>();
        minimizeButton = root.Find("TopPanel/UpperPanel/Minimize")?.GetComponent<Button>();
        executor = root.Find("JavaExec")?.GetComponent<JavaCodeExecutor>();
        codeInput = root.GetComponentInChildren<CodeInput>(true);

        if (codeInput != null && inputField != null)
        {
            codeInput.inputField = inputField;
        }

        if (executor != null && codeInput != null && outputText != null)
        {
            executor.codeInput = codeInput;
            executor.outputText = outputText;
            Debug.Log($"[MiniGame] [{name}] Executor prepojený s input/output.");
        }

        runButton?.onClick.AddListener(OnClickRun);
        exitButton?.onClick.AddListener(CloseWindow);
        minimizeButton?.onClick.AddListener(MinimizeWindow);
    }

    private void DestroyCanvas()
    {
        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
            canvasInstance = null;
        }

        inputField = null;
        outputText = null;
        runButton = null;
        exitButton = null;
        executor = null;
        codeInput = null;
        runtimeChecker = null;
    }

    public void ResetState()
    {
        if (outputText != null)
            outputText.text = "";

        if (inputTextAsset != null && inputField != null)
        {
            inputField.text = inputTextAsset.text;
            StartCoroutine(ApplyMarkdownAfterFrame());
        }

        if (checker != null)
        {
            runtimeChecker = Instantiate(checker);
            Debug.Log($"[MiniGame] [{name}] Nový runtimeChecker: {runtimeChecker.GetInstanceID()}");
        }
    }

    private IEnumerator ApplyMarkdownAfterFrame()
    {
        yield return null;
        inputField.onValueChanged.Invoke(inputField.text);
    }

    public void OnClickRun()
    {
        if (executor == null || executor.codeInput == null || executor.codeInput.inputField == null)
        {
            Debug.LogError($"[MiniGame] [{name}] Executor alebo inputField nie je inicializovaný!");
            return;
        }

        if (runtimeChecker == null)
        {
            Debug.LogError($"[MiniGame] [{name}] runtimeChecker je null!");
            return;
        }

        var result = executor.ExecuteJavaCode(runtimeChecker);

        if (result.success)
        {
            Debug.Log($"[MiniGame] [{name}] Úloha splnená!");
            isCompleted = true;
        }
        else
        {
            Stats stats = FindFirstObjectByType<Player>().PlayerManagment.stats;
            stats.AddMistake();
        }
    }

    void Update()
    {
        if (canvasInstance != null && inputField != null && !inputField.isFocused)
        {
            if (EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            {
                inputField.ActivateInputField();
            }
        }
    }
}