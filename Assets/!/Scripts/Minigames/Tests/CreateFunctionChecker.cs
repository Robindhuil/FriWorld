using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Function Checker")]
public class CreateFunctionChecker : QuestChecker
{
    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        string staticErrors = CollectStaticErrors(userSourceCode);
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(staticErrors))
        {
            sb.AppendLine(staticErrors);
        }

        string expectedOutput = "Prístup je povolený.";
        if (!stdOut.ToLower().Contains(expectedOutput.ToLower()))
        {
            sb.AppendLine("Chyba: Výstup nie je správny. Očakávame: " + expectedOutput);
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
    /// Vykoná statickú analýzu zdrojového kódu a vráti reťazec s chybami.
    /// Ak je reťazec prázdny, kód spĺňa očakávania.
    /// </summary>
    private string CollectStaticErrors(string userSourceCode)
    {
        StringBuilder errorMessages = new StringBuilder();

        string codeNoComments = Regex.Replace(userSourceCode, @"//.*", "");
        codeNoComments = Regex.Replace(codeNoComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        string normalizedCode = Regex.Replace(codeNoComments.ToLower(), @"\s+", "");

        // 1) Kontrola placeholdrov
        if (normalizedCode.Contains("___"))
        {
            errorMessages.AppendLine("Nezabudni nahradiť všetky placeholdre '___' konkrétnymi hodnotami.");
            errorMessages.AppendLine("Pomôcka: Skús sa pozrieť na počiatočný stav dverí a čo sa s nimi má stať po vytvorení.");
        }

        // 2) Kontrola konštruktora
        if (!Regex.IsMatch(normalizedCode, @"zamknute=true;"))
        {
            errorMessages.AppendLine("Dvere by mali byť na začiatku zamknuté. Skontroluj konštruktor.");
        }

        if (!Regex.IsMatch(normalizedCode, @"publicdvere\(\)\{.*odomkni\(\);"))
        {
            errorMessages.AppendLine("Po vytvorení dverí by sa mala zavolať metóda na ich odomknutie.");
        }

        // 3) Kontrola metódy odomkni()
        if (!Regex.IsMatch(normalizedCode, @"odomkni\(\)\{.*zamknute=false;"))
        {
            errorMessages.AppendLine("Metóda odomkni() by mala zmeniť stav dverí na odomknuté.");
        }

        if (!Regex.IsMatch(normalizedCode, @"odomkni\(\)\{.*system\.out\.println\(.*\);"))
        {
            errorMessages.AppendLine("Metóda odomkni() by mala vypísať správu o stave dverí.");
        }

        // 4) Kontrola metódy suZamknute()
        if (!Regex.IsMatch(normalizedCode, @"suzamknute\(\)\{.*returnzamknute;"))
        {
            errorMessages.AppendLine("Metóda suZamknute() by mala vrátiť aktuálny stav dverí.");
        }

        // 5) Kontrola main metódy
        if (!Regex.IsMatch(normalizedCode, @"newdvere\(\)"))
        {
            errorMessages.AppendLine("V main() metóde treba vytvoriť novú inštanciu dverí.");
        }

        if (!Regex.IsMatch(normalizedCode, @"if\(.*suzamknute\(\)\)"))
        {
            errorMessages.AppendLine("V main() metóde skontroluj podmienku pre prístup.");
        }

        return errorMessages.ToString();
    }
}
