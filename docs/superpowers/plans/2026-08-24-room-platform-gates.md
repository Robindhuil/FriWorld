# Room Platform Gates Implementation Plan

> **DONE — 2026-08-24. Do not execute this plan.** Everything in it shipped; the unticked
> checkboxes below are the plan as written, not work outstanding. Kept because the reasoning and
> the measured numbers are the record of how the gates were built.
>
> Where the work diverged from the plan:
>
> | plan | shipped |
> |---|---|
> | four menu items under Tools > Feature Flags | one `Room Gates` window with a branch switch, under `FriWorld` |
> | Task 1 tests via a plan-supplied file | `RoomGateScopeTests`, 11 tests, written against the real API |
> | generators keep reading `Selection` | all three write the prefab asset through `PrefabTarget` |
> | Task 8 cleanup only | also the `Routine` menu, the menu reorganisation and the Sfx routing fix |
>
> Current state: 47 EditMode tests pass, `Report Room Gates` converges to zero on both branches.
> Commits `7aa9bbd` through `43e7971`.

**Goal:** Generate `PlatformGate` and `ComponentGate` into the FriBuilding prefab asset from the per-area decisions in `RoomPlatforms.json`.

**Architecture:** `RoomPlatforms.json` holds one of `all` / `desktopOnly` / `webOnly`, or nothing at all when undecided, keyed on the full area container name. `RoomGateScope` opens `FriBuilding.prefab` in an isolated preview scene and pairs area containers with their decision. Two independent appliers reconcile the components: one for `Objects` (whole-container `PlatformGate`), one for `fri_building` (per-door `ComponentGate`). Writing into the prefab asset rather than the scene instance is the fix for the gates that vanished.

**Tech Stack:** Unity 6000.4.11f1, C#, Newtonsoft.Json, Unity Test Framework (NUnit, EditMode).

**Spec:** `docs/superpowers/specs/2026-08-24-room-platform-gates-design.md`

---

## Already done — commit `7aa9bbd`

The data layer and the sync landed as one change, and the prefix collector was fixed at the same time. Do not redo any of this:

| what | where |
|---|---|
| `RoomPlatforms` — load / save / `Find` / `PlatformOf` / `Reconcile`, undecided floats to top, `platform` omitted when unset | `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs` |
| `RoomGateScope` — `PrefabPath`, `Open` / `Close` / `SaveAndClose`, `AreaNames` | `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs` |
| prefix collector no longer strips trailing `_<int>` | `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs` |
| second guard: withhold a prefix that starts a registered type name | `ObjectRegistryMenu.EatsARegisteredType` |
| `Sync Room Platforms` menu item, also run at the end of `Add Prefixes From Selection` | `ObjectRegistryMenu.SyncRoomPlatformsFile` |
| 12 unit tests | `Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs` |

Verified on FriBuilding: 63 prefixes added (325 total), 9 withheld, 301 areas written to `RoomPlatforms.json` all undecided, 8123 mesh objects still all resolving to a decided type, 36 EditMode tests passing.

**Measured facts the remaining tasks rely on:**

```
areas in Objects            246        areas with their own MeshRenderer     0
areas in fri_building       301        areas only in Objects                 0
areas in the union          301        areas only in fri_building           55
PlatformGate in the asset   144        ComponentGate in the asset            0
Door components             283        names containing "door"             808
```

---

## Before you start

### Language

Code, comments and commit messages in English. `CHANGELOG.md` and `docs/decisions/` in Slovak — Task 8 gives the exact text.

### Assembly boundary — this drives the file layout

`Assets/_Game/Editor/ObjectRegistry/` has its own asmdef, `FriWorld.ObjectRegistry.Editor`, with `"references": []`. It can use `UnityEngine` and `UnityEditor`, but it **cannot see** `PlatformGate`, `ComponentGate` or `Door` — those live in `Assembly-CSharp`.

`Assets/_Game/Editor/` and its subfolders compile into `Assembly-CSharp-Editor`, which auto-references both `Assembly-CSharp` and the registry asmdef, so it sees everything.

That is why `RoomGateScope` sits in the registry asmdef despite touching Unity: `ObjectRegistryMenu` needs it for the sync and could not reach across the boundary otherwise.

### Running the tests

Unity MCP tools are deferred — load them first:

```
ToolSearch  select:mcp__unity-mcp__Unity_RunCommand,mcp__unity-mcp__Unity_ManageEditor,mcp__unity-mcp__Unity_ReadConsole
```

After writing a script to disk, refresh and wait:

```csharp
// Unity_RunCommand — the class MUST be named CommandScript and MUST be internal
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        result.Log("refresh requested");
    }
}
```

Then poll `Unity_ManageEditor` action `GetState` until `IsCompiling` is false, and check `Unity_ReadConsole` with `Types: ["Error"]` for compile errors.

To run the EditMode tests:

```csharp
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Logger());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "FriWorld.ObjectRegistry.Tests" },
        }));
        result.Log("test run started");
    }

    class Logger : ICallbacks
    {
        public void RunStarted(ITestAdaptor t) { }
        public void TestStarted(ITestAdaptor t) { }
        public void TestFinished(ITestResultAdaptor r)
        {
            if (r.HasChildren) return;
            if (r.TestStatus != TestStatus.Passed)
                Debug.Log("[TESTS] " + r.TestStatus + "  " + r.FullName + "  — " + r.Message);
        }
        public void RunFinished(ITestResultAdaptor r)
            => Debug.Log("[TESTS] done: " + r.PassCount + " passed, " + r.FailCount + " failed, "
                       + r.SkipCount + " skipped");
    }
}
```

The run is asynchronous. Read the outcome afterwards with `Unity_ReadConsole`, `FilterText: "TESTS"`. Never claim tests passed without seeing that line.

To run a menu item from MCP, set the selection and execute it by path:

```csharp
Selection.activeGameObject = GameObject.Find("FriBuilding");
EditorApplication.ExecuteMenuItem("Tools/Object Registry/Report On Selection");
```

`Unity_ReadConsole` with a `FilterText` and `IncludeStacktrace: false` keeps the output readable.

### Committing — read this twice

The working tree carries a large amount of the user's unfinished work, including **247k changed lines in `Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab`** and uncommitted furniture types in `Assets/_Game/Editor/ObjectTypes.json`.

- **NEVER `git add -A` or `git add .`** Stage the exact paths each step lists.
- **Never stage `ObjectTypes.json`.** It is the user's work in progress.
- **Task 7 writes the prefab.** Do not commit it without asking the user first — their changes are in the same file.

---

## Task 1: Extend RoomGateScope with area matching

`RoomGateScope` can currently list area names. The appliers also need the `Transform` behind each area, its decision, and a nesting test.

**Files:**
- Modify: `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs` (create)

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.ObjectRegistry.Tests
{
    public class RoomGateScopeTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        /// <summary>Builds "root/a/b/c"-style paths and returns the root.</summary>
        static GameObject Tree(params string[] paths)
        {
            var made = new GameObject("root");
            foreach (var path in paths)
            {
                Transform parent = made.transform;
                foreach (var name in path.Split('/'))
                {
                    var existing = parent.Find(name);
                    if (existing == null)
                    {
                        var go = new GameObject(name);
                        go.transform.SetParent(parent);
                        existing = go.transform;
                    }
                    parent = existing;
                }
            }
            return made;
        }

        [Test]
        public void MatchFindsApprovedContainersAtAnyDepth()
        {
            root = Tree("Objects/rc/rc000_buffet/lamp",
                        "Objects/ra/ra0/ra001/chair");
            var prefixes = new List<string> { "rc000_buffet", "ra001" };

            var matches = RoomGateScope.Match(root.transform.Find("Objects"), prefixes, null);

            Assert.AreEqual(2, matches.Count,
                "areas sit at different depths — rc holds rooms directly, ra has a floor between");
            CollectionAssert.Contains(Names(matches), "rc000_buffet");
            CollectionAssert.Contains(Names(matches), "ra001");
        }

        [Test]
        public void MatchIgnoresContainersThatAreNotApproved()
        {
            root = Tree("Objects/ra001/ra001_lamp/bulb");
            var prefixes = new List<string> { "ra001" };

            var matches = RoomGateScope.Match(root.transform.Find("Objects"), prefixes, null);

            CollectionAssert.AreEqual(new[] { "ra001" }, Names(matches),
                "a lamp group inside a room is not a room");
        }

        [Test]
        public void MatchCarriesThePlatformDecision()
        {
            root = Tree("Objects/ra001/chair", "Objects/ra002/chair");
            var prefixes = new List<string> { "ra001", "ra002" };
            var platforms = RoomPlatforms.FromJson(
                @"{ ""rooms"": [ { ""room"": ""ra001"", ""platform"": ""desktopOnly"" } ] }");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"), prefixes, platforms);

            foreach (var m in matches)
            {
                if (m.area == "ra001") Assert.AreEqual(RoomPlatforms.DesktopOnly, m.platform);
                if (m.area == "ra002") Assert.IsNull(m.platform, "undecided must stay null");
            }
        }

        [Test]
        public void ContainsAnotherAreaSpotsAnAreaInsideAnArea()
        {
            root = Tree("rb_basement/rb_basement_room_1/wall");
            var prefixes = new List<string> { "rb_basement", "rb_basement_room_1" };

            var matches = RoomGateScope.Match(root.transform, prefixes, null);
            var outer = Find(matches, "rb_basement");
            var inner = Find(matches, "rb_basement_room_1");

            Assert.IsTrue(RoomGateScope.ContainsAnotherArea(outer, matches),
                "gating rb_basement would strip the inner rooms and void their decisions");
            Assert.IsFalse(RoomGateScope.ContainsAnotherArea(inner, matches));
        }

        static List<string> Names(List<AreaMatch> matches)
        {
            var names = new List<string>();
            foreach (var m in matches) names.Add(m.area);
            return names;
        }

        static AreaMatch Find(List<AreaMatch> matches, string area)
        {
            foreach (var m in matches) if (m.area == area) return m;
            Assert.Fail("no area named " + area);
            return default;
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Expected: compilation errors — `The type or namespace name 'AreaMatch' could not be found` and `'RoomGateScope' does not contain a definition for 'Match'`.

- [ ] **Step 3: Rewrite RoomGateScope**

Replace the whole of `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs` with:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One area container found in the prefab, paired with the decision that applies.</summary>
    public struct AreaMatch
    {
        public Transform transform;

        /// <summary>Full container name, trimmed. The key into RoomPlatforms.json.</summary>
        public string area;

        /// <summary>null when the area is missing from the file or still undecided.</summary>
        public string platform;
    }

    /// <summary>
    /// Reads the FriBuilding prefab asset.
    ///
    /// Everything the gate tooling writes goes into the PREFAB ASSET, never onto a scene
    /// instance. Components added to the instance are prefab overrides, and one revert or one
    /// .blend reimport wipes them — that is how the door gates were lost before this existed,
    /// while the PlatformGates that happened to sit in the asset survived.
    ///
    /// Lives in the registry assembly rather than beside the appliers because the sync menu
    /// needs it too, and Assembly-CSharp-Editor can see this assembly but not the reverse. It
    /// therefore knows nothing about PlatformGate, ComponentGate or Door.
    /// </summary>
    public static class RoomGateScope
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab";
        public const string ObjectsBranch = "Objects";
        public const string BuildingBranch = "fri_building";

        /// <summary>Opens the prefab in an isolated preview scene. Always pair with Close.</summary>
        public static GameObject Open() => PrefabUtility.LoadPrefabContents(PrefabPath);

        public static void Close(GameObject contents) => PrefabUtility.UnloadPrefabContents(contents);

        public static void SaveAndClose(GameObject contents)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        public static Transform Branch(GameObject prefabRoot, string branchName)
            => prefabRoot == null ? null : prefabRoot.transform.Find(branchName);

        /// <summary>
        /// The area containers in a subtree, each with its decision.
        ///
        /// An area is a container whose name IS an approved prefix — exact match, no stripping.
        /// The scanner proposes container names whole, so ra100_corridor_1 and ra100_corridor_2
        /// are two prefixes and two areas that decide separately. The approved list is also what
        /// keeps furniture out: ra102_lamp is a container, but nobody approved it as a prefix.
        /// </summary>
        public static List<AreaMatch> Match(Transform branchRoot, IReadOnlyList<string> approvedPrefixes,
                                            RoomPlatforms platforms)
        {
            var matches = new List<AreaMatch>();
            if (branchRoot == null || approvedPrefixes == null) return matches;

            var approved = new HashSet<string>(approvedPrefixes, StringComparer.OrdinalIgnoreCase);
            Walk(branchRoot, approved, platforms, matches);
            return matches;
        }

        /// <summary>Distinct area names in the whole prefab. This is what Reconcile consumes.</summary>
        public static List<string> AreaNames(GameObject prefabRoot, IReadOnlyList<string> approvedPrefixes)
        {
            var names = new List<string>();
            if (prefabRoot == null) return names;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var match in Match(prefabRoot.transform, approvedPrefixes, null))
                if (seen.Add(match.area)) names.Add(match.area);

            names.Sort(string.CompareOrdinal);
            return names;
        }

        /// <summary>
        /// True when another area sits inside this one. Gating such a container would strip its
        /// inner areas too and void their own decisions without saying so, which is why the
        /// appliers refuse. Today this catches rb, outside, terrace, rb_basement and
        /// rc000_cafeteria — every one of them a container of other areas.
        /// </summary>
        public static bool ContainsAnotherArea(AreaMatch outer, List<AreaMatch> all)
        {
            foreach (var other in all)
            {
                if (other.transform == null || other.transform == outer.transform) continue;
                if (other.transform.IsChildOf(outer.transform)) return true;
            }
            return false;
        }

        static void Walk(Transform t, HashSet<string> approved, RoomPlatforms platforms,
                         List<AreaMatch> acc)
        {
            foreach (Transform child in t)
            {
                string name = child.name.Trim();
                if (child.childCount > 0 && approved.Contains(name))
                {
                    acc.Add(new AreaMatch
                    {
                        transform = child,
                        area = name,
                        platform = platforms == null ? null : platforms.PlatformOf(name),
                    });
                }

                // Keep descending. Areas sit at different depths: Objects/rc holds rooms
                // directly, Objects/ra puts a floor level in between. An area inside an area is
                // legitimate too — ContainsAnotherArea is what stops it being gated.
                Walk(child, approved, platforms, acc);
            }
        }
    }
}
```

Note the behaviour change in `AreaNames`: it now dedupes after `Match` rather than during the walk, because `Match` must return every occurrence — the same name can appear in both branches and each one needs its own `Transform`.

- [ ] **Step 4: Run the tests to verify they pass**

Expected: `[TESTS] done: 40 passed, 0 failed, 0 skipped`.

- [ ] **Step 5: Confirm the sync still produces the same file**

Run `FriWorld > Registry > Sync Room Platforms`.

Expected: `[RoomPlatforms] 301 areas in Assets/_Game/Editor/RoomPlatforms.json` followed by `already in sync`. If the count moved, `AreaNames` regressed — fix it before committing.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs.meta && git commit -m "feat(registry): pair area containers with their platform decision"
```

---

## Task 2: Migrate the hand-written draft

`Assets/_Game/Editor/Platforms.json` holds 262 prefix-level decisions written by hand: 158 `desktopOnly`, 104 `all`. `RoomPlatforms.json` holds 301 areas, all undecided. Each area takes the value of its name with the trailing instance number removed, so `ra100_corridor: all` decides three corridors and `rb_basement_room: desktopOnly` decides fourteen rooms.

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs`
- Modify: `Assets/_Game/Editor/RoomPlatforms.json` (written by running it)

- [ ] **Step 1: Write the one-off migration**

Create `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// One-off: fills RoomPlatforms.json from the hand-written prefix-level draft. Every area
    /// takes the value of its name with the instance number removed, so one draft row can decide
    /// several areas — which is the point, since the draft could not express them separately.
    ///
    /// Delete this file and the draft once it has run. It is committed only so the conversion is
    /// reproducible and reviewable in the diff.
    /// </summary>
    public static class MigratePlatformsDraft
    {
        const string DraftPath = "Assets/_Game/Editor/Platforms.json";

        [MenuItem("Tools/Object Registry/Migrate Platforms Draft")]
        static void Migrate()
        {
            if (!File.Exists(DraftPath))
            {
                Debug.LogError("[MigrateDraft] " + DraftPath + " not found — already migrated?");
                return;
            }

            // The draft is not valid JSON: it was typed by hand to capture intent, in the shape
            // "name", "platform": "value", and at least one row is missing its comma. One regex
            // over a known one-time input beats a tolerant parser nothing else will ever use.
            var draft = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(File.ReadAllText(DraftPath),
                         "\"([^\"]+)\"\\s*,?\\s*\"platform\"\\s*:\\s*\"([^\"]+)\""))
                draft[m.Groups[1].Value] = m.Groups[2].Value;

            var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
            var platforms = RoomPlatforms.Load(ObjectRegistryMenu.RoomPlatformsPath);

            List<string> areas;
            var contents = RoomGateScope.Open();
            try
            {
                areas = RoomGateScope.AreaNames(contents, prefixes);
            }
            finally
            {
                RoomGateScope.Close(contents);
            }

            platforms.Reconcile(areas);

            int filled = 0;
            var unmatched = new List<string>();
            foreach (var area in areas)
            {
                var entry = platforms.Find(area);
                if (entry == null || entry.IsDecided) continue;

                string key = StripInstanceNumber(area);
                if (draft.TryGetValue(key, out var value) && RoomPlatforms.IsValidPlatform(value))
                {
                    entry.platform = value;
                    filled++;
                }
                else unmatched.Add(area);
            }

            platforms.Save(ObjectRegistryMenu.RoomPlatformsPath);
            AssetDatabase.Refresh();

            var sb = new StringBuilder();
            sb.AppendLine("[MigrateDraft] draft rows " + draft.Count + " → decided " + filled
                        + " of " + platforms.rooms.Count + " areas");
            if (unmatched.Count > 0)
            {
                sb.AppendLine("  no draft value, left undecided (" + unmatched.Count + "):");
                foreach (var a in unmatched) sb.AppendLine("    " + a);
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Removes a trailing "_&lt;digits&gt;". Local to the migration on purpose: the draft is
        /// the only thing left in the project that is keyed without the instance number, and
        /// this helper dies with it.
        /// </summary>
        static string StripInstanceNumber(string s)
        {
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;

            string tail = s.Substring(underscore + 1);
            if (tail.Length == 0) return s;
            foreach (char c in tail) if (!char.IsDigit(c)) return s;
            return s.Substring(0, underscore);
        }
    }
}
```

- [ ] **Step 2: Run the migration**

Refresh, wait for `IsCompiling` to be false, then run `FriWorld > Registry > Migrate Platforms Draft`.

Expected: `[MigrateDraft] draft rows 262 → decided 301 of 301 areas`, with no `no draft value` section.

If areas are listed as unmatched, their stripped name is absent from the draft. Add those rows to `Platforms.json` by hand and run again — do not leave them undecided, because Task 3 and Task 4 will then skip them silently.

- [ ] **Step 3: Check the result**

```bash
python -c "import json; r=json.load(open('Assets/_Game/Editor/RoomPlatforms.json',encoding='utf-8'))['rooms']; print('rooms', len(r)); print('decided', sum(1 for e in r if 'platform' in e)); print('desktopOnly', sum(1 for e in r if e.get('platform')=='desktopOnly')); print('all', sum(1 for e in r if e.get('platform')=='all'))"
```

Expected: `rooms 301`, `decided 301`, and `desktopOnly + all == 301`. `desktopOnly` lands in the 170–200 range: the draft's 158 rows expand across their instances, `rb_basement_room` alone contributing 14.

- [ ] **Step 4: Spot-check the expansion**

```bash
grep -A1 '"room": "rb_basement_room_' Assets/_Game/Editor/RoomPlatforms.json | head -20
```

Expected: fourteen separate `rb_basement_room_N` rows, each `"platform": "desktopOnly"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs.meta Assets/_Game/Editor/RoomPlatforms.json && git commit -m "feat(registry): decide every area from the platform draft"
```

---

## Task 3: The Objects branch applier

**Files:**
- Create: `Assets/_Game/Editor/FeatureFlags/ObjectsPlatformGates.cs`

- [ ] **Step 1: Write the implementation**

Create `Assets/_Game/Editor/FeatureFlags/ObjectsPlatformGates.cs`:

```csharp
using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Brings the PlatformGate components under FriBuilding/Objects in line with
    /// RoomPlatforms.json.
    ///
    /// Reconciles rather than appends: it adds what is missing, retargets what is wrong and
    /// removes what the data says should not be there. That last part is why "all" and "no value
    /// at all" have to be different — only an explicit "all" gives it permission to delete.
    /// </summary>
    public static class ObjectsPlatformGates
    {
        public class Result
        {
            public int added, retargeted, removed, unchanged;

            /// <summary>Every area matched in this branch, whatever its decision.</summary>
            public readonly List<string> areasPresent = new List<string>();

            public readonly List<string> undecided = new List<string>();
            public readonly List<string> nested = new List<string>();
            public readonly List<string> badValues = new List<string>();
            public readonly List<string> orphanGates = new List<string>();
        }

        public static Result Apply(GameObject prefabRoot, IReadOnlyList<string> prefixes,
                                   RoomPlatforms platforms)
        {
            var result = new Result();
            var branch = RoomGateScope.Branch(prefabRoot, RoomGateScope.ObjectsBranch);
            if (branch == null) return result;

            var areas = RoomGateScope.Match(branch, prefixes, platforms);

            var areaTransforms = new HashSet<Transform>();
            foreach (var area in areas)
            {
                areaTransforms.Add(area.transform);
                result.areasPresent.Add(area.area);
            }

            foreach (var area in areas)
            {
                if (area.platform == null) { result.undecided.Add(area.area); continue; }

                if (!RoomPlatforms.IsValidPlatform(area.platform))
                {
                    result.badValues.Add(area.area + " = \"" + area.platform + "\"");
                    continue;
                }

                var existing = area.transform.GetComponent<PlatformGate>();

                // "all" is a licence to delete, so it runs before the nesting guard: a stale gate
                // on a container of areas still has to come off.
                if (area.platform == RoomPlatforms.All)
                {
                    if (existing == null) result.unchanged++;
                    else { Object.DestroyImmediate(existing); result.removed++; }
                    continue;
                }

                // Gating a container of areas would strip them too and void their own decisions
                // without saying so.
                if (RoomGateScope.ContainsAnotherArea(area, areas))
                {
                    result.nested.Add(area.area);
                    continue;
                }

                var target = area.platform == RoomPlatforms.WebOnly
                    ? PlatformGate.Target.WebOnly
                    : PlatformGate.Target.DesktopOnly;

                if (existing == null)
                {
                    var gate = area.transform.gameObject.AddComponent<PlatformGate>();
                    gate.target = target;
                    result.added++;
                }
                else if (existing.target != target)
                {
                    existing.target = target;
                    result.retargeted++;
                }
                else result.unchanged++;
            }

            // A gate somewhere that is not an area is somebody's hand-made exception. Report it,
            // do not delete it — removing another person's setup is not this tool's job.
            foreach (var gate in branch.GetComponentsInChildren<PlatformGate>(true))
                if (!areaTransforms.Contains(gate.transform))
                    result.orphanGates.Add(RegistryScanner.PathOf(gate.transform));

            return result;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Refresh, wait for `IsCompiling` to be false, read the console with `Types: ["Error"]`.

Expected: no errors. There is no menu item yet — Task 6 wires it up.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags Assets/_Game/Editor/FeatureFlags.meta && git commit -m "feat(featureflags): reconcile Objects platform gates from the registry"
```

---

## Task 4: The door branch applier

**Files:**
- Create: `Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs`

- [ ] **Step 1: Write the implementation**

Create `Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs`:

```csharp
using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Brings the ComponentGate components on doors under FriBuilding/fri_building in line with
    /// RoomPlatforms.json.
    ///
    /// This branch never strips whole objects. The same container holds the room's walls,
    /// ceiling and windows, so removing it would leave a hole in the building. It removes only
    /// the door's behaviour — the Door script, its Animator and its AudioSource — and moves the
    /// object to Obstacle so the interaction raycast stops seeing it. The door stays visible and
    /// stops opening.
    ///
    /// Doors come from the object type registry (script == "Door"), never from looking for
    /// "door" in the name: 808 names contain it but only 283 are doors, the rest being
    /// door_frame, door_frame_&lt;int&gt;_glass and doorstep.
    ///
    /// Why a layer change and not a trigger collider: PlayerInteract raycasts with
    /// QueryTriggerInteraction.Ignore, so turning the door into a trigger would break interaction
    /// on every platform rather than only on web.
    /// </summary>
    public static class DoorComponentGates
    {
        public const string DoorScriptName = "Door";
        public const string ObstacleLayerName = "Obstacle";

        public class Result
        {
            public int added, reconfigured, removed, unchanged;

            /// <summary>Areas that own at least one door, whatever their decision.</summary>
            public readonly List<string> areasWithDoors = new List<string>();

            public readonly List<string> undecided = new List<string>();
            public readonly List<string> doorsWithNothingToStrip = new List<string>();
        }

        public static Result Apply(GameObject prefabRoot, IReadOnlyList<string> prefixes,
                                   TypeRegistry registry, RoomPlatforms platforms,
                                   int obstacleLayer)
        {
            var result = new Result();
            var branch = RoomGateScope.Branch(prefabRoot, RoomGateScope.BuildingBranch);
            if (branch == null) return result;

            var areas = RoomGateScope.Match(branch, prefixes, platforms);

            var areaTransforms = new HashSet<Transform>();
            foreach (var area in areas) areaTransforms.Add(area.transform);

            foreach (var area in areas)
            {
                var doors = new List<Transform>();
                CollectDoors(area.transform, areaTransforms, prefixes, registry, doors);
                if (doors.Count > 0) result.areasWithDoors.Add(area.area);

                if (area.platform == null) { result.undecided.Add(area.area); continue; }

                // Only desktopOnly adds a door gate. Every other decided value means "no door
                // gate belongs here", which covers "all" and "webOnly" in one branch instead of
                // a special case for each.
                bool wantGate = area.platform == RoomPlatforms.DesktopOnly;

                foreach (var door in doors)
                {
                    var existing = door.GetComponent<ComponentGate>();

                    if (!wantGate)
                    {
                        if (existing == null) result.unchanged++;
                        else { Object.DestroyImmediate(existing); result.removed++; }
                        continue;
                    }

                    var components = new List<Component>();
                    var script = door.GetComponent<Door>();
                    var animator = door.GetComponent<Animator>();
                    var audio = door.GetComponent<AudioSource>();
                    if (script != null) components.Add(script);
                    if (animator != null) components.Add(animator);
                    if (audio != null) components.Add(audio);

                    if (components.Count == 0)
                    {
                        // A gate with an empty list does nothing but add clutter, and it hides
                        // the real problem: the Door behaviour was never attached. Run
                        // FriWorld > Generate > Layers And Static From Registry first.
                        result.doorsWithNothingToStrip.Add(RegistryScanner.PathOf(door));
                        if (existing != null)
                        {
                            Object.DestroyImmediate(existing);
                            result.removed++;
                        }
                        continue;
                    }

                    if (existing == null)
                    {
                        Configure(door.gameObject.AddComponent<ComponentGate>(), components,
                                  obstacleLayer);
                        result.added++;
                    }
                    else if (NeedsReconfigure(existing, components, obstacleLayer))
                    {
                        Configure(existing, components, obstacleLayer);
                        result.reconfigured++;
                    }
                    else result.unchanged++;
                }
            }

            return result;
        }

        /// <summary>
        /// The doors belonging to this area. The walk stops at any nested area container, so a
        /// door is owned by exactly one area — otherwise rb_basement would claim the doors of all
        /// fourteen rooms inside it and decide on their behalf.
        /// </summary>
        static void CollectDoors(Transform areaRoot, HashSet<Transform> areaTransforms,
                                 IReadOnlyList<string> prefixes, TypeRegistry registry,
                                 List<Transform> acc)
        {
            foreach (Transform child in areaRoot)
            {
                if (areaTransforms.Contains(child)) continue;   // the inner area owns this

                var entry = registry.Find(ObjectTypeKey.Derive(child.name.Trim(), prefixes));
                if (entry != null && entry.script == DoorScriptName) acc.Add(child);

                CollectDoors(child, areaTransforms, prefixes, registry, acc);
            }
        }

        static void Configure(ComponentGate gate, List<Component> components, int obstacleLayer)
        {
            gate.target = PlatformGate.Target.DesktopOnly;
            gate.components = components;
            gate.changeLayerWhenStripped = true;
            gate.strippedLayer = obstacleLayer;
        }

        static bool NeedsReconfigure(ComponentGate gate, List<Component> want, int obstacleLayer)
        {
            if (gate.target != PlatformGate.Target.DesktopOnly) return true;
            if (!gate.changeLayerWhenStripped) return true;
            if (gate.strippedLayer != obstacleLayer) return true;
            if (gate.components == null || gate.components.Count != want.Count) return true;

            for (int i = 0; i < want.Count; i++)
                if (gate.components[i] != want[i]) return true;

            return false;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Refresh, wait, read the console with `Types: ["Error"]`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs.meta && git commit -m "feat(featureflags): gate door behaviour from the room platform data"
```

---

## Task 5: The report

**Files:**
- Create: `Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs`

- [ ] **Step 1: Write the implementation**

Create `Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs`:

```csharp
using System.Collections.Generic;
using System.Text;
using FriWorld.ObjectRegistry;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Formats what the two appliers found. Short on purpose: the moment this becomes a
    /// thousand-line dump it gets skipped, and then nothing is being caught at all.
    /// </summary>
    public static class RoomGateReport
    {
        public const int MaxListed = 20;

        public static string Build(ObjectsPlatformGates.Result objects,
                                   DoorComponentGates.Result doors,
                                   RoomPlatforms platforms)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[RoomGates] Objects: " + objects.added + " added, "
                        + objects.retargeted + " retargeted, " + objects.removed + " removed, "
                        + objects.unchanged + " already correct");
            sb.AppendLine("[RoomGates] Doors:   " + doors.added + " added, "
                        + doors.reconfigured + " reconfigured, " + doors.removed + " removed, "
                        + doors.unchanged + " already correct");

            Section(sb, "UNDECIDED areas (no platform in RoomPlatforms.json — left untouched)",
                    Union(objects.undecided, doors.undecided));
            Section(sb, "BAD VALUES (not all / desktopOnly / webOnly)", objects.badValues);
            Section(sb, "NESTED (contains another area — gating it would void the inner decisions)",
                    objects.nested);
            Section(sb, "NO EFFECT (desktopOnly, but the area has neither furniture nor doors)",
                    NoEffect(objects, doors, platforms));
            Section(sb, "ORPHAN gates (PlatformGate on something that is not an area — left alone)",
                    objects.orphanGates);
            Section(sb, "DOORS with nothing to strip (Door behaviour was never attached)",
                    doors.doorsWithNothingToStrip);

            return sb.ToString();
        }

        /// <summary>
        /// Areas marked desktopOnly whose decision currently does nothing, because they have no
        /// furniture container and no doors. Not an error — outside_gazebo is one today, and it
        /// starts working by itself the moment something is put there.
        /// </summary>
        static List<string> NoEffect(ObjectsPlatformGates.Result objects,
                                     DoorComponentGates.Result doors,
                                     RoomPlatforms platforms)
        {
            var inert = new List<string>();
            foreach (var entry in platforms.rooms)
            {
                if (entry == null || entry.platform != RoomPlatforms.DesktopOnly) continue;
                if (objects.areasPresent.Contains(entry.room)) continue;
                if (doors.areasWithDoors.Contains(entry.room)) continue;
                inert.Add(entry.room);
            }
            inert.Sort(string.CompareOrdinal);
            return inert;
        }

        static List<string> Union(List<string> a, List<string> b)
        {
            var all = new List<string>(a);
            foreach (var s in b) if (!all.Contains(s)) all.Add(s);
            all.Sort(string.CompareOrdinal);
            return all;
        }

        static void Section(StringBuilder sb, string title, List<string> items)
        {
            if (items == null || items.Count == 0) return;

            sb.AppendLine("  " + title + ": " + items.Count);
            for (int i = 0; i < items.Count && i < MaxListed; i++)
                sb.AppendLine("    " + items[i]);
            if (items.Count > MaxListed)
                sb.AppendLine("    … and " + (items.Count - MaxListed) + " more");
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Refresh, wait, read the console with `Types: ["Error"]`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs.meta && git commit -m "feat(featureflags): report room gate mismatches"
```

---

## Task 6: The menu

**Files:**
- Create: `Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs`

- [ ] **Step 1: Write the implementation**

Create `Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs`:

```csharp
using FriWorld.ObjectRegistry;
using UnityEditor;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Entry points for the two room gate branches. They are separate because they fail
    /// separately: a .blend reimport wipes the door gates inside fri_building and leaves the
    /// Objects branch alone, so the fix should not have to touch Objects either.
    ///
    /// Report runs both appliers and then closes the prefab WITHOUT saving, so the dry run and
    /// the real run go down the same code path and cannot drift apart.
    /// </summary>
    public static class RoomGateMenu
    {
        [MenuItem("Tools/Feature Flags/Report Room Gates")]
        static void Report() => Run(objectsBranch: true, doorBranch: true, save: false);

        [MenuItem("Tools/Feature Flags/Apply Object Gates")]
        static void ApplyObjectGates() => Run(objectsBranch: true, doorBranch: false, save: true);

        [MenuItem("Tools/Feature Flags/Apply Door Gates")]
        static void ApplyDoorGates() => Run(objectsBranch: false, doorBranch: true, save: true);

        [MenuItem("Tools/Feature Flags/Apply All Room Gates")]
        static void ApplyAll() => Run(objectsBranch: true, doorBranch: true, save: true);

        static void Run(bool objectsBranch, bool doorBranch, bool save)
        {
            int obstacleLayer = LayerMask.NameToLayer(DoorComponentGates.ObstacleLayerName);
            if (doorBranch && obstacleLayer < 0)
            {
                Debug.LogError("[RoomGates] the layer \"" + DoorComponentGates.ObstacleLayerName
                    + "\" is not defined. Add it in Project Settings > Tags and Layers — without "
                    + "it a stripped door would stay on Interactable and still show the prompt.");
                return;
            }

            var platforms = RoomPlatforms.Load(ObjectRegistryMenu.RoomPlatformsPath);
            if (platforms.rooms.Count == 0)
            {
                Debug.LogError("[RoomGates] " + ObjectRegistryMenu.RoomPlatformsPath
                    + " is empty. Run FriWorld > Registry > Sync Room Platforms first, "
                    + "otherwise every area would be skipped as undecided.");
                return;
            }

            var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
            var registry = TypeRegistry.Load(ObjectRegistryMenu.TypesPath);
            if (doorBranch && registry.types.Count == 0)
            {
                Debug.LogError("[RoomGates] " + ObjectRegistryMenu.TypesPath
                    + " is empty, so no object can be recognised as a door.");
                return;
            }

            var objectsResult = new ObjectsPlatformGates.Result();
            var doorsResult = new DoorComponentGates.Result();

            var contents = RoomGateScope.Open();
            bool closed = false;
            try
            {
                if (objectsBranch)
                    objectsResult = ObjectsPlatformGates.Apply(contents, prefixes, platforms);
                if (doorBranch)
                    doorsResult = DoorComponentGates.Apply(contents, prefixes, registry,
                                                           platforms, obstacleLayer);

                if (save) RoomGateScope.SaveAndClose(contents);
                else RoomGateScope.Close(contents);
                closed = true;
            }
            finally
            {
                if (!closed) RoomGateScope.Close(contents);
            }

            if (save) AssetDatabase.Refresh();

            Debug.Log((save ? "" : "DRY RUN — the prefab was not written.\n")
                    + RoomGateReport.Build(objectsResult, doorsResult, platforms));
        }
    }
}
```

- [ ] **Step 2: Run the dry run**

Refresh, wait, then run `FriWorld > Feature Flags > Report Room Gates`.

Expected shape:

```
DRY RUN — the prefab was not written.
[RoomGates] Objects: <a> added, 0 retargeted, <b> removed, <c> already correct
[RoomGates] Doors:   <d> added, 0 reconfigured, 0 removed, <e> already correct
  NO EFFECT (desktopOnly, but the area has neither furniture nor doors): 1
    outside_gazebo
```

What the numbers must satisfy:

- **Doors `added`** — every door inside a `desktopOnly` area. The prefab asset holds **zero** `ComponentGate` components today, so none can be "already correct" among them. Strictly between 0 and 283.
- **Doors `already correct`** — the rest, sitting in `all` areas with no gate. `added + already correct` equals **283** minus anything under `DOORS with nothing to strip`.
- **Doors `removed`** — must be `0`. The one existing `ComponentGate` lives on the scene instance, not in the asset, so this branch never sees it.
- **Objects `removed`** — areas the draft switched from gated to `all`. A small number; before the migration seven were gated while the draft said `all`.

There must be **no** `UNDECIDED` and no `BAD VALUES` section — Task 2 decided all 301 areas. `NESTED` should be absent too, since the five container-of-areas rows are all `all`.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs.meta && git commit -m "feat(featureflags): add the room gate menu"
```

---

## Task 7: Apply and verify

**This task writes `Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab`, which carries 247k lines of the user's uncommitted work. Ask the user to commit or stash their prefab changes before running Step 1, and do not commit the prefab yourself without their explicit go-ahead.**

- [ ] **Step 1: Apply everything**

Run `FriWorld > Feature Flags > Apply All Room Gates`.

Expected: the same counts as the dry run, without the `DRY RUN` line.

- [ ] **Step 2: Verify it converges**

Run `FriWorld > Feature Flags > Report Room Gates` again.

Expected — this is the real check that the reconcile is complete and stable:

```
[RoomGates] Objects: 0 added, 0 retargeted, 0 removed, <c> already correct
[RoomGates] Doors:   0 added, 0 reconfigured, 0 removed, 283 already correct
```

Any non-zero `added` / `removed` / `retargeted` on a second run means an applier is not converging. Fix that before continuing.

- [ ] **Step 3: Verify the gates landed in the asset, not the scene**

```csharp
using UnityEngine;
using UnityEditor;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        const string path = "Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        var sb = new StringBuilder();
        try
        {
            sb.AppendLine("PlatformGate in asset : "
                + contents.GetComponentsInChildren<PlatformGate>(true).Length);
            sb.AppendLine("ComponentGate in asset: "
                + contents.GetComponentsInChildren<ComponentGate>(true).Length);
            sb.AppendLine("Door in asset         : "
                + contents.GetComponentsInChildren<Door>(true).Length);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        result.Log(sb.ToString());
    }
}
```

Expected: `Door in asset` still `283`; `ComponentGate in asset` equal to the doors branch `added` count from Step 1; `PlatformGate in asset` equal to the number of decided, non-nested areas present in `Objects` — it was `144` before and will move to match the data.

- [ ] **Step 4: Verify a door in play mode**

Open `Assets/_Game/Scenes/Demo.unity`, switch the build target to WebGL so `PlatformFlags.IsWeb` reports true, and enter play mode. Walk up to a door in a `desktopOnly` room.

Expected: the door is visible, shows no interaction prompt and does not open. A door in an `all` room still opens. Switch the build target back afterwards.

- [ ] **Step 5: Commit — only with the user's go-ahead**

```bash
git add Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab && git commit -m "chore(prefab): generate room gates from RoomPlatforms.json"
```

---

## Task 8: Clean up and document

**Files:**
- Delete: `Assets/_Game/Editor/DoorGateSetup.cs`, `Assets/_Game/Editor/Platforms.json`, `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs` (each with its `.meta`)
- Modify: `CHANGELOG.md`
- Create: `docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`

- [ ] **Step 1: Delete what this replaces**

```bash
git rm Assets/_Game/Editor/DoorGateSetup.cs Assets/_Game/Editor/DoorGateSetup.cs.meta Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs.meta
```

`Platforms.json` is untracked, so remove it from disk:

```bash
rm Assets/_Game/Editor/Platforms.json Assets/_Game/Editor/Platforms.json.meta
```

- [ ] **Step 2: Confirm nothing referenced them**

```bash
grep -rn "DoorGateSetup\|MigratePlatformsDraft" Assets/_Game --include=*.cs
```

Expected: no output.

- [ ] **Step 3: Verify Unity still compiles**

Refresh, wait, read the console with `Types: ["Error"]`.

Expected: no errors, `Tools > Setup Door Gates` gone from the menu, the four `Tools > Feature Flags` items present.

- [ ] **Step 4: Add the changelog lines**

In `CHANGELOG.md` under `## [Unreleased]`, add to `### Added`:

```
- `FriWorld > Feature Flags > Apply All Room Gates` — platformové gaty sa generujú z `RoomPlatforms.json` priamo do prefab assetu. Dá sa spustiť aj po vetvách: `Apply Object Gates` pre nábytok, `Apply Door Gates` po reimporte `.blend`.
```

and to `### Fixed`:

```
- Dverné gaty už nežijú len ako override v scéne, takže ich reimport `.blend` nezmetie. Doteraz z 283 dverí prežil jediný.
```

- [ ] **Step 5: Write the decision record**

This one qualifies under the CLAUDE.md rule: the cause was somewhere other than where the symptom showed. Create `docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`:

```markdown
# Platformové gaty patria do prefab assetu

## Kontext

Dverné `ComponentGate`y z budovy zmizli. Podozrenie padlo na samotný FF systém — že si gate
odstraňuje sám, keďže `ComponentGate.Awake` naozaj končí `Destroy(this)`.

Meranie ukázalo niečo iné:

| | |
|---|---|
| `PlatformGate` v `FriBuilding.prefab` | 144 |
| `ComponentGate` v tom istom assete | 0 |
| `ComponentGate` na inštancii v scéne | 1 |
| pridaných komponentov ako override na inštancii | 2671 |

`PlatformGate`y prežili, lebo ich niekto pridal do **prefab assetu**. Dverné gaty boli pridané
na **inštanciu v scéne**, kde sú to prefab overrides. Reimport `.blend` vymenil podstrom
`fri_building` aj s tým, čo na ňom viselo. Zostal jediný gate — ten pridaný po poslednom
reimporte.

`Awake` s tým nemal nič spoločné. Play mode zmeny sa aj tak vracajú.

## Rozhodnutie

Nástroje, ktoré nasadzujú gaty, zapisujú do prefab assetu cez
`PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset`, nikdy na inštanciu v scéne.

Samotné rozhodnutie „ktorá miestnosť je len pre desktop" sa presunulo do
`Assets/_Game/Editor/RoomPlatforms.json`. Komponenty sú odvodený výstup, kedykoľvek znovu
vygenerovateľný.

## Dôsledky

- Aj keby sa gaty znova stratili, `FriWorld > Feature Flags > Apply All Room Gates` ich vráti.
  Strata komponentu prestala byť stratou informácie.
- **`GenerateColliders` má ten istý problém** a zatiaľ nevystrelil: tých 2671 override
  komponentov sú prevažne `BoxCollider` a `NavMeshModifier`. Prvý reimport `.blend` ich zmetie
  rovnako. Riešenie je rovnaké, len sa zatiaľ neurobilo.
- Ručná úprava gatu priamo v hierarchii sa pri najbližšom behu prepíše. Zmena patrí do JSON‑u.
```

- [ ] **Step 6: Commit**

```bash
git add CHANGELOG.md docs/decisions/2026-08-24-platform-gaty-v-prefabe.md && git commit -m "docs: record the room platform gates and why they belong in the prefab"
```

---

## Done when

- [ ] `FriWorld > Feature Flags > Report Room Gates` reports `0 added, 0 retargeted, 0 removed` on both branches
- [ ] no `UNDECIDED` and no `BAD VALUES` section in that report
- [ ] all EditMode tests in `FriWorld.ObjectRegistry.Tests` pass
- [ ] `Tools > Setup Door Gates` is gone; the four `Tools > Feature Flags` items work
- [ ] `RoomPlatforms.json` holds 301 decided areas
- [ ] a door in a `desktopOnly` room does not open in a WebGL play mode session
