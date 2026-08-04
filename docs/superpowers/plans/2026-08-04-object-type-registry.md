# Object Type Registry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the hand-maintained keyword arrays in `GenerateColliders` and `GenerateLayersAndStatic` with an explicit, JSON-backed registry keyed by object type, so an unrecognised name is reported instead of silently inheriting another type's behaviour.

**Architecture:** A new editor-only assembly `FriWorld.ObjectRegistry.Editor` holds the pure key-derivation logic, the JSON registry, the scanner and the report. It is unit-testable because asmdef assemblies can be referenced by the predefined `Assembly-CSharp-Editor` (the reverse is not allowed), so the two existing tools call into it while NUnit tests call it directly. Derivation is driven **only** by the human-approved `ObjectPrefixes.json`; the scanner proposes prefixes but never applies them itself.

**Tech Stack:** Unity 6000.4.11f1, C#, Newtonsoft.Json (`com.unity.nuget.newtonsoft-json` 3.2.1 — required because `JsonUtility` cannot distinguish `null` from `""`), Unity Test Framework 1.6.0 (EditMode).

**Spec:** `docs/superpowers/specs/2026-08-04-object-type-registry-design.md`

---

## File Structure

**Create:**
- `Assets/_Game/Editor/ObjectRegistry/FriWorld.ObjectRegistry.Editor.asmdef` — editor-only assembly, auto-referenced so the predefined editor assembly can use it
- `Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs` — pure derivation, no Unity dependencies
- `Assets/_Game/Editor/ObjectRegistry/TypeRegistry.cs` — data model + JSON load/save preserving `null`
- `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs` — hierarchy walk, prefix proposals, key collection with paths
- `Assets/_Game/Editor/ObjectRegistry/RegistryReport.cs` — report formatting
- `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs` — menu items
- `Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs` — the current keyword arrays, kept only for seeding and the dry-run diff, deleted in Task 11
- `Assets/_Game/Editor/ObjectRegistry/Tests/FriWorld.ObjectRegistry.Tests.asmdef`
- `Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs`
- `Assets/_Game/Editor/ObjectRegistry/Tests/TypeRegistryTests.cs`
- `Assets/_Game/Editor/ObjectPrefixes.json` — generated, human-approved
- `Assets/_Game/Editor/ObjectTypes.json` — generated skeleton, human-filled

**Modify:**
- `Packages/manifest.json` — promote Newtonsoft from transitive to direct dependency
- `Assets/_Game/Editor/GenerateColliders.cs` — read rules from the registry
- `Assets/_Game/Editor/GenerateLayersAndStatic.cs` — read rules from the registry
- `CHANGELOG.md`, `docs/decisions/` — per the project documentation rule in `CLAUDE.md`

**Why JSON files live in `Assets/_Game/Editor/`:** assets under an `Editor` folder are excluded from player builds, so the registry never ships.

---

### Task 1: Assembly and dependency setup

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/FriWorld.ObjectRegistry.Editor.asmdef`
- Create: `Assets/_Game/Editor/ObjectRegistry/Tests/FriWorld.ObjectRegistry.Tests.asmdef`
- Modify: `Packages/manifest.json`

- [ ] **Step 1: Add Newtonsoft as a direct dependency**

Open `Packages/manifest.json` and add this line inside `"dependencies"`, keeping alphabetical order:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1",
```

It is currently only a transitive dependency. Relying on that is fragile — if whichever package pulls it in is removed, this tool stops compiling.

- [ ] **Step 2: Create the tool assembly definition**

`Assets/_Game/Editor/ObjectRegistry/FriWorld.ObjectRegistry.Editor.asmdef`:

```json
{
    "name": "FriWorld.ObjectRegistry.Editor",
    "rootNamespace": "FriWorld.ObjectRegistry",
    "references": [
        "Unity.Plastic.Newtonsoft.Json"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`autoReferenced: true` is what lets `GenerateColliders` (in the predefined `Assembly-CSharp-Editor`) call into this assembly.

- [ ] **Step 3: Create the test assembly definition**

`Assets/_Game/Editor/ObjectRegistry/Tests/FriWorld.ObjectRegistry.Tests.asmdef`:

```json
{
    "name": "FriWorld.ObjectRegistry.Tests",
    "rootNamespace": "FriWorld.ObjectRegistry.Tests",
    "references": [
        "FriWorld.ObjectRegistry.Editor",
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

- [ ] **Step 4: Verify Unity compiles and the assemblies exist**

In Unity: `Assets → Refresh`, wait for compilation, then open `Window → General → Test Runner → EditMode`. Expected: `FriWorld.ObjectRegistry.Tests` appears in the list with no tests under it yet, and the Console has no compile errors.

If `Unity.Plastic.Newtonsoft.Json` fails to resolve, change the reference to `Newtonsoft.Json` — the assembly name differs between package versions. Check which exists with:

```bash
ls E:/UNITY/FriWorld/Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/*.asmdef
```

- [ ] **Step 5: Commit**

```bash
git -C E:/UNITY/FriWorld add Packages/manifest.json "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "chore(registry): add editor assembly and test assembly for the object registry"
```

---

### Task 2: Type key derivation

This is the heart of the system. Pure C#, no Unity types, so it is fully unit-testable.

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_Game/Editor/ObjectRegistry/Tests/ObjectTypeKeyTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class ObjectTypeKeyTests
    {
        static List<string> Prefixes(params string[] p) => new List<string>(p);

        [Test]
        public void StripsPrefixAndTrailingInstanceNumber()
        {
            var key = ObjectTypeKey.Derive("ra000_cleaners_room_ceiling_1",
                                           Prefixes("ra000_cleaners_room"));
            Assert.AreEqual("ceiling", key);
        }

        [Test]
        public void StripsTheRoomsOwnLeadingInstanceNumber()
        {
            // Without this, door_frame fragments into 1_door_frame, 2_door_frame, ...
            var key = ObjectTypeKey.Derive("ra100_corridor_1_door_frame_2",
                                           Prefixes("ra100_corridor"));
            Assert.AreEqual("door_frame", key);
        }

        [Test]
        public void KeepsMultiWordTypesIntact()
        {
            var key = ObjectTypeKey.Derive("rb254_window_1_glass_1", Prefixes("rb254"));
            Assert.AreEqual("window_1_glass", key);
        }

        [Test]
        public void SkipsAPrefixThatWouldLeaveOnlyANumber()
        {
            // "lamp_2" must not become "2" just because a prefix "lamp" exists.
            var key = ObjectTypeKey.Derive("lamp_2", Prefixes("lamp"));
            Assert.AreEqual("lamp", key);
        }

        [Test]
        public void PrefersTheLongestMatchingPrefix()
        {
            var key = ObjectTypeKey.Derive("ra100_corridor_2_radiator_3",
                                           Prefixes("ra100", "ra100_corridor"));
            Assert.AreEqual("radiator", key);
        }

        [Test]
        public void LeavesTheNameAloneWhenNoPrefixMatches()
        {
            var key = ObjectTypeKey.Derive("rb308_nav_1", Prefixes("ra100"));
            Assert.AreEqual("rb308_nav", key);
        }

        [Test]
        public void IsCaseInsensitiveOnThePrefix()
        {
            var key = ObjectTypeKey.Derive("RA100_Corridor_wall_1", Prefixes("ra100_corridor"));
            Assert.AreEqual("wall", key);
        }

        [Test]
        public void HandlesNullAndEmptyNames()
        {
            Assert.AreEqual("", ObjectTypeKey.Derive(null, Prefixes("x")));
            Assert.AreEqual("", ObjectTypeKey.Derive("", Prefixes("x")));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Unity → `Window → General → Test Runner → EditMode → Run All`.
Expected: compile error `The name 'ObjectTypeKey' does not exist`. That counts as the failing state.

- [ ] **Step 3: Implement the derivation**

`Assets/_Game/Editor/ObjectRegistry/ObjectTypeKey.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// Reduces a scene object's name to the type key used to look up its behaviour.
    ///
    /// Four steps: strip the longest matching prefix, strip the room's own leading instance
    /// number, strip the trailing instance number, and whatever remains is the key. Pure C#
    /// with no Unity dependencies so it can be unit-tested directly.
    /// </summary>
    public static class ObjectTypeKey
    {
        /// <summary>
        /// Derives the type key. <paramref name="prefixes"/> must be the approved list from
        /// ObjectPrefixes.json — never a set harvested on the fly, or a prefix that happens to
        /// equal a type word ("wall") would eat part of a multi-word type ("wall_edge").
        /// </summary>
        public static string Derive(string objectName, IReadOnlyList<string> prefixes)
        {
            if (string.IsNullOrEmpty(objectName))
                return string.Empty;

            // Longest first, so "ra100_corridor" wins over "ra100".
            var ordered = new List<string>(prefixes ?? new List<string>());
            ordered.Sort((a, b) => (b ?? "").Length.CompareTo((a ?? "").Length));

            foreach (var prefix in ordered)
            {
                if (string.IsNullOrEmpty(prefix)) continue;
                if (objectName.Length <= prefix.Length + 1) continue;
                if (!objectName.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)) continue;

                string candidate = objectName.Substring(prefix.Length + 1);
                candidate = StripLeadingInt(candidate);
                candidate = StripTrailingInt(candidate);

                // The strip has to leave a word behind. Otherwise "lamp_2" against a prefix
                // "lamp" collapses to "2", and that one key swallows every lamp in the project.
                if (!HasLetter(candidate)) continue;

                return candidate;
            }

            // No prefix applied: only the trailing instance number comes off. A leading number
            // is not stripped here because there is no prefix it could have belonged to.
            return StripTrailingInt(objectName);
        }

        static string StripLeadingInt(string s)
        {
            int underscore = s.IndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(0, underscore)) ? s.Substring(underscore + 1) : s;
        }

        static string StripTrailingInt(string s)
        {
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(underscore + 1)) ? s.Substring(0, underscore) : s;
        }

        static bool AllDigits(string s)
        {
            if (s.Length == 0) return false;
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        static bool HasLetter(string s)
        {
            foreach (char c in s) if (char.IsLetter(c)) return true;
            return false;
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Test Runner → Run All. Expected: 8 passing tests under `ObjectTypeKeyTests`.

- [ ] **Step 5: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "feat(registry): derive object type keys from name, prefix and instance number"
```

---

### Task 3: Registry model and JSON round-trip

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/TypeRegistry.cs`
- Test: `Assets/_Game/Editor/ObjectRegistry/Tests/TypeRegistryTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_Game/Editor/ObjectRegistry/Tests/TypeRegistryTests.cs`:

```csharp
using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class TypeRegistryTests
    {
        [Test]
        public void NullFieldsSurviveTheJsonRoundTrip()
        {
            const string json = @"{ ""types"": [ { ""name"": ""sun_lamp"", ""collider"": null, ""layer"": null, ""occluder"": null } ] }";

            var registry = TypeRegistry.FromJson(json);
            var entry = registry.Find("sun_lamp");

            Assert.IsNotNull(entry);
            Assert.IsNull(entry.collider, "null must stay null, not become an empty string");
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void AFullyFilledEntryCountsAsDecided()
        {
            const string json = @"{ ""types"": [ { ""name"": ""wall"", ""collider"": ""mesh"", ""layer"": ""obstacle"", ""occluder"": ""auto"" } ] }";

            var entry = TypeRegistry.FromJson(json).Find("wall");

            Assert.IsTrue(entry.IsDecided);
            Assert.AreEqual("mesh", entry.collider);
        }

        [Test]
        public void ExplicitNoneIsDecidedAndDifferentFromNull()
        {
            const string json = @"{ ""types"": [ { ""name"": ""Cedulka"", ""collider"": ""none"", ""layer"": ""keep"", ""occluder"": ""no"" } ] }";

            var entry = TypeRegistry.FromJson(json).Find("Cedulka");

            Assert.IsTrue(entry.IsDecided, "'I decided nothing' is not the same as 'I have not decided'");
            Assert.AreEqual("none", entry.collider);
        }

        [Test]
        public void LookupIsExactNotSubstring()
        {
            const string json = @"{ ""types"": [ { ""name"": ""lamp"", ""collider"": ""none"", ""layer"": ""noObstacle"", ""occluder"": ""no"" } ] }";

            var registry = TypeRegistry.FromJson(json);

            Assert.IsNotNull(registry.Find("lamp"));
            Assert.IsNull(registry.Find("sun_lamp"), "substring matching is the bug this replaces");
        }

        [Test]
        public void SeededEntriesAreUndecided()
        {
            var registry = TypeRegistry.FromJson(@"{ ""types"": [] }");

            registry.Seed("new_thing");

            var entry = registry.Find("new_thing");
            Assert.IsNotNull(entry);
            Assert.IsNull(entry.collider);
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void SeedingDoesNotOverwriteAnExistingEntry()
        {
            const string json = @"{ ""types"": [ { ""name"": ""wall"", ""collider"": ""mesh"", ""layer"": ""obstacle"", ""occluder"": ""auto"" } ] }";
            var registry = TypeRegistry.FromJson(json);

            registry.Seed("wall");

            Assert.AreEqual("mesh", registry.Find("wall").collider);
        }

        [Test]
        public void PrefixesRoundTrip()
        {
            const string json = @"{ ""prefixes"": [ ""ra100_corridor"", ""rb254"" ] }";

            var prefixes = TypeRegistry.PrefixesFromJson(json);

            Assert.AreEqual(2, prefixes.Count);
            Assert.Contains("ra100_corridor", prefixes);
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Test Runner → Run All. Expected: compile error `The name 'ObjectRegistry' does not exist`.

- [ ] **Step 3: Implement the registry**

`Assets/_Game/Editor/ObjectRegistry/TypeRegistry.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One type and how the generators should treat it. Null means "not decided yet".</summary>
    public class TypeEntry
    {
        public string name;
        public string collider;   // none | mesh | box | sphere
        public string layer;      // interactable | obstacle | noObstacle | nav | keep
        public string occluder;   // auto | yes | no
        public string @static;    // optional override: yes | no
        public string tag;        // optional override, e.g. "Door"

        /// <summary>
        /// False while any required field is still null. An undecided entry is reported and its
        /// objects are left untouched — the whole point is that "present but blank" must not
        /// quietly mean "no collider, default layer".
        /// </summary>
        [JsonIgnore]
        public bool IsDecided => collider != null && layer != null && occluder != null;
    }

    public class TypeRegistry
    {
        public List<TypeEntry> types = new List<TypeEntry>();

        [JsonIgnore]
        readonly Dictionary<string, TypeEntry> index = new Dictionary<string, TypeEntry>();

        public static ObjectRegistry FromJson(string json)
        {
            var registry = JsonConvert.DeserializeObject<TypeRegistry>(json) ?? new TypeRegistry();
            registry.types ??= new List<TypeEntry>();
            registry.Reindex();
            return registry;
        }

        public static ObjectRegistry Load(string path)
            => File.Exists(path) ? FromJson(File.ReadAllText(path)) : new TypeRegistry();

        public void Save(string path)
        {
            types.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        void Reindex()
        {
            index.Clear();
            foreach (var t in types)
                if (t != null && !string.IsNullOrEmpty(t.name))
                    index[t.name] = t;
        }

        /// <summary>Exact lookup. Never a substring match — that is the bug this replaces.</summary>
        public TypeEntry Find(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey)) return null;
            return index.TryGetValue(typeKey, out var entry) ? entry : null;
        }

        /// <summary>Adds an undecided entry. Existing entries are left alone.</summary>
        public void Seed(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey) || index.ContainsKey(typeKey)) return;
            var entry = new TypeEntry { name = typeKey };
            types.Add(entry);
            index[typeKey] = entry;
        }

        class PrefixFile { public List<string> prefixes = new List<string>(); }

        public static List<string> PrefixesFromJson(string json)
            => (JsonConvert.DeserializeObject<PrefixFile>(json) ?? new PrefixFile()).prefixes
               ?? new List<string>();

        public static List<string> LoadPrefixes(string path)
            => File.Exists(path) ? PrefixesFromJson(File.ReadAllText(path)) : new List<string>();

        public static void SavePrefixes(string path, List<string> prefixes)
        {
            prefixes.Sort(string.CompareOrdinal);
            File.WriteAllText(path,
                JsonConvert.SerializeObject(new PrefixFile { prefixes = prefixes }, Formatting.Indented));
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Test Runner → Run All. Expected: 8 tests from Task 2 plus 7 from this task, all green.

- [ ] **Step 5: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "feat(registry): JSON model where null means undecided, not empty"
```

---

### Task 4: Scanner

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs`

- [ ] **Step 1: Implement the scanner**

`Assets/_Game/Editor/ObjectRegistry/RegistryScanner.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One mesh object found by the scan, with the key it derived to.</summary>
    public struct ScannedObject
    {
        public GameObject gameObject;
        public string typeKey;
        public string path;
    }

    public class ScanResult
    {
        public readonly List<ScannedObject> objects = new List<ScannedObject>();
        /// <summary>Container names that could serve as prefixes. Proposals only — never applied.</summary>
        public readonly List<string> proposedPrefixes = new List<string>();
        /// <summary>Proposed prefixes that are also a derived type key. Applying one would eat a type.</summary>
        public readonly List<string> riskyPrefixes = new List<string>();
    }

    public static class RegistryScanner
    {
        /// <summary>
        /// Walks the subtree and derives a type key for every mesh object, using the approved
        /// prefix list only. Container names are collected separately as proposals for the
        /// human to review — a prefix equal to a type word ("wall") would silently eat part of
        /// a multi-word type ("wall_edge"), which no automatic guard can catch.
        /// </summary>
        public static ScanResult Scan(GameObject root, IReadOnlyList<string> approvedPrefixes)
        {
            var result = new ScanResult();
            var containers = new HashSet<string>();
            var keys = new HashSet<string>();

            void Walk(Transform t)
            {
                if (t.childCount > 0) containers.Add(StripTrailingInt(t.name));

                if (t.GetComponent<MeshRenderer>() != null)
                {
                    string key = ObjectTypeKey.Derive(t.name, approvedPrefixes);
                    keys.Add(key);
                    result.objects.Add(new ScannedObject
                    {
                        gameObject = t.gameObject,
                        typeKey = key,
                        path = PathOf(t),
                    });
                }

                foreach (Transform child in t) Walk(child);
            }

            Walk(root.transform);

            foreach (var c in containers)
            {
                if (string.IsNullOrEmpty(c)) continue;
                result.proposedPrefixes.Add(c);
                if (keys.Contains(c)) result.riskyPrefixes.Add(c);
            }
            result.proposedPrefixes.Sort(string.CompareOrdinal);
            result.riskyPrefixes.Sort(string.CompareOrdinal);
            return result;
        }

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts.ToArray());
        }

        static string StripTrailingInt(string s)
        {
            int u = s.LastIndexOf('_');
            if (u <= 0) return s;
            string tail = s.Substring(u + 1);
            if (tail.Length == 0) return s;
            foreach (char c in tail) if (!char.IsDigit(c)) return s;
            return s.Substring(0, u);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Unity → `Assets → Refresh`. Expected: no compile errors in the Console.

- [ ] **Step 3: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "feat(registry): scanner that derives keys and proposes prefixes without applying them"
```

---

### Task 5: Report

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/RegistryReport.cs`

- [ ] **Step 1: Implement the report**

`Assets/_Game/Editor/ObjectRegistry/RegistryReport.cs`:

```csharp
using System.Collections.Generic;
using System.Text;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// Formats what the scan found. Deliberately short: a key with its count and at most a few
    /// example paths, not every hit. The whole system only works while this stays readable — a
    /// thousand-line dump gets skipped, and then nothing is being caught at all.
    /// </summary>
    public static class RegistryReport
    {
        public const int MaxExamplePaths = 3;

        public static string Build(ScanResult scan, TypeRegistry registry)
        {
            var unknown = new Dictionary<string, List<string>>();
            var undecided = new Dictionary<string, List<string>>();
            var used = new HashSet<string>();

            foreach (var o in scan.objects)
            {
                var entry = registry.Find(o.typeKey);
                if (entry == null) Add(unknown, o.typeKey, o.path);
                else
                {
                    used.Add(o.typeKey);
                    if (!entry.IsDecided) Add(undecided, o.typeKey, o.path);
                }
            }

            var dead = new List<string>();
            foreach (var t in registry.types)
                if (t != null && !string.IsNullOrEmpty(t.name) && !used.Contains(t.name))
                    dead.Add(t.name);
            dead.Sort(string.CompareOrdinal);

            // A key that still carries a room code means no prefix covered this object, so the
            // key is the whole name and will never match anything. That is a missing prefix,
            // not a missing type — a different fix, so it gets its own section.
            var unresolvedKey = new Dictionary<string, List<string>>();
            foreach (var o in scan.objects)
                if (LooksUnstripped(o.typeKey)) Add(unresolvedKey, o.typeKey, o.path);

            var sb = new StringBuilder();
            sb.AppendLine("[ObjectRegistry] scanned " + scan.objects.Count + " mesh objects");
            Section(sb, "UNKNOWN types (not in the registry — likely a naming mistake)", unknown);
            Section(sb, "UNDECIDED types (in the registry, fields still null)", undecided);
            Section(sb, "UNSTRIPPED keys (a prefix is missing from ObjectPrefixes.json)", unresolvedKey);

            if (scan.riskyPrefixes.Count > 0)
            {
                sb.AppendLine("  RISKY proposed prefixes (also a type key — do NOT approve these):");
                foreach (var p in scan.riskyPrefixes) sb.AppendLine("    " + p);
            }

            if (dead.Count > 0)
            {
                sb.AppendLine("  DEAD registry entries (" + dead.Count + ", nothing uses them):");
                foreach (var d in dead) sb.AppendLine("    " + d);
            }

            if (unknown.Count == 0 && undecided.Count == 0)
                sb.AppendLine("  every scanned object resolved to a decided type");

            return sb.ToString();
        }

        /// <summary>True when the key still starts with a room code such as ra100 or rb308.</summary>
        static bool LooksUnstripped(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            int underscore = key.IndexOf('_');
            string head = underscore > 0 ? key.Substring(0, underscore) : key;
            if (head.Length < 3) return false;
            if (char.ToLowerInvariant(head[0]) != 'r' || !char.IsLetter(head[1])) return false;
            for (int i = 2; i < head.Length; i++) if (!char.IsDigit(head[i])) return false;
            return true;
        }

        static void Add(Dictionary<string, List<string>> map, string key, string path)
        {
            if (!map.TryGetValue(key, out var list)) { list = new List<string>(); map[key] = list; }
            list.Add(path);
        }

        static void Section(StringBuilder sb, string title, Dictionary<string, List<string>> map)
        {
            if (map.Count == 0) return;
            sb.AppendLine("  " + title + ": " + map.Count);
            foreach (var kv in map)
            {
                sb.AppendLine("    " + kv.Key + "  x" + kv.Value.Count);
                for (int i = 0; i < kv.Value.Count && i < MaxExamplePaths; i++)
                    sb.AppendLine("        " + kv.Value[i]);
                if (kv.Value.Count > MaxExamplePaths)
                    sb.AppendLine("        … and " + (kv.Value.Count - MaxExamplePaths) + " more");
            }
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Unity → `Assets → Refresh`. Expected: no compile errors.

- [ ] **Step 3: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "feat(registry): short, path-carrying report of unknown and undecided types"
```

---

### Task 6: Menu commands

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`

- [ ] **Step 1: Implement the menu items**

`Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`:

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    public static class ObjectRegistryMenu
    {
        public const string PrefixesPath = "Assets/_Game/Editor/ObjectPrefixes.json";
        public const string TypesPath    = "Assets/_Game/Editor/ObjectTypes.json";

        [MenuItem("Tools/Object Registry/Report On Selection")]
        static void Report()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;
            Debug.Log(RegistryReport.Build(scan, registry));
        }

        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection")]
        static void Seed()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int added = 0;
            foreach (var o in scan.objects)
                if (registry.Find(o.typeKey) == null) { registry.Seed(o.typeKey); added++; }

            registry.Save(TypesPath);
            AssetDatabase.Refresh();
            Debug.Log("[ObjectRegistry] seeded " + added + " undecided types into " + TypesPath
                    + "\nFill them in — until then their objects are left untouched.");
        }

        [MenuItem("Tools/Object Registry/Propose Prefixes From Selection")]
        static void ProposePrefixes()
        {
            if (!TryScanSelection(out var scan, out _)) return;

            var existing = TypeRegistry.LoadPrefixes(PrefixesPath);
            var known = new HashSet<string>(existing);
            var fresh = new List<string>();
            foreach (var p in scan.proposedPrefixes)
                if (!known.Contains(p) && !scan.riskyPrefixes.Contains(p)) fresh.Add(p);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[ObjectRegistry] " + fresh.Count + " prefix proposals (NOT written — review, then add by hand):");
            foreach (var p in fresh) sb.AppendLine("    " + p);
            if (scan.riskyPrefixes.Count > 0)
            {
                sb.AppendLine("  withheld as risky (each is also a type key):");
                foreach (var p in scan.riskyPrefixes) sb.AppendLine("    " + p);
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Object Registry/Report On Selection", true)]
        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection", true)]
        [MenuItem("Tools/Object Registry/Propose Prefixes From Selection", true)]
        static bool ValidateSelection() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

        internal static bool TryScanSelection(out ScanResult scan, out TypeRegistry registry)
        {
            scan = null;
            registry = TypeRegistry.Load(TypesPath);

            var roots = Selection.gameObjects;
            if (roots == null || roots.Length == 0)
            {
                Debug.LogWarning("[ObjectRegistry] Nothing selected in the Hierarchy.");
                return false;
            }

            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            scan = new ScanResult();
            foreach (var root in roots)
            {
                if (root == null) continue;
                var partial = RegistryScanner.Scan(root, prefixes);
                scan.objects.AddRange(partial.objects);
                foreach (var p in partial.proposedPrefixes)
                    if (!scan.proposedPrefixes.Contains(p)) scan.proposedPrefixes.Add(p);
                foreach (var p in partial.riskyPrefixes)
                    if (!scan.riskyPrefixes.Contains(p)) scan.riskyPrefixes.Add(p);
            }
            return true;
        }
    }
}
```

- [ ] **Step 2: Create the two empty data files**

`Assets/_Game/Editor/ObjectPrefixes.json`:

```json
{
  "prefixes": []
}
```

`Assets/_Game/Editor/ObjectTypes.json`:

```json
{
  "types": []
}
```

- [ ] **Step 3: Verify end to end on a small subtree**

In Unity, select `FriBuilding/Objects/lamp` in the Hierarchy (or any small subtree), then run
`Tools → Object Registry → Report On Selection`.

Expected: a log listing `lamp` (and neighbours) under **UNKNOWN types**, each with example
hierarchy paths, because the registry is still empty. That is the correct starting state.

- [ ] **Step 4: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry" "Assets/_Game/Editor/ObjectPrefixes.json" "Assets/_Game/Editor/ObjectTypes.json"
git -C E:/UNITY/FriWorld commit -m "feat(registry): menu commands for report, seeding and prefix proposals"
```

---

### Task 7: Capture the current keyword rules for seeding and diffing

The existing arrays must survive long enough to seed the registry and to diff against. They are deleted in Task 11.

**Files:**
- Create: `Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs`

- [ ] **Step 1: Copy the current rules into the new assembly**

`Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// The keyword arrays as they exist in GenerateColliders and GenerateLayersAndStatic today,
    /// kept only to seed the registry and to diff old behaviour against new. Deleted once the
    /// migration is verified — see the plan's Task 11.
    /// </summary>
    public static class LegacyKeywordRules
    {
        public static readonly string[] ColliderMesh = {
            "door_frame","wall","outer_wall","big_window","ceiling","fence","roof","sofa","table",
            "parapet","pillar","curb","tree_pot","rock","tree_bot","sun_block","nav","barrier" };

        public static readonly string[] ColliderBox = {
            "window","glass","door","thick_door","foor","door_slide","vent","box","couch","counter",
            "counter_bar","chair","desk","board","calcetto","trash_bin","handicap_machine",
            "bycicle_shelter_rack","plant_tree_pot","ventilator","billboard","drainage","preform",
            "ramp","trash_container","gazebo_bench" };

        public static readonly string[] ColliderIgnore = {
            "lamp","doorstep","room_sign","thick_door_headlight","sign","poster","e_plug",
            "e_plug_cover","hydrant","construction","bell_thingy","radiator_pipe","drain","support",
            "radiator","e_box","bush","shrub","plant_pot","tree_top" };

        public static readonly string[] LayerInteractable = { "door", "door_slide" };

        public static readonly string[] LayerObstacle = {
            "door_frame","thick_door","foor","wall","outer_wall","big_window","fence","sofa","window",
            "pillar","couch","counter","desk","trash_bin","barrier","pot_tree","rock","tree_bot",
            "plant_tree_pot","ventilator","billboard","preform","ramp","trash_container","sun_block" };

        public static readonly string[] LayerNoObstacle = {
            "lamp","doorstep","thick_door_headlight","room_sign","sign","poster","e_plug",
            "e_plug_cover","hydrant","construction","bell_thingy","radiator_pipe","drain","support",
            "ceiling","roof","table","parapet","radiator","e_box","vent","box","counter_bar","chair",
            "board","bycicle_shelter_rack","gazebo_bench","glass","handicap_machine","calcetto",
            "plant_pot","bush","shrub","tree_top" };

        public static readonly string[] LayerNav = { "nav", "drainage", "curb" };

        public static readonly string[] OccluderKeywords = {
            "wall","outer_wall","ceiling","roof","nav","door_frame","foor","pillar" };

        /// <summary>Longest token-sequence match, mirroring the old resolver.</summary>
        public static string BestMatch(string objectName, string[] keywords)
        {
            var nameTokens = Tokenize(objectName);
            string best = null;
            int bestLen = 0;
            foreach (var kw in keywords)
            {
                var kwTokens = Tokenize(kw);
                if (kwTokens.Count == 0 || kwTokens.Count <= bestLen) continue;
                if (ContainsSequence(nameTokens, kwTokens)) { best = kw; bestLen = kwTokens.Count; }
            }
            return best;
        }

        public static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return tokens;
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        public static bool ContainsSequence(List<string> haystack, List<string> needle)
        {
            if (needle.Count > haystack.Count) return false;
            for (int i = 0; i <= haystack.Count - needle.Count; i++)
            {
                bool all = true;
                for (int j = 0; j < needle.Count; j++)
                    if (!string.Equals(haystack[i + j], needle[j], StringComparison.OrdinalIgnoreCase))
                    { all = false; break; }
                if (all) return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Unity → `Assets → Refresh`. Expected: no compile errors.

- [ ] **Step 3: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "chore(registry): capture today's keyword rules for seeding and diffing"
```

---

### Task 8: Seed from legacy rules, and the dry-run diff

**Files:**
- Modify: `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`

- [ ] **Step 1: Add the seeding-with-legacy-values and diff commands**

Append these methods inside the `ObjectRegistryMenu` class, before the closing brace:

```csharp
        [MenuItem("Tools/Object Registry/Seed From Legacy Keyword Rules")]
        static void SeedFromLegacy()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int seeded = 0, guessed = 0;
            var seen = new HashSet<string>();
            foreach (var o in scan.objects)
            {
                if (!seen.Add(o.typeKey)) continue;
                if (registry.Find(o.typeKey) != null) continue;

                registry.Seed(o.typeKey);
                seeded++;

                var entry = registry.Find(o.typeKey);
                string collider = LegacyCollider(o.typeKey);
                string layer = LegacyLayer(o.typeKey);
                if (collider != null && layer != null)
                {
                    entry.collider = collider;
                    entry.layer = layer;
                    entry.occluder = "auto";
                    guessed++;
                }
            }

            registry.Save(TypesPath);
            AssetDatabase.Refresh();
            Debug.Log("[ObjectRegistry] seeded " + seeded + " types, of which " + guessed
                    + " were pre-filled from the old keyword rules.\n"
                    + (seeded - guessed) + " remain undecided (null) and need a decision.");
        }

        static string LegacyCollider(string typeKey)
        {
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderIgnore) != null) return "none";
            var mesh = LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderMesh);
            var box  = LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderBox);
            if (mesh == null && box == null) return null;
            if (mesh == null) return "box";
            if (box == null) return "mesh";
            return mesh.Length >= box.Length ? "mesh" : "box";
        }

        static string LegacyLayer(string typeKey)
        {
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerInteractable) != null) return "interactable";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerNoObstacle) != null) return "noObstacle";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerObstacle) != null) return "obstacle";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerNav) != null) return "nav";
            return null;
        }

        [MenuItem("Tools/Object Registry/Dry Run Diff (old rules vs registry)")]
        static void DryRunDiff()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int same = 0, changed = 0, nowUnhandled = 0;
            var lines = new System.Text.StringBuilder();

            foreach (var o in scan.objects)
            {
                string oldCollider = LegacyCollider(LegacyKeyOf(o.gameObject.name)) ?? "none";
                string oldLayer    = LegacyLayer(LegacyKeyOf(o.gameObject.name)) ?? "keep";

                var entry = registry.Find(o.typeKey);
                if (entry == null || !entry.IsDecided)
                {
                    nowUnhandled++;
                    continue;
                }

                if (entry.collider == oldCollider && entry.layer == oldLayer) { same++; continue; }

                changed++;
                if (changed <= 40)
                    lines.AppendLine("    " + o.path
                                   + "\n        old: " + oldCollider + " / " + oldLayer
                                   + "   new: " + entry.collider + " / " + entry.layer);
            }

            Debug.Log("[ObjectRegistry] dry run over " + scan.objects.Count + " objects\n"
                    + "  identical:   " + same + "\n"
                    + "  CHANGED:     " + changed + "   (each is either a fix or a regression — check them)\n"
                    + "  unhandled:   " + nowUnhandled + "   (unknown or undecided; left untouched)\n"
                    + (changed > 0 ? "  first " + Mathf.Min(changed, 40) + " changes:\n" + lines : ""));
        }

        /// <summary>The old rules matched against the raw object name, not a derived key.</summary>
        static string LegacyKeyOf(string objectName) => objectName;
```

Add `using System.Collections.Generic;` and `using UnityEngine;` at the top of the file if the compiler asks — both should already be present from Task 6.

- [ ] **Step 2: Run the seeding on the whole building**

In Unity select the `FriBuilding` root in the Hierarchy, then
`Tools → Object Registry → Propose Prefixes From Selection`.

Copy the proposals you accept into `Assets/_Game/Editor/ObjectPrefixes.json`. **Do not accept
anything listed under "withheld as risky".**

Then run `Tools → Object Registry → Seed From Legacy Keyword Rules`.

Expected: `ObjectTypes.json` fills with roughly 500–600 entries, most pre-filled, the rest
`null`. Confirm in the log that the pre-filled count is the large majority.

- [ ] **Step 3: Run the dry run and read every change**

`Tools → Object Registry → Dry Run Diff (old rules vs registry)`.

Expected: `identical` dominates. Every entry under `CHANGED` must be classified by hand as
either a fix (e.g. `window_1_glass` no longer treated as `window`) or a regression. Do not
continue past this step with unexplained changes.

- [ ] **Step 4: Commit the data files**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/ObjectPrefixes.json" "Assets/_Game/Editor/ObjectTypes.json" "Assets/_Game/Editor/ObjectRegistry"
git -C E:/UNITY/FriWorld commit -m "feat(registry): seed the registry from the legacy rules and add the dry-run diff"
```

---

### Task 9: GenerateColliders reads the registry

**Files:**
- Modify: `Assets/_Game/Editor/GenerateColliders.cs`

- [ ] **Step 1: Replace rule resolution with a registry lookup**

In `GenerateFromSelectedHierarchy`, replace the `ResolveRuleForName(...)` call and the
`switch (resolved.Rule)` block with:

```csharp
                string typeKey = FriWorld.ObjectRegistry.ObjectTypeKey.Derive(go.name, prefixes);
                var entry = registry.Find(typeKey);

                if (entry == null || !entry.IsDecided)
                {
                    // Unknown or undecided: leave the object exactly as it is and report it.
                    // Never fall back to a similar name — that is the failure this replaces.
                    unresolved.Add(typeKey + "  " + GetHierarchyPath(go.transform));
                    continue;
                }

                switch (entry.collider)
                {
                    case "none":
                        outdatedRemoved += RemoveManagedColliders(go, true, true, true);
                        ignored++;
                        break;

                    case "mesh":
                        outdatedRemoved += RemoveManagedColliders(go, false, true, true);
                        if (go.GetComponent<MeshCollider>() == null) { Undo.AddComponent<MeshCollider>(go); meshAdded++; }
                        else alreadyHad++;
                        break;

                    case "box":
                        outdatedRemoved += RemoveManagedColliders(go, true, false, true);
                        BoxCollider box = go.GetComponent<BoxCollider>();
                        if (box == null) { box = Undo.AddComponent<BoxCollider>(go); boxAdded++; }
                        else alreadyHad++;
                        if (TryGetLocalBounds(go, out Bounds boxBounds)) { box.center = boxBounds.center; box.size = boxBounds.size; }
                        break;

                    case "sphere":
                        outdatedRemoved += RemoveManagedColliders(go, true, true, false);
                        SphereCollider sphere = go.GetComponent<SphereCollider>();
                        if (sphere == null) { sphere = Undo.AddComponent<SphereCollider>(go); sphereAdded++; }
                        else alreadyHad++;
                        if (TryGetLocalBounds(go, out Bounds sphereBounds)) { sphere.center = sphereBounds.center; sphere.radius = sphereBounds.extents.magnitude; }
                        break;

                    default:
                        unresolved.Add(typeKey + "  (unrecognised collider value '" + entry.collider + "')");
                        break;
                }
```

Declare these before the loop, next to the other counters:

```csharp
        var prefixes = FriWorld.ObjectRegistry.TypeRegistry.LoadPrefixes(
            FriWorld.ObjectRegistry.ObjectRegistryMenu.PrefixesPath);
        var registry = FriWorld.ObjectRegistry.TypeRegistry.Load(
            FriWorld.ObjectRegistry.ObjectRegistryMenu.TypesPath);
        List<string> unresolved = new List<string>();
```

And replace the old `unmatchedNames` warning block at the end with:

```csharp
        if (unresolved.Count > 0)
        {
            Debug.LogWarning("[GenerateColliders] " + unresolved.Count
                + " objects were left untouched because their type is unknown or undecided:\n"
                + string.Join("\n", unresolved.ToArray()));
        }
```

- [ ] **Step 2: Verify on a subtree**

Select `FriBuilding/Objects/lamp`, run `Tools → Colliders → Generate From Name Keywords`.

Expected: the summary log reports colliders handled, and any object whose type is still `null`
in the registry appears in the warning rather than silently getting a collider.

- [ ] **Step 3: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/GenerateColliders.cs"
git -C E:/UNITY/FriWorld commit -m "feat(colliders): resolve collider rules from the object registry"
```

---

### Task 10: GenerateLayersAndStatic reads the registry

**Files:**
- Modify: `Assets/_Game/Editor/GenerateLayersAndStatic.cs`

- [ ] **Step 1: Replace rule resolution with a registry lookup**

Keep `TryGetOverrideMatch` — the `UNO`/`UYO` name overrides still take precedence and the
registry cannot express a per-instance exception.

Replace the `ResolveRuleForName(...)` call and the `switch (resolved.Rule)` block with:

```csharp
                int targetLayer;
                bool targetStatic;
                string resolvedSource;

                if (TryGetOverrideMatch(go.name, out RuleMatch overrideMatch))
                {
                    // UNO / UYO in the name win over the registry.
                    targetLayer = overrideMatch.Rule == LayerRule.Obstacle
                        ? layerMap[ObstacleLayerName] : layerMap[NoObstacleLayerName];
                    targetStatic = true;
                    resolvedSource = "name override " + overrideMatch.Keyword;
                }
                else
                {
                    string typeKey = FriWorld.ObjectRegistry.ObjectTypeKey.Derive(go.name, prefixes);
                    var entry = registry.Find(typeKey);
                    if (entry == null || !entry.IsDecided)
                    {
                        unresolved.Add(typeKey + "  " + GetHierarchyPath(go.transform));
                        continue;
                    }

                    switch (entry.layer)
                    {
                        case "interactable": targetLayer = layerMap[InteractableLayerName]; targetStatic = false; break;
                        case "obstacle":     targetLayer = layerMap[ObstacleLayerName];     targetStatic = true;  break;
                        case "noObstacle":   targetLayer = layerMap[NoObstacleLayerName];   targetStatic = true;  break;
                        case "nav":          targetLayer = layerMap[NavLayerName];          targetStatic = true;  break;
                        case "keep":         targetLayer = go.layer;                        targetStatic = false; break;
                        default:
                            unresolved.Add(typeKey + "  (unrecognised layer value '" + entry.layer + "')");
                            continue;
                    }

                    if (entry.@static == "yes") targetStatic = true;
                    else if (entry.@static == "no") targetStatic = false;

                    resolvedSource = entry.layer;
                    currentEntry = entry;
                }
```

Declare before the loop:

```csharp
        var prefixes = FriWorld.ObjectRegistry.TypeRegistry.LoadPrefixes(
            FriWorld.ObjectRegistry.ObjectRegistryMenu.PrefixesPath);
        var registry = FriWorld.ObjectRegistry.TypeRegistry.Load(
            FriWorld.ObjectRegistry.ObjectRegistryMenu.TypesPath);
        List<string> unresolved = new List<string>();
        FriWorld.ObjectRegistry.TypeEntry currentEntry = null;
```

- [ ] **Step 2: Route the occluder decision through the entry**

Replace the `desiredFlags` assignment with:

```csharp
                StaticEditorFlags desiredFlags = (StaticEditorFlags)0;
                if (targetStatic)
                {
                    bool occluder;
                    if (currentEntry != null && currentEntry.occluder == "yes") occluder = true;
                    else if (currentEntry != null && currentEntry.occluder == "no") occluder = false;
                    else occluder = ShouldBeOccluder(go);   // "auto", and the path taken by UNO/UYO

                    desiredFlags = occluder ? AllStaticFlags : OccludeeOnlyFlags;
                }
```

`ShouldBeOccluder` stays exactly as it is — the material and size checks from commit `3f7813b`
are what `"auto"` means.

- [ ] **Step 3: Report the unresolved objects**

Add before the final `Debug.Log` summary:

```csharp
        if (unresolved.Count > 0)
        {
            Debug.LogWarning("[GenerateLayersAndStatic] " + unresolved.Count
                + " objects were left untouched because their type is unknown or undecided:\n"
                + string.Join("\n", unresolved.ToArray()));
        }
```

- [ ] **Step 4: Verify on a subtree**

Select `FriBuilding/Objects/lamp`, run `Tools → Layers → Assign Layers And Static From Keywords`.

Expected: layers and static flags applied for decided types; undecided ones listed in the
warning and left untouched. Objects named with `UNO`/`UYO` still follow the override.

- [ ] **Step 5: Commit**

```bash
git -C E:/UNITY/FriWorld add "Assets/_Game/Editor/GenerateLayersAndStatic.cs"
git -C E:/UNITY/FriWorld commit -m "feat(layers): resolve layer, static and occluder rules from the object registry"
```

---

### Task 11: Remove the keyword arrays and document

Only after Task 8's dry run has been read and every change classified.

**Files:**
- Modify: `Assets/_Game/Editor/GenerateColliders.cs`
- Modify: `Assets/_Game/Editor/GenerateLayersAndStatic.cs`
- Delete: `Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs`
- Modify: `Assets/_Game/Editor/ObjectRegistry/ObjectRegistryMenu.cs`
- Modify: `CHANGELOG.md`, `CLAUDE.md`
- Create: `docs/decisions/2026-08-04-object-type-registry.md`

- [ ] **Step 1: Delete the dead keyword code**

From `GenerateColliders.cs` remove: `MeshColliderKeywords`, `BoxColliderKeywords`,
`SphereColliderKeywords`, `IgnoreKeywords`, `ColliderRule`, `RuleMatch`, `KeywordEntry`,
`ResolveRuleForName`, `CollectMatches`, `TryAddMatches`, `IsBetterMatch`, `GetRulePriority`,
`ContainsTokenSequence`, `AreTokensEqual`, `BuildKeywordEntries`, `AddKeywordEntries`,
`Tokenize`, and the `Tools/Colliders/Validate Keyword Integrity` menu item.

From `GenerateLayersAndStatic.cs` remove: `InteractableKeywords`, `ObstacleKeywords`,
`NoObstacleKeywords`, `NavKeywords`, `OccluderKeywords`, `IsOccluderName`, `LayerRule`,
`ResolveRuleForName`, `CollectMatches`, `TryAddMatches`, `IsBetterMatch`, `GetRulePriority`,
`BuildKeywordEntries`, `AddKeywordEntries`, and the
`Tools/Layers/Validate Layer Keyword Integrity` menu item.

Keep in `GenerateLayersAndStatic.cs`: `TryGetOverrideMatch`, `TokenizeCaseSensitive`,
`ShouldBeOccluder`, `EnsureObstacleNavMeshModifier`, `TryGetRequiredLayers`,
`GetHierarchyPath`, `IsTagDefined`, and the layer/tag name constants — all still used.

`ShouldBeOccluder` calls `IsOccluderName`, so fold the keyword check out of it: `"auto"` now
means the material and size checks only, since the name has already been resolved via the
registry. Replace its first lines with:

```csharp
    public static bool ShouldBeOccluder(GameObject go)
    {
        if (go == null) return false;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return false;
```

- [ ] **Step 2: Delete the legacy rules file and its menu commands**

```bash
rm "E:/UNITY/FriWorld/Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs"
rm "E:/UNITY/FriWorld/Assets/_Game/Editor/ObjectRegistry/LegacyKeywordRules.cs.meta"
```

From `ObjectRegistryMenu.cs` remove `SeedFromLegacy`, `LegacyCollider`, `LegacyLayer`,
`DryRunDiff` and `LegacyKeyOf`. Keep `Report`, `Seed`, `ProposePrefixes` and
`TryScanSelection` — those are the ongoing workflow.

- [ ] **Step 3: Verify everything still compiles and the tools still run**

Unity → `Assets → Refresh`. Expected: no compile errors.

Select `FriBuilding`, run both tools. Expected: same counts as the last run in Tasks 9 and 10.

- [ ] **Step 4: Run the full test suite**

Test Runner → EditMode → Run All. Expected: all tests from Tasks 2 and 3 green.

- [ ] **Step 5: Update the documentation**

Add to `CHANGELOG.md` under `## [Unreleased]` → `### Changed`:

```markdown
- Collidery a vrstvy sa už neurčujú podľa kľúčových slov v kóde, ale podľa registra
  typov v `ObjectPrefixes.json` a `ObjectTypes.json`. Neznámy alebo nevyplnený typ sa
  nahlási a objekt zostane nedotknutý — namiesto toho, aby ticho prevzal vlastnosti
  podobne pomenovaného typu.
```

Create `docs/decisions/2026-08-04-object-type-registry.md` with sections **Kontext →
Rozhodnutie → Dôsledky**, covering: why substring matching cannot report an unknown name;
the four-step derivation and the guard that stops `lamp_2` collapsing to `2`; why `null`
must differ from `"none"`; and why the scanner only proposes prefixes.

Add to `CLAUDE.md` under the platform/conventions section:

```markdown
- Collidery a vrstvy riadi register v `Assets/_Game/Editor/ObjectTypes.json`, nie kľúčové
  slová v kóde. Nový objekt sa musí do registra doplniť, inak sa nahlási a nechá tak.
```

- [ ] **Step 6: Commit**

```bash
git -C E:/UNITY/FriWorld add -u
git -C E:/UNITY/FriWorld add docs/decisions
git -C E:/UNITY/FriWorld commit -m "refactor(editor): drop the keyword arrays now that the registry drives both tools"
```

---

## Verification checklist

- [ ] `sun_lamp` added to the project with no registry entry is **reported**, and receives no collider and no layer change
- [ ] `window_1_glass` resolves to its own entry, not to `window`
- [ ] A type seeded but left `null` is reported on every run and its objects stay untouched
- [ ] `UNO` / `UYO` in a name still override the registry
- [ ] A prefix that is also a type key is withheld from the proposal list as risky
- [ ] An object whose key still starts with a room code appears under UNSTRIPPED, not UNKNOWN
- [ ] EditMode tests pass
