using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class IdeUI
{
    // Event fired after code execution with the result
    public System.Action<Minigames.InterpreterResult> OnCodeExecuted;

    public bool IsMenuOn { get; private set; }

    private UIDocument ideUIDocument;
    private UIManager uiManager;
    private Minigames.InterpreterType selectedInterpreter;
    private Button closeButton;
    private Button minimizeButton;
    private Button runButton;
    private TextField textEditor;
    private ScrollView scrollView;
    private Label lineNumbers;
    private Label syntaxHighlightOverlay;
    private Label terminalOutput;
    private IVisualElementScheduledItem cursorBlinkScheduler;
    private string savedCode = "";
    private const float cursorBlinkInterval = 500; // milliseconds
    private float lastRunTime = -999f; // Track last execution time
    private const float runCooldown = 1.0f; // Cooldown in seconds

    public IdeUI(UIDocument ideUIDocument, UIManager uiManager)
    {
        this.ideUIDocument = ideUIDocument;
        this.uiManager = uiManager;

        if (ideUIDocument != null)
        {
            // Setup UI elements
            SetupButtons();
            
            // Hide UI at start
            ideUIDocument.rootVisualElement.style.display = DisplayStyle.None;
            IsMenuOn = false;
        }
        else
        {
            Debug.LogError("[IdeUI] UIDocument nie je priradený!");
        }
    }

    private void SetupButtons()
    {
        if (ideUIDocument == null || ideUIDocument.rootVisualElement == null)
        {
            Debug.LogError("[IdeUI] UIDocument alebo rootVisualElement je null!");
            return;
        }

        // Find and setup the close button
        closeButton = ideUIDocument.rootVisualElement.Q<Button>("IdeCloseWindowButton");
        if (closeButton != null)
        {
            // Ensure the button can receive pointer events
            closeButton.pickingMode = PickingMode.Position;
            closeButton.clicked += ResetAndClose;
        }
        else
        {
            Debug.LogError("[IdeUI] IdeCloseWindowButton nebol nájdený v UI!");
        }

        // Find and setup the minimize button
        minimizeButton = ideUIDocument.rootVisualElement.Q<Button>("IdeMinimizeWindowButton");
        if (minimizeButton != null)
        {
            minimizeButton.pickingMode = PickingMode.Position;
            minimizeButton.clicked += SaveAndMinimize;
        }
        else
        {
            Debug.LogError("[IdeUI] IdeMinimizeWindowButton nebol nájdený v UI!");
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
            Debug.LogError("[IdeUI] TPERunButton nebol nájdený v UI!");
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

            // Setup cursor blinking with UIToolkit scheduler
            SetupCursorBlink();
        }
        else
        {
            Debug.LogError("[IdeUI] TextEditorSpace nebol nájdený v UI!");
        }

        // Find the ScrollView by name
        scrollView = ideUIDocument.rootVisualElement.Q<ScrollView>("TextEditorScrollView");
        if (scrollView == null)
        {
            Debug.LogWarning("[IdeUI] ScrollView nebol nájdený!");
        }

        // Find the LineNumbers label
        lineNumbers = ideUIDocument.rootVisualElement.Q<Label>("LineNumbers");
        if (lineNumbers != null)
        {
            UpdateLineNumbers();
        }
        else
        {
            Debug.LogWarning("[IdeUI] LineNumbers label nebol nájdený!");
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
            Debug.LogWarning("[IdeUI] SyntaxHighlightOverlay label nebol nájdený!");
        }

        // Find the TerminalOutput label
        terminalOutput = ideUIDocument.rootVisualElement.Q<Label>("TerminalOutput");
        if (terminalOutput != null)
        {
            terminalOutput.enableRichText = true;
        }
        else
        {
            Debug.LogWarning("[IdeUI] TerminalOutput label nebol nájdený!");
        }
    }

    /// <summary>
    /// Initialize the IDE with default code and interpreter type.
    /// </summary>
    public void Initialize(TextAsset defaultCodeFile, Minigames.InterpreterType interpreterType)
    {
        selectedInterpreter = interpreterType;
        
        // Clear terminal output
        if (terminalOutput != null)
        {
            terminalOutput.text = string.Empty;
        }
        
        // Load code - use saved code if available, otherwise use default file
        // Defer loading by one frame to prevent input events (like 'E' key) from being processed before the value is set
        if (textEditor != null)
        {
            ideUIDocument.rootVisualElement.schedule.Execute(() =>
            {
                if (textEditor != null)
                {
                    // Use saved code if it exists, otherwise use the default file
                    if (!string.IsNullOrEmpty(savedCode))
                    {
                        textEditor.value = savedCode;
                    }
                    else if (defaultCodeFile != null)
                    {
                        textEditor.value = defaultCodeFile.text;
                    }
                    
                    textEditor.Focus();
                    UpdateLineNumbers();
                    ApplySyntaxHighlighting();
                }
            }).ExecuteLater(10); // 10ms delay to ensure input events are processed
        }
    }

    private void SetupCursorBlink()
    {
        if (textEditor == null) return;

        // Setup cursor blinking with UIToolkit scheduler
        cursorBlinkScheduler = ideUIDocument.rootVisualElement.schedule.Execute(() =>
        {
            // Handle cursor blinking when text editor is focused
            if (textEditor.focusController?.focusedElement == textEditor)
            {
                // Toggle cursor visibility
                if (textEditor.ClassListContains("transparentCursor"))
                    textEditor.RemoveFromClassList("transparentCursor");
                else
                    textEditor.AddToClassList("transparentCursor");
            }
            else
            {
                // Ensure cursor is visible when not focused
                if (textEditor.ClassListContains("transparentCursor"))
                    textEditor.RemoveFromClassList("transparentCursor");
            }
        }).Every((long)cursorBlinkInterval);

        // Register callbacks for user input to reset cursor visibility
        textEditor.RegisterCallback<KeyDownEvent>(evt =>
        {
            ScrollToCursor();
            // Show cursor immediately on keypress
            if (textEditor.ClassListContains("transparentCursor"))
                textEditor.RemoveFromClassList("transparentCursor");
        });

        textEditor.RegisterCallback<MouseDownEvent>(evt =>
        {
            ScrollToCursor();
            // Show cursor immediately on click
            if (textEditor.ClassListContains("transparentCursor"))
                textEditor.RemoveFromClassList("transparentCursor");
        });
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
        
        // Apply syntax highlighting using the SyntaxHighlighter class
        syntaxHighlightOverlay.text = Minigames.SyntaxHighlighter.ApplyHighlighting(text);
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

    public void OpenWindow()
    {
        if (ideUIDocument == null)
        {
            Debug.LogError("[IdeUI] UIDocument nie je priradený!");
            return;
        }

        // Show the UI
        ideUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        IsMenuOn = true;

        // Focus the text editor
        if (textEditor != null)
        {
            textEditor.Focus();
        }
    }

    public void CloseWindow()
    {
        if (ideUIDocument == null)
        {
            Debug.LogWarning("[IdeUI] UIDocument je null pri zatváraní.");
            return;
        }

        // Hide the UI
        ideUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        IsMenuOn = false;
    }

    /// <summary>
    /// Saves the current code and minimizes (closes) the IDE window.
    /// </summary>
    public void SaveAndMinimize()
    {
        // Save the current code
        if (textEditor != null)
        {
            savedCode = textEditor.value ?? "";
        }

        // Close the IDE window
        uiManager.CloseIde();
    }

    /// <summary>
    /// Resets the saved code and closes the IDE window.
    /// Next time the IDE opens, it will load the default code file.
    /// </summary>
    public void ResetAndClose()
    {
        // Clear the saved code so default file loads next time
        savedCode = "";

        // Close the IDE window
        uiManager.CloseIde();
    }

    /// <summary>
    /// Appends a colored message to the terminal output.
    /// </summary>
    /// <param name="message">The message to append</param>
    /// <param name="isSuccess">True for green (success), false for red (error)</param>
    public void AppendTerminalMessage(string message, bool isSuccess)
    {
        if (terminalOutput == null) return;

        string color = isSuccess ? "#00CC00" : "#FF0000"; // Green for success, Red for error
        string coloredMessage = $"<color={color}>{message}</color>";

        if (string.IsNullOrEmpty(terminalOutput.text))
        {
            terminalOutput.text = coloredMessage;
        }
        else
        {
            terminalOutput.text += "\n" + coloredMessage;
        }
    }


    private void RunCode()
    {
        // Check cooldown to prevent rapid multiple executions
        float currentTime = Time.time;
        if (currentTime - lastRunTime < runCooldown)
        {
            Debug.Log($"[IdeUI] Run button on cooldown. Please wait {runCooldown - (currentTime - lastRunTime):F1}s");
            if (terminalOutput != null)
            {
                AppendTerminalMessage("⏳ Please wait before running again...", false);
            }
            return;
        }
        
        lastRunTime = currentTime;
        
        if (textEditor == null)
        {
            Debug.LogWarning("[IdeUI] TextEditorSpace nie je dostupný.");
            return;
        }

        var code = textEditor.value ?? string.Empty;
        var interpreter = CreateInterpreter();
        var result = interpreter.Run(code);

        const string prefix = @"E:\program\FriWorld> ";

        if (!result.Success)
        {
            var errorMsg = $"{prefix}Error at {result.ErrorLine}:{result.ErrorColumn}: {result.ErrorMessage}";
            Debug.LogError($"[IdeUI] {errorMsg}");
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
        }
        else
        {
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
                    Debug.Log($"[IdeUI] {line}");
                }
            }
        }

        // Fire the OnCodeExecuted event with the result
        OnCodeExecuted?.Invoke(result);
    }

    /// <summary>
    /// Creates an interpreter instance based on the selected interpreter type.
    /// </summary>
    /// <returns>An IInterpreter instance for the selected type.</returns>
    private Minigames.IInterpreter CreateInterpreter()
    {
        return selectedInterpreter switch
        {
            Minigames.InterpreterType.Java => new Minigames.JavaInterpreter(),
            // Add more interpreters here as you extend the system:
            // Minigames.InterpreterType.Python => new Minigames.PythonInterpreter(),
            // Minigames.InterpreterType.CSharp => new Minigames.CSharpInterpreter(),
            _ => throw new System.NotImplementedException($"Interpreter type '{selectedInterpreter}' is not implemented.")
        };
    }
}
