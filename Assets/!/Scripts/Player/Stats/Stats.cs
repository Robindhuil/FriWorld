using UnityEngine;

public class Stats
{
    public int SecretsCollected { get; private set; } = 0;
    public int TotalSecretCount { get; private set; } = 0;
    public int Mistakes { get; set; } = 0;
    public int Walks { get; set; } = 0;

    public Stats(MonoBehaviour runner)
    {
        SecretsCollected = 0;
        TotalSecretCount = GameObject.FindGameObjectsWithTag("Secret").Length;
    }

    public void AddSecret()
    {
        SecretsCollected++;
    }

    public void AddMistake()
    {
        Mistakes++;
    }

    public void AddWalk(int walk)
    {
        Walks += walk;
    }
}
