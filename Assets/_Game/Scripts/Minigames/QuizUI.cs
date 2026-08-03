using UnityEngine;
using UnityEngine.UIElements;

public class QuizUI : BaseUi
{
    public bool IsMenuOn { get; private set; }

    private UIDocument quizUIDocument;
    private UIManager uiManager;
    
    // UI Elements
    private Button closeButton;
    private Button beginRetryQuizButton;
    private Button endQuizButton;
    private Label quizNameLabel;
    private Label questionLabel;
    private Label quizInfoLabel;
    private Label quizStatusLabel;
    private Image questionImage;
    private VisualElement quizWindowBase;
    private VisualElement quizStatBase;
    private Button[] optionButtons = new Button[4];
    
    // Track callbacks to remove them properly
    private System.Action currentBeginCallback;
    private System.Action<int>[] currentOptionCallbacks = new System.Action<int>[4];

    public QuizUI(UIDocument quizUIDocument, UIManager uiManager)
    {
        this.quizUIDocument = quizUIDocument;
        this.uiManager = uiManager;

        if (quizUIDocument != null)
        {
            SetupUIElements();
            
            // Hide UI at start
            quizUIDocument.rootVisualElement.style.display = DisplayStyle.None;
            IsMenuOn = false;
        }
        else
        {
            Debug.LogError("[QuizUI] UIDocument is not assigned!");
        }
    }

    private void SetupUIElements()
    {
        if (quizUIDocument == null || quizUIDocument.rootVisualElement == null)
        {
            Debug.LogError("[QuizUI] UIDocument or rootVisualElement is null!");
            return;
        }

        var root = quizUIDocument.rootVisualElement;

        // Close button
        closeButton = root.Q<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.pickingMode = PickingMode.Position;
            closeButton.clicked += OnCloseButtonClicked;
        }
        else
        {
            Debug.LogError("[QuizUI] CloseButton not found in UI!");
        }

        // Begin/Retry Quiz button
        beginRetryQuizButton = root.Q<Button>("BeginRetryQuizButton");
        if (beginRetryQuizButton != null)
        {
            beginRetryQuizButton.pickingMode = PickingMode.Position;
        }

        // End Quiz button
        endQuizButton = root.Q<Button>("EndQuizButton");
        if (endQuizButton != null)
        {
            endQuizButton.pickingMode = PickingMode.Position;
            endQuizButton.clicked += OnEndQuizButtonClicked;
        }

        // Labels
        quizNameLabel = root.Q<Label>("QuizName");
        questionLabel = root.Q<Label>("QuestionLabel");
        quizInfoLabel = root.Q<Label>("QuizInfoLabel");
        quizStatusLabel = root.Q<Label>("QuizStatusLabel");

        // Question image
        questionImage = root.Q<Image>("QuestionImage");

        // Visual elements for quiz windows
        quizWindowBase = root.Q<VisualElement>("QuizWindowBase");
        quizStatBase = root.Q<VisualElement>("QuizStatBase");

        // Option buttons
        optionButtons[0] = root.Q<Button>("OptionButton1");
        optionButtons[1] = root.Q<Button>("OptionButton2");
        optionButtons[2] = root.Q<Button>("OptionButton3");
        optionButtons[3] = root.Q<Button>("OptionButton4");

        foreach (var button in optionButtons)
        {
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
            }
        }
    }

    private void OnCloseButtonClicked()
    {
        uiManager.CloseQuiz();
    }

    private void OnEndQuizButtonClicked()
    {
        uiManager.CloseQuiz();
    }

    public void SetQuizName(string quizName)
    {
        if (quizNameLabel != null)
        {
            quizNameLabel.text = quizName;
        }
    }

    public void UpdateQuizStatus(int currentQuestion, int totalQuestions)
    {
        if (quizStatusLabel != null)
        {
            quizStatusLabel.text = $"{currentQuestion}/{totalQuestions}";
        }
    }

    public void StartQuiz(int numberOfQuestions)
    {
        if (quizInfoLabel != null)
        {
            quizInfoLabel.text = $"Are you ready for quiz?\n\nNumber of quiz questions: {numberOfQuestions}";
        }

        if (beginRetryQuizButton != null)
        {
            beginRetryQuizButton.text = "Start quiz!";
        }
        
        // Clear quiz status label
        if (quizStatusLabel != null)
        {
            quizStatusLabel.text = "";
        }

        // Show stat base, hide quiz window
        if (quizStatBase != null)
        {
            quizStatBase.style.display = DisplayStyle.Flex;
        }

        if (quizWindowBase != null)
        {
            quizWindowBase.style.display = DisplayStyle.None;
        }
    }

    public void EndQuiz(int score, int totalQuestions)
    {
        if (quizInfoLabel != null)
        {
            if (score == totalQuestions)
            {
                quizInfoLabel.text = $"Congratulations! You scored {score}/{totalQuestions}!";
            }
            else
            {
                quizInfoLabel.text = $"You scored {score}/{totalQuestions}. Better luck next time!";
            }
        }

        if (beginRetryQuizButton != null)
        {
            if (score == totalQuestions)
            {
                beginRetryQuizButton.text = "Start quiz!";
            }
            else
            {
                beginRetryQuizButton.text = "Retry quiz!";
            }
        }
        
        // Clear quiz status label
        if (quizStatusLabel != null)
        {
            quizStatusLabel.text = "";
        }

        // Show stat base, hide quiz window
        ShowStatWindow();
    }

    public void ShowQuizWindow()
    {
        if (quizWindowBase != null)
        {
            quizWindowBase.style.display = DisplayStyle.Flex;
        }

        if (quizStatBase != null)
        {
            quizStatBase.style.display = DisplayStyle.None;
        }
    }

    public void ShowStatWindow()
    {
        if (quizStatBase != null)
        {
            quizStatBase.style.display = DisplayStyle.Flex;
        }

        if (quizWindowBase != null)
        {
            quizWindowBase.style.display = DisplayStyle.None;
        }
    }

    public void DisplayQuestion(string questionText, string[] answers)
    {
        if (questionLabel != null)
        {
            questionLabel.text = questionText;
        }

        // Display answers on buttons (max 4 answers)
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                if (i < answers.Length)
                {
                    optionButtons[i].text = answers[i];
                    optionButtons[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    optionButtons[i].style.display = DisplayStyle.None;
                }
            }
        }
    }

    public void SetQuizWindowDefaultStyle()
    {
        if (quizWindowBase != null)
        {
            quizWindowBase.RemoveFromClassList("correctQuizElement");
            quizWindowBase.RemoveFromClassList("wrongQuizElement");
            quizWindowBase.AddToClassList("defaultQuizElement");
        }
    }

    public void SetQuizWindowCorrectStyle()
    {
        if (quizWindowBase != null)
        {
            quizWindowBase.RemoveFromClassList("defaultQuizElement");
            quizWindowBase.RemoveFromClassList("wrongQuizElement");
            quizWindowBase.AddToClassList("correctQuizElement");
        }
    }

    public void SetQuizWindowWrongStyle()
    {
        if (quizWindowBase != null)
        {
            quizWindowBase.RemoveFromClassList("defaultQuizElement");
            quizWindowBase.RemoveFromClassList("correctQuizElement");
            quizWindowBase.AddToClassList("wrongQuizElement");
        }
    }

    public void ResetOptionButtonColors()
    {
        // Reset each button to its original color class and re-enable interaction
        if (optionButtons[0] != null)
        {
            optionButtons[0].RemoveFromClassList("correctQuizElement");
            optionButtons[0].RemoveFromClassList("wrongQuizElement");
            optionButtons[0].RemoveFromClassList("defaultOptionButton");
            if (!optionButtons[0].ClassListContains("optionButton1"))
            {
                optionButtons[0].AddToClassList("optionButton1");
            }
            optionButtons[0].pickingMode = PickingMode.Position;
        }
        if (optionButtons[1] != null)
        {
            optionButtons[1].RemoveFromClassList("correctQuizElement");
            optionButtons[1].RemoveFromClassList("wrongQuizElement");
            optionButtons[1].RemoveFromClassList("defaultOptionButton");
            if (!optionButtons[1].ClassListContains("optionButton2"))
            {
                optionButtons[1].AddToClassList("optionButton2");
            }
            optionButtons[1].pickingMode = PickingMode.Position;
        }
        if (optionButtons[2] != null)
        {
            optionButtons[2].RemoveFromClassList("correctQuizElement");
            optionButtons[2].RemoveFromClassList("wrongQuizElement");
            optionButtons[2].RemoveFromClassList("defaultOptionButton");
            if (!optionButtons[2].ClassListContains("optionButton3"))
            {
                optionButtons[2].AddToClassList("optionButton3");
            }
            optionButtons[2].pickingMode = PickingMode.Position;
        }
        if (optionButtons[3] != null)
        {
            optionButtons[3].RemoveFromClassList("correctQuizElement");
            optionButtons[3].RemoveFromClassList("wrongQuizElement");
            optionButtons[3].RemoveFromClassList("defaultOptionButton");
            if (!optionButtons[3].ClassListContains("optionButton4"))
            {
                optionButtons[3].AddToClassList("optionButton4");
            }
            optionButtons[3].pickingMode = PickingMode.Position;
        }
    }

    public void HighlightAnswerButtons(int selectedIndex, int correctIndex, int totalAnswers)
    {
        for (int i = 0; i < optionButtons.Length && i < totalAnswers; i++)
        {
            if (optionButtons[i] != null)
            {
                // Remove original color classes to prevent hover issues
                optionButtons[i].RemoveFromClassList("optionButton1");
                optionButtons[i].RemoveFromClassList("optionButton2");
                optionButtons[i].RemoveFromClassList("optionButton3");
                optionButtons[i].RemoveFromClassList("optionButton4");
                
                // Disable interaction to prevent hover and clicks
                optionButtons[i].pickingMode = PickingMode.Ignore;
                
                if (i == correctIndex)
                {
                    // Correct answer gets green
                    optionButtons[i].AddToClassList("correctQuizElement");
                }
                else if (i == selectedIndex)
                {
                    // Wrong selected answer gets red
                    optionButtons[i].AddToClassList("wrongQuizElement");
                }
                else
                {
                    // Other buttons get default gray
                    optionButtons[i].AddToClassList("defaultOptionButton");
                }
            }
        }
    }

    public void SetBeginButtonCallback(System.Action callback)
    {
        if (beginRetryQuizButton != null)
        {
            // Remove old callback if exists
            if (currentBeginCallback != null)
            {
                beginRetryQuizButton.clicked -= currentBeginCallback;
            }
            
            // Add new callback
            currentBeginCallback = callback;
            beginRetryQuizButton.clicked += callback;
        }
    }

    public void SetOptionButtonCallbacks(System.Action<int> callback)
    {
        // Only set callbacks once - check if already set
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Capture index for closure
            if (optionButtons[i] != null && currentOptionCallbacks[i] == null)
            {
                // Add callback (lambda captures the index)
                optionButtons[i].clicked += () => callback(index);
                
                // Mark that callback is added (store a dummy action for tracking)
                currentOptionCallbacks[i] = (idx) => { };
            }
        }
    }

    public void ClearBeginButtonCallbacks()
    {
        if (beginRetryQuizButton != null && currentBeginCallback != null)
        {
            beginRetryQuizButton.clicked -= currentBeginCallback;
            currentBeginCallback = null;
        }
    }

    public void ClearOptionButtonCallbacks()
    {
        // Mark all callbacks as cleared (we can't easily remove lambdas in UI Toolkit)
        // The isProcessingAnswer lock in QuizMiniGame prevents double-clicks
        for (int i = 0; i < currentOptionCallbacks.Length; i++)
        {
            currentOptionCallbacks[i] = null;
        }
    }

    public override void OpenWindow()
    {
        if (quizUIDocument != null)
        {
            quizUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            IsMenuOn = true;
        }
    }

    public override void CloseWindow()
    {
        if (quizUIDocument != null)
        {
            quizUIDocument.rootVisualElement.style.display = DisplayStyle.None;
            IsMenuOn = false;
        }
    }
}
