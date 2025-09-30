using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

public class JavaCodeExecutor : MonoBehaviour
{
    [Header("Odkiaľ zobrať zdrojový kód a kam vypisovať výsledok")]
    public CodeInput codeInput;
    public TMP_Text outputText;

    public int executionTimeout = 5000;

    /// <summary>
    /// Metóda na spustenie Java kódu. 
    /// Ak checker != null a kód sa skompiluje bez chýb, zavolá test.
    /// Vráti (stdOut, stdErr, checkMessage, success).
    /// </summary>
    public (string stdOut, string stdErr, string checkMessage, bool success) ExecuteJavaCode(QuestChecker checker = null)
    {

        outputText.text = "";

        string javaCode = codeInput.inputField.text;

        string sourceFile = "TempProgram.java";
        File.WriteAllText(sourceFile, javaCode);

        string jarPath = Path.Combine(Application.streamingAssetsPath, "JaninoExecutor.jar");

        string myJdkLitePath = "";
#if UNITY_STANDALONE_WIN
        myJdkLitePath = Path.Combine(Application.streamingAssetsPath, "jdk/windows/bin/java.exe");
#elif UNITY_STANDALONE_OSX
    myJdkLitePath = Path.Combine(Application.streamingAssetsPath, "jdk/macos_arm/bin/java");
#elif UNITY_STANDALONE_LINUX
    myJdkLitePath = Path.Combine(Application.streamingAssetsPath, "jdk/linux_x64/bin/java");
#else
        UnityEngine.Debug.LogError("[JavaCodeExecutor] Nepodporovaná platforma!");
#endif

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = myJdkLitePath,
            Arguments = $"-jar \"{jarPath}\" \"{sourceFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        string commandPrompt = $"<color=yellow>$ {myJdkLitePath} -jar \"{jarPath}\" \"{sourceFile}\"</color>\n";
        outputText.text += commandPrompt;

        Process process = Process.Start(psi);

        if (!process.WaitForExit(executionTimeout))
        {
            process.Kill();
            string timeoutMsg = "\n<color=red>Execution timed out.</color>\n";
            outputText.text += timeoutMsg;
            return ("", timeoutMsg, "", false);
        }

        string stdOut = process.StandardOutput.ReadToEnd();
        string stdErr = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(stdErr))
        {
            stdErr = SimplifyError(stdErr);
            outputText.text += FormatTerminalOutput(stdErr, true);

            string partialMessage = "";
            if (checker != null)
            {
                bool tipsAvailable = checker.PartialCheckSolution(javaCode, out partialMessage);
                if (tipsAvailable && !string.IsNullOrEmpty(partialMessage))
                {
                    outputText.text += $"\n<color=red>{partialMessage}</color>\n";
                }
            }
            return (stdOut, stdErr, partialMessage, false);
        }
        else
        {
            outputText.text += FormatTerminalOutput(stdOut, false);
        }

        bool success = false;
        string checkMessage = "";
        if (checker != null)
        {
            success = checker.CheckSolution(javaCode, stdOut, stdErr, out checkMessage);
            string colorTag = success ? "green" : "red";
            outputText.text += $"\n<color={colorTag}>{checkMessage}</color>\n";
        }

        return (stdOut, stdErr, checkMessage, success);
    }


    /// <summary>
    /// Extrahuje relevantnú časť chybového hlásenia (napr. "Line 6, Column 30: Expression '___' is not an rvalue").
    /// </summary>
    private string SimplifyError(string errorText)
    {
        Match m = Regex.Match(errorText, @"Line\s*\d+,\s*Column\s*\d+:\s*.*");
        if (m.Success)
        {
            return m.Value;
        }
        else
        {
            string[] lines = errorText.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
                return lines[0];
            return errorText;
        }
    }

    /// <summary>
    /// Formátuje text na riadky a pridáva terminálový vzhľad.
    /// </summary>
    private string FormatTerminalOutput(string text, bool isError)
    {
        string color = isError ? "red" : "white";
        string[] lines = text.Split(new[] { '\n' }, System.StringSplitOptions.None);
        string formatted = "";
        foreach (string line in lines)
        {
            formatted += $"<color={color}>  {line}</color>\n";
        }
        return formatted;
    }

}
