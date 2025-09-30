using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntryActivator : MonoBehaviour
{
    [SerializeField]
    private List<EntryMapping> entryMappings = new List<EntryMapping>();
    private PlayerUI playerUI;

    void Start()
    {
        UIManager uIManager = FindFirstObjectByType<UIManager>();

        playerUI = uIManager.playerUI;
    }
    /// <summary>
    /// Aktivuje codex entry na základe quest ID.
    /// Uistite sa, že mapovania tu sú len pre quest-specific entry,
    /// nie pre učiteľské entry.
    /// </summary>
    public void ActivateEntryForQuest(int questId)
    {
        var mappings = entryMappings.Where(m => m.questId == questId);
        foreach (var mapping in mappings)
        {
            CodexEntry entryToUnlock = Codex.Instance.Entries.FirstOrDefault(e =>
                e.name == mapping.codexEntryName && e.category != "Učitelia");

            if (entryToUnlock != null && !Codex.Instance.IsEntryUnlocked(entryToUnlock))
            {
                Codex.Instance.UnlockEntry(entryToUnlock);
                playerUI?.DisplayCodexUnlock(mapping.codexEntryName);
                Debug.Log($"[EntryActivator] Odomkol sa quest-specific entry: {mapping.codexEntryName}");
            }
        }
    }

}
