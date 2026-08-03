using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Minigames
{
    /// <summary>
    /// Provides syntax highlighting functionality for code editor.
    /// VS Code inspired color scheme.
    /// </summary>
    public static class SyntaxHighlighter
    {
        // Syntax highlighting colors (VS Code inspired)
        private static readonly string[] keywords = new string[] 
        { 
            "void", "public", "private", "protected", "class", "interface", 
            "int", "float", "double", "boolean", "String", "char", "long", "short", "byte",
            "new", "this", "static", "const", "var", "null", "true", "false", "using", "readonly"
        };
        
        private static readonly string[] controlKeywords = new string[]
        {
            "return", "if", "else", "for", "while", "switch", "case", "do", "break", "continue"
        };
        
        private const string keywordColor = "#569CD6";      // Blue
        private const string controlKeywordColor = "#C586C0"; // Purple
        private const string functionColor = "#DCDCAA";     // Yellow
        private const string stringColor = "#CE9178";       // Orange
        private const string commentColor = "#6A9955";      // Green
        private const string numberColor = "#B5CEA8";       // Light cyan
        private const string bracketColor = "#D4D4D4";      // Light gray

        /// <summary>
        /// Applies syntax highlighting to the given text using rich-text color tags.
        /// </summary>
        /// <param name="text">The code text to highlight</param>
        /// <returns>The text with rich-text color markup applied</returns>
        public static string ApplyHighlighting(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";

            // Replace angle brackets to prevent rich-text parsing in the overlay
            text = text.Replace("<", "‹").Replace(">", "›");

            // Step 1: Extract strings first so comment markers inside strings are ignored
            var stringPlaceholders = new List<string>();
            text = Regex.Replace(
                text,
                @"""([^""\\]|\\.)*""",
                match =>
                {
                    stringPlaceholders.Add(match.Value);
                    return $"__STR{stringPlaceholders.Count - 1}__";
                }
            );
            text = Regex.Replace(
                text,
                @"'([^'\\]|\\.)*'",
                match =>
                {
                    stringPlaceholders.Add(match.Value);
                    return $"__STR{stringPlaceholders.Count - 1}__";
                }
            );

            // Step 2: Extract comments so they always stay green
            var commentPlaceholders = new List<string>();
            text = Regex.Replace(
                text,
                @"//.*|/\*(.|\n)*?\*/",
                match =>
                {
                    commentPlaceholders.Add(match.Value);
                    return $"__CMT{commentPlaceholders.Count - 1}__";
                }
            );

            // Step 3: Apply control keyword highlighting (purple)
            foreach (string keyword in controlKeywords)
            {
                string pattern = $@"\b{keyword}\b";
                text = Regex.Replace(
                    text,
                    pattern,
                    $"<color={controlKeywordColor}>{keyword}</color>"
                );
            }

            // Step 4: Apply keyword highlighting
            foreach (string keyword in keywords)
            {
                string pattern = $@"\b{keyword}\b";
                text = Regex.Replace(
                    text, 
                    pattern, 
                    $"<color={keywordColor}>{keyword}</color>"
                );
            }
            
            // Step 5: Apply function call highlighting (word followed by opening parenthesis)
            text = Regex.Replace(
                text,
                @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(",
                $"<color={functionColor}>$1</color>("
            );
            
            // Step 6: Apply number highlighting
            text = Regex.Replace(
                text,
                @"\b(\d+\.?\d*f?)\b",
                $"<color={numberColor}>$1</color>"
            );
            
            // Step 7: Apply bracket highlighting
            text = Regex.Replace(
                text,
                @"[\{\}\[\]]",
                match => $"<color={bracketColor}>{match.Value}</color>"
            );

            // Step 8: Restore strings
            for (int i = 0; i < stringPlaceholders.Count; i++)
            {
                text = text.Replace(
                    $"__STR{i}__",
                    $"<color={stringColor}>{stringPlaceholders[i]}</color>"
                );
            }

            // Step 9: Restore comments last so they override everything inside
            for (int i = 0; i < commentPlaceholders.Count; i++)
            {
                text = text.Replace(
                    $"__CMT{i}__",
                    $"<color={commentColor}>{commentPlaceholders[i]}</color>"
                );
            }
            
            return text;
        }
    }
}
