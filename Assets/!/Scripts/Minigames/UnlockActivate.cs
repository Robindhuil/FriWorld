using System.Collections;
using UnityEngine;

public class UnlockActivate : MonoBehaviour
{
    [SerializeField] private LockedDoor doorToUnlock;

    void Update()
    {
        if (gameObject.GetComponent<MiniGame>() == null)
        {
            doorToUnlock.GetComponent<Animator>().SetBool("IsOpen", true);
            StartCoroutine(DestroyAfterDelay(2f));

            Destroy(doorToUnlock);
            Destroy(this);
        }
    }

    private IEnumerator DestroyAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(this);
    }


}