using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Dvere While Loop Checker")]
public class CreateDvereWhileLoopChecker : QuestChecker
{
    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        string staticErrors = CollectStaticErrors(userSourceCode);
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(staticErrors))
            sb.AppendLine(staticErrors);

        string expectedOutput = "Program ukončený!";
        if (!stdOut.ToLower().Contains(expectedOutput.ToLower()))
            sb.AppendLine("Chyba: Výstup nie je správny. Očakávame: " + expectedOutput);

        if (sb.Length > 0)
        {
            message = sb.ToString();
            return false;
        }
        else
        {
            message = "Úloha splnená! Skvelá práca!";
            return true;
        }
    }

    public override bool PartialCheckSolution(string userSourceCode, out string partialMessage)
    {
        string staticErrors = CollectStaticErrors(userSourceCode);
        if (!string.IsNullOrEmpty(staticErrors))
        {
            partialMessage = staticErrors;
            return true;
        }
        else
        {
            partialMessage = "Statická kontrola nehlási chyby. Skontroluj si prosím chybu kompilátora.";
            return false;
        }
    }

    private string CollectStaticErrors(string userSourceCode)
    {
        StringBuilder errorMessages = new StringBuilder();

        string codeNoComments = Regex.Replace(userSourceCode, @"//.*", "");
        codeNoComments = Regex.Replace(codeNoComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        string normalizedCode = Regex.Replace(codeNoComments.ToLower(), @"\s+", "");

        if (normalizedCode.Contains("___"))
            errorMessages.AppendLine("Chyba: V kóde stále figuruje placeholder '___'. Doplň všetky chýbajúce hodnoty a operátory.");

        if (!Regex.IsMatch(normalizedCode, @"booleanzastav=false;"))
            errorMessages.AppendLine("Chyba: Premenná 'zastav' musí byť deklarovaná ako boolean a inicializovaná na false (napr. 'boolean zastav = false;').");

        if (!Regex.IsMatch(normalizedCode, @"if\s*\(\s*zastav\s*==\s*true\s*\)\s*\{[^}]*break;"))
            errorMessages.AppendLine("Chyba: V cykle musí byť podmienka 'if (zastav == true)' a vo vnútri bloku musí byť príkaz 'break;'.");

        if (!Regex.IsMatch(normalizedCode, @"while\(true\)\{.*zastav=true;"))
            errorMessages.AppendLine("Chyba: V cykle by mal byť príkaz, ktorý nastaví 'zastav' na true;').");

        if (!Regex.IsMatch(normalizedCode, @"system\.out\.println\(""programukončený!""\)"))
            errorMessages.AppendLine("Chyba: Po cykle musí byť výpis 'Program ukončený!'.");

        return errorMessages.ToString();
    }
}
