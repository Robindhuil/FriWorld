
using UnityEngine;

public abstract class QuestChecker : ScriptableObject
{
    /// <summary>
    /// Plná kontrola – volá sa len ak kód úspešne prejde kompiláciou a spustí sa.
    /// Môže používať runtime výstup (stdOut) a overiť logiku.
    /// </summary>
    public abstract bool CheckSolution(string userSourceCode, string stdOut, string stdErr, out string message);

    /// <summary>
    /// Čiastočná kontrola – volá sa, ak kód NEprejde kompiláciou.
    /// Môže analyzovať iba text userSourceCode a dať tipy, prečo mohol nastať problém.
    /// </summary>
    public abstract bool PartialCheckSolution(string userSourceCode, out string partialMessage);
}
