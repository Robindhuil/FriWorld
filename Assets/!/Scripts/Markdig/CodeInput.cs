using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CodeInput : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextMeshProUGUI formattedText;
    public MarkdownRenderer markdownRenderer;
    public ScrollRect scrollRect;

    private bool justFocused = false;
    private bool wasFocused = false;

    void Start()
    {
        if (inputField == null)
        {
            Debug.LogError($"[CodeInput:{gameObject.name}] ❌ inputField NIE JE priradený!");
            return;
        }

        inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        inputField.onValueChanged.AddListener(OnInputValueChanged);
        inputField.onValidateInput += HandleValidateInput;
        inputField.onSelect.AddListener(HandleSelect);

        inputField.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        inputField.textComponent.overflowMode = TextOverflowModes.Overflow;
        inputField.textComponent.raycastTarget = false;

        inputField.caretColor = Color.white;
        inputField.customCaretColor = true;
        inputField.caretWidth = 2;

        if (formattedText != null)
        {
            formattedText.richText = true;
            formattedText.raycastTarget = false;
            formattedText.textWrappingMode = TextWrappingModes.NoWrap;
            formattedText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Update()
    {
        if (inputField == null)
            return;

        if (!inputField.isFocused && wasFocused)
        {
            wasFocused = false;
            inputField.selectionStringAnchorPosition = inputField.caretPosition;
            inputField.selectionStringFocusPosition = inputField.caretPosition;
        }

        if (inputField.isFocused && !wasFocused)
        {
            wasFocused = true;
        }

        if (formattedText != null && scrollRect != null)
        {
            formattedText.rectTransform.sizeDelta = inputField.textComponent.rectTransform.sizeDelta;
            formattedText.rectTransform.anchoredPosition = inputField.textComponent.rectTransform.anchoredPosition;
        }
    }

    private char HandleValidateInput(string text, int charIndex, char addedChar)
    {
        if (addedChar == '\t')
        {
            inputField.text = inputField.text.Insert(charIndex, "    ");
            inputField.caretPosition = charIndex + 4;
            return '\0';
        }

        if (addedChar == '(') { InsertPair("(", ")"); return '\0'; }
        if (addedChar == '{') { InsertPair("{", "}"); return '\0'; }
        if (addedChar == '[') { InsertPair("[", "]"); return '\0'; }
        if (addedChar == '"') { InsertPair("\"", "\""); return '\0'; }

        if (addedChar == '\n')
        {
            ApplyAutoIndent();
            return '\0';
        }

        return addedChar;
    }

    private void InsertPair(string left, string right)
    {
        int pos = inputField.caretPosition;
        inputField.text = inputField.text.Insert(pos, left + right);
        inputField.caretPosition = pos + 1;
    }

    private void ApplyAutoIndent()
    {
        int pos = inputField.caretPosition;
        string text = inputField.text;

        int lineStart = text.LastIndexOf('\n', Mathf.Clamp(pos - 1, 0, text.Length - 1));
        if (lineStart == -1) lineStart = 0;
        else lineStart++;

        string indent = "";
        while (lineStart < text.Length && (text[lineStart] == ' ' || text[lineStart] == '\t'))
        {
            indent += text[lineStart];
            lineStart++;
        }

        inputField.text = inputField.text.Insert(pos, "\n" + indent);
        inputField.caretPosition = pos + 1 + indent.Length;
    }

    private void OnInputValueChanged(string text)
    {
        if (markdownRenderer != null && formattedText != null)
        {
            formattedText.text = markdownRenderer.ConvertMarkdownToTMP(text);
        }
    }

    private void HandleSelect(string selectedText)
    {
        if (!justFocused)
        {
            StartCoroutine(FixCaretNextFrame());
            justFocused = true;
        }
    }

    private IEnumerator FixCaretNextFrame()
    {
        yield return null;
        inputField.selectionStringAnchorPosition = inputField.caretPosition;
        inputField.selectionStringFocusPosition = inputField.caretPosition;
    }

    void OnDisable()
    {
        justFocused = false;
        inputField?.onSelect.RemoveListener(HandleSelect);
    }

    public void FocusInputField()
    {
        StartCoroutine(FocusRoutine());
    }

    private IEnumerator FocusRoutine()
    {
        yield return null;
        inputField.ActivateInputField();
        yield return null;
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionStringAnchorPosition = inputField.caretPosition;
        inputField.selectionStringFocusPosition = inputField.caretPosition;
    }
}
