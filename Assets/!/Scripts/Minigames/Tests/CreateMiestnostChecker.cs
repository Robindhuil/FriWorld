using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Miestnost Checker")]
public class CreateMiestnostChecker : QuestChecker
{
    private readonly int expectedPocitace = 47;
    private readonly int expectedStolicky = 47;
    private readonly int expectedOkna = 8;
    private readonly int expectedStoly = 23;
    private readonly string expectedMiestnost = "RA013";

    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        if (HasPlaceholders(userSourceCode))
        {
            message = GetPlaceholderHints();
            return false;
        }

        string syntaxErrors = CheckSyntax(userSourceCode);
        if (!string.IsNullOrEmpty(syntaxErrors))
        {
            message = syntaxErrors;
            return false;
        }

        if (!string.IsNullOrEmpty(stdErr))
        {
            message = "Chyba kompilácie:\n" + stdErr + "\n\n" + GetCompilationTips(stdErr);
            return false;
        }

        message = "Úloha splnená! Skvelá práca!";
        return true;
    }

    public override bool PartialCheckSolution(string userSourceCode, out string partialMessage)
    {
        if (HasPlaceholders(userSourceCode))
        {
            partialMessage = GetPlaceholderHints();
            return true;
        }

        string syntaxErrors = CheckSyntax(userSourceCode, true);
        if (!string.IsNullOrEmpty(syntaxErrors))
        {
            partialMessage = syntaxErrors;
            return true;
        }

        partialMessage = "Kód vyzerá dobre, pokračujte v kompilácii.";
        return false;
    }

    private bool HasPlaceholders(string code)
    {
        return Regex.IsMatch(code, @"\b___\b");
    }

    private string CheckSyntax(string code, bool isPartialCheck = false)
    {
        StringBuilder errors = new StringBuilder();
        string normalizedCode = NormalizeCode(code);

        string[] requiredVariables = {
            @"privateintpocetpocitacov;",
            @"privateintpocetstolick;",
            @"privateintpocetokien;",
            @"privateintpocetstolov;",
            @"privatestringnazovmiestnosti;"
        };

        foreach (var pattern in requiredVariables)
        {
            if (!Regex.IsMatch(normalizedCode, pattern))
            {
                errors.AppendLine("Chýbajú všetky potrebné deklarácie premenných v triede");
                break;
            }
        }

        if (!Regex.IsMatch(normalizedCode, @"publicmiestnost\(\s*intpocetpocitacov\s*,\s*intpocetstolick\s*,\s*intpocetokien\s*,\s*intpocetstolov\s*,\s*stringnazovmiestnosti\s*\)"))
        {
            errors.AppendLine("Konštruktor musí mať 5 parametrov (4 čísla, 1 reťazec)");
        }

        string[] requiredAssignments = {
            @"this\.pocetpocitacov=pocetpocitacov;",
            @"this\.pocetstolick=pocetstolick;",
            @"this\.pocetokien=pocetokien;",
            @"this\.pocetstolov=pocetstolov;",
            @"this\.nazovmiestnosti=nazovmiestnosti;"
        };

        foreach (var pattern in requiredAssignments)
        {
            if (!Regex.IsMatch(normalizedCode, pattern))
            {
                errors.AppendLine("Chýbajú všetky potrebné priradenia v konštruktore");
                break;
            }
        }

        if (!isPartialCheck && !Regex.IsMatch(normalizedCode,
            $@"newmiestnost\(\s*{expectedPocitace}\s*,\s*{expectedStolicky}\s*,\s*{expectedOkna}\s*,\s*{expectedStoly}\s*,\s*""{expectedMiestnost.ToLower()}""\s*\)"))
        {
            errors.AppendLine($"Nesprávne parametre: new Miestnost({expectedPocitace}, {expectedStolicky}, {expectedOkna}, {expectedStoly}, \"{expectedMiestnost}\")");
        }

        return errors.ToString();
    }

    private string GetPlaceholderHints()
    {
        return "Doplňte všetky placeholdery '___' podľa zadania:\n\n" +
               "V konštruktore:\n" +
               "this.pocetPocitacov = pocetPocitacov;\n" +
               "this.pocetStolick = pocetStolick;\n" +
               "this.pocetOkien = pocetOkien;\n" +
               "this.pocetStolov = pocetStolov;\n" +
               "this.nazovMiestnosti = nazovMiestnosti;\n\n" +
               "V metóde main():\n" +
               $"new Miestnost({expectedPocitace}, {expectedStolicky}, {expectedOkna}, {expectedStoly}, \"{expectedMiestnost}\");";
    }

    private string GetCompilationTips(string error)
    {
        if (error.Contains("cannot find symbol"))
            return "Skontrolujte pravopis premenných a metód";
        if (error.Contains("missing return statement"))
            return "Chýba návratová hodnota v metóde";
        if (error.Contains("';' expected"))
            return "Chýba bodkočiarka na konci príkazu";

        return "Skontrolujte syntax a typy premenných";
    }

    private string NormalizeCode(string code)
    {
        string codeNoComments = Regex.Replace(code, @"//.*", "");
        codeNoComments = Regex.Replace(codeNoComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(codeNoComments.ToLower(), @"\s+", "");
    }
}