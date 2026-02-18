using UnityEngine;
using UnityEngine.UIElements;

public class IdeMiniGame : MonoBehaviour
{
    [Header("IDE UI Document")]
    [SerializeField] private UIDocument ideUIDocument;

    // private InputManager inputManager;
    private Button closeButton;
    private TextField textEditor;
    private ScrollView scrollView;
    private Label lineNumbers;
    private float cursorBlinkTimer = 0f;
    private const float cursorBlinkInterval = 0.5f;

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
            textEditor.style.minHeight = 700;
            textEditor.style.height = StyleKeyword.Auto;
            textEditor.style.whiteSpace = WhiteSpace.Normal;
            
            // Register callback for auto-scroll on typing
            textEditor.RegisterValueChangedCallback(evt =>
            {
                UpdateLineNumbers();
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
    }
}
