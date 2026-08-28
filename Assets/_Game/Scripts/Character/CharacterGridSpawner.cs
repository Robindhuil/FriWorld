using UnityEngine;

namespace FriWorld.Character
{
    /// <summary>
    /// Spawns a grid of characters on Play so a batch of variations can be looked at side by side.
    ///
    /// A test harness, not a game system: NPCSpawner is what the game will use. This exists to
    /// answer one question by eye — do the presets, the hides masks and the colorways combine
    /// into people who look different from one another and are not wearing their own skin
    /// through their shirt.
    ///
    /// Seeds are consecutive from firstSeed, so the same inspector settings give the same twenty
    /// characters every run. Change firstSeed to see a different twenty.
    /// </summary>
    public sealed class CharacterGridSpawner : MonoBehaviour
    {
        [Header("Catalog")]
        [Tooltip("Baked by Character > 3 — Bake Catalog. Loaded from Resources when left empty.")]
        [SerializeField] private CharacterCatalog catalog;

        [Header("Grid")]
        [Min(1)]
        [SerializeField] private int count = 20;
        [Min(1)]
        [SerializeField] private int columns = 5;
        [SerializeField] private Vector2 spacing = new Vector2(1.5f, 2f);

        [Header("Variation")]
        [Tooltip("Consecutive seeds from here, so a run is reproducible.")]
        [SerializeField] private int firstSeed = 1000;

        [Range(0f, 1f)]
        [Tooltip("Share of spawns that use the female body. Ignored while only one body exists.")]
        [SerializeField] private float femaleShare = 0.5f;

        [Tooltip("Turn the spawned characters to face the camera at spawn.")]
        [SerializeField] private bool faceCamera = true;

        void Start()
        {
            if (catalog == null) catalog = Resources.Load<CharacterCatalog>("CharacterCatalog");
            if (catalog == null)
            {
                Debug.LogError("[CharacterGridSpawner] No CharacterCatalog. "
                               + "Run Character > 3 — Bake Catalog.", this);
                return;
            }

            int spawned = 0;
            int skipped = 0;

            for (int i = 0; i < count; i++)
            {
                int seed = firstSeed + i;

                // One draw per character rather than Random.value, so which body a slot gets does
                // not shift when count changes.
                var pick = new System.Random(seed);
                var gender = pick.NextDouble() < femaleShare ? Gender.Female : Gender.Male;

                var bundle = catalog.Bundle(gender);
                if (bundle == null || bundle.basePrefab == null)
                {
                    // Only one body modelled so far: fall back rather than leave a hole in the
                    // grid, and say so once at the end.
                    var other = gender == Gender.Male ? Gender.Female : Gender.Male;
                    bundle = catalog.Bundle(other);
                    gender = other;
                    skipped++;

                    if (bundle == null || bundle.basePrefab == null)
                    {
                        Debug.LogError("[CharacterGridSpawner] The catalog has no base prefab at all.", this);
                        return;
                    }
                }

                int column = i % columns;
                int row = i / columns;

                Vector3 position = transform.position
                                   + transform.right * (column * spacing.x)
                                   + transform.forward * (row * spacing.y);

                var rotation = faceCamera ? transform.rotation : Quaternion.identity;
                var instance = Instantiate(bundle.basePrefab, position, rotation, transform);
                instance.name = $"npc_{i:00}_{gender}_{seed}";

                CharacterBuilder.Apply(instance, CharacterRandomizer.Roll(seed, catalog, gender), catalog);
                spawned++;
            }

            string note = skipped > 0
                ? $" {skipped} fell back to the other body because theirs is not modelled yet."
                : string.Empty;
            Debug.Log($"[CharacterGridSpawner] {spawned} characters, seeds {firstSeed}..{firstSeed + count - 1}.{note}", this);
        }
    }
}
