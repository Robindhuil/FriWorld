using System.Linq;
using UnityEngine;

public class CodexEntryCollectible : Collectible
{
    [SerializeField] private string entryId;
    private PlayerUI playerUI;

    void Start()
    {
        UIManager uIManager = FindFirstObjectByType<UIManager>();

        playerUI = uIManager.playerUI;
    }

    protected override void Interact()
    {
        CodexEntry entryToUnlock = Codex.Instance.Entries.FirstOrDefault(e => e.id == entryId);

        if (entryToUnlock != null && !Codex.Instance.IsEntryUnlocked(entryToUnlock))
        {
            Codex.Instance.UnlockEntry(entryToUnlock);
            playerUI?.DisplayCodexUnlock(entryToUnlock.name);
            Debug.Log($"[CodexEntryCollectible] Odomkol sa codex entry: {entryToUnlock.name}");
        }

        base.Interact();
    }
}