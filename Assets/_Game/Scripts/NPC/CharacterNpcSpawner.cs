using System.Collections;
using FriWorld.Character;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns wandering NPCs built from the character catalog.
///
/// Replaces NPCSpawner, which picked one of thirty pre-made prefabs. Here there is one body per
/// gender and the look is rolled from a seed, so the crowd stops repeating itself and a new
/// outfit is a line in CharacterPresets.json rather than a new model.
///
/// Everything a spawned NPC needs is added here rather than sitting on the prefab: the character
/// prefab is a plain model wrapper and must stay that way, or a reimport of npc.blend would have
/// something to sweep away.
/// </summary>
public class CharacterNpcSpawner : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Baked by Character > 3 — Bake Catalog. Loaded from Resources when left empty.")]
    [SerializeField] private CharacterCatalog catalog;

    [Range(0f, 1f)]
    [Tooltip("Share of spawns using the female body. A gender with no body falls back to the other.")]
    [SerializeField] private float femaleShare = 0.5f;

    [Header("Spawn settings")]
    [SerializeField] private int maxActiveNPCs = 20;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float minLifetime = 300f;
    [SerializeField] private float maxLifetime = 600f;
    [SerializeField] private float minSpawnDelay = 1f;
    [SerializeField] private float maxSpawnDelay = 4f;

    [Tooltip("Seconds to give an NPC to walk back before it despawns where it stands.")]
    [SerializeField] private float walkHomeTimeout = 30f;

    [Header("Wandering")]
    [Tooltip("Waypoints the spawned NPCs walk between.")]
    [SerializeField] private PathWay wanderPath;

    [Header("Agent")]
    [Tooltip("Must match the NavMesh surface they walk on. -334000983 is the NPC agent type.")]
    [SerializeField] private int agentTypeId = -334000983;
    [SerializeField] private float agentRadius = 0.1f;
    [SerializeField] private float agentHeight = 2f;
    [SerializeField] private float agentSpeed = 1.5f;
    [SerializeField] private float agentAngularSpeed = 120f;
    [SerializeField] private float agentAcceleration = 8f;

    int activeNpcs;
    int spawnCounter;

    void Start()
    {
        if (catalog == null) catalog = Resources.Load<CharacterCatalog>("CharacterCatalog");
        if (catalog == null)
        {
            Debug.LogError("[CharacterNpcSpawner] No CharacterCatalog. "
                           + "Run Character > 3 — Bake Catalog.", this);
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (activeNpcs < maxActiveNPCs)
        {
            SpawnNPC();
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
        }
    }

    void SpawnNPC()
    {
        // The seed mixes the spawn ordinal with this spawner's position, so two spawners do not
        // march through the same sequence of looks and a respawn stays reproducible.
        int seed = unchecked(spawnCounter++ * 397 ^ transform.position.GetHashCode());
        var pick = new System.Random(seed);

        var gender = pick.NextDouble() < femaleShare ? Gender.Female : Gender.Male;
        var bundle = catalog.Bundle(gender);

        if (bundle == null || bundle.basePrefab == null)
        {
            // Only one body modelled so far — fall back rather than skip the spawn.
            gender = gender == Gender.Male ? Gender.Female : Gender.Male;
            bundle = catalog.Bundle(gender);

            if (bundle == null || bundle.basePrefab == null)
            {
                Debug.LogError("[CharacterNpcSpawner] The catalog has no base prefab at all.", this);
                return;
            }
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject npc = Instantiate(bundle.basePrefab, spawnPosition, Quaternion.identity);
        npc.name = $"npc_{gender}_{seed}";

        CharacterBuilder.Apply(npc, CharacterRandomizer.Roll(seed, catalog, gender), catalog);

        var agent = npc.AddComponent<NavMeshAgent>();
        agent.agentTypeID = agentTypeId;
        agent.radius = agentRadius;
        agent.height = agentHeight;
        agent.speed = agentSpeed;
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;

        npc.AddComponent<NpcWander>().Configure(wanderPath, seed);

        StartCoroutine(HandleNpcLifetime(npc, Random.Range(minLifetime, maxLifetime)));
        activeNpcs++;
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnRadius;
        randomPos.y = transform.position.y;

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    IEnumerator HandleNpcLifetime(GameObject npc, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (npc != null)
        {
            // Send them home first, so nobody blinks out of existence in front of the player.
            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                var wander = npc.GetComponent<NpcWander>();
                if (wander != null) wander.enabled = false;

                agent.SetDestination(transform.position);

                // Bounded. An NPC that cannot get back — blocked, or the spawner moved off the
                // navmesh — would otherwise hold this coroutine and its slot in activeNpcs for
                // the rest of the session, and the crowd would thin out with nothing in the log.
                float deadline = Time.time + walkHomeTimeout;
                while (npc != null
                       && Time.time < deadline
                       && Vector3.Distance(npc.transform.position, transform.position) > 1.5f)
                    yield return null;
            }

            if (npc != null) Destroy(npc);
        }

        // Unconditional: an NPC destroyed by anything else still has to give its slot back, or
        // the spawner quietly spawns fewer and fewer until it stops.
        activeNpcs--;

        if (activeNpcs < maxActiveNPCs)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
            SpawnNPC();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
