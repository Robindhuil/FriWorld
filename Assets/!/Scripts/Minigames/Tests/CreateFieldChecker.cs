using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Priezviska Checker")]
public class CreatePriezviskaChecker : QuestChecker
{
    private readonly HashSet<string> teacherSurnames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "janech", "ďuračík", "kvet", "meško", "tóth", "petríková", "gregorová"
    };

    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        if (!HasValidSyntax(userSourceCode, out var syntaxError))
        {
            message = syntaxError;
            return false;
        }

        if (!ContainsAllTeachers(userSourceCode, out var missingTeachers))
        {
            message = $"Chýbajú títo učitelia: {string.Join(", ", missingTeachers)}";
            return false;
        }

        if (!HasCorrectOutput(stdOut, out var missingOutput))
        {
            message = $"Výstup neobsahuje: {string.Join(", ", missingOutput)}";
            return false;
        }

        message = "Výborne! Správne zoznam všetkých učiteľov!";
        return true;
    }

    public override bool PartialCheckSolution(string userSourceCode, out string partialMessage)
    {
        if (!userSourceCode.Contains("String[] priezviska"))
        {
            partialMessage = "Zabudol si vytvoriť pole 'priezviska'";
            return true;
        }

        if (!userSourceCode.Contains("for (int i = 0;"))
        {
            partialMessage = "Chýba ti for-cyklus na prechádzanie poľa";
            return true;
        }

        ContainsAllTeachers(userSourceCode, out var missingTeachers);
        if (missingTeachers.Count > 0)
        {
            partialMessage = $"Ešte treba doplniť: {string.Join(", ", missingTeachers)}";
            return true;
        }

        partialMessage = "Kód vyzerá dobre, skús ho skompilovať!";
        return false;
    }

    private bool HasValidSyntax(string code, out string error)
    {
        if (!Regex.IsMatch(code, @"String\[\]\s*priezviska\s*=\s*\{.*\}", RegexOptions.Singleline))
        {
            error = "Nesprávna deklarácia poľa. Použi: String[] priezviska = {...}";
            return false;
        }

        if (!Regex.IsMatch(code, @"for\s*\(\s*int\s+i\s*=\s*0\s*;.*i\s*<\s*priezviska\.length\s*;.*i\s*\+\+\s*\)"))
        {
            error = "Potrebuješ správny for-cyklus na prechádzanie poľa";
            return false;
        }

        error = "";
        return true;
    }

    private bool ContainsAllTeachers(string code, out List<string> missing)
    {
        missing = teacherSurnames
            .Where(t => !code.Contains(t, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return missing.Count == 0;
    }

    private bool HasCorrectOutput(string output, out List<string> missing)
    {
        missing = teacherSurnames
            .Where(t => !output.Contains(t, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return missing.Count == 0;
    }
}