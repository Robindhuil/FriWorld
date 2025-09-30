using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create If Condition Checker - Dokup Stoličky")]
public class CreateIfConditionChecker : QuestChecker
{
    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        message = "";
        StringBuilder errors = new StringBuilder();
        bool isValid = true;

        if (userSourceCode.Contains("___"))
        {
            errors.AppendLine("Chyba: Ešte stále máte nedoplnené placeholdery '___'");
            isValid = false;
        }

        if (!Regex.IsMatch(userSourceCode, @"int\s+monitory\s*=\s*\d+\s*;"))
        {
            errors.AppendLine("Chyba: Chýba alebo je nesprávna inicializácia premennej 'monitory'");
            isValid = false;
        }

        if (!Regex.IsMatch(userSourceCode, @"int\s+stolicky\s*=\s*\d+\s*;"))
        {
            errors.AppendLine("Chyba: Chýba alebo je nesprávna inicializácia premennej 'stolicky'");
            isValid = false;
        }

        string[] requiredConditions = {
            @"if\s*\(\s*monitory\s*>\s*stolicky\s*\)",
            @"if\s*\(\s*monitory\s*<\s*stolicky\s*\)",
            @"if\s*\(\s*monitory\s*==\s*stolicky\s*\)"
        };

        string[] conditionErrors = {
            "Chyba: Chýba podmienka pre prípad keď je viac monitorov než stoličiek",
            "Chyba: Chýba podmienka pre prípad keď je viac stoličiek než monitorov",
            "Chyba: Chýba podmienka pre rovnaký počet stoličiek a monitorov"
        };

        for (int i = 0; i < requiredConditions.Length; i++)
        {
            if (!Regex.IsMatch(userSourceCode, requiredConditions[i]))
            {
                errors.AppendLine(conditionErrors[i]);
                isValid = false;
            }
        }

        Match monitoryMatch = Regex.Match(userSourceCode, @"int\s+monitory\s*=\s*(\d+)\s*;");
        Match stolickyMatch = Regex.Match(userSourceCode, @"int\s+stolicky\s*=\s*(\d+)\s*;");

        if (monitoryMatch.Success && stolickyMatch.Success)
        {
            int monitory = int.Parse(monitoryMatch.Groups[1].Value);
            int stolicky = int.Parse(stolickyMatch.Groups[1].Value);
            int rozdiel = Math.Abs(monitory - stolicky);

            if (monitory > stolicky)
            {
                string expected = $"Treba dokúpiť {rozdiel} stoličk{(rozdiel == 1 ? "u" : rozdiel < 5 ? "y" : "y")}.";
                if (!stdOut.Contains(expected))
                {
                    errors.AppendLine($"Chyba: Chýba správny výpis pre prípad keď je viac monitorov. Očakávané: '{expected}'");
                    isValid = false;
                }
            }
            else if (monitory < stolicky)
            {
                if (!stdOut.Contains("Stoličiek je viac ako monitorov, netreba dokupovať."))
                {
                    errors.AppendLine("Chyba: Chýba správny výpis pre prípad keď je viac stoličiek");
                    isValid = false;
                }
            }
            else
            {
                if (!stdOut.Contains("Počet stoličiek sa rovná počtu monitorov, netreba nič dokupovať."))
                {
                    errors.AppendLine("Chyba: Chýba správny výpis pre rovnaký počet");
                    isValid = false;
                }
            }
        }

        message = isValid ? "Úloha splnená! Výborná práca!" : errors.ToString();
        return isValid;
    }

    public override bool PartialCheckSolution(string userSourceCode, out string partialMessage)
    {
        partialMessage = "";
        bool needsHelp = false;
        StringBuilder hints = new StringBuilder();

        if (userSourceCode.Contains("___"))
        {
            hints.AppendLine("Ešte máte nedoplnené miesta (___). Skontrolujte:");
            if (Regex.IsMatch(userSourceCode, @"int\s+monitory\s*=\s*___"))
                hints.AppendLine("- Doplňte počet monitorov (celé číslo)");
            if (Regex.IsMatch(userSourceCode, @"int\s+stolicky\s*=\s*___"))
                hints.AppendLine("- Doplňte počet stoličiek (celé číslo)");
            needsHelp = true;
        }

        if (!userSourceCode.Contains("if ("))
        {
            hints.AppendLine("Potrebujete podmienky if na porovnanie monitorov a stoličiek");
            needsHelp = true;
        }
        else
        {
            if (!userSourceCode.Contains("monitory >"))
                hints.AppendLine("Chýba podmienka pre prípad keď je viac monitorov");
            if (!userSourceCode.Contains("monitory <"))
                hints.AppendLine("Chýba podmienka pre prípad keď je viac stoličiek");
            if (!userSourceCode.Contains("monitory =="))
                hints.AppendLine("Chýba podmienka pre rovnaký počet");

            needsHelp = hints.Length > 0;
        }

        partialMessage = hints.ToString();
        return needsHelp;
    }
}