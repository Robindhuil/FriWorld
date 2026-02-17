using System.Linq;
using UnityEngine;

/// <summary>
/// Utility class for unlocking codex entries
/// </summary>
public static class EntryActivator
{
    /// <summary>
    /// Activates a codex entry by ID.
    /// Works for all categories including teachers.
    /// </summary>
    public static void ActivateEntryById(string entryId, PlayerUI playerUI)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            Debug.LogWarning("[EntryActivator] Entry ID is null or empty!");
            return;
        }

        if (Codex.Instance == null)
        {
            Debug.LogError("[EntryActivator] Codex.Instance is null!");
            return;
        }

        CodexEntry entryToUnlock = Codex.Instance.Entries.FirstOrDefault(e => e.id == entryId);

        if (entryToUnlock != null && !Codex.Instance.IsEntryUnlocked(entryToUnlock))
        {
            Codex.Instance.UnlockEntry(entryToUnlock);
            playerUI?.DisplayCodexUnlock(entryToUnlock.name);
            Debug.Log($"[EntryActivator] Odomkol sa entry: {entryToUnlock.name}");
        }
        else if (entryToUnlock == null)
        {
            Debug.LogWarning($"[EntryActivator] Entry s ID '{entryId}' nebola nájdená."); 
        }

        if (entryToUnlock != null && GameState.Instance != null)
        {
            GameState.Instance.SetBool($"{entryId}_Unlocked", true);
        }
    }
}
