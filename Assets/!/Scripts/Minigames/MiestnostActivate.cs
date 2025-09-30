using UnityEngine;

public class MiestnostActivate : MonoBehaviour
{
    [SerializeField] private GameObject objectToActivate;

    void Update()
    {
        if (gameObject.GetComponent<MiniGame>() == null)
        {
            objectToActivate.SetActive(true);
            Destroy(this);
        }
    }

}