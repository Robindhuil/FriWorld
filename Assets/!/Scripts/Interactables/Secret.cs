using System.Linq;
using UnityEngine;

public class Secret : Collectible
{
    [SerializeField]
    private string secretName;
    protected override void Interact()
    {
        Player player = FindFirstObjectByType<Player>();

        player.PlayerManagment.stats.AddSecret();
        PlayerUI playerUI = player.GetComponent<UIManager>().playerUI;


        CodexEntry secretEntry = Codex.Instance.Entries.FirstOrDefault(e => e.name == secretName);
        if (secretEntry != null && !Codex.Instance.IsEntryUnlocked(secretEntry))
        {
            Codex.Instance.UnlockEntry(secretEntry);
            playerUI.DisplaySecretUnlock(secretName);
        }
        base.Interact();
    }
}
