using System.Linq;
using UnityEngine;

public class Secret : Collectible
{
    [SerializeField]
    private string secretName;
    [SerializeField]
    private string secretId;
    protected override void Interact()
    {
        Player player = FindFirstObjectByType<Player>();

        player.PlayerManagment.stats.AddSecret();
        PlayerUI playerUI = player.GetComponent<UIManager>().playerUI;

        EntryActivator.ActivateEntryById(secretId, playerUI);

        base.Interact();
    }
}
