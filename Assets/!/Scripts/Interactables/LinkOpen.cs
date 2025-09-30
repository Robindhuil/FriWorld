using UnityEngine;

public class LinkOpen : Interactable
{
    [SerializeField] private string link = "";

    protected override void Interact()
    {
        base.Interact();

        Application.OpenURL(link);
    }
}
