using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class IdeMiniGame : MonoBehaviour
{
    [Header("IDE UI Document")]
    [SerializeField] private UIDocument ideUIDocument;

    // private InputManager inputManager;
    private Button closeButton;
    private Button runButton;
    private TextField textEditor;
    private ScrollView scrollView;
    private Label lineNumbers;
    private Label syntaxHighlightOverlay;
    private Label terminalOutput;
    private float cursorBlinkTimer = 0f;
    private const float cursorBlinkInterval = 0.5f;
    
    // Syntax highlighting colors (VS Code inspired)
    private static readonly string[] keywords = new string[] 
    { 
        "void", "public", "private", "protected", "class", "interface", 
        "int", "float", "double", "bool", "string", "char", "long", "short", "byte",
        "new", "this", "static", "const", "var", "null", "true", "false", "using", "readonly"
    };
    private static readonly string[] controlKeywords = new string[]
    {
        "return", "if", "else", "for", "while", "switch", "case", "do", "break", "continue"
    };
    private const string keywordColor = "#569CD6";      // Blue
    private const string controlKeywordColor = "#C586C0"; // Purple
    private const string functionColor = "#DCDCAA";     // Yellow
    private const string stringColor = "#CE9178";       // Orange
    private const string commentColor = "#6A9955";      // Green
    private const string numberColor = "#B5CEA8";       // Light cyan
    private const string bracketColor = "#D4D4D4";      // Light gray

    void Start()
    {
        if (ideUIDocument != null)
        {
            // Setup buttons at start
            SetupButtons();
        }
        else
        {
            Debug.LogError($"[IdeMiniGame] [{name}] UIDocument nie je priradený!");
        }
    }

    private void SetupButtons()
    {
        if (ideUIDocument == null || ideUIDocument.rootVisualElement == null)
        {
            Debug.LogError($"[IdeMiniGame] [{name}] UIDocument alebo rootVisualElement je null!");
            return;
        }

        // Find and setup the close button
        closeButton = ideUIDocument.rootVisualElement.Q<Button>("IdeCloseWindowButton");
        if (closeButton != null)
        {
            // Ensure the button can receive pointer events
            closeButton.pickingMode = PickingMode.Position;
            closeButton.clicked += CloseWindow;
        }
        else
        {
            Debug.LogError($"[IdeMiniGame] [{name}] IdeCloseWindowButton nebol nájdený v UI!");
        }

        // Find and setup the run button
        runButton = ideUIDocument.rootVisualElement.Q<Button>("TPERunButton");
        if (runButton != null)
        {
            runButton.pickingMode = PickingMode.Position;
            runButton.clicked += RunCode;
        }
        else
        {
            Debug.LogError($"[IdeMiniGame] [{name}] TPERunButton nebol nájdený v UI!");
        }

        // Find and setup the text editor
        textEditor = ideUIDocument.rootVisualElement.Q<TextField>("TextEditorSpace");
        if (textEditor != null)
        {
            // Enable multiline mode for the text editor
            textEditor.multiline = true;
            
            // Disable auto-select behavior
            textEditor.selectAllOnFocus = false;
            textEditor.selectAllOnMouseUp = false;
            
            // Set minimum height to fill viewport, let it grow with content
            textEditor.style.minHeight = 720;
            textEditor.style.height = StyleKeyword.Auto;
            textEditor.style.whiteSpace = WhiteSpace.Normal;
            
            // Register callback for auto-scroll on typing
            textEditor.RegisterValueChangedCallback(evt =>
            {
                UpdateLineNumbers();
                ApplySyntaxHighlighting();
                ScrollToCursor();
            });
        }
        else
        {
            Debug.LogError($"[IdeMiniGame] [{name}] TextEditorSpace nebol nájdený v UI!");
        }

        // Find the ScrollView by name
        scrollView = ideUIDocument.rootVisualElement.Q<ScrollView>("TextEditorScrollView");
        if (scrollView == null)
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] ScrollView nebol nájdený!");
        }

        // Find the LineNumbers label
        lineNumbers = ideUIDocument.rootVisualElement.Q<Label>("LineNumbers");
        if (lineNumbers != null)
        {
            UpdateLineNumbers();
        }
        else
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] LineNumbers label nebol nájdený!");
        }
        
        // Find the SyntaxHighlightOverlay label
        syntaxHighlightOverlay = ideUIDocument.rootVisualElement.Q<Label>("SyntaxHighlightOverlay");
        if (syntaxHighlightOverlay != null)
        {
            syntaxHighlightOverlay.pickingMode = PickingMode.Ignore;
            syntaxHighlightOverlay.enableRichText = true;
            ApplySyntaxHighlighting();
        }
        else
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] SyntaxHighlightOverlay label nebol nájdený!");
        }

        // Find the TerminalOutput label
        terminalOutput = ideUIDocument.rootVisualElement.Q<Label>("TerminalOutput");
        if (terminalOutput == null)
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] TerminalOutput label nebol nájdený!");
        }
    }



    private void UpdateLineNumbers()
    {
        if (lineNumbers == null || textEditor == null) return;

        string text = textEditor.value ?? "";
        
        // Count the number of lines
        int lineCount = 1; // At least one line
        if (!string.IsNullOrEmpty(text))
        {
            lineCount = text.Split('\n').Length;
        }

        // Generate line numbers from 1 to lineCount
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine(i.ToString());
        }

        // Set the line numbers text
        lineNumbers.text = sb.ToString();
    }

    private void ApplySyntaxHighlighting()
    {
        if (syntaxHighlightOverlay == null || textEditor == null) return;

        string text = textEditor.value ?? "";
        
        // Replace angle brackets to prevent rich-text parsing in the overlay
        text = text.Replace("<", "‹").Replace(">", "›");

        // Step 1: Extract strings first so comment markers inside strings are ignored
        var stringPlaceholders = new System.Collections.Generic.List<string>();
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"""([^""\\]|\\.)*""",
            match =>
            {
                stringPlaceholders.Add(match.Value);
                return $"__STR{stringPlaceholders.Count - 1}__";
            }
        );
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"'([^'\\]|\\.)*'",
            match =>
            {
                stringPlaceholders.Add(match.Value);
                return $"__STR{stringPlaceholders.Count - 1}__";
            }
        );

        // Step 2: Extract comments so they always stay green
        var commentPlaceholders = new System.Collections.Generic.List<string>();
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"//.*|/\*(.|\n)*?\*/",
            match =>
            {
                commentPlaceholders.Add(match.Value);
                return $"__CMT{commentPlaceholders.Count - 1}__";
            }
        );

        // Step 3: Apply control keyword highlighting (purple)
        foreach (string keyword in controlKeywords)
        {
            string pattern = $@"\b{keyword}\b";
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                pattern,
                $"<color={controlKeywordColor}>{keyword}</color>"
            );
        }

        // Step 4: Apply keyword highlighting
        foreach (string keyword in keywords)
        {
            string pattern = $@"\b{keyword}\b";
            text = System.Text.RegularExpressions.Regex.Replace(
                text, 
                pattern, 
                $"<color={keywordColor}>{keyword}</color>"
            );
        }
        
        // Step 5: Apply function call highlighting (word followed by opening parenthesis)
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(",
            $"<color={functionColor}>$1</color>("
        );
        
        // Step 6: Apply number highlighting
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\b(\d+\.?\d*f?)\b",
            $"<color={numberColor}>$1</color>"
        );
        
        // Step 7: Apply bracket highlighting
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[\{\}\[\]]",
            match => $"<color={bracketColor}>{match.Value}</color>"
        );

        // Step 8: Restore strings
        for (int i = 0; i < stringPlaceholders.Count; i++)
        {
            text = text.Replace(
                $"__STR{i}__",
                $"<color={stringColor}>{stringPlaceholders[i]}</color>"
            );
        }

        // Step 9: Restore comments last so they override everything inside
        for (int i = 0; i < commentPlaceholders.Count; i++)
        {
            text = text.Replace(
                $"__CMT{i}__",
                $"<color={commentColor}>{commentPlaceholders[i]}</color>"
            );
        }
        
        // Set the syntax highlighted text
        syntaxHighlightOverlay.text = text;
    }

    private void ScrollToCursor()
    {
        if (textEditor == null || scrollView == null) return;

        // Schedule to run after UI updates
        ideUIDocument.rootVisualElement.schedule.Execute(() =>
        {
            string text = textEditor.value ?? "";
            int cursorIndex = textEditor.cursorIndex;
            
            // Calculate line number
            int lineNumber = 0;
            for (int i = 0; i < cursorIndex && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    lineNumber++;
            }
            
            // Estimate line height (font size 20px + spacing)
            float lineHeight = 28f;
            float cursorY = lineNumber * lineHeight;
            
            // Get viewport dimensions
            float viewportHeight = scrollView.contentViewport.layout.height;
            float currentScroll = scrollView.scrollOffset.y;
            
            // Padding to keep cursor away from edges
            float padding = lineHeight * 2f;
            
            // Check if we need to scroll
            if (cursorY < currentScroll + padding)
            {
                // Scroll up to keep cursor visible
                scrollView.scrollOffset = new Vector2(0, Mathf.Max(0, cursorY - padding));
            }
            else if (cursorY + lineHeight > currentScroll + viewportHeight - padding)
            {
                // Scroll down to keep cursor visible
                float targetScroll = cursorY - viewportHeight + padding + lineHeight;
                scrollView.scrollOffset = new Vector2(0, Mathf.Max(0, targetScroll));
            }
        }).ExecuteLater(10);
    }

    void Update()
    {
        // Handle cursor blinking when text editor is focused
        if (textEditor != null && textEditor.focusController?.focusedElement == textEditor)
        {
            cursorBlinkTimer += Time.deltaTime;
            
            if (cursorBlinkTimer >= cursorBlinkInterval)
            {
                cursorBlinkTimer = 0f;
                
                // Toggle cursor visibility
                if (textEditor.ClassListContains("transparentCursor"))
                    textEditor.RemoveFromClassList("transparentCursor");
                else
                    textEditor.AddToClassList("transparentCursor");
            }
            
            // Scroll to cursor when navigating with arrow keys or clicking
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                ScrollToCursor();
                
                // Reset blink timer on user input to show cursor immediately
                cursorBlinkTimer = 0f;
                if (textEditor.ClassListContains("transparentCursor"))
                    textEditor.RemoveFromClassList("transparentCursor");
            }
        }
        else
        {
            // Ensure cursor is visible when not focused
            if (textEditor != null && textEditor.ClassListContains("transparentCursor"))
                textEditor.RemoveFromClassList("transparentCursor");
        }
    }


    private void OpenWindow()
    {
        if (ideUIDocument == null)
        {
            Debug.LogError($"[IdeMiniGame] [{name}] UIDocument nie je priradený!");
            return;
        }

        // Show the UI
        ideUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        // Focus the text editor
        if (textEditor != null)
        {
            textEditor.Focus();
        }

        // Disable player movement and show cursor
        // inputManager?.onFoot.Disable();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Fix cursor offset by setting hotspot to top-left (0,0)
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void CloseWindow()
    {
        if (ideUIDocument == null)
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] UIDocument je null pri zatváraní.");
            return;
        }

        // Hide the UI
        ideUIDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Enable player movement and hide cursor
        // inputManager?.onFoot.Enable();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    void OnDestroy()
    {
        // Clean up event listener
        if (closeButton != null)
        {
            closeButton.clicked -= CloseWindow;
        }

        if (runButton != null)
        {
            runButton.clicked -= RunCode;
        }
    }

    private void RunCode()
    {
        if (textEditor == null)
        {
            Debug.LogWarning($"[IdeMiniGame] [{name}] TextEditorSpace nie je dostupný.");
            return;
        }

        var code = textEditor.value ?? string.Empty;
        var interpreter = new Minigames.JavaInterpreter();
        var result = interpreter.Run(code);

        const string prefix = @"E:\program\FriWorld> ";

        if (!result.Success)
        {
            var errorMsg = $"{prefix}Error at {result.ErrorLine}:{result.ErrorColumn}: {result.ErrorMessage}";
            Debug.LogError($"[IdeMiniGame] [{name}] {errorMsg}");
            if (terminalOutput != null)
            {
                terminalOutput.text = errorMsg;
            }
            return;
        }

        if (result.Output.Count == 0)
        {
            var noOutputMsg = $"{prefix}(No output)";
            if (terminalOutput != null)
            {
                terminalOutput.text = noOutputMsg;
            }
            return;
        }

        var outputLines = result.Output.Select(line => prefix + line);
        var outputText = string.Join("\n", outputLines);
        if (terminalOutput != null)
        {
            terminalOutput.text = outputText;
        }
        else
        {
            // Fallback to Debug.Log if label not found
            foreach (var line in result.Output)
            {
                Debug.Log($"[IdeMiniGame] [{name}] {line}");
            }
        }
    }
}
