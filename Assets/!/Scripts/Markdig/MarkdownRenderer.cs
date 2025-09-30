using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarkdownRenderer : MonoBehaviour
{
    private Color keywordColor;
    private Color commentColor;
    private Color stringColor;
    private Color numberColor;
    private Color bracketColor;
    private Color inlineCodeColor;
    private Color annotationColor;
    private Color methodColor;
    private Color classColor;
    private Color staticFieldColor;
    private Color importColor;
    private Color packageColor;
    private Color controlFlowColor;
    private Color typeColor;
    private Color exceptionColor;

    private Dictionary<string, string> placeholders;
    private int placeholderIndex;

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#CC7832", out keywordColor);     // Orange
        ColorUtility.TryParseHtmlString("#808080", out commentColor);     // Gray
        ColorUtility.TryParseHtmlString("#6A8759", out stringColor);      // Green
        ColorUtility.TryParseHtmlString("#6897BB", out numberColor);      // Blue
        ColorUtility.TryParseHtmlString("#A9B7C6", out bracketColor);     // Light Gray
        ColorUtility.TryParseHtmlString("#A9B7C6", out inlineCodeColor);  // Light Gray
        ColorUtility.TryParseHtmlString("#BBB529", out annotationColor);  // Yellow
        ColorUtility.TryParseHtmlString("#FFC66D", out methodColor);      // Light Orange
        ColorUtility.TryParseHtmlString("#A9B7C6", out classColor);       // Light Gray
        ColorUtility.TryParseHtmlString("#9876AA", out staticFieldColor); // Purple
        ColorUtility.TryParseHtmlString("#A9B7C6", out importColor);      // Light Gray
        ColorUtility.TryParseHtmlString("#A9B7C6", out packageColor);     // Light Gray
        ColorUtility.TryParseHtmlString("#CC7832", out controlFlowColor); // Orange
        ColorUtility.TryParseHtmlString("#CC7832", out typeColor);        // Orange
        ColorUtility.TryParseHtmlString("#CC7832", out exceptionColor);   // Orange
    }

    public string ConvertMarkdownToTMP(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return "";

        string[] lines = markdown.Split(new[] { '\n' }, StringSplitOptions.None);
        string result = "";

        foreach (string line in lines)
        {
            placeholders = new Dictionary<string, string>();
            placeholderIndex = 0;

            string processed = line;

            // 1. Kódové bloky (``` ... ```)
            processed = ReplaceWithPlaceholder(processed, @"```(?<lang>\w+)?\n(?<code>[\s\S]*?)```", m =>
            {
                string codeBlock = m.Groups["code"].Value;
                string highlightedCode = HighlightText(codeBlock);
                return $"<color=#{ColorUtility.ToHtmlStringRGB(inlineCodeColor)}>{highlightedCode}</color>";
            });

            // 2. Blokové komentáre (/* ... */)
            processed = ReplaceWithPlaceholder(processed, @"/\*[\s\S]*?\*/", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(commentColor)}>{m.Value}</color>";
            });

            // 3. Riadkové komentáre (// ...)
            processed = ReplaceWithPlaceholder(processed, @"//.*", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(commentColor)}>{m.Value}</color>";
            });

            // 4. Inline kód (`...`)
            processed = ReplaceWithPlaceholder(processed, @"`([^`]+)`", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(inlineCodeColor)}>{m.Value}</color>";
            });

            // 5. Markdown značky pre bold (**text**) a italic (*text*)
            processed = ReplaceWithPlaceholder(processed, @"\*\*(.+?)\*\*", m =>
            {
                return $"<b>{m.Groups[1].Value}</b>";
            });
            processed = ReplaceWithPlaceholder(processed, @"\*(.+?)\*", m =>
            {
                return $"<i>{m.Groups[1].Value}</i>";
            });

            // 6. Zvýraznenie syntaxe na zostávajúci text
            processed = HighlightText(processed);

            // 7. Obnovenie placeholderov
            processed = RestorePlaceholders(processed);

            result += processed + "\n";
        }

        if (result.Length > 0)
        {
            result = result.Remove(result.Length - 1);
        }

        result = result.Replace("___", "<color=#FF0000>___</color>");

        return result;
    }

    private string ReplaceWithPlaceholder(string text, string pattern, Func<Match, string> evaluator)
    {
        return Regex.Replace(text, pattern, m =>
        {
            string placeholder = $"__PLACEHOLDER_{placeholderIndex}__";
            placeholderIndex++;
            placeholders[placeholder] = evaluator(m);
            return placeholder;
        }, RegexOptions.Multiline);
    }

    private string RestorePlaceholders(string text)
    {
        foreach (var kvp in placeholders)
        {
            text = text.Replace(kvp.Key, kvp.Value);
        }
        return text;
    }

    private string GetSimpleMethodPattern()
    {
        return @"
        (?<returnType>[A-Za-z_][A-Za-z0-9_]*)   # Návratový typ
        \s+                                     # Medzera
        (?<methodName>[A-Za-z_][A-Za-z0-9_]*)     # Názov metódy
        \s*\(                                   # Otváracia zátvorka
            (?<params>[^)]*)                    # Parametre
        \)
        (?<trailing>\s*)                        # Zachytí trailing whitespace (medzery, nové riadky) po zatvorkách
    ";
    }

    public string HighlightText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var methodPattern = GetSimpleMethodPattern();
        var matches = Regex.Matches(
            text,
            methodPattern,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline
        );
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            string returnType = match.Groups["returnType"].Value;
            string methodName = match.Groups["methodName"].Value;
            string parameters = match.Groups["params"].Value;
            string trailing = match.Groups["trailing"].Value;

            string replacement =
                $"{returnType} <color=#{ColorUtility.ToHtmlStringRGB(methodColor)}>{methodName}</color>({parameters}){trailing}";
            text = text.Remove(match.Index, match.Length).Insert(match.Index, replacement);
        }

        text = Regex.Replace(text, "\"(.*?)\"", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(stringColor)}>{m.Value}</color>";
        });

        text = Regex.Replace(text, @"\b(\d+)\b", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(numberColor)}>{m.Value}</color>";
        });

        text = text.Replace("(", $"<color=#{ColorUtility.ToHtmlStringRGB(bracketColor)}>(</color>");
        text = text.Replace(")", $"<color=#{ColorUtility.ToHtmlStringRGB(bracketColor)}>)</color>");

        text = Regex.Replace(text, @"@\w+", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(annotationColor)}>{m.Value}</color>";
        });

        string[] keywords = { "public", "void", "class", "int", "String", "new", "return", "if", "else", "for", "while", "static", "final", "extends", "implements" };
        foreach (var keyword in keywords)
        {
            text = Regex.Replace(
                text,
                $@"\b{Regex.Escape(keyword)}\b",
                m => $"<color=#{ColorUtility.ToHtmlStringRGB(keywordColor)}>{m.Value}</color>",
                RegexOptions.Multiline
            );
        }

        text = text.Replace("{", $"<color=#{ColorUtility.ToHtmlStringRGB(bracketColor)}>{{</color>");
        text = text.Replace("}", $"<color=#{ColorUtility.ToHtmlStringRGB(bracketColor)}>}}</color>");

        text = Regex.Replace(text, @"(?<=^|\s)\b[A-Z]\w*\b(?=\s*\{)", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(classColor)}>{m.Value}</color>";
        });

        text = Regex.Replace(text, @"static\s+final\s+\w+", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(staticFieldColor)}>{m.Value}</color>";
        });

        text = Regex.Replace(text, @"\bimport\s+[\w\.]+\s*;", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(importColor)}>{m.Value}</color>";
        });

        text = Regex.Replace(text, @"\bpackage\s+[\w\.]+\s*;", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(packageColor)}>{m.Value}</color>";
        });

        string[] controlFlowKeywords = { "break", "continue", "switch", "case", "default" };
        foreach (var keyword in controlFlowKeywords)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(keyword)}\b", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(controlFlowColor)}>{m.Value}</color>";
            }, RegexOptions.Multiline);
        }

        string[] typeKeywords = { "boolean", "char", "double", "float", "long", "short", "byte" };
        foreach (var keyword in typeKeywords)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(keyword)}\b", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(typeColor)}>{m.Value}</color>";
            }, RegexOptions.Multiline);
        }

        string[] exceptionKeywords = { "try", "catch", "finally", "throw", "throws" };
        foreach (var keyword in exceptionKeywords)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(keyword)}\b", m =>
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(exceptionColor)}>{m.Value}</color>";
            }, RegexOptions.Multiline);
        }

        text = text.Replace(";", $"<color=#{ColorUtility.ToHtmlStringRGB(keywordColor)}>;</color>");

        text = Regex.Replace(text, @"\bSystem\.out\b", m =>
        {
            return $"System.<color=#{ColorUtility.ToHtmlStringRGB(staticFieldColor)}>out</color>";
        });

        text = Regex.Replace(text, @"(?<!\.)\b(?<methodCall>[A-Za-z_][A-Za-z0-9_]*)\s*(?=\()", m =>
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(methodColor)}>{m.Value}</color>";
        });

        return text;
    }
}
