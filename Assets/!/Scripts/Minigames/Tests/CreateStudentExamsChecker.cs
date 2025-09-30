using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "QuestCheckers/Create Student Exams Checker")]
public class CreateStudentExamsChecker : QuestChecker
{
    public override bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message)
    {
        message = "";

        string codeNoComments = Regex.Replace(userSourceCode, @"//.*", "");
        codeNoComments = Regex.Replace(codeNoComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        string normalizedCode = Regex.Replace(codeNoComments.ToLower(), @"\s+", "");

        if (!Regex.IsMatch(normalizedCode, @"intpocetstudentov=5;"))
        {
            message = "Chyba: Premenná 'pocetStudentov' nie je správne definovaná. Správny zápis: int pocetStudentov = 5;";
            return false;
        }

        if (!Regex.IsMatch(normalizedCode, @"for\(inti=1;i<=pocetstudentov;i\+\+\)"))
        {
            message = "Chyba: For-cyklus nie je správne napísaný. Správny zápis: for (int i = 1; i <= pocetStudentov; i++)";
            return false;
        }

        if (!Regex.IsMatch(normalizedCode, @"system\.out\.println\(""generovanieskusokdokoncene\.""\);"))
        {
            message = "Chyba: Ukončovacia správa nie je správna. Správny zápis: System.out.println(\"Generovanie skusok dokoncene.\");";
            return false;
        }

        string[] lines = stdOut.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 7)
        {
            message = "Chyba: Výstup obsahuje príliš málo riadkov.";
            return false;
        }

        if (!lines[0].Contains("Spustam generovanie skusok pre studentov"))
        {
            message = "Chyba: Prvý riadok výstupu by mal obsahovať 'Spustam generovanie skusok pre studentov...'";
            return false;
        }

        if (!lines[1].Contains("Pocet studentov: 5"))
        {
            message = "Chyba: Druhý riadok výstupu by mal obsahovať 'Pocet studentov: 5'";
            return false;
        }

        for (int i = 1; i <= 5; i++)
        {
            string expectedLine = "Skuska pre studenta s id:" + i + " bola vygenerovana.";
            bool found = false;
            for (int j = 2; j < lines.Length - 1; j++)
            {
                if (lines[j].Contains(expectedLine))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                message = $"Chyba: Výstup neobsahuje správu pre študenta #{i}. Očakávané: '{expectedLine}'";
                return false;
            }
        }

        if (!lines[lines.Length - 1].Contains("Generovanie skusok dokoncene"))
        {
            message = "Chyba: Posledný riadok výstupu by mal obsahovať 'Generovanie skusok dokoncene.'";
            return false;
        }

        message = "Uloha splnena! Vyborna praca!";
        return true;
    }

    public override bool PartialCheckSolution(string userSourceCode, out string partialMessage)
    {
        partialMessage = "";
        bool hasTip = false;

        if (!userSourceCode.Contains("pocetStudentov"))
        {
            partialMessage += "Tip: Potrebuješ premennú pre počet študentov\n";
            hasTip = true;
        }
        else if (!userSourceCode.Contains("= 5"))
        {
            partialMessage += "Tip: Počet študentov má byť 5\n";
            hasTip = true;
        }

        if (!userSourceCode.Contains("for ("))
        {
            partialMessage += "Tip: Potrebuješ for-cyklus pre každého študenta\n";
            hasTip = true;
        }
        else if (!userSourceCode.Contains("i = 1"))
        {
            partialMessage += "Tip: Cyklus by mal začínať od 1\n";
            hasTip = true;
        }

        if (!userSourceCode.Contains("System.out.println"))
        {
            partialMessage += "Tip: Zabudol si na výpis správ?\n";
            hasTip = true;
        }

        return hasTip;
    }
}
