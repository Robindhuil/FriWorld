using UnityEngine;

public class PlayerManagment
{
    public Journal journal { get; set; }
    public Stats stats { get; set; }

    public PlayerManagment(MonoBehaviour runner)
    {
        journal = new Journal();
        stats = new Stats(runner);
    }
}