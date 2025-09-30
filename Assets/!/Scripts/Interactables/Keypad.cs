using UnityEngine;

public class Keypad : Interactable
{

    [SerializeField]
    private GameObject door;
    private bool isOpen;
    void Start()
    {

    }

    void Update()
    {

    }


    protected override void Interact()
    {
        isOpen = !isOpen;
        door.GetComponent<Animator>().SetBool("IsOpen", isOpen);
    }
}