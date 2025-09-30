using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] npcPrefabs;
    public int maxActiveNPCs = 20;
    public float spawnRadius = 5f;
    public float minLifetime = 300f;
    public float maxLifetime = 600f;
    public float minSpawnDelay = 1f;
    public float maxSpawnDelay = 4f;

    private int activeNpcs = 0;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (activeNpcs < maxActiveNPCs)
        {
            SpawnNPC();
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
        }
    }

    private void SpawnNPC()
    {
        if (npcPrefabs.Length == 0) return;

        GameObject npcPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Vector3 spawnPosition = GetRandomSpawnPosition();

        GameObject npcObj = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);
        Npc npc = npcObj.GetComponent<Npc>();

        StartCoroutine(HandleNpcLifetime(npcObj, Random.Range(minLifetime, maxLifetime)));
        activeNpcs++;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnRadius;
        randomPos.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, spawnRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }

    private IEnumerator HandleNpcLifetime(GameObject npc, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (npc != null)
        {
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(transform.position);
                while (Vector3.Distance(npc.transform.position, transform.position) > 1.5f)
                {
                    yield return null;
                }
            }

            Destroy(npc);
            activeNpcs--;

            if (activeNpcs < maxActiveNPCs)
            {
                yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
                SpawnNPC();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
