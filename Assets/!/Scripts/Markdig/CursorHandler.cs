using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CursorHandler : MonoBehaviour
{
    private GameObject cursorObject;
    public int cursorPosition;
    private TextMeshProUGUI previewText;
    private bool cursorVisible = true;
    private float cursorBlinkRate = 0.5f;

    public void Initialize(TextMeshProUGUI previewText)
    {
        this.previewText = previewText;

        cursorObject = new GameObject("Cursor");
        var cursorRect = cursorObject.AddComponent<RectTransform>();
        cursorRect.SetParent(previewText.transform, false);
        cursorRect.sizeDelta = new Vector2(2f, previewText.fontSize);
        var cursorImage = cursorObject.AddComponent<Image>();
        cursorImage.color = Color.white;

        cursorPosition = 0;
        StartCoroutine(CursorBlink());
        UpdateCursorPosition(0);
    }

    IEnumerator CursorBlink()
    {
        while (true)
        {
            cursorVisible = !cursorVisible;
            cursorObject.SetActive(cursorVisible);
            yield return new WaitForSeconds(cursorBlinkRate);
        }
    }

    public void UpdateCursorPosition(int newPosition)
    {
        cursorPosition = newPosition;

        if (previewText == null || cursorObject == null) return;

        TMP_TextInfo textInfo = previewText.textInfo;
        if (textInfo.characterCount > 0 && cursorPosition < textInfo.characterCount)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[cursorPosition];

            Vector3 cursorPos = new Vector3(charInfo.bottomLeft.x, (charInfo.ascender + charInfo.descender) / 2, 0);

            cursorObject.GetComponent<RectTransform>().localPosition = cursorPos;
        }
        else
        {
            if (textInfo.characterCount > 0)
            {
                TMP_CharacterInfo lastCharInfo = textInfo.characterInfo[textInfo.characterCount - 1];
                Vector3 cursorPos = new Vector3(lastCharInfo.topRight.x, (lastCharInfo.ascender + lastCharInfo.descender) / 2, 0);
                cursorObject.GetComponent<RectTransform>().localPosition = cursorPos;
            }
            else
            {
                cursorObject.GetComponent<RectTransform>().localPosition = Vector3.zero;
            }
        }
    }
}