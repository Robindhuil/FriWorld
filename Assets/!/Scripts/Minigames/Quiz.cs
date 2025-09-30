using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

[Serializable]
public class QuizData
{
    public List<QuestionData> questions;
}

[Serializable]
public class QuestionData
{
    public string questionText;
    public string questionImagePath;
    public List<AnswerData> answers;
}

[Serializable]
public class AnswerData
{
    public string answerText;
    public bool isCorrect;
}

public class Quiz : Questable
{
    [Header("Prefab pre Canvas UI")]
    [SerializeField] private GameObject canvasPrefab;

    [Header("JSON s dátami pre kvíz")]
    [SerializeField] private TextAsset quizJson;

    [SerializeField] private GameObject answerButtonPrefab;

    [Header("Nastavenia fontu pre otázku")]
    [SerializeField] private TMP_FontAsset questionFont;

    private GameObject canvasInstance;
    private InputManager inputManager;

    private TMP_Text totalScoreTMP;
    private TMP_Text goodScoreTMP;
    private Transform answersParent;
    private Transform questionPanel;

    private QuizData quizData;
    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;
    private bool isCompleted = false;

    void Start()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
            Debug.LogWarning($"[Quiz] [{name}] InputManager nebol nájdený.");
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

        if (canvasInstance == null)
        {
            Debug.LogError($"[Quiz] [{name}] UI nie je správne inicializované, canvas alebo input je NULL!");
            return;
        }

        canvasInstance.SetActive(true);
        ResetState();

        inputManager?.onFoot.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseWindow()
    {
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

    private void InitializeCanvas()
    {
        if (canvasPrefab == null)
        {
            Debug.LogError($"[Quiz] [{name}] canvasPrefab nie je priradený!");
            return;
        }

        if (canvasInstance != null) return;

        canvasInstance = Instantiate(canvasPrefab, transform);
        Transform root = canvasInstance.transform;

        // -------------------------------
        // 1) ExitButton
        // -------------------------------
        Transform exitButtonT = root.Find("Background/ExitButton");
        if (exitButtonT != null)
        {
            Button exitButton = exitButtonT.GetComponent<Button>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(CloseWindow);
            }
            else
            {
                Debug.LogWarning("[Quiz] ExitButton nemá komponent Button!");
            }
        }
        else
        {
            Debug.LogWarning("[Quiz] ExitButton nebol nájdený v canvas prefabe.");
        }

        // -------------------------------
        // 2) TotalScore
        // -------------------------------
        Transform totalScoreT = root.Find("Background/QuestionBackground/TotalScore");
        if (totalScoreT != null)
        {
            totalScoreTMP = totalScoreT.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogWarning("[Quiz] TotalScore nebol nájdený v hierarchii: QuestionBackground/TotalScore");
        }

        // -------------------------------
        // 3) GoodScore
        // -------------------------------
        Transform goodScoreT = root.Find("Background/QuestionBackground/GoodScore");
        if (goodScoreT != null)
        {
            goodScoreTMP = goodScoreT.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogWarning("[Quiz] GoodScore nebol nájdený v hierarchii: QuestionBackground/GoodScore");
        }

        // -------------------------------
        // 4) QuestionPanel (kontajner pre text/obrázok otázky)
        // -------------------------------
        Transform questionPanelT = root.Find("Background/QuestionBackground/Panel");
        if (questionPanelT != null)
        {
            questionPanel = questionPanelT;
        }
        else
        {
            Debug.LogWarning("[Quiz] Panel nebol nájdený v hierarchii: QuestionBackground/Panel");
        }

        // -------------------------------
        // 5) AnswerBackground (parent pre tlačidlá odpovedí)
        // -------------------------------
        Transform answerBackgroundT = root.Find("Background/AnswerBackground");
        if (answerBackgroundT != null)
        {
            answersParent = answerBackgroundT;
        }
        else
        {
            Debug.LogWarning("[Quiz] AnswerBackground nebol nájdený v canvas prefabe.");
        }

        // -------------------------------
        // 6) Načítanie JSON s otázkami
        // -------------------------------
        if (quizJson != null)
        {
            quizData = JsonUtility.FromJson<QuizData>(quizJson.text);
        }
        else
        {
            Debug.LogError($"[Quiz] [{name}] TextAsset (quizJson) nie je priradený!");
            return;
        }

        canvasInstance.SetActive(false);
    }

    private void DestroyCanvas()
    {
        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
            canvasInstance = null;
        }
    }

    public void ResetState()
    {
        if (quizData == null || quizData.questions == null || quizData.questions.Count == 0)
        {
            Debug.LogWarning($"[Quiz] [{name}] Neboli nájdené žiadne otázky v JSON!");
            CloseWindow();
            return;
        }

        currentQuestionIndex = 0;
        correctAnswersCount = 0;
        isCompleted = false;

        LoadQuestion(currentQuestionIndex);
    }

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= quizData.questions.Count) return;

        QuestionData question = quizData.questions[index];

        if (questionPanel == null)
        {
            Debug.LogError("[Quiz] questionPanel nie je nastavený.");
            return;
        }

        foreach (Transform child in questionPanel)
        {
            Destroy(child.gameObject);
        }

        if (!string.IsNullOrEmpty(question.questionText))
        {
            GameObject textGO = new GameObject("QuestionText", typeof(RectTransform));
            textGO.transform.SetParent(questionPanel, false);
            TMP_Text tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = question.questionText;
            tmpText.fontSize = 45;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            tmpText.overflowMode = TextOverflowModes.Ellipsis;
            if (questionFont != null)
                tmpText.font = questionFont;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 0f);
            textRect.sizeDelta = new Vector2(0f, 200f);
        }

        if (!string.IsNullOrEmpty(question.questionImagePath))
        {
            GameObject imageGO = new GameObject("QuestionImage", typeof(RectTransform));
            imageGO.transform.SetParent(questionPanel, false);
            Image img = imageGO.AddComponent<Image>();
            Sprite loadedSprite = Resources.Load<Sprite>(question.questionImagePath);
            if (loadedSprite != null)
            {
                img.sprite = loadedSprite;
                img.SetNativeSize();
            }
            else
            {
                Debug.LogWarning($"[Quiz] Obrázok na ceste '{question.questionImagePath}' sa nenačítal.");
            }
            RectTransform imageRect = imageGO.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0f);
            imageRect.anchorMax = new Vector2(0.5f, 0f);
            imageRect.pivot = new Vector2(0.5f, 0f);
            imageRect.anchoredPosition = new Vector2(0f, 10f);
        }

        if (totalScoreTMP != null)
            totalScoreTMP.text = $"{index + 1}/{quizData.questions.Count}";
        if (goodScoreTMP != null)
            goodScoreTMP.text = $"{correctAnswersCount}/{quizData.questions.Count}";

        CreateAnswerButtons(question.answers);
    }

    private void CreateAnswerButtons(List<AnswerData> answers)
    {
        if (answersParent == null)
        {
            Debug.LogError("[Quiz] answersParent nie je nastavený!");
            return;
        }
        foreach (Transform child in answersParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < answers.Count; i++)
        {
            AnswerData answer = answers[i];
            GameObject btnObj = Instantiate(answerButtonPrefab, answersParent);

            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = answer.answerText;

            Button btn = btnObj.GetComponent<Button>();
            int indexCaptured = i;
            btn.onClick.AddListener(() => OnAnswerSelected(indexCaptured));
        }
    }

    private void OnAnswerSelected(int answerIndex)
    {
        QuestionData question = quizData.questions[currentQuestionIndex];
        bool isCorrect = question.answers[answerIndex].isCorrect;

        Button[] allButtons = answersParent.GetComponentsInChildren<Button>();
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button b = allButtons[i];
            Image bg = b.GetComponent<Image>();
            if (bg != null)
            {
                if (i == answerIndex)
                {
                    bg.color = isCorrect ? Color.green : Color.red;
                }
                else
                {
                    if (question.answers[i].isCorrect)
                        bg.color = new Color(0.2f, 0.8f, 0.2f);
                    else
                        bg.color = Color.white;
                }
            }
            b.interactable = false;
        }

        if (isCorrect)
        {
            correctAnswersCount++;
        }
        else
        {
            Stats stats = FindFirstObjectByType<Player>().PlayerManagment.stats;
            stats.AddMistake();
        }

        if (goodScoreTMP != null)
            goodScoreTMP.text = $"{correctAnswersCount}/{quizData.questions.Count}";

        StartCoroutine(WaitAndLoadNextQuestion());
    }

    private IEnumerator WaitAndLoadNextQuestion()
    {
        yield return new WaitForSeconds(1.5f);
        currentQuestionIndex++;

        if (currentQuestionIndex < quizData.questions.Count)
        {
            LoadQuestion(currentQuestionIndex);
        }
        else
        {
            ShowResults();
        }
    }

    private void ShowResults()
    {
        foreach (Transform child in questionPanel)
        {
            Destroy(child.gameObject);
        }

        GameObject resultTextGO = new GameObject("ResultText", typeof(RectTransform));
        resultTextGO.transform.SetParent(questionPanel, false);
        TMP_Text resultText = resultTextGO.AddComponent<TextMeshProUGUI>();
        resultText.fontSize = 45;
        resultText.alignment = TextAlignmentOptions.Center;
        if (questionFont != null)
            resultText.font = questionFont;

        bool passed = (correctAnswersCount == quizData.questions.Count);
        string resultMessage = $"Skóre: {correctAnswersCount}/{quizData.questions.Count}\n";
        resultMessage += passed ? "Prešiel si kvíz!" : "Neúspešný prechod kvízu.";
        resultText.text = resultMessage;

        RectTransform resultTextRect = resultTextGO.GetComponent<RectTransform>();
        resultTextRect.anchorMin = new Vector2(0f, 1f);
        resultTextRect.anchorMax = new Vector2(1f, 1f);
        resultTextRect.pivot = new Vector2(0.5f, 1f);
        resultTextRect.anchoredPosition = new Vector2(0f, -10f);
        resultTextRect.sizeDelta = new Vector2(0f, 100f);

        foreach (Transform child in answersParent)
        {
            Destroy(child.gameObject);
        }

        if (passed)
        {
            CreateResultButton("Finish Quiz", () =>
            {
                isCompleted = true;
                CloseWindow();
            });
        }
        else
        {
            CreateResultButton("Reset Quiz", () =>
            {
                ResetState();
            });
            CreateResultButton("Exit", () =>
            {
                CloseWindow();
            });
        }
    }

    private void CreateResultButton(string buttonText, UnityAction onClickAction)
    {
        GameObject btnObj = Instantiate(answerButtonPrefab, answersParent);
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
            btnText.text = buttonText;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClickAction);
    }

    void Update()
    {
        if (canvasInstance != null && Input.GetKeyDown(KeyCode.Escape))
            CloseWindow();
    }
}
