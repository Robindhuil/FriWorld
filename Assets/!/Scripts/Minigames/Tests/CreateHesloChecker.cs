using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Heslo Checker")]

public class CreateHesloChecker : QuestChecker
{
    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        string staticErrors = CollectStaticErrors(userSourceCode);

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(staticErrors))
        {
            sb.AppendLine(staticErrors);
        }

        string expectedOutputLower = "o je: 813a222";
        if (!stdOut.ToLower().Contains(expectedOutputLower))
        {
            sb.AppendLine("Chyba: Výstup nie je správny.");
        }

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

    /// <summary>
    /// Vykoná statickú analýzu zdrojového kódu (Regex) a vráti reťazec s chybami.
    /// Ak je reťazec prázdny, kód spĺňa očakávania z hľadiska statickej kontroly.
    /// </summary>
    private string CollectStaticErrors(string userSourceCode)
    {
        StringBuilder errorMessages = new StringBuilder();

        string codeNoComments = Regex.Replace(userSourceCode, @"//.*", "");
        codeNoComments = Regex.Replace(codeNoComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        string normalizedCode = Regex.Replace(codeNoComments.ToLower(), @"\s+", "");

        if (!Regex.IsMatch(normalizedCode, @"intpocetokien=8;"))
        {
            errorMessages.AppendLine("Chyba: Premenná 'pocetOkien' je nesprávna! Tip: spočítaj okná;");
        }
        if (!Regex.IsMatch(normalizedCode, @"intcashodina=13;"))
        {
            errorMessages.AppendLine("Chyba: Premenná 'casHodina' je nesprávna! Tip: pozri koľko je hodín vedľa počítača;");
        }
        if (!Regex.IsMatch(normalizedCode, @"stringmenomiestnosti=""ra222"";"))
        {
            errorMessages.AppendLine("Chyba: Premenná 'menoMiestnosti' je nesprávna! Tip: pozri sa na štítok pri dverách;");
        }
        if (!Regex.IsMatch(normalizedCode, @"booleanvonkujetma=false;"))
        {
            errorMessages.AppendLine("Chyba: Premenná 'vonkuJeTma' je nesprávna! Tip: ak je vonku tma, nastav ju na true/ak je vonku deň nastav false;");
        }
        if (!Regex.IsMatch(normalizedCode, @"charaktualnyblok='a';"))
        {
            errorMessages.AppendLine("Chyba: Premenná 'charaktualnyBlok' je nesprávna! Tip: písmeno bloku je druhé písmenko označenia miestnosti(napr. RB002 - B je blok);");
        }
        if (!normalizedCode.Contains("substring(2,5)"))
        {
            errorMessages.AppendLine("Chyba: Chýba volanie 'substring(2, 5)' na premennej 'menoMiestnosti'.");
        }
        if (!normalizedCode.Contains("boolean.compare("))
        {
            errorMessages.AppendLine("Chyba: Chýba volanie 'Boolean.compare(vonkujetma,false)' vo výpise.");
        }
        if (!Regex.IsMatch(normalizedCode, @"system\.out\.println\(""oje:"""))
        {
            errorMessages.AppendLine("Chyba: Nevidím správne volanie 'System.out.println(\"O je: \" + ... )'.");
        }

        return errorMessages.ToString();
    }
}
