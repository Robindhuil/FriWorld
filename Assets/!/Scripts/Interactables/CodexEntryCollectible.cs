using System.Linq;
using UnityEngine;

public class CodexEntryCollectible : Collectible
{
    [SerializeField] private string codexEntryName;
    private PlayerUI playerUI;

    void Start()
    {
        UIManager uIManager = FindFirstObjectByType<UIManager>();

        playerUI = uIManager.playerUI;
    }

    protected override void Interact()
    {
        CodexEntry entryToUnlock = Codex.Instance.Entries.FirstOrDefault(e => e.name == codexEntryName);

        if (entryToUnlock != null && !Codex.Instance.IsEntryUnlocked(entryToUnlock))
        {
            Codex.Instance.UnlockEntry(entryToUnlock);
            playerUI?.DisplayCodexUnlock(codexEntryName);
            Debug.Log($"[CodexEntryCollectible] Odomkol sa codex entry: {codexEntryName}");
        }

        base.Interact();
    }
}