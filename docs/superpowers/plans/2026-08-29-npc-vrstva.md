# NPC vrstva — implementačný plán

> **Pre agentov:** POVINNÁ SUB-SKILL — na vykonanie použi `superpowers:executing-plans`,
> úloha po úlohe. Kroky sú checkboxy (`- [ ]`).

**Verzia projektu pri písaní:** 0.1.2-alpha · **Dátum:** 2026-08-29 · **Stav:** návrh, nezačaté

**Cieľ:** generovaní študenti chodia po budove ako dnes, ale na pasívnom tele riadenom zvonku.

**Architektúra:** `NpcActor` obaľuje `NavMeshAgent` a nerozhoduje o ničom — vie `GoTo`, `Stop`,
hlási `Activity` a `HasArrived`. `WaypointDirector` ho tiká a vyberá body z `PathWay`.
`AmbientNpcSpawner` vyrobí telo z katalógu postáv a pripne oboje. Agentová simulácia neskôr
nahradí riadiča, nie telo.

**Spec:** [`docs/superpowers/specs/2026-08-29-npc-vrstva-design.md`](../specs/2026-08-29-npc-vrstva-design.md)

**Odchýlka od specu:** `INpcDirector` sa nerobí. Malo by jednu implementáciu a nikto by ho
nekonzumoval — spawner aj tak volá `AddComponent<WaypointDirector>()`. Seam je v tom, že
`NpcActor` netuší, kto mu volá `GoTo`, nie v rozhraní nad riadičom.

---

## Než začneš

Kód, komentáre a commit messages **po anglicky**. `CHANGELOG.md` a `docs/` **po slovensky**.
Vetva `master`, **nikdy `git add -A`**, stageuj vypísané cesty.

Po zmene skriptu daj `Assets/Refresh` a počkaj na dokompilovanie
(`Unity_ManageEditor` → `GetState` → `IsCompiling`).

### Čo sa dá a čo sa nedá otestovať

`NpcActor` aj `WaypointDirector` visia na `NavMeshAgent`e, ktorý bez upečeného navmeshu
nefunguje, takže **EditMode test na ne napísať nejde**. Testovateľná je z toho výberová
logika, a tá sa preto vytiahne von ako `WaypointCursor` — čistý C#, žiadne Unity.

Zvyšok sa overí v play mode v `Demo.unity` a čísla z toho patria do commitu, nie do testu.

---

## Úloha 1: Assembly definitions

**Súbory:**
- Create: `Assets/_Game/Scripts/Npc/FriWorld.Npc.asmdef`
- Create: `Assets/_Game/Editor/Npc/Tests/FriWorld.Npc.Tests.asmdef`

- [ ] **Krok 1: Runtime asmdef**

```json
{
    "name": "FriWorld.Npc",
    "rootNamespace": "FriWorld.Npc",
    "references": [
        "FriWorld.Character"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Krok 2: Tests asmdef**

```json
{
    "name": "FriWorld.Npc.Tests",
    "rootNamespace": "FriWorld.Npc.Tests",
    "references": [
        "FriWorld.Npc",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Pozor:** `FriWorld.Npc` **nesmie** referencovať `Assembly-CSharp` — nedá sa to. `PathWay`
a `NavMeshCenteringUtility` v ňom dnes sedia, takže sa v úlohe 3 presunú sem.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Scripts/Npc Assets/_Game/Editor/Npc && git commit -m "chore(npc): add assembly definitions for the ambient NPC layer"
```

---

## Úloha 2: Presun `PathWay` a `NavMeshCenteringUtility`

Obe sedia v `Assembly-CSharp` a obe nová vrstva potrebuje. Asmdef predefinovanú assembly
referencovať nevie, takže musia ísť s ňou.

**Súbory:**
- Move: `Assets/_Game/Scripts/NPC/PathWay.cs` → `Assets/_Game/Scripts/Npc/PathWay.cs`
- Move: `Assets/_Game/Scripts/Navigation/NavMeshCenteringUtility.cs` → `Assets/_Game/Scripts/Npc/NavMeshCenteringUtility.cs`

- [ ] **Krok 1: Presuň oba súbory aj s `.meta`**

`.meta` musí ísť s nimi, inak Unity vygeneruje nové GUID a **každá referencia na `PathWay`
v scéne sa odtrhne** — v `Demo.unity` je päť ciest a `WanderPath` má 30 waypointov.

```bash
git mv Assets/_Game/Scripts/NPC/PathWay.cs Assets/_Game/Scripts/Npc/PathWay.cs
git mv Assets/_Game/Scripts/NPC/PathWay.cs.meta Assets/_Game/Scripts/Npc/PathWay.cs.meta
git mv Assets/_Game/Scripts/Navigation/NavMeshCenteringUtility.cs Assets/_Game/Scripts/Npc/NavMeshCenteringUtility.cs
git mv Assets/_Game/Scripts/Navigation/NavMeshCenteringUtility.cs.meta Assets/_Game/Scripts/Npc/NavMeshCenteringUtility.cs.meta
```

- [ ] **Krok 2: Nechaj ich v globálnom namespace**

Ani jeden nepridávaj do `namespace FriWorld.Npc`. Oba používa starý `WonderState`
v `Assembly-CSharp`, ktorá vidí typy z asmdefov, ale len ak sú tam, kde ich čaká.
Presun stačí; namespace by ho rozbil.

- [ ] **Krok 3: Over, že sa starý `WonderState` stále kompiluje**

`Assets/Refresh`, počkaj. V konzole nesmie byť chyba. Otvor `Demo.unity` a over, že
`WanderPath` má stále 30 waypointov a NPC prefaby nemajú missing script.

- [ ] **Krok 4: Commit**

```bash
git add -u Assets/_Game/Scripts && git commit -m "refactor(npc): move PathWay and NavMeshCenteringUtility into the NPC assembly"
```

---

## Úloha 3: `NpcActivity` a `WaypointCursor`

Dva malé kusy, oba bez Unity závislosti okrem enumu.

**Súbory:**
- Create: `Assets/_Game/Scripts/Npc/NpcActivity.cs`
- Create: `Assets/_Game/Scripts/Npc/WaypointCursor.cs`
- Test: `Assets/_Game/Editor/Npc/Tests/WaypointCursorTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Npc;
using NUnit.Framework;

namespace FriWorld.Npc.Tests
{
    public class WaypointCursorTests
    {
        [Test]
        public void SequentialWrapsAround()
        {
            var cursor = new WaypointCursor(3, randomOrder: false, new System.Random(1));
            cursor.Reset(2);

            Assert.AreEqual(0, cursor.Next());
            Assert.AreEqual(1, cursor.Next());
            Assert.AreEqual(2, cursor.Next());
            Assert.AreEqual(0, cursor.Next());
        }

        [Test]
        public void RandomStaysInRange()
        {
            var cursor = new WaypointCursor(5, randomOrder: true, new System.Random(7));

            for (int i = 0; i < 200; i++)
            {
                int index = cursor.Next();
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 5);
            }
        }

        [Test]
        public void RandomNeverRepeatsTheCurrentPoint()
        {
            // Picking the point you are standing on reads as a frozen NPC, not as a pause.
            var cursor = new WaypointCursor(4, randomOrder: true, new System.Random(3));

            int previous = cursor.Next();
            for (int i = 0; i < 200; i++)
            {
                int index = cursor.Next();
                Assert.AreNotEqual(previous, index, "step " + i);
                previous = index;
            }
        }

        [Test]
        public void OnePointIsAlwaysThatPoint()
        {
            // With nowhere else to go, repeating is the only option and must not loop forever.
            var cursor = new WaypointCursor(1, randomOrder: true, new System.Random(3));

            Assert.AreEqual(0, cursor.Next());
            Assert.AreEqual(0, cursor.Next());
        }

        [Test]
        public void NoPointsGivesMinusOne()
        {
            Assert.AreEqual(-1, new WaypointCursor(0, true, new System.Random(1)).Next());
        }

        [Test]
        public void TheSameSeedWalksTheSameRoute()
        {
            var a = new WaypointCursor(6, true, new System.Random(99));
            var b = new WaypointCursor(6, true, new System.Random(99));

            for (int i = 0; i < 50; i++) Assert.AreEqual(a.Next(), b.Next(), "step " + i);
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `WaypointCursor` neexistuje.

- [ ] **Krok 3: Napíš `NpcActivity.cs`**

```csharp
namespace FriWorld.Npc
{
    /// <summary>
    /// What an NPC is doing, published by the body and read by nobody yet.
    ///
    /// This is the seam between behaviour and animation. Behaviour must never call
    /// Animator.SetBool — the old WonderState does, which is exactly why it throws on a
    /// generated character that has no animator. The body says what it is doing; a view
    /// component will later turn that into animator parameters, and a character with no
    /// animator keeps working.
    ///
    /// Two values today. The list grows with the animations, not with the behaviour.
    /// </summary>
    public enum NpcActivity
    {
        Idle,
        Walking,
    }
}
```

- [ ] **Krok 4: Napíš `WaypointCursor.cs`**

```csharp
namespace FriWorld.Npc
{
    /// <summary>
    /// Which waypoint comes next. Pure C# so the choice can be tested without a navmesh.
    /// </summary>
    public sealed class WaypointCursor
    {
        readonly int count;
        readonly bool randomOrder;
        readonly System.Random random;

        int current = -1;

        public WaypointCursor(int count, bool randomOrder, System.Random random)
        {
            this.count = count;
            this.randomOrder = randomOrder;
            this.random = random;
        }

        /// <summary>Start from a known point without counting it as a visit.</summary>
        public void Reset(int index) => current = index;

        /// <returns>The next waypoint index, or -1 when there are none.</returns>
        public int Next()
        {
            if (count <= 0) return -1;
            if (count == 1) return current = 0;

            if (!randomOrder) return current = (current + 1) % count;

            // Draw from the other points rather than rejecting repeats in a loop: an NPC sent
            // to the point it is already standing on reads as frozen, not as pausing.
            int step = random.Next(1, count);
            return current = current < 0 ? random.Next(0, count) : (current + step) % count;
        }
    }
}
```

- [ ] **Krok 5: Spusti testy, over, že prechádzajú**

Očakávané: 6 testov PASS.

- [ ] **Krok 6: Commit**

```bash
git add Assets/_Game/Scripts/Npc/NpcActivity.cs Assets/_Game/Scripts/Npc/NpcActivity.cs.meta Assets/_Game/Scripts/Npc/WaypointCursor.cs Assets/_Game/Scripts/Npc/WaypointCursor.cs.meta Assets/_Game/Editor/Npc/Tests/WaypointCursorTests.cs Assets/_Game/Editor/Npc/Tests/WaypointCursorTests.cs.meta && git commit -m "feat(npc): add the activity seam and the waypoint cursor"
```

---

## Úloha 4: `NpcActor`

**Súbory:**
- Create: `Assets/_Game/Scripts/Npc/NpcActor.cs`

- [ ] **Krok 1: Napíš implementáciu**

```csharp
using UnityEngine;
using UnityEngine.AI;

namespace FriWorld.Npc
{
    /// <summary>
    /// An NPC body. It walks where it is told and decides nothing.
    ///
    /// That passivity is the whole point of this class. Today a WaypointDirector calls GoTo;
    /// later the agent simulation will, from a timetable, for whichever NPCs are near enough
    /// to be worth drawing. Because the body has no opinion about where it goes, that is a
    /// change of caller rather than a rewrite — which is exactly what the old Npc and the
    /// short-lived NpcWander both got wrong by ticking themselves.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcActor : MonoBehaviour
    {
        [Tooltip("How close counts as arrived, on top of the agent's own stopping distance.")]
        [SerializeField] float arriveDistance = 1.5f;

        // Keeping an NPC off the wall: the lateral nudge from the old WonderState, the one part
        // of that class worth carrying over. Without it they grind along corridor walls.
        const float EdgeBiasStartDistance = 0.8f;
        const float EdgeBiasProbeOffset = 0.35f;
        const float EdgeBiasMaxLateralSpeed = 0.65f;
        const float EdgeBiasSmoothing = 6f;
        const float EdgeBiasDeadZone = 0.04f;
        const float EdgeBiasMinVelocity = 0.05f;

        NavMeshAgent agent;
        float smoothedLateralBias;
        Vector3 lastMoveForward = Vector3.forward;

        public NpcActivity Activity { get; private set; } = NpcActivity.Idle;

        /// <summary>False until the agent is actually on the navmesh — a body spawned off it
        /// cannot be told to go anywhere yet.</summary>
        public bool IsReady => agent != null && agent.isOnNavMesh;

        /// <summary>
        /// Nothing left to walk to. A path that is not complete counts as arrived: the point is
        /// unreachable, and an incomplete path leaves remainingDistance at Infinity, so waiting
        /// for the distance to come down is how an NPC freezes on the spot for good.
        /// </summary>
        public bool HasArrived
        {
            get
            {
                if (!IsReady || agent.pathPending) return false;
                if (!agent.hasPath) return true;
                if (agent.pathStatus != NavMeshPathStatus.PathComplete) return true;
                return agent.remainingDistance <= agent.stoppingDistance + arriveDistance;
            }
        }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public void GoTo(Vector3 destination)
        {
            if (!IsReady) return;
            agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!IsReady) return;
            agent.ResetPath();
        }

        void Update()
        {
            if (!IsReady) return;

            Activity = agent.velocity.sqrMagnitude > EdgeBiasMinVelocity * EdgeBiasMinVelocity
                ? NpcActivity.Walking
                : NpcActivity.Idle;

            ApplyEdgeCenteringBias();
        }

        void ApplyEdgeCenteringBias()
        {
            if (!agent.hasPath || agent.pathPending) return;
            if (agent.velocity.sqrMagnitude < EdgeBiasMinVelocity * EdgeBiasMinVelocity) return;

            Vector3 forward = agent.desiredVelocity;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                lastMoveForward = forward;
            }
            else forward = lastMoveForward;

            if (!NavMeshCenteringUtility.TryCalculateNormalizedEdgeBias(
                    transform.position, forward,
                    EdgeBiasProbeOffset, EdgeBiasStartDistance, EdgeBiasDeadZone,
                    out float targetBias, out Vector3 right))
                return;

            float blend = 1f - Mathf.Exp(-EdgeBiasSmoothing * Time.deltaTime);
            smoothedLateralBias = Mathf.Lerp(smoothedLateralBias, targetBias, blend);

            agent.Move(right * (smoothedLateralBias * EdgeBiasMaxLateralSpeed * Time.deltaTime));
        }
    }
}
```

- [ ] **Krok 2: Over, že sa kompiluje**

`Assets/Refresh`, počkaj na dokompilovanie, konzola bez chýb.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Scripts/Npc/NpcActor.cs Assets/_Game/Scripts/Npc/NpcActor.cs.meta && git commit -m "feat(npc): add the passive NPC body"
```

---

## Úloha 5: `WaypointDirector`

**Súbory:**
- Create: `Assets/_Game/Scripts/Npc/WaypointDirector.cs`

- [ ] **Krok 1: Napíš implementáciu**

```csharp
using UnityEngine;

namespace FriWorld.Npc
{
    /// <summary>
    /// Walks an NpcActor between the waypoints of a PathWay.
    ///
    /// The only thing driving a body today. When the agent simulation arrives it takes this
    /// role over — reading a timetable instead of a list of points — and NpcActor does not
    /// change, because it never knew who was calling it.
    /// </summary>
    [RequireComponent(typeof(NpcActor))]
    public sealed class WaypointDirector : MonoBehaviour
    {
        [SerializeField] PathWay path;
        [SerializeField] bool randomOrder = true;

        [Tooltip("Seconds to linger on arrival before heading somewhere else.")]
        [SerializeField] float waitOnArrival = 3f;

        NpcActor actor;
        WaypointCursor cursor;
        float waitTimer;
        bool waiting;

        /// <summary>Called by the spawner before Start, so a run stays reproducible.</summary>
        public void Configure(PathWay wanderPath, int seed)
        {
            path = wanderPath;
            cursor = new WaypointCursor(Count, randomOrder, new System.Random(seed));
        }

        int Count => path != null && path.Waypoints != null ? path.Waypoints.Count : 0;

        void Awake()
        {
            actor = GetComponent<NpcActor>();
            if (cursor == null) cursor = new WaypointCursor(Count, randomOrder, new System.Random(GetInstanceID()));
        }

        void Update()
        {
            if (Count == 0 || !actor.IsReady) return;
            if (!actor.HasArrived) { waiting = false; return; }

            if (!waiting)
            {
                waiting = true;
                waitTimer = 0f;
            }

            waitTimer += Time.deltaTime;
            if (waitTimer < waitOnArrival) return;

            int index = cursor.Next();
            if (index < 0) return;

            var waypoint = path.Waypoints[index];
            if (waypoint != null) actor.GoTo(waypoint.position);

            waiting = false;
        }
    }
}
```

- [ ] **Krok 2: Commit**

```bash
git add Assets/_Game/Scripts/Npc/WaypointDirector.cs Assets/_Game/Scripts/Npc/WaypointDirector.cs.meta && git commit -m "feat(npc): drive a body between waypoints"
```

---

## Úloha 6: `AmbientNpcSpawner`

**Súbory:**
- Create: `Assets/_Game/Scripts/Npc/AmbientNpcSpawner.cs`

Prevezme z `CharacterNpcSpawner` všetko vrátane oboch opráv z review — nepodmienený dekrement
`activeNpcs` a časový limit na cestu domov.

- [ ] **Krok 1: Napíš implementáciu**

Skopíruj `Assets/_Game/Scripts/NPC/CharacterNpcSpawner.cs` do nového súboru a zmeň:

- namespace `FriWorld.Npc`, trieda `AmbientNpcSpawner`
- `using FriWorld.Character;`
- namiesto `npc.AddComponent<NpcWander>().Configure(wanderPath, seed);`:

```csharp
        npc.AddComponent<NpcActor>();
        npc.AddComponent<WaypointDirector>().Configure(wanderPath, seed);
```

- v `HandleNpcLifetime` namiesto `npc.GetComponent<NpcWander>()`:

```csharp
            var director = npc.GetComponent<WaypointDirector>();
            if (director != null) director.enabled = false;
```

Poradie `AddComponent` je dôležité: `WaypointDirector` má `[RequireComponent(typeof(NpcActor))]`,
takže aktér musí byť skôr — inak si ho Unity pridá samo a `Awake` prebehne v inom poradí.

- [ ] **Krok 2: Commit**

```bash
git add Assets/_Game/Scripts/Npc/AmbientNpcSpawner.cs Assets/_Game/Scripts/Npc/AmbientNpcSpawner.cs.meta && git commit -m "feat(npc): spawn ambient NPCs onto the new body"
```

---

## Úloha 7: Zapojiť do Demo scény a overiť

- [ ] **Krok 1: Pridaj `AmbientNpcSpawner` k obom objektom v `Demo.unity`**

Vedľa `CharacterNpcSpawner`, s tými istými hodnotami a tou istou `WanderPath`.
`CharacterNpcSpawner` **vypni, nemaž** — ide preč až keď nový odbehne bez prekvapení.

- [ ] **Krok 2: Play mode, over**

Očakávané: NPC nabehnú, všetci na navmeshi, všetci s cestou, všetci sa hýbu rýchlosťou
blízkou nastavenej, žiadny zaseknutý, konzola bez chýb z nového kódu. Zapíš čísla do commitu.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Scenes/Demo.unity && git commit -m "feat(demo): run the ambient spawner beside the old one"
```

---

## Úloha 8: Dokumentácia

- [ ] **Krok 1: Riadok do `CHANGELOG.md`** pod `### Changed`

```markdown
- NPC telo je pasívne: `NpcActor` vie `GoTo` a `Stop` a nerozhoduje, kam ide — to mu povie
  `WaypointDirector`. Správanie je rovnaké ako predtým, ale agentová simulácia neskôr vymení
  riadiča namiesto toho, aby telo prepisovala. Pribudol aj `NpcActivity`, seam medzi
  správaním a animátorom, ktorý zatiaľ nikto nečíta.
```

- [ ] **Krok 2: Doplň `docs/2026-08-28-npc-skripty-na-prerobenie.md`**

Tabuľka „Čo beží dnes" má tri riadky, ktoré prestali platiť — `CharacterNpcSpawner`
a `NpcWander` nahradili `AmbientNpcSpawner`, `NpcActor` a `WaypointDirector`.

- [ ] **Krok 3: Commit**

```bash
git add CHANGELOG.md docs/2026-08-28-npc-skripty-na-prerobenie.md && git commit -m "docs(npc): record the passive body"
```

---

## Čo tento plán zámerne nerieši

| vec | kam patrí |
|---|---|
| Rozvrhy, cvičenia, obedy, rezervácia stoličiek | agentová simulácia |
| Animácie a ich stavový stroj | seam áno, graf nie |
| Dialóg a questy na novom type | až po prepise dialógu |
| Zmazanie `NpcWander`, `CharacterNpcSpawner`, `NPCSpawner`, `student_*.fbx` | až keď nový beží overene |
| `NavMeshLink` na dvere a schody | agentová simulácia |
