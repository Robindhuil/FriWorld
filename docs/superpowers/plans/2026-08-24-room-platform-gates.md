# Room Platform Gates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the desktop/web decision for every room out of the Unity hierarchy and into `RoomPlatforms.json`, and generate `PlatformGate` / `ComponentGate` from it into the prefab asset.

**Architecture:** A new JSON file keyed on the full area container name (`ra100_corridor_2`, not the stripped prefix `ra100_corridor`) holds one of `all` / `desktopOnly` / `webOnly`, or nothing at all when undecided. `RoomGateScope` opens `FriBuilding.prefab` in an isolated preview scene and pairs area containers with their decision. Two independent appliers then reconcile the components: one for `Objects` (whole-container `PlatformGate`), one for `fri_building` (per-door `ComponentGate`). Writing into the prefab asset rather than the scene instance is the fix for the gates that vanished.

**Tech Stack:** Unity 6000.4.11f1, C#, Newtonsoft.Json (already a project dependency), Unity Test Framework (NUnit, EditMode).

**Spec:** `docs/superpowers/specs/2026-08-24-room-platform-gates-design.md`

---

## Before you start

### Naming and language

Code, comments and commit messages are in English. Project prose documents (`CHANGELOG.md`, `docs/decisions/`) are in Slovak — Task 13 shows the exact text.

### Assembly boundary — this drives the file layout

`Assets/_Game/Editor/ObjectRegistry/` has its own asmdef, `FriWorld.ObjectRegistry.Editor`, with `"references": []`. It can use `UnityEngine` and `UnityEditor`, but it **cannot see** `PlatformGate`, `ComponentGate` or `Door` — those live in `Assembly-CSharp`.

`Assets/_Game/Editor/` (and subfolders without their own asmdef) compiles into `Assembly-CSharp-Editor`, which auto-references both `Assembly-CSharp` and the registry asmdef. So it sees everything.

| goes in the registry asmdef | goes in `Assets/_Game/Editor/FeatureFlags/` |
|---|---|
| `RoomPlatforms.cs` — pure data | `ObjectsPlatformGates.cs` — needs `PlatformGate` |
| `RoomGateScope.cs` — only `Transform` + `PrefabUtility` | `DoorComponentGates.cs` — needs `ComponentGate`, `Door` |
| `ObjectTypeKey.cs`, `ObjectRegistryMenu.cs` | `RoomGateReport.cs`, `RoomGateMenu.cs` |

Putting `RoomGateScope` in the registry asmdef is deliberate: `ObjectRegistryMenu` needs it for the sync step and could not reference it across the boundary otherwise.

### Running the tests

Tests live in `Assets/_Game/Editor/ObjectRegistry/Tests/` (assembly `FriWorld.ObjectRegistry.Tests`).

**In the editor:** `Window > General > Test Runner` → **EditMode** tab → select the class → **Run Selected**. Pass/fail shows in the window.

**Over MCP** (for an agentic worker), two calls. First write the script to disk and refresh:

```
Unity_ManageEditor → GetState   (wait until IsCompiling is false)
```

then run the tests and read the outcome:

```csharp
// Unity_RunCommand
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
        result.Log("test run started — read the console for [TESTS] lines");
    }

    class Logger : ICallbacks
    {
        public void RunStarted(ITestAdaptor t) { }
        public void TestStarted(ITestAdaptor t) { }
        public void TestFinished(ITestResultAdaptor r)
        {
            if (r.HasChildren) return;
            Debug.Log("[TESTS] " + r.TestStatus + "  " + r.FullName
                    + (string.IsNullOrEmpty(r.Message) ? "" : "  — " + r.Message));
        }
        public void RunFinished(ITestResultAdaptor r)
            => Debug.Log("[TESTS] done: " + r.PassCount + " passed, " + r.FailCount + " failed");
    }
}
```

The run is asynchronous, so read results with `Unity_ReadConsole` afterwards, filtering for `[TESTS]`.

### After every script change

Unity must recompile before a menu item exists. `Assets/Refresh`, then poll `Unity_ManageEditor → GetState` until `IsCompiling` is false.

### Committing

Conventional commits. **Never `git add -A`** — the working tree carries unrelated user changes. Stage the exact paths shown in each step.

---

## File Structure

**Create**

| file | responsibility |
|---|---|
| `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs` | the data file: load, save, exact lookup, reconcile |
| `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs` | open the prefab, find area containers, pair with decisions |
| `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs` | one-off draft conversion, deleted in Task 12 |
| `Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs` | unit tests for the data model |
| `Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs` | unit test for the "is this an area?" rule |
| `Assets/_Game/Editor/FeatureFlags/ObjectsPlatformGates.cs` | `Objects` branch → `PlatformGate` |
| `Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs` | `fri_building` branch → `ComponentGate` on doors |
| `Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs` | formats what the appliers found |
| `Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs` | the four menu items |
| `Assets/_Game/Editor/RoomPlatforms.json` | generated in Task 6, committed |

**Modify**

| file | change |
|---|---|
| `Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs` | `StripTrailingInt` → public `StripInstanceNumber` |
| `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs` | drop its private copy, call the shared one |
| `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs` | `RoomPlatformsPath`, `Sync Room Platforms`, hook into `Add Prefixes` |
| `Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs` | test for the extracted helper |

**Delete**

| file | why |
|---|---|
| `Assets/_Game/Editor/DoorGateSetup.cs` | replaced by `DoorComponentGates` |
| `Assets/_Game/Editor/Platforms.json` | hand-written draft, consumed by the migration |
| `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs` | one-off, already run |

---

## Task 1: The RoomPlatforms data model

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs`:

```csharp
using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class RoomPlatformsTests
    {
        [Test]
        public void AMissingPlatformMeansUndecided()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra102"" } ] }";

            var entry = RoomPlatforms.FromJson(json).Find("ra102");

            Assert.IsNotNull(entry);
            Assert.IsNull(entry.platform);
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void AllIsDecidedAndDifferentFromUndecided()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra101"", ""platform"": ""all"" } ] }";

            var entry = RoomPlatforms.FromJson(json).Find("ra101");

            Assert.IsTrue(entry.IsDecided, "'no gate here' is not the same as 'not decided'");
            Assert.AreEqual(RoomPlatforms.All, entry.platform);
        }

        [Test]
        public void LookupIsExactNotSubstring()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra100_corridor"", ""platform"": ""all"" } ] }";

            var platforms = RoomPlatforms.FromJson(json);

            Assert.IsNotNull(platforms.Find("ra100_corridor"));
            Assert.IsNull(platforms.Find("ra100_corridor_1"),
                "corridor 1 is its own area and must not inherit a prefix-shaped entry");
        }

        [Test]
        public void PlatformOfIsNullForAnUnknownArea()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [] }");

            Assert.IsNull(platforms.PlatformOf("ra102"));
        }

        [Test]
        public void UndecidedEntriesAreWrittenWithoutAPlatformField()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [ { ""room"": ""ra102"" } ] }");

            StringAssert.DoesNotContain("platform", platforms.ToJson(),
                "a written null would be indistinguishable from a deliberate decision");
        }

        [Test]
        public void UndecidedEntriesFloatToTheTop()
        {
            const string json = @"{ ""rooms"": [
                { ""room"": ""aaa"", ""platform"": ""all"" },
                { ""room"": ""zzz"" } ] }";

            var text = RoomPlatforms.FromJson(json).ToJson();

            Assert.Less(text.IndexOf("zzz"), text.IndexOf("aaa"),
                "a freshly synced area must be the first thing in the file");
        }

        [Test]
        public void DecidedEntriesAreOrderedAlphabetically()
        {
            const string json = @"{ ""rooms"": [
                { ""room"": ""zzz"", ""platform"": ""all"" },
                { ""room"": ""aaa"", ""platform"": ""all"" } ] }";

            var text = RoomPlatforms.FromJson(json).ToJson();

            Assert.Less(text.IndexOf("aaa"), text.IndexOf("zzz"));
        }

        [Test]
        public void OnlyTheThreeKnownPlatformValuesAreValid()
        {
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("all"));
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("desktopOnly"));
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("webOnly"));
            Assert.IsFalse(RoomPlatforms.IsValidPlatform("desktop"));
            Assert.IsFalse(RoomPlatforms.IsValidPlatform(null));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests` (see "Running the tests" above).

Expected: compilation error — `The name 'RoomPlatforms' does not exist in the current context`.

- [ ] **Step 3: Write the implementation**

Create `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// One area and which build it belongs to. A missing platform means "not decided yet" — the
    /// appliers leave such an area exactly as it is and report it.
    /// </summary>
    public class RoomEntry
    {
        /// <summary>
        /// Full container name, e.g. "ra100_corridor_2". Deliberately NOT the stripped prefix:
        /// corridor 1 and corridor 2 are different places and decide for themselves.
        /// </summary>
        public string room;

        // Omitted from the file entirely when unset. A null written into the file would be
        // indistinguishable from "all", and those two need opposite behaviour — one leaves a
        // gate alone, the other removes it.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string platform;   // all | desktopOnly | webOnly

        [JsonIgnore]
        public bool IsDecided => platform != null;
    }

    /// <summary>What one Reconcile call added, and which entries it could no longer place.</summary>
    public class ReconcileResult
    {
        public readonly List<string> added = new List<string>();
        public readonly List<string> orphans = new List<string>();
    }

    public class RoomPlatforms
    {
        public const string All = "all";
        public const string DesktopOnly = "desktopOnly";
        public const string WebOnly = "webOnly";

        public List<RoomEntry> rooms = new List<RoomEntry>();

        [JsonIgnore]
        readonly Dictionary<string, RoomEntry> index = new Dictionary<string, RoomEntry>();

        public static bool IsValidPlatform(string platform)
            => platform == All || platform == DesktopOnly || platform == WebOnly;

        public static RoomPlatforms FromJson(string json)
        {
            var file = JsonConvert.DeserializeObject<RoomPlatforms>(json) ?? new RoomPlatforms();
            if (file.rooms == null) file.rooms = new List<RoomEntry>();
            file.Reindex();
            return file;
        }

        public static RoomPlatforms Load(string path)
            => File.Exists(path) ? FromJson(File.ReadAllText(path)) : new RoomPlatforms();

        /// <summary>
        /// Undecided areas float to the top, so a freshly synced area is the first thing in the
        /// file rather than something to scroll for. Once decided it settles into the
        /// alphabetical body, which keeps diffs readable — plain insertion order would leave the
        /// file permanently unsorted.
        /// </summary>
        public string ToJson()
        {
            rooms.Sort((a, b) =>
            {
                bool aDecided = a.IsDecided, bDecided = b.IsDecided;
                if (aDecided != bDecided) return aDecided ? 1 : -1;
                return string.CompareOrdinal(a.room, b.room);
            });
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void Save(string path) => File.WriteAllText(path, ToJson());

        /// <summary>Exact name only — substring matching is the bug the type registry removed.</summary>
        public RoomEntry Find(string room)
        {
            if (string.IsNullOrEmpty(room)) return null;
            return index.TryGetValue(room, out var entry) ? entry : null;
        }

        /// <summary>The area's platform, or null when the area is unknown or undecided.</summary>
        public string PlatformOf(string room)
        {
            var entry = Find(room);
            return entry == null ? null : entry.platform;
        }

        void Reindex()
        {
            index.Clear();
            foreach (var entry in rooms)
                if (entry != null && !string.IsNullOrEmpty(entry.room))
                    index[entry.room] = entry;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: 8 passed, 0 failed in `RoomPlatformsTests` (existing `ObjectTypeKeyTests` and `TypeRegistryTests` still pass too).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs.meta Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs.meta && git commit -m "feat(registry): add the room platform data model"
```

---

## Task 2: Reconcile against the hierarchy

**Files:**
- Modify: `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs`

- [ ] **Step 1: Write the failing tests**

Append these three tests inside the `RoomPlatformsTests` class, before its closing brace:

```csharp
        [Test]
        public void ReconcileAddsUnknownAreasAsUndecided()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [] }");

            var result = platforms.Reconcile(new[] { "ra102", "ra103" });

            Assert.AreEqual(2, result.added.Count);
            Assert.IsNotNull(platforms.Find("ra102"));
            Assert.IsFalse(platforms.Find("ra102").IsDecided);
        }

        [Test]
        public void ReconcileNeverOverwritesAnExistingDecision()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra102"", ""platform"": ""desktopOnly"" } ] }";
            var platforms = RoomPlatforms.FromJson(json);

            var result = platforms.Reconcile(new[] { "ra102", "ra103" });

            Assert.AreEqual(RoomPlatforms.DesktopOnly, platforms.Find("ra102").platform);
            CollectionAssert.AreEqual(new[] { "ra103" }, result.added);
        }

        [Test]
        public void ReconcileKeepsAndReportsAreasThatNoLongerExist()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra999"", ""platform"": ""all"" } ] }";
            var platforms = RoomPlatforms.FromJson(json);

            var result = platforms.Reconcile(new[] { "ra102" });

            CollectionAssert.AreEqual(new[] { "ra999" }, result.orphans);
            Assert.IsNotNull(platforms.Find("ra999"),
                "deleting a decision because a container was briefly renamed is a one-way loss");
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: compilation error — `'RoomPlatforms' does not contain a definition for 'Reconcile'`.

- [ ] **Step 3: Write the implementation**

In `Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs`, insert this method directly above `void Reindex()`:

```csharp
        /// <summary>
        /// Brings the file in line with the areas that actually exist. New areas get an
        /// undecided entry; entries whose container is gone are kept and reported. Existing
        /// decisions are never touched — re-deciding three hundred rows by hand is exactly what
        /// this file exists to prevent.
        /// </summary>
        public ReconcileResult Reconcile(IReadOnlyList<string> areasInHierarchy)
        {
            var result = new ReconcileResult();
            var live = new HashSet<string>();

            if (areasInHierarchy != null)
            {
                foreach (var area in areasInHierarchy)
                {
                    if (string.IsNullOrEmpty(area)) continue;
                    live.Add(area);
                    if (index.ContainsKey(area)) continue;

                    var entry = new RoomEntry { room = area };
                    rooms.Add(entry);
                    index[area] = entry;
                    result.added.Add(area);
                }
            }

            foreach (var entry in rooms)
                if (entry != null && !string.IsNullOrEmpty(entry.room) && !live.Contains(entry.room))
                    result.orphans.Add(entry.room);

            result.added.Sort(string.CompareOrdinal);
            result.orphans.Sort(string.CompareOrdinal);
            return result;
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: 11 passed, 0 failed in `RoomPlatformsTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/RoomPlatforms.cs Assets/_Game/Editor/ObjectRegistry/Tests/RoomPlatformsTests.cs && git commit -m "feat(registry): reconcile room platforms against the hierarchy"
```

---

## Task 3: One shared instance-number strip

`StripTrailingInt` exists twice today — privately in `ObjectTypeKey` and again in `RegistryScanner`. `RoomGateScope` needs a third caller, so extract it once.

**Files:**
- Modify: `Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs:88-96`
- Modify: `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs:104-112`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs`

- [ ] **Step 1: Write the failing test**

Append inside the `ObjectTypeKeyTests` class, before its closing brace:

```csharp
        [Test]
        public void StripInstanceNumberRemovesOnlyATrailingAllDigitToken()
        {
            Assert.AreEqual("ra100_corridor", ObjectTypeKey.StripInstanceNumber("ra100_corridor_2"));
            Assert.AreEqual("rb_basement_room", ObjectTypeKey.StripInstanceNumber("rb_basement_room_14"));

            // No trailing "_<digits>" — these names are already the whole key.
            Assert.AreEqual("ra001", ObjectTypeKey.StripInstanceNumber("ra001"));
            Assert.AreEqual("rb051", ObjectTypeKey.StripInstanceNumber("rb051"));
            Assert.AreEqual("door_frame_1_glass", ObjectTypeKey.StripInstanceNumber("door_frame_1_glass"));
        }
```

- [ ] **Step 2: Run the test to verify it fails**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: compilation error — `'ObjectTypeKey' does not contain a definition for 'StripInstanceNumber'`.

- [ ] **Step 3: Extract the helper**

In `Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs`, replace the private `StripTrailingInt`:

```csharp
        static string StripTrailingInt(string s)
        {
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(underscore + 1)) ? s.Substring(0, underscore) : s;
        }
```

with the public version:

```csharp
        /// <summary>
        /// Removes a trailing "_&lt;digits&gt;" instance number. Shared with RegistryScanner and
        /// RoomGateScope: all three have to agree on what "the same thing, numbered" means, and
        /// three copies of this would drift.
        /// </summary>
        public static string StripInstanceNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(underscore + 1)) ? s.Substring(0, underscore) : s;
        }
```

Then update the two internal call sites in the same file — inside `Derive`, change

```csharp
                candidate = StripTrailingInt(candidate);
```

to

```csharp
                candidate = StripInstanceNumber(candidate);
```

and change the fallback return

```csharp
            return StripTrailingInt(StripOverrideTokens(objectName));
```

to

```csharp
            return StripInstanceNumber(StripOverrideTokens(objectName));
```

- [ ] **Step 4: Remove the duplicate from RegistryScanner**

In `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs`, delete this method entirely:

```csharp
        static string StripTrailingInt(string s)
        {
            int u = s.LastIndexOf('_');
            if (u <= 0) return s;
            string tail = s.Substring(u + 1);
            if (tail.Length == 0) return s;
            foreach (char c in tail) if (!char.IsDigit(c)) return s;
            return s.Substring(0, u);
        }
```

and change its one call site inside `Walk` from

```csharp
                if (t.childCount > 0) containers.Add(StripTrailingInt(t.name));
```

to

```csharp
                if (t.childCount > 0) containers.Add(ObjectTypeKey.StripInstanceNumber(t.name));
```

- [ ] **Step 5: Run the tests to verify they pass**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: all tests pass, including the seven pre-existing `ObjectTypeKeyTests` — they cover the derivation paths that just changed helper, so a regression here would show up immediately.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs && git commit -m "refactor(registry): share one instance-number strip"
```

---

## Task 4: RoomGateScope — find the areas

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs`

- [ ] **Step 1: Write the failing test**

Only the pure rule is unit-tested; the prefab walk is exercised for real in Task 11.

Create `Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class RoomGateScopeTests
    {
        static List<string> Prefixes(params string[] p) => new List<string>(p);

        [Test]
        public void AnAreaIsAContainerWhoseStrippedNameIsAnApprovedPrefix()
        {
            var prefixes = Prefixes("ra100_corridor", "ra102", "rb_basement_room");

            Assert.IsTrue(RoomGateScope.IsAreaName("ra100_corridor_2", prefixes));
            Assert.IsTrue(RoomGateScope.IsAreaName("ra102", prefixes));
            Assert.IsTrue(RoomGateScope.IsAreaName("rb_basement_room_14", prefixes));
        }

        [Test]
        public void FurnitureContainersAreNotAreas()
        {
            var prefixes = Prefixes("ra102", "ra000_corridor");

            Assert.IsFalse(RoomGateScope.IsAreaName("ra102_lamp", prefixes),
                "a lamp group inside a room is not a room");
            Assert.IsFalse(RoomGateScope.IsAreaName("ra000_corridor_2_poster", prefixes));
            Assert.IsFalse(RoomGateScope.IsAreaName("chair_classroom_1_yellow", prefixes));
        }

        [Test]
        public void AnUnapprovedNameIsNotAnArea()
        {
            Assert.IsFalse(RoomGateScope.IsAreaName("ra103", Prefixes("ra102")));
            Assert.IsFalse(RoomGateScope.IsAreaName("", Prefixes("ra102")));
            Assert.IsFalse(RoomGateScope.IsAreaName("ra102", null));
        }

        [Test]
        public void MatchingIgnoresCaseAndSurroundingWhitespace()
        {
            var prefixes = Prefixes("ra100_corridor");

            Assert.IsTrue(RoomGateScope.IsAreaName("RA100_Corridor_2", prefixes));
            Assert.IsTrue(RoomGateScope.IsAreaName("  ra100_corridor_2  ", prefixes),
                "a stray space in a Blender export must not silently skip a whole room");
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: compilation error — `The name 'RoomGateScope' does not exist in the current context`.

- [ ] **Step 3: Write the implementation**

Create `Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs`:

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
    /// Finds the room-sized containers inside FriBuilding.prefab and pairs them with their
    /// platform decision.
    ///
    /// Deliberately knows nothing about PlatformGate, ComponentGate or Door. It lives in the
    /// registry assembly so both the sync menu and the two appliers can use it; the appliers sit
    /// in Assembly-CSharp-Editor because they do need those types, and that assembly can see
    /// this one but not the other way round.
    ///
    /// Everything is written into the PREFAB ASSET, never onto a scene instance. Components
    /// added to the instance are prefab overrides and a single revert or model reimport wipes
    /// them — that is how the door gates were lost before this system existed.
    /// </summary>
    public static class RoomGateScope
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab";
        public const string ObjectsBranch = "Objects";
        public const string BuildingBranch = "fri_building";

        /// <summary>
        /// True when this container is a room-sized area: its name with the trailing instance
        /// number removed is an approved prefix. Without that test the walk would also treat
        /// ra102_lamp and chair_classroom_1_yellow as areas.
        /// </summary>
        public static bool IsAreaName(string containerName, IReadOnlyList<string> prefixes)
        {
            if (string.IsNullOrEmpty(containerName) || prefixes == null) return false;

            string stripped = ObjectTypeKey.StripInstanceNumber(containerName.Trim());
            for (int i = 0; i < prefixes.Count; i++)
                if (string.Equals(prefixes[i], stripped, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

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

        /// <summary>Every area container in a branch, paired with its decision.</summary>
        public static List<AreaMatch> Match(Transform branchRoot, IReadOnlyList<string> prefixes,
                                            RoomPlatforms platforms)
        {
            var matches = new List<AreaMatch>();
            if (branchRoot == null) return matches;
            Walk(branchRoot, prefixes, platforms ?? new RoomPlatforms(), matches);
            return matches;
        }

        /// <summary>Distinct area names in a branch, sorted. This is what Reconcile consumes.</summary>
        public static List<string> AreaNames(Transform branchRoot, IReadOnlyList<string> prefixes)
        {
            var names = new List<string>();
            foreach (var match in Match(branchRoot, prefixes, null))
                if (!names.Contains(match.area)) names.Add(match.area);
            names.Sort(string.CompareOrdinal);
            return names;
        }

        /// <summary>
        /// True when another area sits inside this one. Gating such a container would strip its
        /// inner areas too and void their own decisions without saying so, which is why the
        /// appliers refuse. Today this catches rb, outside, terrace, rb_basement and
        /// rc000_cafeteria — all of them containers of other areas.
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

        static void Walk(Transform t, IReadOnlyList<string> prefixes, RoomPlatforms platforms,
                         List<AreaMatch> acc)
        {
            foreach (Transform child in t)
            {
                if (child.childCount > 0 && IsAreaName(child.name, prefixes))
                {
                    string area = child.name.Trim();
                    acc.Add(new AreaMatch
                    {
                        transform = child,
                        area = area,
                        platform = platforms.PlatformOf(area),
                    });
                }

                // Keep descending. Areas sit at different depths: Objects/rc holds rooms
                // directly, Objects/ra puts a floor level in between.
                Walk(child, prefixes, platforms, acc);
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the EditMode tests for `FriWorld.ObjectRegistry.Tests`.

Expected: 4 passed, 0 failed in `RoomGateScopeTests`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs Assets/_Game/Editor/ObjectRegistry/RoomGateScope.cs.meta Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs Assets/_Game/Editor/ObjectRegistry/Tests/RoomGateScopeTests.cs.meta && git commit -m "feat(registry): locate room areas in the FriBuilding prefab"
```

---

## Task 5: Sync Room Platforms

**Files:**
- Modify: `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`

- [ ] **Step 1: Add the path constant**

In `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`, below the two existing constants:

```csharp
        public const string PrefixesPath = "Assets/_Game/Editor/ObjectPrefixes.json";
        public const string TypesPath    = "Assets/_Game/Editor/ObjectTypes.json";
```

add:

```csharp
        public const string RoomPlatformsPath = "Assets/_Game/Editor/RoomPlatforms.json";
```

- [ ] **Step 2: Add the sync method and its menu item**

In the same class, above `internal static bool TryScanSelection(...)`, add:

```csharp
        [MenuItem("Tools/Object Registry/Sync Room Platforms")]
        static void SyncRoomPlatforms() => Debug.Log(SyncRoomPlatformsFile());

        /// <summary>
        /// Brings RoomPlatforms.json in line with the areas in the prefab. Scans the PREFAB, not
        /// the selection: a partial selection would leave the file half-filled, and the file has
        /// to describe the whole building for the appliers to be able to remove a stale gate.
        /// </summary>
        internal static string SyncRoomPlatformsFile()
        {
            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            var platforms = RoomPlatforms.Load(RoomPlatformsPath);

            var contents = RoomGateScope.Open();
            ReconcileResult result;
            try
            {
                result = platforms.Reconcile(AreasInPrefab(contents, prefixes));
            }
            finally
            {
                RoomGateScope.Close(contents);
            }

            platforms.Save(RoomPlatformsPath);
            AssetDatabase.Refresh();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RoomPlatforms] " + platforms.rooms.Count + " areas in " + RoomPlatformsPath);
            if (result.added.Count > 0)
            {
                sb.AppendLine("  NEW, undecided — they are at the TOP of the file ("
                            + result.added.Count + "):");
                for (int i = 0; i < result.added.Count && i < 30; i++)
                    sb.AppendLine("    + " + result.added[i]);
                if (result.added.Count > 30)
                    sb.AppendLine("    … and " + (result.added.Count - 30) + " more");
            }
            if (result.orphans.Count > 0)
            {
                sb.AppendLine("  ORPHANS — no such container any more, the decision was KEPT ("
                            + result.orphans.Count + "):");
                foreach (var o in result.orphans) sb.AppendLine("    ? " + o);
            }
            if (result.added.Count == 0 && result.orphans.Count == 0)
                sb.AppendLine("  already in sync");
            return sb.ToString();
        }

        /// <summary>Area names from both branches of the prefab, deduplicated.</summary>
        internal static List<string> AreasInPrefab(GameObject prefabContents,
                                                   List<string> prefixes)
        {
            var areas = new List<string>();
            foreach (var branch in new[] { RoomGateScope.ObjectsBranch, RoomGateScope.BuildingBranch })
                foreach (var name in RoomGateScope.AreaNames(
                             RoomGateScope.Branch(prefabContents, branch), prefixes))
                    if (!areas.Contains(name)) areas.Add(name);
            areas.Sort(string.CompareOrdinal);
            return areas;
        }
```

- [ ] **Step 3: Hook it into Add Prefixes From Selection**

In the same file, at the very end of `static void AddPrefixes()`, the last statement is currently:

```csharp
            Debug.Log(sb.ToString());
```

Replace it with:

```csharp
            Debug.Log(sb.ToString());

            // One scan, two outputs. New prefixes mean new areas, and an area with no row in
            // RoomPlatforms.json is invisible to the appliers.
            Debug.Log(SyncRoomPlatformsFile());
```

- [ ] **Step 4: Verify it compiles and runs**

`Assets/Refresh`, wait for `IsCompiling` to be false, then run the menu item `Tools > Object Registry > Sync Room Platforms`.

Expected console output — the file did not exist, so every area is new:

```
[RoomPlatforms] 300 areas in Assets/_Game/Editor/RoomPlatforms.json
  NEW, undecided — they are at the TOP of the file (300):
    + outside
    …
```

Expected file: `Assets/_Game/Editor/RoomPlatforms.json` exists with 300 entries, none carrying a `platform` field.

- [ ] **Step 5: Commit**

Commit the code only. The generated file is committed in Task 6, once it carries the decisions.

```bash
git add Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs && git commit -m "feat(registry): sync room platforms from the prefab hierarchy"
```

---

## Task 6: Migrate the hand-written draft

`Assets/_Game/Editor/Platforms.json` holds 262 prefix-level decisions written by hand. Every area inherits the value of its stripped prefix, so `ra100_corridor: all` becomes three decided corridors and `rb_basement_room: desktopOnly` becomes fourteen.

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs`
- Create: `Assets/_Game/Editor/RoomPlatforms.json` (generated by running it)

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
    /// One-off: converts the hand-written prefix-level draft Platforms.json into the area-level
    /// RoomPlatforms.json. Every area takes the value of its stripped prefix, so one draft row
    /// can decide several areas — which is the point, since the draft could not express them
    /// separately.
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
            // "name", "platform": "value". One regex over a known one-time input beats writing a
            // tolerant parser that nothing else will ever use.
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
                areas = ObjectRegistryMenu.AreasInPrefab(contents, prefixes);
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

                string key = ObjectTypeKey.StripInstanceNumber(area);
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
    }
}
```

- [ ] **Step 2: Run the migration**

`Assets/Refresh`, wait for `IsCompiling` to be false, then run `Tools > Object Registry > Migrate Platforms Draft`.

Expected console output:

```
[MigrateDraft] draft rows 262 → decided 300 of 300 areas
```

`unmatched` must be empty. If it is not, the listed areas have a stripped prefix that the draft never mentioned — add those rows to the draft by hand and re-run, rather than leaving them undecided.

- [ ] **Step 3: Sanity-check the generated file**

```bash
grep -c '"room"' Assets/_Game/Editor/RoomPlatforms.json
```

Expected: `300`

```bash
grep -c '"platform": "desktopOnly"' Assets/_Game/Editor/RoomPlatforms.json
```

Expected: a number in the 170–200 range. The 158 draft rows marked `desktopOnly` expand across their instances — `rb_basement_room` alone contributes 14.

```bash
grep -c '"platform"' Assets/_Game/Editor/RoomPlatforms.json
```

Expected: `300` — every area decided, none left blank.

- [ ] **Step 4: Spot-check the expansion**

```bash
grep -A1 '"room": "rb_basement_room_' Assets/_Game/Editor/RoomPlatforms.json | head -20
```

Expected: fourteen separate `rb_basement_room_N` rows, each with `"platform": "desktopOnly"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs.meta Assets/_Game/Editor/RoomPlatforms.json Assets/_Game/Editor/RoomPlatforms.json.meta && git commit -m "feat(registry): migrate the platform draft to per-area decisions"
```

---

## Task 7: The Objects branch applier

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

`Assets/Refresh`, wait for `IsCompiling` to be false, then check the console.

Expected: no compilation errors. There is no menu item yet, so nothing to run — Task 10 wires it up.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags Assets/_Game/Editor/FeatureFlags.meta && git commit -m "feat(featureflags): reconcile Objects platform gates from the registry"
```

---

## Task 8: The door branch applier

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
    /// Why the layer change and not a trigger collider: PlayerInteract raycasts with
    /// QueryTriggerInteraction.Ignore, so turning the door into a trigger would break interaction
    /// everywhere rather than only on web.
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
                // gate belongs here" — that covers "all" and "webOnly" with the same branch
                // rather than a special case for each.
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
                        // Tools > Layers > Assign Layers And Static From Registry first.
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
        /// fourteen rooms inside it and decide for them.
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

`Assets/Refresh`, wait for `IsCompiling` to be false, then check the console.

Expected: no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs Assets/_Game/Editor/FeatureFlags/DoorComponentGates.cs.meta && git commit -m "feat(featureflags): gate door behaviour from the room platform data"
```

---

## Task 9: The report

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
        /// will start working by itself the moment something is put there.
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

`Assets/Refresh`, wait for `IsCompiling` to be false, then check the console.

Expected: no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs Assets/_Game/Editor/FeatureFlags/RoomGateReport.cs.meta && git commit -m "feat(featureflags): report room gate mismatches"
```

---

## Task 10: The menu

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
    /// Report runs both appliers and then closes the prefab WITHOUT saving, so a dry run and a
    /// real run go down the same code path and cannot drift apart.
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
                    + " is empty. Run Tools > Object Registry > Sync Room Platforms first, "
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

- [ ] **Step 2: Run the report and read it**

`Assets/Refresh`, wait for `IsCompiling` to be false, then run `Tools > Feature Flags > Report Room Gates`.

Expected: a dry run showing the drift between the data and the prefab. Its shape:

```
DRY RUN — the prefab was not written.
[RoomGates] Objects: <a> added, 0 retargeted, <b> removed, <c> already correct
[RoomGates] Doors:   <d> added, 0 reconfigured, 0 removed, <e> already correct
  NO EFFECT (desktopOnly, but the area has neither furniture nor doors): 1
    outside_gazebo
```

What each number must satisfy:

- **Doors `added` (`<d>`)** — every door inside a `desktopOnly` area. The prefab asset holds **zero** `ComponentGate` components today, so nothing can be "already correct" among them. `<d>` is therefore strictly between 0 and 283.
- **Doors `already correct` (`<e>`)** — the remaining doors, which sit in `all` areas and correctly have no gate. `<d> + <e>` must equal **283** minus whatever appears under `DOORS with nothing to strip`.
- **Doors `removed`** — must be `0`. The single existing `ComponentGate` lives on the scene instance, not in the asset, so this branch never sees it.
- **Objects `removed` (`<b>`)** — the areas the draft switched from gated to `all`. Expect a small number; before the migration seven areas were gated while the draft said `all`.

There must be **no** `UNDECIDED` and no `BAD VALUES` section: Task 6 decided all 300 areas. `NESTED` is expected to be absent too, since all five container-of-areas rows are `all`.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs Assets/_Game/Editor/FeatureFlags/RoomGateMenu.cs.meta && git commit -m "feat(featureflags): add the room gate menu"
```

---

## Task 11: Apply and verify

**Files:**
- Modify: `Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab` (written by the tool)

- [ ] **Step 1: Apply everything**

Run `Tools > Feature Flags > Apply All Room Gates`.

Expected: the same counts as the dry run in Task 10, this time without the `DRY RUN` line.

- [ ] **Step 2: Verify it is idempotent**

Run `Tools > Feature Flags > Report Room Gates` again.

Expected — this is the real check that the reconcile is complete and stable:

```
[RoomGates] Objects: 0 added, 0 retargeted, 0 removed, K already correct
[RoomGates] Doors:   0 added, 0 reconfigured, 0 removed, 283 already correct
```

Any non-zero `added` / `removed` / `retargeted` on a second run means an applier is not converging — fix that before continuing.

- [ ] **Step 3: Verify the gates landed in the prefab asset, not the scene**

Run over MCP with `Unity_RunCommand`:

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

Expected: `Door in asset` is still `283`, and `ComponentGate in asset` is now within one or two of `283` — the difference being any door reported under `DOORS with nothing to strip`. `PlatformGate in asset` is the number of decided, non-nested areas present in `Objects`; it was `144` before and will change to match the data.

- [ ] **Step 4: Verify a door in play mode**

Open `Assets/_Game/Scenes/Demo.unity`, switch the build target to WebGL so `PlatformFlags.IsWeb` reports true, and enter play mode. Walk up to a door in a `desktopOnly` room.

Expected: the door is visible, shows no interaction prompt, and does not open. A door in an `all` room still opens.

Switch the build target back to your usual one when done.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab && git commit -m "chore(prefab): generate room gates from RoomPlatforms.json"
```

---

## Task 12: Remove what this replaces

**Files:**
- Delete: `Assets/_Game/Editor/DoorGateSetup.cs` (+ `.meta`)
- Delete: `Assets/_Game/Editor/Platforms.json` (+ `.meta`)
- Delete: `Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs` (+ `.meta`)

- [ ] **Step 1: Delete the three files**

```bash
git rm Assets/_Game/Editor/DoorGateSetup.cs Assets/_Game/Editor/DoorGateSetup.cs.meta Assets/_Game/Editor/Platforms.json Assets/_Game/Editor/Platforms.json.meta Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs Assets/_Game/Editor/ObjectRegistry/MigratePlatformsDraft.cs.meta
```

- [ ] **Step 2: Confirm nothing referenced them**

```bash
grep -rn "DoorGateSetup\|Platforms.json\|MigratePlatformsDraft" Assets/_Game --include=*.cs
```

Expected: no output. `RoomPlatforms.json` is referenced through the constant `ObjectRegistryMenu.RoomPlatformsPath`, so the literal string appears only in that one declaration and will not match `Platforms.json` here — if it does match, check you are not looking at the constant's own line.

- [ ] **Step 3: Verify Unity still compiles**

`Assets/Refresh`, wait for `IsCompiling` to be false, then check the console.

Expected: no compilation errors, and `Tools > Setup Door Gates` is gone from the menu while the four `Tools > Feature Flags` items remain.

- [ ] **Step 4: Commit**

```bash
git commit -m "chore(editor): drop DoorGateSetup and the platform draft"
```

---

## Task 13: Documentation

**Files:**
- Modify: `CHANGELOG.md`
- Create: `docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`

- [ ] **Step 1: Add the changelog lines**

In `CHANGELOG.md`, under `## [Unreleased]`, add to the `### Added` section:

```
- Platformové rozhodnutie pre každú miestnosť žije v `RoomPlatforms.json` a gaty sa z neho generujú — `Tools > Feature Flags > Apply All Room Gates`.
```

and to the `### Fixed` section:

```
- Dverné gaty už neprežívajú len ako override v scéne, takže ich reimport `.blend` nezmetie.
```

If a section does not exist yet, create it in the order `Added` / `Fixed` / `Changed` / `Performance` / `Removed`.

- [ ] **Step 2: Write the decision record**

This one qualifies: the cause was somewhere other than where the symptom showed. Create `docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`:

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

- Aj keby sa gaty znova stratili, `Tools > Feature Flags > Apply All Room Gates` ich vráti.
  Strata komponentu prestala byť stratou informácie.
- **`GenerateColliders` má ten istý problém** a zatiaľ nevystrelil: tých 2671 override
  komponentov sú prevažne `BoxCollider` a `NavMeshModifier`. Prvý reimport `.blend` ich zmetie
  rovnako. Riešenie je rovnaké, len sa zatiaľ neurobilo.
- Ručná úprava gatu priamo v hierarchii sa pri najbližšom behu prepíše. Zmena patrí do JSON‑u.
```

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md docs/decisions/2026-08-24-platform-gaty-v-prefabe.md && git commit -m "docs: record the room platform gates and why they belong in the prefab"
```

---

## Done when

- [ ] `Tools > Feature Flags > Report Room Gates` reports `0 added, 0 retargeted, 0 removed` on both branches
- [ ] no `UNDECIDED` and no `BAD VALUES` section in that report
- [ ] all EditMode tests in `FriWorld.ObjectRegistry.Tests` pass
- [ ] `Tools > Setup Door Gates` is gone; the four `Tools > Feature Flags` items work
- [ ] `Assets/_Game/Editor/RoomPlatforms.json` holds 300 decided areas
- [ ] a door in a `desktopOnly` room does not open in a WebGL play mode session
