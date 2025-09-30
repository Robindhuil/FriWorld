using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class MarkdownInputHighlight : MonoBehaviour
{
    public TextMeshProUGUI inputField;
    public TextMeshProUGUI previewText;
    public MarkdownRenderer markdownRenderer;
    public CursorHandler cursorHandler;
    public InputHandler inputHandler;

    private StringBuilder inputText = new StringBuilder();

    void Start()
    {
        if (inputField == null || previewText == null || markdownRenderer == null || cursorHandler == null || inputHandler == null)
        {
            Debug.LogError("[MarkdownInputHighlight] Chýbajú komponenty!");
            return;
        }

        previewText.font = inputField.font;
        previewText.fontSize = inputField.fontSize;
        previewText.alignment = inputField.alignment;
        previewText.raycastTarget = false;
        inputField.richText = true;

        cursorHandler.Initialize(previewText);
        inputHandler.Initialize(inputText, previewText, markdownRenderer, cursorHandler);
    }

    void Update()
    {
        inputHandler.HandleInput();
    }
}