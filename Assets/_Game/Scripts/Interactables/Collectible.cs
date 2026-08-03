using UnityEngine;
using System.Collections;

public class Collectible : Interactable
{
    protected override void Interact()
    {
        base.Interact();
        StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy()
    {
        if (transform.childCount > 0)
        {
            Destroy(transform.GetChild(0).gameObject);
        }

        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            Destroy(outline);
        }

        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
