using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuizAnswer
{
    public string answerText;
    public bool isCorrect;
}

[System.Serializable]
public class QuizQuestion
{
    public string questionText;
    public string questionImagePath;
    public List<QuizAnswer> answers;
}

[System.Serializable]
public class QuizData
{
    public List<QuizQuestion> questions;
}

public class QuizMiniGame : Questable
{
    [SerializeField] private string quizJsonPath = "minigames/Quiz/entryQuiz";
    [SerializeField] private string quizName = "Entry Quiz";
    
    private QuizData quizData;
    private QuizUI quizUI;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isQuizActive = false;
    private bool isProcessingAnswer = false;
    private UIManager uiManager;
    private int[] currentAnswerMapping; // Maps displayed button index to original answer index
    private int[] shuffledQuestionIndices; // Maps current question progression to original question index

    private void Start()
    {
        LoadQuizData();
        uiManager = FindFirstObjectByType<UIManager>();
    }

    protected override void Interact()
    {
        base.Interact();
        
        if (!CanBeInteracted)
        {
            Debug.Log("[QuizMiniGame] Cannot interact - quest not active or prerequisites not met.");
            return;
        }

        if (uiManager != null)
        {
            OpenQuiz(uiManager);
        }
        else
        {
            Debug.LogError("[QuizMiniGame] UIManager not found!");
        }
    }

    public void InitializeQuiz(QuizUI quizUI)
    {
        this.quizUI = quizUI;
        
        if (quizData != null && quizData.questions.Count > 0)
        {
            // Set quiz name
            quizUI.SetQuizName(quizName);
            
            // Setup option button callbacks ONCE (not every time quiz starts)
            quizUI.SetOptionButtonCallbacks(OnAnswerSelected);
            
            // Show start screen
            quizUI.StartQuiz(quizData.questions.Count);
            
            // Setup begin button callback
            quizUI.ClearBeginButtonCallbacks();
            quizUI.SetBeginButtonCallback(OnBeginQuizClicked);
        }
        else
        {
            Debug.LogError("[QuizMiniGame] Quiz data is empty or not loaded!");
        }
    }

    private void LoadQuizData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(quizJsonPath);
        
        if (jsonFile != null)
        {
            quizData = JsonUtility.FromJson<QuizData>(jsonFile.text);
            Debug.Log($"[QuizMiniGame] Loaded quiz with {quizData.questions.Count} questions");
        }
        else
        {
            Debug.LogError($"[QuizMiniGame] Failed to load quiz data from Resources/{quizJsonPath}");
        }
    }

    private void OnBeginQuizClicked()
    {
        if (isQuizActive)
        {
            // Retry quiz
            ResetQuiz();
        }
        
        StartQuizSequence();
    }

    private void StartQuizSequence()
    {
        if (quizData == null || quizData.questions.Count == 0)
        {
            Debug.LogError("[QuizMiniGame] Cannot start quiz - no questions available!");
            return;
        }

        isQuizActive = true;
        currentQuestionIndex = 0;
        score = 0;
        
        // Shuffle questions
        int questionCount = quizData.questions.Count;
        shuffledQuestionIndices = new int[questionCount];
        for (int i = 0; i < questionCount; i++)
        {
            shuffledQuestionIndices[i] = i;
        }
        
        // Fisher-Yates shuffle for questions
        for (int i = questionCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = shuffledQuestionIndices[i];
            shuffledQuestionIndices[i] = shuffledQuestionIndices[j];
            shuffledQuestionIndices[j] = temp;
        }
        
        // Show quiz window
        quizUI.ShowQuizWindow();
        
        // Display first question
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= quizData.questions.Count)
        {
            EndQuiz();
            return;
        }

        // Reset quiz window style to default
        quizUI.SetQuizWindowDefaultStyle();
        
        // Reset option button colors to their original styles
        quizUI.ResetOptionButtonColors();
        
        // Update quiz status label
        quizUI.UpdateQuizStatus(currentQuestionIndex + 1, quizData.questions.Count);

        // Get the actual question using shuffled index
        int actualQuestionIndex = shuffledQuestionIndices[currentQuestionIndex];
        QuizQuestion question = quizData.questions[actualQuestionIndex];
        
        // Create and shuffle answer mapping indices
        int answerCount = question.answers.Count;
        currentAnswerMapping = new int[answerCount];
        for (int i = 0; i < answerCount; i++)
        {
            currentAnswerMapping[i] = i;
        }
        
        // Fisher-Yates shuffle
        for (int i = answerCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = currentAnswerMapping[i];
            currentAnswerMapping[i] = currentAnswerMapping[j];
            currentAnswerMapping[j] = temp;
        }
        
        // Get shuffled answer texts
        string[] answerTexts = new string[answerCount];
        for (int i = 0; i < answerCount; i++)
        {
            answerTexts[i] = question.answers[currentAnswerMapping[i]].answerText;
        }
        
        // Display question and shuffled answers
        quizUI.DisplayQuestion(question.questionText, answerTexts);
    }

    private void OnAnswerSelected(int answerIndex)
    {
        // Prevent multiple clicks from being processed
        if (!isQuizActive || isProcessingAnswer || currentQuestionIndex >= quizData.questions.Count)
        {
            return;
        }
        
        // Lock to prevent multiple rapid clicks
        isProcessingAnswer = true;

        // Get the actual question using shuffled index
        int actualQuestionIndex = shuffledQuestionIndices[currentQuestionIndex];
        QuizQuestion currentQuestion = quizData.questions[actualQuestionIndex];
        
        // Check if answer is within bounds
        if (answerIndex >= currentQuestion.answers.Count || answerIndex >= currentAnswerMapping.Length)
        {
            Debug.LogWarning($"[QuizMiniGame] Answer index {answerIndex} out of bounds");
            isProcessingAnswer = false;
            return;
        }

        // Map displayed button index to original answer index
        int originalAnswerIndex = currentAnswerMapping[answerIndex];

        // Find which button displays the correct answer
        int correctButtonIndex = -1;
        for (int i = 0; i < currentAnswerMapping.Length; i++)
        {
            if (currentQuestion.answers[currentAnswerMapping[i]].isCorrect)
            {
                correctButtonIndex = i;
                break;
            }
        }

        // Check if answer is correct and show visual feedback
        if (currentQuestion.answers[originalAnswerIndex].isCorrect)
        {
            score++;
            quizUI.SetQuizWindowCorrectStyle();
            Debug.Log($"[QuizMiniGame] Question {currentQuestionIndex + 1}: Correct answer! Current score: {score}");
        }
        else
        {
            quizUI.SetQuizWindowWrongStyle();
            Debug.Log($"[QuizMiniGame] Question {currentQuestionIndex + 1}: Wrong answer. Current score: {score}");
        }
        
        // Highlight buttons to show correct/wrong answers
        quizUI.HighlightAnswerButtons(answerIndex, correctButtonIndex, currentQuestion.answers.Count);

        // Move to next question
        currentQuestionIndex++;
        
        // Wait 1 second before showing next question
        StartCoroutine(ShowNextQuestionAfterDelay(1f));
    }

    private IEnumerator ShowNextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Unlock after delay
        isProcessingAnswer = false;
        
        DisplayCurrentQuestion();
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        
        Debug.Log($"[QuizMiniGame] Quiz ended. Score: {score}/{quizData.questions.Count}");
        
        // Show end screen
        quizUI.EndQuiz(score, quizData.questions.Count);
        
        // Setup begin button for retry
        quizUI.ClearBeginButtonCallbacks();
        quizUI.SetBeginButtonCallback(OnBeginQuizClicked);
        
        // Complete quest if player got full score
        if (score == quizData.questions.Count)
        {
            if (Journal != null)
            {
                Debug.Log($"[QuizMiniGame] Full score achieved! Completing quest...");
                CompleteQuest(Journal);
            }
            else
            {
                Debug.LogWarning("[QuizMiniGame] Full score achieved but Journal is null. Cannot complete quest.");
            }
        }
        else
        {
            Debug.Log($"[QuizMiniGame] Quest not completed. Score: {score}/{quizData.questions.Count}");
        }
    }

    private void ResetQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        isQuizActive = false;
        isProcessingAnswer = false;
    }

    public void OpenQuiz(UIManager uiManager)
    {
        QuizUI quizUI = uiManager.GetQuizUI();
        
        if (quizUI != null)
        {
            InitializeQuiz(quizUI);
            uiManager.OpenQuiz();
        }
        else
        {
            Debug.LogError("[QuizMiniGame] QuizUI not found in UIManager!");
        }
    }
}
