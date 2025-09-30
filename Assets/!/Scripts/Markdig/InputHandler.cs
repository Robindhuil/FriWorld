using System.Text;
using TMPro;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private StringBuilder inputText;
    private TextMeshProUGUI previewText;
    private MarkdownRenderer markdownRenderer;
    private CursorHandler cursorHandler;

    public void Initialize(StringBuilder inputText, TextMeshProUGUI previewText, MarkdownRenderer markdownRenderer, CursorHandler cursorHandler)
    {
        this.inputText = inputText;
        this.previewText = previewText;
        this.markdownRenderer = markdownRenderer;
        this.cursorHandler = cursorHandler;
    }

    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursor(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursor(1);
        if (Input.GetKeyDown(KeyCode.Backspace)) RemoveCharacter();
        if (Input.GetKeyDown(KeyCode.Return)) InsertNewLine();
        if (Input.GetKeyDown(KeyCode.Tab)) InsertTab();
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursorUp();
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursorDown();

        foreach (char c in Input.inputString)
        {
            if (c != '\b' && c != '\n' && c != '\r') InsertCharacter(c);
        }

        UpdatePreview();
    }

    private void MoveCursor(int direction)
    {
        int newPosition = Mathf.Clamp(cursorHandler.cursorPosition + direction, 0, inputText.Length);
        cursorHandler.UpdateCursorPosition(newPosition);
    }

    private void InsertCharacter(char c)
    {
        inputText.Insert(cursorHandler.cursorPosition, c);
        cursorHandler.UpdateCursorPosition(cursorHandler.cursorPosition + 1);
        UpdatePreview();
    }

    private void InsertNewLine()
    {
        int lineStart = inputText.ToString().LastIndexOf('\n', cursorHandler.cursorPosition - 1);
        string previousIndentation = "";
        if (lineStart != -1)
        {
            int i = lineStart + 1;
            while (i < inputText.Length && (inputText[i] == ' ' || inputText[i] == '\t'))
            {
                previousIndentation += inputText[i];
                i++;
            }
        }
        inputText.Insert(cursorHandler.cursorPosition, "\n" + previousIndentation);
        cursorHandler.UpdateCursorPosition(cursorHandler.cursorPosition + previousIndentation.Length + 1);
        UpdatePreview();
    }

    private void InsertTab()
    {
        const int tabSize = 4;
        inputText.Insert(cursorHandler.cursorPosition, new string(' ', tabSize));
        cursorHandler.UpdateCursorPosition(cursorHandler.cursorPosition + tabSize);
        UpdatePreview();
    }

    private void RemoveCharacter()
    {
        if (cursorHandler.cursorPosition > 0)
        {
            inputText.Remove(cursorHandler.cursorPosition - 1, 1);
            cursorHandler.UpdateCursorPosition(cursorHandler.cursorPosition - 1);
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        previewText.text = markdownRenderer.HighlightText(inputText.ToString());
        previewText.ForceMeshUpdate();
    }

    private void MoveCursorUp()
    {
        int currentLineStart = inputText.ToString().LastIndexOf('\n', cursorHandler.cursorPosition - 1);
        if (currentLineStart != -1)
        {
            int previousLineStart = inputText.ToString().LastIndexOf('\n', currentLineStart - 1);
            if (previousLineStart == -1) previousLineStart = 0;

            int offset = cursorHandler.cursorPosition - currentLineStart - 1;
            int newPosition = Mathf.Clamp(previousLineStart + offset, previousLineStart, currentLineStart);
            cursorHandler.UpdateCursorPosition(newPosition);
        }
    }

    private void MoveCursorDown()
    {
        int currentLineStart = inputText.ToString().LastIndexOf('\n', cursorHandler.cursorPosition - 1);
        int nextLineStart = inputText.ToString().IndexOf('\n', cursorHandler.cursorPosition);
        if (nextLineStart != -1)
        {
            int nextLineEnd = inputText.ToString().IndexOf('\n', nextLineStart + 1);
            if (nextLineEnd == -1) nextLineEnd = inputText.Length;

            int offset = cursorHandler.cursorPosition - currentLineStart - 1;
            int newPosition = Mathf.Clamp(nextLineStart + offset, nextLineStart, nextLineEnd);
            cursorHandler.UpdateCursorPosition(newPosition);
        }
    }
}