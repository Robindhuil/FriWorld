# Character Customization — implementačný plán

> **VYKONANÉ — 2026-08-29. Tento plán nespúšťaj znova.** Úlohy 1 až 15 sú hotové a v repozitári;
> neodškrtnuté checkboxy nižšie sú plán tak, ako bol napísaný, nie zvyšná práca. Ponechané kvôli
> úvahám a kódu, ktoré sú záznamom o tom, ako sa systém staval.
>
> Kde sa to od plánu odchýlilo:
>
> | plán | výsledok |
> |---|---|
> | `Prefabs/Character/char_base_male.prefab` | `Prefabs/npc/character_male.prefab` — použili sa cesty, ktoré existujú |
> | úloha 15 prepíše `NPCSpawner` | pribudol nový `CharacterNpcSpawner` + `NpcWander`; starý je vypnutý, nie zmazaný |
> | colorway je balík farieb pre triedu | paleta patrí **slotu** `(trieda, kľúč)`, colorway je jedna farba |
> | — | pribudla **výška** ako losovaná vlastnosť (`BodySize`, uniformný scale) |
> | — | pribudol `CharacterGridSpawner` na vizuálnu kontrolu bez hry |
> | — | bake pečie telá, ktoré existujú; chýbajúce hlási, ale neblokuje |
>
> **Testy:** 66 NUnit EditMode testov je napísaných, ale **cez Test Runner neboli spustené** — MCP
> naň nemá nástroj a reflexia do testovacej assembly harness zhadzuje. Tie isté tvrdenia prešli
> priamo cez verejné API, spolu 138 + 28 + 14 + 16 + 11 kontrol bez zlyhania. Formálne zelené
> kolečko treba dať ručne: **Window → General → Test Runner → EditMode → Run All**.
>
> **Nedokončené:** ženské telo, meranie výkonu na webe (úloha 16), zmazanie starého spawnera
> a `student_*.fbx`.

> **Pre agentov:** POVINNÁ SUB-SKILL — na vykonanie použi `superpowers:subagent-driven-development`
> (odporúčané) alebo `superpowers:executing-plans`, úloha po úlohe. Kroky sú checkboxy (`- [ ]`).

**Verzia projektu pri písaní:** 0.1.1-alpha · **Dátum:** 2026-08-28 · **Stav:** vykonané, viď banner vyššie

**Cieľ:** NPC sa skladá za behu z presetov a farieb podľa pravidiel v JSON registroch,
namiesto 30 samostatných FBX modelov.

**Architektúra:** Tri ručne editované JSON registre v `Assets/_Game/Editor/` sú zdroj pravdy.
Editor krok ich skompiluje do `CharacterCatalog.asset` s vyriešenými referenciami na materiály
a prefaby a s mapou materiálových slotov kľúčovanou menom objektu. Za behu `CharacterRandomizer`
vyberie vzhľad zo seedu, `CharacterBuilder` odstráni nevybrané presety aj zakryté sekcie kože
a prepíše `sharedMaterials`. Žiadne parsovanie reťazcov a žiadne inštancie materiálov za behu.

**Tech stack:** Unity 6000.4.11f1, URP, C# 9, Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`
3.2.2, už v `Packages/manifest.json`), Unity Test Framework (NUnit, EditMode).

**Spec:** [`docs/superpowers/specs/2026-08-28-character-customization-design.md`](../specs/2026-08-28-character-customization-design.md)

---

## Než začneš

### Jazyk

Kód, komentáre a commit messages **po anglicky**. `CHANGELOG.md`, `docs/decisions/`
a `displayName` v registroch **po slovensky**. Úloha 15 dáva presné texty.

### Git

Vetva je `master`. **Nikdy `git add -A`** — repozitár máva rozrobené zmeny používateľa.
Každý commit stageuje konkrétne cesty, ktoré sú v kroku vypísané. Push len na vyžiadanie.

### Spúšťanie testov

Unity → **Window → General → Test Runner → EditMode → Run All**. Cez Unity MCP sa to dá
spustiť aj bez okna; názov nástroja si over v danej session, nie je stabilný naprieč verziami.

Po každej zmene skriptu na disku daj `Assets/Refresh` a počkaj na dokompilovanie
(`Unity_ManageEditor` → `GetState` → `IsCompiling`), inak testy bežia proti starému kódu.

### Čo blokuje čo

Úlohy **1–9** sú čistý C# a dajú sa spraviť **bez akéhokoľvek modelu**. Netreba čakať na Blender.

Úlohy **10–16** potrebujú mužské aj ženské telo. Mužské už existuje ako
`Assets/_Game/Prefabs/npc/character_male.prefab` — tenká obálka nad `Assets/3Dmodels/Npc/npc.blend`,
z ktorej ide celý obsah. Na ženské zatiaľ stačia **placeholder kocky** so správnymi menami
objektov a správne pomenovanými materiálmi; finálne meshe môžu prísť neskôr, systém
ich nerozozná.

Prefab nechaj obálkou nad `.blend`, neunpackuj ho. Nič v pipeline doňho nezapisuje — bake
len číta mená a builder pracuje až za behu na inštancii — takže reimport modelu nemá čo
zmiesť a nové oblečenie z Blenderu sa objaví samo.

### Prerekvizita z Blenderu (úloha 0)

Toto nie je kód, ale bez toho úlohy 10+ nemajú na čom bežať.

- [ ] `character_male` **hotové**. Ženský náprotivok v tej istej štruktúre,
      spoločný rig.
- [ ] V každom z nich **presne týchto 16 sekcií tela** so `SkinnedMeshRenderer`, pomenovaných
      `<pohlavie>_body_<kľúč>` — `male_body_upperarm_L`, `female_body_upperarm_L`.
      Kľúče, **veľké `_L`/`_R` sa kontrolujú ordinálne**:
      `neck chest abdomen hips upperarm_L upperarm_R forearm_L forearm_R hand_L hand_R
      thigh_L thigh_R calf_L calf_R foot_L foot_R`
- [ ] Presety ležia pod kontajnerom podľa triedy (`Clothes/Torso`, `Clothes/Legs`,
      `Clothes/Foot`, `Face/Hair`). **Meno objektu je voľné** — `shirt_1`, `t-shirt_2`,
      `boots_1`; triedu určí `slotClass` v JSON, nie prefix mena.
      **Mená objektov musia byť v rámci jedného prefabu jedinečné.**
- [ ] Materiály na presetoch pomenované `char_<farebná trieda>_<kľúč>`. Materiál, ktorý sa
      meniť nemá, dostane keyword mimo farebných tried — `char_leather_1`.
- [ ] Materiály extrahované na disk do `Assets/_Game/Art/Materials/Character/_source/`
      pod tým istým menom (`char_torso_1.mat`). Slúžia ako šablóny — nesú shader, normálky
      a smoothness, generátor im mení len `_BaseColor`.
- [ ] Oblečenie, ktoré v `hides` nič neskrýva (tielko), musí byť **odsadené od kože**,
      inak z-fighting.
- [ ] **Exportovať len deform kosti.** Zmerané na `character_male`: 599 transformov
      v prefabe, 404 kostí naviazaných na každom z 32 rendererov, z toho `DEF-` 71,
      `MCH-` 138, `ORG-` 65 a 130 Rigify ovládačov bez prefixu. Reálnu váhu nesie
      najviac 36 kostí (dlaň), väčšina meshov pod 20. Pri 20 NPC je to **~12 000
      transformov**, drvivá väčšina na kosti, ktoré nikdy nič nepohnú — na webe to váži
      viac než počet rendererov, ktorý meria úloha 16.
      Po odstránení ovládačov treba prepnúť `rootBone`: dnes je to `MCH-torso.parent`,
      teda mechanická kosť, ktorá tam po strihu nebude.

      **Toto nie je blokujúce a nerob to teraz.** Cena je CPU za snímok pri dvadsiatich
      postavách, nie pri jednej v generátorovej scéne, a animácie pre túto postavu budú
      celé nové — budú sa autorovať proti tomu skeletonu, ktorý bude aktuálny vtedy,
      takže sa nič nepretargetuje. Poradie je: dokonči presety, potom orež kosti, až
      potom animácie. Unity si `.blend` konvertuje s pevnými parametrami a voľbu
      **Armature → Only Deform Bones** mu podať nevie, takže orez znamená prejsť
      na ručne exportované `.fbx`. Dovtedy je `.blend` živý link to, čo chceš.

---

## Úloha 1: Assembly definitions a kostra priečinkov

Bez asmdefov sa nedajú písať EditMode testy — asmdef nevie referencovať `Assembly-CSharp`.
Rovnaké rozdelenie má už `FriWorld.ObjectRegistry.Editor`.

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/FriWorld.Character.asmdef`
- Create: `Assets/_Game/Editor/Character/FriWorld.Character.Editor.asmdef`
- Create: `Assets/_Game/Editor/Character/Tests/FriWorld.Character.Tests.asmdef`

- [ ] **Krok 1: Runtime asmdef**

```json
{
    "name": "FriWorld.Character",
    "rootNamespace": "FriWorld.Character",
    "references": [],
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

- [ ] **Krok 2: Editor asmdef**

`autoReferenced: true` nechá Newtonsoft.Json dotiahnuť sa sám, presne ako
`FriWorld.ObjectRegistry.Editor`.

```json
{
    "name": "FriWorld.Character.Editor",
    "rootNamespace": "FriWorld.Character.Editor",
    "references": [
        "FriWorld.Character"
    ],
    "includePlatforms": [
        "Editor"
    ],
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

- [ ] **Krok 3: Tests asmdef**

```json
{
    "name": "FriWorld.Character.Tests",
    "rootNamespace": "FriWorld.Character.Tests",
    "references": [
        "FriWorld.Character",
        "FriWorld.Character.Editor",
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

- [ ] **Krok 4: Over, že Unity zkompilovalo tri nové assembly**

`Assets/Refresh`, počkaj na dokompilovanie. V konzole nesmie byť chyba. Test Runner musí
zobraziť `FriWorld.Character.Tests` (zatiaľ bez testov).

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Character Assets/_Game/Editor/Character && git commit -m "chore(character): add assembly definitions for the customization system"
```

---

## Úloha 2: `BodySection` a mapovanie mien

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/BodySection.cs`
- Create: `Assets/_Game/Scripts/Character/BodySectionNames.cs`
- Test: `Assets/_Game/Editor/Character/Tests/BodySectionNamesTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using System;
using System.Collections.Generic;
using FriWorld.Character;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class BodySectionNamesTests
    {
        [Test]
        public void ParsesEverySectionKey()
        {
            string[] keys =
            {
                "neck", "chest", "abdomen", "hips",
                "upperarm_L", "upperarm_R", "forearm_L", "forearm_R",
                "hand_L", "hand_R", "thigh_L", "thigh_R",
                "calf_L", "calf_R", "foot_L", "foot_R",
            };

            foreach (string key in keys)
            {
                Assert.IsTrue(BodySectionNames.TryParseKey(key, out var section), key);
                Assert.AreNotEqual(BodySection.None, section, key);
            }
        }

        [Test]
        public void StripsTheBodyPrefixOffAnObjectName()
        {
            Assert.IsTrue(BodySectionNames.TryParseObject("male_body_upperarm_L", out var male));
            Assert.IsTrue(BodySectionNames.TryParseObject("female_body_upperarm_L", out var female));

            // Both bodies land on the same section, which is the whole point: hides masks in
            // CharacterPresets.json are written once and apply to either body.
            Assert.AreEqual(BodySection.UpperArmL, male);
            Assert.AreEqual(male, female);
        }

        [Test]
        public void AnObjectNameWithoutTheBodyPrefixIsReadAsAKey()
        {
            Assert.IsTrue(BodySectionNames.TryParseObject("chest", out var section));
            Assert.AreEqual(BodySection.Chest, section);
        }

        [Test]
        public void OnlyTheLastBodyMarkerCounts()
        {
            // A container could conceivably be called "body" too; the key is whatever follows
            // the final _body_, never the first one.
            Assert.IsTrue(BodySectionNames.TryParseObject("body_male_body_chest", out var section));
            Assert.AreEqual(BodySection.Chest, section);
        }

        [Test]
        public void TheHeadIsNotASection()
        {
            // male_body_head exists in the prefab today as a stand-in until head presets are
            // modelled. It is a slot class, so it must never resolve to a hideable section.
            Assert.IsFalse(BodySectionNames.TryParseObject("male_body_head", out _));
        }

        [Test]
        public void RejectsAnUnknownKey()
        {
            Assert.IsFalse(BodySectionNames.TryParseKey("torso", out _));
            Assert.IsFalse(BodySectionNames.TryParseKey("", out _));
            Assert.IsFalse(BodySectionNames.TryParseKey(null, out _));
        }

        [Test]
        public void TheSideSuffixIsCaseSensitive()
        {
            // Blender writes _L and _R. Accepting _l would let two objects claim one section
            // and the second would silently win.
            Assert.IsFalse(BodySectionNames.TryParseKey("upperarm_l", out _));
            Assert.IsFalse(BodySectionNames.TryParseObject("male_body_upperarm_l", out _));
        }

        [Test]
        public void SixteenSectionsWithDistinctBits()
        {
            var seen = new HashSet<BodySection>();
            int count = 0;
            foreach (var entry in BodySectionNames.All)
            {
                Assert.IsTrue(seen.Add(entry.section), entry.key);
                count++;
            }
            Assert.AreEqual(16, count);
        }

        [Test]
        public void KeyOfRoundTrips()
        {
            foreach (var entry in BodySectionNames.All)
                Assert.AreEqual(entry.key, BodySectionNames.KeyOf(entry.section));
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Test Runner → EditMode → `BodySectionNamesTests`.
Očakávané: **kompilačná chyba**, `BodySection` neexistuje.

- [ ] **Krok 3: Napíš `BodySection.cs`**

```csharp
using System;

namespace FriWorld.Character
{
    /// <summary>
    /// The pieces of bare skin a clothing preset can hide. One bit each, so a preset's whole
    /// coverage is a single int and several presets combine with OR.
    ///
    /// The head is deliberately missing: head shape is a slot class, so the head arrives from a
    /// preset and is never hidden.
    /// </summary>
    [Flags]
    public enum BodySection
    {
        None      = 0,
        Neck      = 1 << 0,
        Chest     = 1 << 1,
        Abdomen   = 1 << 2,
        Hips      = 1 << 3,
        UpperArmL = 1 << 4,
        UpperArmR = 1 << 5,
        ForearmL  = 1 << 6,
        ForearmR  = 1 << 7,
        HandL     = 1 << 8,
        HandR     = 1 << 9,
        ThighL    = 1 << 10,
        ThighR    = 1 << 11,
        CalfL     = 1 << 12,
        CalfR     = 1 << 13,
        FootL     = 1 << 14,
        FootR     = 1 << 15,
    }
}
```

- [ ] **Krok 4: Napíš `BodySectionNames.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace FriWorld.Character
{
    /// <summary>
    /// Maps between a body section and the GameObject that carries it in the base prefab.
    ///
    /// The object is called &lt;gender&gt;_body_&lt;key&gt; — male_body_upperarm_L — and the
    /// key is whatever follows the final "_body_". The prefix exists so both bodies can live in
    /// one blend file without a name clash; the key must not carry it, so a hides mask written
    /// once in CharacterPresets.json applies to either body. Same split as ObjectTypeKey: the
    /// name is what the author sees, the key is what the register looks up.
    ///
    /// Matching is ordinal and case-sensitive on purpose. Blender writes the side as _L and _R;
    /// accepting _l as well would let two objects claim the same section and the loser would
    /// disappear without a word.
    /// </summary>
    public static class BodySectionNames
    {
        const string Marker = "_body_";

        static readonly (string key, BodySection section)[] Table =
        {
            ("neck",       BodySection.Neck),
            ("chest",      BodySection.Chest),
            ("abdomen",    BodySection.Abdomen),
            ("hips",       BodySection.Hips),
            ("upperarm_L", BodySection.UpperArmL),
            ("upperarm_R", BodySection.UpperArmR),
            ("forearm_L",  BodySection.ForearmL),
            ("forearm_R",  BodySection.ForearmR),
            ("hand_L",     BodySection.HandL),
            ("hand_R",     BodySection.HandR),
            ("thigh_L",    BodySection.ThighL),
            ("thigh_R",    BodySection.ThighR),
            ("calf_L",     BodySection.CalfL),
            ("calf_R",     BodySection.CalfR),
            ("foot_L",     BodySection.FootL),
            ("foot_R",     BodySection.FootR),
        };

        public static IReadOnlyList<(string key, BodySection section)> All => Table;

        /// <summary>Exact key lookup — "upperarm_L". This is what CharacterPresets.json writes
        /// in its hides array.</summary>
        public static bool TryParseKey(string key, out BodySection section)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var entry in Table)
                {
                    if (string.Equals(entry.key, key, StringComparison.Ordinal))
                    {
                        section = entry.section;
                        return true;
                    }
                }
            }

            section = BodySection.None;
            return false;
        }

        /// <summary>Key lookup from a GameObject name — "male_body_upperarm_L". A name with no
        /// "_body_" in it is taken as a bare key, so a body that drops the prefix later still
        /// works.</summary>
        public static bool TryParseObject(string objectName, out BodySection section)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                section = BodySection.None;
                return false;
            }

            // Last occurrence, not first: a container could itself be called "body".
            int marker = objectName.LastIndexOf(Marker, StringComparison.Ordinal);
            string key = marker < 0
                ? objectName
                : objectName.Substring(marker + Marker.Length);

            return TryParseKey(key, out section);
        }

        /// <summary>The key, without any body prefix. For reports.</summary>
        public static string KeyOf(BodySection section)
        {
            foreach (var entry in Table)
                if (entry.section == section)
                    return entry.key;
            return null;
        }
    }
}
```

- [ ] **Krok 5: Spusti testy, over, že prechádzajú**

Očakávané: 5 testov PASS.

- [ ] **Krok 6: Commit**

```bash
git add Assets/_Game/Scripts/Character/BodySection.cs Assets/_Game/Scripts/Character/BodySectionNames.cs Assets/_Game/Editor/Character/Tests/BodySectionNamesTests.cs && git commit -m "feat(character): add the body section flags and their object names"
```

---

## Úloha 3: `MaterialSlotKey` — parsovanie mena materiálu

**Súbory:**
- Create: `Assets/_Game/Editor/Character/MaterialSlotKey.cs`
- Test: `Assets/_Game/Editor/Character/Tests/MaterialSlotKeyTests.cs`

Parser žije v editor assembly, lebo za behu sa už neparsuje nič — runtime číta indexy z bake.

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class MaterialSlotKeyTests
    {
        [Test]
        public void ParsesABaseColourSlot()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_1", out var slot));
            Assert.AreEqual("torso", slot.ColorClass);
            Assert.AreEqual(1, slot.BaseKey);
            Assert.AreEqual(0, slot.ShadeLevel);
        }

        [Test]
        public void ParsesASecondaryColourSlot()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_2", out var slot));
            Assert.AreEqual(2, slot.BaseKey);
            Assert.AreEqual(0, slot.ShadeLevel);
        }

        [Test]
        public void ParsesTheDarkerShadeOfTheFirstColour()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_11", out var slot));
            Assert.AreEqual("torso", slot.ColorClass);
            Assert.AreEqual(1, slot.BaseKey);
            Assert.AreEqual(1, slot.ShadeLevel);
        }

        [Test]
        public void ParsesTheDarkerShadeOfTheSecondColour()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_21", out var slot));
            Assert.AreEqual(2, slot.BaseKey);
            Assert.AreEqual(1, slot.ShadeLevel);
        }

        [Test]
        public void ParsingIsSyntaxOnly_ClassMembershipIsSomebodyElsesJob()
        {
            // char_leather_1 is a perfectly well-formed name. That "leather" is not a colour
            // class is decided by the catalog, not here — mixing the two would make the parser
            // the place where art naming rules live.
            Assert.IsTrue(MaterialSlotKey.TryParse("char_leather_1", out var slot));
            Assert.AreEqual("leather", slot.ColorClass);
        }

        [Test]
        public void KeepsMultiWordClassNamesIntact()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_upper_body_1", out var slot));
            Assert.AreEqual("upper_body", slot.ColorClass);
        }

        [Test]
        public void RejectsAMissingSuffix()
        {
            Assert.IsFalse(MaterialSlotKey.TryParse("char_skin", out _));
        }

        [Test]
        public void RejectsATenthBaseColour()
        {
            // "10" would be indistinguishable from "the darker shade of colour 1" at shade
            // level 0. Nine base colours per class is the documented ceiling.
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_10", out _));
        }

        [Test]
        public void RejectsNamesOutsideTheScheme()
        {
            Assert.IsFalse(MaterialSlotKey.TryParse("mt_floor_1", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_1x", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_111", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_0", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_1", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse(null, out _));
        }

        [Test]
        public void ParsesAShadeLevelAboveOne_TheCatalogRejectsItLater()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_12", out var slot));
            Assert.AreEqual(2, slot.ShadeLevel);
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `MaterialSlotKey` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System;

namespace FriWorld.Character.Editor
{
    /// <summary>One material slot declared by a material name on the mesh.</summary>
    public readonly struct MaterialSlot
    {
        /// <summary>The keyword between "char_" and the numeric suffix. Not necessarily a
        /// colour class — that is the catalog's call.</summary>
        public readonly string ColorClass;

        /// <summary>1..9. Which base colour of the class this slot wants.</summary>
        public readonly int BaseKey;

        /// <summary>0 for the base colour, 1 for the first darker shade.</summary>
        public readonly int ShadeLevel;

        public MaterialSlot(string colorClass, int baseKey, int shadeLevel)
        {
            ColorClass = colorClass;
            BaseKey = baseKey;
            ShadeLevel = shadeLevel;
        }

        public override string ToString() => $"char_{ColorClass}_{BaseKey}"
            + (ShadeLevel > 0 ? ShadeLevel.ToString() : string.Empty);
    }

    /// <summary>
    /// Reads "char_&lt;class&gt;_&lt;key&gt;" off a material name.
    ///
    /// The name on the mesh is a declaration of intent, not a colour: it says which slot of
    /// which class this material fills. The colour arrives from the colorway at bake time.
    ///
    /// This is syntax only. Whether the keyword is a real colour class is decided by the
    /// catalog — that split is what lets "char_leather_1" be a valid name that simply never
    /// gets recoloured, while "char_torzo_1" shows up as a typo instead of silently working.
    /// </summary>
    public static class MaterialSlotKey
    {
        const string Prefix = "char_";

        public static bool TryParse(string materialName, out MaterialSlot slot)
        {
            slot = default;

            if (string.IsNullOrEmpty(materialName)) return false;
            if (!materialName.StartsWith(Prefix, StringComparison.Ordinal)) return false;

            int split = materialName.LastIndexOf('_');

            // The underscore has to come after at least one character of class name, otherwise
            // we matched the one inside "char_" itself.
            if (split < Prefix.Length) return false;

            string colorClass = materialName.Substring(Prefix.Length, split - Prefix.Length);
            string digits = materialName.Substring(split + 1);

            if (colorClass.Length == 0) return false;
            if (digits.Length < 1 || digits.Length > 2) return false;

            foreach (char c in digits)
                if (c < '0' || c > '9') return false;

            int baseKey = digits[0] - '0';
            if (baseKey < 1) return false;

            int shadeLevel = digits.Length == 1 ? 0 : digits[1] - '0';

            // "10" is not "colour ten", it is a malformed shade. Nine base colours is the cap.
            if (digits.Length == 2 && shadeLevel < 1) return false;

            slot = new MaterialSlot(colorClass, baseKey, shadeLevel);
            return true;
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 10 testov PASS.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Editor/Character/MaterialSlotKey.cs Assets/_Game/Editor/Character/Tests/MaterialSlotKeyTests.cs && git commit -m "feat(character): parse the material slot key off a material name"
```

---

## Úloha 4: JSON registre a ich načítanie

**Súbory:**
- Create: `Assets/_Game/Editor/Character/CharacterRegistries.cs`
- Create: `Assets/_Game/Editor/CharacterClasses.json`
- Create: `Assets/_Game/Editor/CharacterColorways.json`
- Create: `Assets/_Game/Editor/CharacterPresets.json`
- Test: `Assets/_Game/Editor/Character/Tests/CharacterRegistriesTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using System.IO;
using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class CharacterRegistriesTests
    {
        string temp;

        [SetUp]
        public void SetUp() => temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(temp)) File.Delete(temp);
        }

        void Write(string json) => File.WriteAllText(temp, json);

        [Test]
        public void ReadsAColourClassWithAShade()
        {
            Write(@"{ ""colorClasses"": [
                { ""name"": ""torso"", ""mainColors"": 2,
                  ""shadeValue"": 0.62, ""shadeSaturation"": 1.12 } ],
                ""slotClasses"": [ ""torso"" ] }");

            var registry = CharacterRegistries.LoadFrom<ClassRegistry>(temp);

            Assert.AreEqual(1, registry.colorClasses.Count);
            Assert.AreEqual("torso", registry.colorClasses[0].name);
            Assert.AreEqual(2, registry.colorClasses[0].mainColors);
            Assert.AreEqual(0.62f, registry.colorClasses[0].shadeValue.Value, 0.0001f);
            Assert.AreEqual(1, registry.slotClasses.Count);
        }

        [Test]
        public void AShadelessClassKeepsNullNotZero()
        {
            // Zero would read as "multiply value by 0", i.e. black. Null means "no shade".
            Write(@"{ ""colorClasses"": [
                { ""name"": ""eye"", ""mainColors"": 1,
                  ""shadeValue"": null, ""shadeSaturation"": null } ],
                ""slotClasses"": [] }");

            var registry = CharacterRegistries.LoadFrom<ClassRegistry>(temp);

            Assert.IsFalse(registry.colorClasses[0].shadeValue.HasValue);
        }

        [Test]
        public void ReadsAColorway()
        {
            Write(@"{ ""colorways"": [
                { ""colorClass"": ""torso"", ""id"": ""navy"", ""displayName"": ""Tmavomodrá"",
                  ""colors"": [ ""#243B6B"", ""#C8CEDA"" ] } ] }");

            var registry = CharacterRegistries.LoadFrom<ColorwayRegistry>(temp);

            Assert.AreEqual("navy", registry.colorways[0].id);
            Assert.AreEqual(2, registry.colorways[0].colors.Count);
        }

        [Test]
        public void ReadsAPresetAndMapsTheObjectKeyword()
        {
            // "object" is a C# keyword, so the field is objectName and JsonProperty bridges it.
            Write(@"{ ""presets"": [
                { ""slotClass"": ""torso"", ""object"": ""torso_hoodie_1"",
                  ""displayName"": ""Mikina"", ""gender"": ""any"",
                  ""hides"": [ ""chest"", ""abdomen"" ],
                  ""tags"": [ ""casual"" ], ""conflicts"": [], ""weight"": 3 } ] }");

            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(temp);

            Assert.AreEqual("torso_hoodie_1", registry.presets[0].objectName);
            Assert.AreEqual(2, registry.presets[0].hides.Count);
            Assert.AreEqual(3, registry.presets[0].weight);
        }

        [Test]
        public void AMissingFileGivesAnEmptyRegistryNotAnException()
        {
            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(
                Path.Combine(Path.GetTempPath(), "no-such-file.json"));

            Assert.IsNotNull(registry);
            Assert.AreEqual(0, registry.presets.Count);
        }

        [Test]
        public void AnAbsentWeightDefaultsToOne()
        {
            Write(@"{ ""presets"": [
                { ""slotClass"": ""torso"", ""object"": ""torso_tank_1"" } ] }");

            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(temp);

            Assert.AreEqual(1, registry.presets[0].weight);
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `CharacterRegistries` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.Character.Editor
{
    public sealed class ColorClassDef
    {
        public string name;
        public int mainColors = 1;

        /// <summary>Null means the class has no darker shade. Zero would mean black.</summary>
        public float? shadeValue;
        public float? shadeSaturation;
    }

    public sealed class ClassRegistry
    {
        public List<ColorClassDef> colorClasses = new List<ColorClassDef>();
        public List<string> slotClasses = new List<string>();
    }

    public sealed class ColorwayDef
    {
        public string colorClass;
        public string id;
        public string displayName;
        public List<string> colors = new List<string>();
    }

    public sealed class ColorwayRegistry
    {
        public List<ColorwayDef> colorways = new List<ColorwayDef>();
    }

    public sealed class PresetDef
    {
        public string slotClass;

        /// <summary>The GameObject name in the base prefab. "object" is a C# keyword.</summary>
        [JsonProperty("object")] public string objectName;

        public string displayName;
        public string gender = "any";
        public List<string> hides = new List<string>();
        public List<string> tags = new List<string>();
        public List<string> conflicts = new List<string>();
        public int weight = 1;
    }

    public sealed class PresetRegistry
    {
        public List<PresetDef> presets = new List<PresetDef>();
    }

    /// <summary>
    /// The three hand-edited registers, next to ObjectTypes.json and RoomPlatforms.json.
    ///
    /// They are the source of truth and nothing but the editor reads them: turning "navy" into
    /// an actual Material is what Bake Catalog is for.
    /// </summary>
    public static class CharacterRegistries
    {
        public const string ClassesPath   = "Assets/_Game/Editor/CharacterClasses.json";
        public const string ColorwaysPath = "Assets/_Game/Editor/CharacterColorways.json";
        public const string PresetsPath   = "Assets/_Game/Editor/CharacterPresets.json";

        /// <summary>A missing file reads as an empty register, so a fresh clone can still run
        /// Report and be told what to fill in.</summary>
        public static T LoadFrom<T>(string path) where T : new()
        {
            if (!File.Exists(path)) return new T();
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) ?? new T();
        }

        public static ClassRegistry LoadClasses() => LoadFrom<ClassRegistry>(ClassesPath);
        public static ColorwayRegistry LoadColorways() => LoadFrom<ColorwayRegistry>(ColorwaysPath);
        public static PresetRegistry LoadPresets() => LoadFrom<PresetRegistry>(PresetsPath);
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 6 testov PASS.

- [ ] **Krok 5: Založ tri registre s reálnym obsahom**

`Assets/_Game/Editor/CharacterClasses.json`:

```json
{
  "colorClasses": [
    { "name": "torso", "mainColors": 2, "shadeValue": 0.62, "shadeSaturation": 1.12 },
    { "name": "legs",  "mainColors": 1, "shadeValue": 0.62, "shadeSaturation": 1.12 },
    { "name": "feet",  "mainColors": 1, "shadeValue": 0.55, "shadeSaturation": 1.10 },
    { "name": "hair",  "mainColors": 1, "shadeValue": 0.70, "shadeSaturation": 1.05 },
    { "name": "skin",  "mainColors": 1, "shadeValue": 0.80, "shadeSaturation": 1.08 }
  ],
  "slotClasses": ["hair", "torso", "legs", "feet"]
}
```

**Deklaruj len to, čo v prefabe naozaj je.** Trieda bez jediného presetu alebo bez jediného
colorwayu je v Reporte chyba, a je to tak správne — inak by NPC prišlo o časť tela a nikto
by sa to nedozvedel. `head`, `beard`, `eye` a `lips` sa doplnia, keď pribudnú meshe;
každé z nich je jeden riadok. `feet` má `mainColors: 1`, lebo `boots_*` nesú len
`char_feet_1`.

`Assets/_Game/Editor/CharacterColorways.json` — začni jedným colorwayom na triedu, doplní sa:

```json
{
  "colorways": [
    { "colorClass": "torso", "id": "navy",  "displayName": "Tmavomodrá", "colors": ["#243B6B", "#C8CEDA"] },
    { "colorClass": "torso", "id": "rust",  "displayName": "Hrdzavá",    "colors": ["#A6482B", "#E8D9C0"] },
    { "colorClass": "legs",  "id": "denim", "displayName": "Džínsová",   "colors": ["#3A4A63"] },
    { "colorClass": "feet",  "id": "black", "displayName": "Čierna",     "colors": ["#2A2A2E"] },
    { "colorClass": "hair",  "id": "brown", "displayName": "Hnedá",      "colors": ["#4A3227"] },
    { "colorClass": "skin",  "id": "light", "displayName": "Svetlá",     "colors": ["#F2CDB4"] }
  ]
}
```

`Assets/_Game/Editor/CharacterPresets.json` — mená objektov sedia s `character_male.prefab`:

```json
{
  "presets": [
    { "slotClass": "hair", "object": "hair_1", "displayName": "Krátke",
      "gender": "any", "hides": [], "tags": [], "conflicts": [], "weight": 1 },
    { "slotClass": "hair", "object": "hair_2", "displayName": "Rozstrapatené",
      "gender": "any", "hides": [], "tags": [], "conflicts": [], "weight": 1 },

    { "slotClass": "torso", "object": "shirt_1", "displayName": "Košeľa",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R", "forearm_L", "forearm_R"],
      "tags": ["formal"], "conflicts": [], "weight": 1 },
    { "slotClass": "torso", "object": "shirt_2", "displayName": "Košeľa s pruhmi",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R", "forearm_L", "forearm_R"],
      "tags": ["formal"], "conflicts": [], "weight": 1 },
    { "slotClass": "torso", "object": "shirt_3", "displayName": "Košeľa dvojfarebná",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R", "forearm_L", "forearm_R"],
      "tags": ["formal"], "conflicts": [], "weight": 1 },
    { "slotClass": "torso", "object": "shirt_4", "displayName": "Košeľa s lemom",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R", "forearm_L", "forearm_R"],
      "tags": ["formal"], "conflicts": [], "weight": 1 },
    { "slotClass": "torso", "object": "t-shirt_1", "displayName": "Tričko",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R"],
      "tags": ["casual"], "conflicts": [], "weight": 2 },
    { "slotClass": "torso", "object": "t-shirt_2", "displayName": "Tričko s potlačou",
      "gender": "any",
      "hides": ["chest", "abdomen", "upperarm_L", "upperarm_R"],
      "tags": ["casual"], "conflicts": [], "weight": 2 },

    { "slotClass": "legs", "object": "pants_1", "displayName": "Nohavice",
      "gender": "any", "hides": ["hips", "thigh_L", "thigh_R", "calf_L", "calf_R"],
      "tags": ["formal"], "conflicts": [], "weight": 1 },
    { "slotClass": "legs", "object": "pants_2", "displayName": "Rifle",
      "gender": "any", "hides": ["hips", "thigh_L", "thigh_R", "calf_L", "calf_R"],
      "tags": ["casual"], "conflicts": [], "weight": 1 },
    { "slotClass": "legs", "object": "pants_3", "displayName": "Kapsáče",
      "gender": "any", "hides": ["hips", "thigh_L", "thigh_R", "calf_L", "calf_R"],
      "tags": ["casual"], "conflicts": [], "weight": 1 },
    { "slotClass": "legs", "object": "shorts_1", "displayName": "Kraťasy",
      "gender": "any", "hides": ["hips", "thigh_L", "thigh_R"],
      "tags": ["casual"], "conflicts": [], "weight": 1 },
    { "slotClass": "legs", "object": "shorts_2", "displayName": "Kraťasy športové",
      "gender": "any", "hides": ["hips", "thigh_L", "thigh_R"],
      "tags": ["casual"], "conflicts": [], "weight": 1 },

    { "slotClass": "feet", "object": "boots_1", "displayName": "Topánky",
      "gender": "any", "hides": ["foot_L", "foot_R"], "tags": [], "conflicts": [], "weight": 1 },
    { "slotClass": "feet", "object": "boots_2", "displayName": "Tenisky",
      "gender": "any", "hides": ["foot_L", "foot_R"], "tags": [], "conflicts": [], "weight": 1 }
  ]
}
```

**Masky `hides` sú odhad z počtu submeshov, nie zmerané.** Dlhé rukávy pri `shirt_*`
a krátke pri `t-shirt_*` treba **pozrieť očami v scéne** a opraviť — zle nastavená maska
sa prejaví buď kožou prerastajúcou cez látku, alebo useknutým predlaktím. To je jediná
vec v tomto kroku, ktorú nevie skontrolovať Report.

`conflicts` sú zatiaľ všade prázdne zámerne: konflikt na tag, ktorý žiadny preset nedáva,
je v Reporte mŕtve pravidlo a chyba. Tagy `formal` a `casual` sú už tu, takže prvé
pravidlo je jeden riadok.

- [ ] **Krok 6: Commit**

```bash
git add Assets/_Game/Editor/Character/CharacterRegistries.cs Assets/_Game/Editor/Character/Tests/CharacterRegistriesTests.cs Assets/_Game/Editor/CharacterClasses.json Assets/_Game/Editor/CharacterColorways.json Assets/_Game/Editor/CharacterPresets.json && git commit -m "feat(character): add the three JSON registers and their loader"
```

---

## Úloha 5: `ShadeColor` — odvodenie tmavšieho odtieňa

**Súbory:**
- Create: `Assets/_Game/Editor/Character/ShadeColor.cs`
- Test: `Assets/_Game/Editor/Character/Tests/ShadeColorTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class ShadeColorTests
    {
        [Test]
        public void TheShadeIsDarkerThanTheBase()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f);
            var shade = ShadeColor.Derive(baseColor, 0.62f, 1.12f);

            Color.RGBToHSV(baseColor, out _, out _, out float baseValue);
            Color.RGBToHSV(shade, out _, out _, out float shadeValue);

            Assert.Less(shadeValue, baseValue);
        }

        [Test]
        public void TheHueSurvives()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f);
            var shade = ShadeColor.Derive(baseColor, 0.62f, 1.12f);

            Color.RGBToHSV(baseColor, out float baseHue, out _, out _);
            Color.RGBToHSV(shade, out float shadeHue, out _, out _);

            Assert.AreEqual(baseHue, shadeHue, 0.002f);
        }

        [Test]
        public void AlphaIsCarriedOverUntouched()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f, 0.5f);
            Assert.AreEqual(0.5f, ShadeColor.Derive(baseColor, 0.62f, 1.12f).a, 0.0001f);
        }

        [Test]
        public void AGreyStaysGrey()
        {
            // Saturation 0 multiplied by anything is still 0, so a neutral never picks up a tint.
            var shade = ShadeColor.Derive(new Color(0.6f, 0.6f, 0.6f), 0.62f, 1.5f);

            Assert.AreEqual(shade.r, shade.g, 0.0001f);
            Assert.AreEqual(shade.g, shade.b, 0.0001f);
        }

        [Test]
        public void SaturationIsClampedNotWrapped()
        {
            var shade = ShadeColor.Derive(new Color(1f, 0f, 0f), 0.9f, 4f);

            Color.RGBToHSV(shade, out _, out float saturation, out _);
            Assert.LessOrEqual(saturation, 1f);
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `ShadeColor` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Derives the darker shade of a base colour.
    ///
    /// In HSV, not by multiplying RGB: multiplying RGB washes the hue towards whichever channel
    /// was already dominant, so a warm red goes brown. Dropping value and nudging saturation up
    /// is what a fold in cloth actually does to a colour.
    ///
    /// The factors come from the colour class, because a fold in fabric and a strand of hair are
    /// not the same number.
    /// </summary>
    public static class ShadeColor
    {
        public static Color Derive(Color baseColor, float value, float saturation)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            s = Mathf.Clamp01(s * saturation);
            v = Mathf.Clamp01(v * value);

            var derived = Color.HSVToRGB(h, s, v);
            derived.a = baseColor.a;
            return derived;
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 5 testov PASS.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Editor/Character/ShadeColor.cs Assets/_Game/Editor/Character/Tests/ShadeColorTests.cs && git commit -m "feat(character): derive the darker shade in HSV, not RGB"
```

---

## Úloha 6: Dátové typy katalógu

Bez logiky — len tvar, na ktorý sa vie oprieť randomizér aj builder. Presety sú zoradené podľa
slot triedy a `presetStart` je CSR index, takže výber kandidátov nič nealokuje.

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/CharacterCatalog.cs`
- Test: `Assets/_Game/Editor/Character/Tests/CharacterCatalogTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterCatalogTests
    {
        static CharacterCatalog Build()
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso", "legs" };
            catalog.colorClasses = new[] { "torso", "legs" };
            catalog.tags = new[] { "casual" };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorClass = 0, id = "navy" },
                new ColorwayEntry { colorClass = 0, id = "rust" },
                new ColorwayEntry { colorClass = 1, id = "denim" },
            };
            catalog.colorwayStart = new[] { 0, 2, 3 };

            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry { slotClass = 0, objectName = "torso_hoodie_1" },
                    new PresetEntry { slotClass = 0, objectName = "torso_tank_1" },
                    new PresetEntry { slotClass = 1, objectName = "legs_jeans_1" },
                },
                presetStart = new[] { 0, 2, 3 },
                slotMaps = new[]
                {
                    new RendererSlotMap { objectName = "male_body_chest" },
                },
            };
            catalog.female = new GenderBundle();

            return catalog;
        }

        [Test]
        public void CountsPresetsPerSlotClass()
        {
            var catalog = Build();
            Assert.AreEqual(2, catalog.PresetCount(Gender.Male, 0));
            Assert.AreEqual(1, catalog.PresetCount(Gender.Male, 1));
        }

        [Test]
        public void IndexesPresetsWithinTheirSlotClass()
        {
            var catalog = Build();
            Assert.AreEqual("torso_tank_1", catalog.Preset(Gender.Male, 0, 1).objectName);
            Assert.AreEqual("legs_jeans_1", catalog.Preset(Gender.Male, 1, 0).objectName);
        }

        [Test]
        public void CountsAndIndexesColorwaysPerColourClass()
        {
            var catalog = Build();
            Assert.AreEqual(2, catalog.ColorwayCount(0));
            Assert.AreEqual(1, catalog.ColorwayCount(1));
            Assert.AreEqual("denim", catalog.Colorway(1, 0).id);
        }

        [Test]
        public void FindsASlotMapByObjectName()
        {
            var catalog = Build();
            Assert.IsNotNull(catalog.SlotMap(Gender.Male, "male_body_chest"));
            Assert.IsNull(catalog.SlotMap(Gender.Male, "no_such_object"));
        }

        [Test]
        public void AnEmptyBundleAnswersZeroInsteadOfThrowing()
        {
            var catalog = Build();
            Assert.AreEqual(0, catalog.PresetCount(Gender.Female, 0));
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `CharacterCatalog` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    public enum Gender { Male = 0, Female = 1 }

    /// <summary>Which bodies a preset may appear on.</summary>
    public enum GenderGate { Any = 0, Male = 1, Female = 2 }

    [Serializable]
    public sealed class PresetEntry
    {
        public int slotClass;
        public string objectName;
        public string displayName;
        public GenderGate gender;

        /// <summary>BodySection bitmask of the skin this preset covers completely.</summary>
        public int hides;

        /// <summary>Bit per tag in CharacterCatalog.tags.</summary>
        public int tagMask;
        public int conflictMask;

        public int weight = 1;
    }

    [Serializable]
    public sealed class ColorwayEntry
    {
        public int colorClass;
        public string id;
        public string displayName;

        /// <summary>Dense, index = (baseKey - 1) * 2 + shadeLevel. Entries the class does not
        /// declare are null and are never asked for.</summary>
        public Material[] materials = Array.Empty<Material>();

        /// <summary>Not called "Material" — a method whose name equals its return type makes
        /// every later use of that type inside this class ambiguous.</summary>
        public Material MaterialFor(int baseKey, int shadeLevel)
        {
            int index = (baseKey - 1) * 2 + shadeLevel;
            return index >= 0 && index < materials.Length ? materials[index] : null;
        }
    }

    /// <summary>What to do with each material slot of one renderer, baked from its names.</summary>
    [Serializable]
    public sealed class RendererSlotMap
    {
        public string objectName;

        /// <summary>Per material slot: index into colorClasses, or -1 to leave it as authored.
        /// That -1 is how char_leather_1 survives untouched.</summary>
        public int[] colorClass = Array.Empty<int>();

        /// <summary>Per material slot: (baseKey - 1) * 2 + shadeLevel.</summary>
        public int[] materialIndex = Array.Empty<int>();
    }

    [Serializable]
    public sealed class GenderBundle
    {
        public GameObject basePrefab;

        /// <summary>Sorted by slotClass so presetStart can index into it.</summary>
        public PresetEntry[] presets = Array.Empty<PresetEntry>();

        /// <summary>CSR offsets, length slotClasses.Length + 1.</summary>
        public int[] presetStart = Array.Empty<int>();

        public RendererSlotMap[] slotMaps = Array.Empty<RendererSlotMap>();
    }

    /// <summary>
    /// The baked output of the three JSON registers: the only thing the game reads.
    ///
    /// Everything is an index, not a string, because Apply runs once per NPC spawn and string
    /// work there is pure waste. The names are kept only so a report can be read by a human.
    /// </summary>
    [CreateAssetMenu(menuName = "FriWorld/Character Catalog", fileName = "CharacterCatalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        public string[] slotClasses = Array.Empty<string>();
        public string[] colorClasses = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();

        /// <summary>Sorted by colorClass so colorwayStart can index into it.</summary>
        public ColorwayEntry[] colorways = Array.Empty<ColorwayEntry>();

        /// <summary>CSR offsets, length colorClasses.Length + 1.</summary>
        public int[] colorwayStart = Array.Empty<int>();

        public GenderBundle male = new GenderBundle();
        public GenderBundle female = new GenderBundle();

        Dictionary<string, RendererSlotMap> maleMaps;
        Dictionary<string, RendererSlotMap> femaleMaps;

        public GenderBundle Bundle(Gender gender) => gender == Gender.Male ? male : female;

        public int PresetCount(Gender gender, int slotClass)
        {
            var bundle = Bundle(gender);
            if (bundle?.presetStart == null || slotClass + 1 >= bundle.presetStart.Length) return 0;
            return bundle.presetStart[slotClass + 1] - bundle.presetStart[slotClass];
        }

        public PresetEntry Preset(Gender gender, int slotClass, int index)
        {
            var bundle = Bundle(gender);
            return bundle.presets[bundle.presetStart[slotClass] + index];
        }

        public int ColorwayCount(int colorClass)
        {
            if (colorwayStart == null || colorClass + 1 >= colorwayStart.Length) return 0;
            return colorwayStart[colorClass + 1] - colorwayStart[colorClass];
        }

        public ColorwayEntry Colorway(int colorClass, int index) =>
            colorways[colorwayStart[colorClass] + index];

        public RendererSlotMap SlotMap(Gender gender, string objectName)
        {
            var cache = gender == Gender.Male
                ? maleMaps ?? (maleMaps = Index(male))
                : femaleMaps ?? (femaleMaps = Index(female));

            return cache.TryGetValue(objectName, out var map) ? map : null;
        }

        void OnDisable()
        {
            // Domain reload or an edit to the asset invalidates the caches.
            maleMaps = null;
            femaleMaps = null;
        }

        static Dictionary<string, RendererSlotMap> Index(GenderBundle bundle)
        {
            var map = new Dictionary<string, RendererSlotMap>(StringComparer.Ordinal);
            if (bundle?.slotMaps == null) return map;

            foreach (var entry in bundle.slotMaps)
                if (!string.IsNullOrEmpty(entry.objectName))
                    map[entry.objectName] = entry;

            return map;
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 5 testov PASS.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Character/CharacterCatalog.cs Assets/_Game/Editor/Character/Tests/CharacterCatalogTests.cs && git commit -m "feat(character): add the baked catalog data types"
```

---

## Úloha 7: `PresetRules` — kto smie ísť s kým

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/PresetRules.cs`
- Test: `Assets/_Game/Editor/Character/Tests/PresetRulesTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class PresetRulesTests
    {
        static PresetEntry Preset(GenderGate gate, int tags, int conflicts) =>
            new PresetEntry { gender = gate, tagMask = tags, conflictMask = conflicts };

        [Test]
        public void AnyPassesForBothGenders()
        {
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Any, Gender.Male));
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Any, Gender.Female));
        }

        [Test]
        public void AGatedPresetOnlyPassesForItsGender()
        {
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Male, Gender.Male));
            Assert.IsFalse(PresetRules.GenderAllows(GenderGate.Male, Gender.Female));
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Female, Gender.Female));
            Assert.IsFalse(PresetRules.GenderAllows(GenderGate.Female, Gender.Male));
        }

        [Test]
        public void APresetIsRejectedWhenSomethingAlreadyChosenForbidsItsTag()
        {
            // Already wearing a jacket that forbids "bulky_torso"; a backpack tagged
            // "bulky_torso" must not pass.
            var backpack = Preset(GenderGate.Any, tags: 1, conflicts: 0);

            Assert.IsFalse(PresetRules.IsAllowed(backpack, Gender.Male,
                takenTags: 0, forbiddenTags: 1));
        }

        [Test]
        public void APresetIsRejectedWhenItForbidsSomethingAlreadyChosen()
        {
            // The same rule seen from the other side: order of picking must not change the
            // outcome, which is why both masks are tested on every candidate.
            var jacket = Preset(GenderGate.Any, tags: 0, conflicts: 1);

            Assert.IsFalse(PresetRules.IsAllowed(jacket, Gender.Male,
                takenTags: 1, forbiddenTags: 0));
        }

        [Test]
        public void UnrelatedTagsDoNotCollide()
        {
            var preset = Preset(GenderGate.Any, tags: 0b0010, conflicts: 0b1000);

            Assert.IsTrue(PresetRules.IsAllowed(preset, Gender.Male,
                takenTags: 0b0100, forbiddenTags: 0b0001));
        }

        [Test]
        public void PickWeightedRespectsTheBoundaries()
        {
            var weights = new[] { 1, 3 };   // total 4: [0, 1) then [1, 4)

            Assert.AreEqual(0, PresetRules.PickWeighted(weights, 0.0));
            Assert.AreEqual(0, PresetRules.PickWeighted(weights, 0.2499));
            Assert.AreEqual(1, PresetRules.PickWeighted(weights, 0.25));
            Assert.AreEqual(1, PresetRules.PickWeighted(weights, 0.9999));
        }

        [Test]
        public void PickWeightedTreatsAllZeroWeightsAsTheFirstEntry()
        {
            Assert.AreEqual(0, PresetRules.PickWeighted(new[] { 0, 0 }, 0.7));
        }

        [Test]
        public void PickWeightedReturnsMinusOneOnAnEmptyList()
        {
            Assert.AreEqual(-1, PresetRules.PickWeighted(new int[0], 0.5));
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `PresetRules` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    /// <summary>
    /// Whether a preset may be worn given what has already been chosen.
    ///
    /// Conflicts are symmetric and both directions are checked on every candidate, so the order
    /// the slot classes are visited in cannot change which combinations are legal. That is what
    /// lets the randomizer be a single pass with no backtracking.
    /// </summary>
    public static class PresetRules
    {
        public static bool GenderAllows(GenderGate gate, Gender gender)
        {
            switch (gate)
            {
                case GenderGate.Any:    return true;
                case GenderGate.Male:   return gender == Gender.Male;
                case GenderGate.Female: return gender == Gender.Female;
                default:                return false;
            }
        }

        /// <param name="takenTags">OR of the tags provided by everything chosen so far.</param>
        /// <param name="forbiddenTags">OR of the tags forbidden by everything chosen so far.</param>
        public static bool IsAllowed(PresetEntry preset, Gender gender, int takenTags, int forbiddenTags)
        {
            if (preset == null) return false;
            if (!GenderAllows(preset.gender, gender)) return false;
            if ((preset.tagMask & forbiddenTags) != 0) return false;
            if ((preset.conflictMask & takenTags) != 0) return false;
            return true;
        }

        /// <param name="roll">Uniform in [0, 1).</param>
        /// <returns>Index into <paramref name="weights"/>, or -1 when there is nothing to pick.</returns>
        public static int PickWeighted(IReadOnlyList<int> weights, double roll)
        {
            if (weights == null || weights.Count == 0) return -1;

            long total = 0;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0, weights[i]);

            // Every candidate weighted zero still has to produce something rather than nothing;
            // Report already flags a weight below 1.
            if (total <= 0) return 0;

            double target = roll * total;
            long running = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                running += Mathf.Max(0, weights[i]);
                if (target < running) return i;
            }

            return weights.Count - 1;
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 8 testov PASS.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Character/PresetRules.cs Assets/_Game/Editor/Character/Tests/PresetRulesTests.cs && git commit -m "feat(character): add the preset compatibility rules"
```

---

## Úloha 8: `CharacterAppearance` a `CharacterRandomizer`

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/CharacterAppearance.cs`
- Create: `Assets/_Game/Scripts/Character/CharacterRandomizer.cs`
- Test: `Assets/_Game/Editor/Character/Tests/CharacterRandomizerTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterRandomizerTests
    {
        /// One slot class "torso" with three presets, one of them female-only and one pair that
        /// conflicts through the tag "bulky_torso"; one colour class with two colorways.
        static CharacterCatalog Build()
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso", "legs" };
            catalog.colorClasses = new[] { "torso" };
            catalog.tags = new[] { "bulky_torso" };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorClass = 0, id = "navy" },
                new ColorwayEntry { colorClass = 0, id = "rust" },
            };
            catalog.colorwayStart = new[] { 0, 2 };

            var presets = new[]
            {
                new PresetEntry { slotClass = 0, objectName = "torso_hoodie_1",
                                  gender = GenderGate.Any, tagMask = 1, weight = 1 },
                new PresetEntry { slotClass = 0, objectName = "torso_tank_1",
                                  gender = GenderGate.Any, weight = 1 },
                new PresetEntry { slotClass = 0, objectName = "torso_blouse_1",
                                  gender = GenderGate.Female, weight = 1 },
                new PresetEntry { slotClass = 1, objectName = "legs_backpack_strap_1",
                                  gender = GenderGate.Any, conflictMask = 1, weight = 1 },
                new PresetEntry { slotClass = 1, objectName = "legs_jeans_1",
                                  gender = GenderGate.Any, weight = 1 },
            };

            catalog.male = new GenderBundle { presets = presets, presetStart = new[] { 0, 3, 5 } };
            catalog.female = new GenderBundle { presets = presets, presetStart = new[] { 0, 3, 5 } };
            return catalog;
        }

        [Test]
        public void TheSameSeedGivesTheSameLook()
        {
            var catalog = Build();

            var a = CharacterRandomizer.Roll(1234, catalog, Gender.Male);
            var b = CharacterRandomizer.Roll(1234, catalog, Gender.Male);

            Assert.AreEqual(a.gender, b.gender);
            CollectionAssert.AreEqual(a.preset, b.preset);
            CollectionAssert.AreEqual(a.colorway, b.colorway);
        }

        [Test]
        public void DifferentSeedsEventuallyDiffer()
        {
            var catalog = Build();
            bool sawADifference = false;

            var first = CharacterRandomizer.Roll(0, catalog, Gender.Male);
            for (int seed = 1; seed < 50 && !sawADifference; seed++)
            {
                var other = CharacterRandomizer.Roll(seed, catalog, Gender.Male);
                for (int i = 0; i < first.preset.Length; i++)
                    if (first.preset[i] != other.preset[i]) sawADifference = true;
            }

            Assert.IsTrue(sawADifference, "50 seeds produced one single look");
        }

        [Test]
        public void AFemaleOnlyPresetNeverLandsOnAMaleBody()
        {
            var catalog = Build();

            for (int seed = 0; seed < 200; seed++)
            {
                var look = CharacterRandomizer.Roll(seed, catalog, Gender.Male);
                Assert.AreNotEqual("torso_blouse_1",
                    catalog.Preset(Gender.Male, 0, look.preset[0]).objectName);
            }
        }

        [Test]
        public void ConflictingPresetsNeverAppearTogether()
        {
            var catalog = Build();

            for (int seed = 0; seed < 200; seed++)
            {
                var look = CharacterRandomizer.Roll(seed, catalog, Gender.Male);

                bool hoodie = catalog.Preset(Gender.Male, 0, look.preset[0]).objectName
                              == "torso_hoodie_1";
                bool strap = catalog.Preset(Gender.Male, 1, look.preset[1]).objectName
                             == "legs_backpack_strap_1";

                Assert.IsFalse(hoodie && strap, $"seed {seed} put the strap over the hoodie");
            }
        }

        [Test]
        public void EveryColourClassGetsAColorway()
        {
            var catalog = Build();
            var look = CharacterRandomizer.Roll(7, catalog, Gender.Male);

            Assert.AreEqual(1, look.colorway.Length);
            Assert.Less(look.colorway[0], catalog.ColorwayCount(0));
        }

        [Test]
        public void ASlotClassWithNoLegalPresetIsLeftEmpty()
        {
            var catalog = Build();
            // Forbid everything in slot class 0 by giving the class a single gated preset the
            // rolled gender cannot wear.
            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry { slotClass = 0, objectName = "torso_blouse_1",
                                      gender = GenderGate.Female, weight = 1 },
                    new PresetEntry { slotClass = 1, objectName = "legs_jeans_1",
                                      gender = GenderGate.Any, weight = 1 },
                },
                presetStart = new[] { 0, 1, 2 },
            };

            var look = CharacterRandomizer.Roll(3, catalog, Gender.Male);

            Assert.AreEqual(CharacterAppearance.None, look.preset[0]);
            Assert.AreEqual(0, look.preset[1]);
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `CharacterAppearance` neexistuje.

- [ ] **Krok 3: Napíš `CharacterAppearance.cs`**

```csharp
using System;

namespace FriWorld.Character
{
    /// <summary>
    /// One character's whole look, as indices. Small enough to be a save file field and a spawn
    /// argument at once: this is what gets stored for the player and what a seed produces for
    /// an NPC.
    /// </summary>
    [Serializable]
    public struct CharacterAppearance
    {
        /// <summary>No legal preset for that slot class. Apply strips the class entirely.</summary>
        public const byte None = byte.MaxValue;

        public Gender gender;

        /// <summary>Index within the slot class, parallel to CharacterCatalog.slotClasses.</summary>
        public byte[] preset;

        /// <summary>Index within the colour class, parallel to CharacterCatalog.colorClasses.</summary>
        public byte[] colorway;
    }
}
```

- [ ] **Krok 4: Napíš `CharacterRandomizer.cs`**

```csharp
using System.Collections.Generic;

namespace FriWorld.Character
{
    /// <summary>
    /// Rolls a legal look from a seed.
    ///
    /// Deterministic on purpose: an NPC whose seed comes from its identity looks the same after
    /// a respawn without anything being stored. System.Random rather than UnityEngine.Random so
    /// a roll cannot be disturbed by, or disturb, whatever else is drawing random numbers.
    /// </summary>
    public static class CharacterRandomizer
    {
        public static CharacterAppearance Roll(int seed, CharacterCatalog catalog, Gender gender)
        {
            var rng = new System.Random(seed);

            var look = new CharacterAppearance
            {
                gender = gender,
                preset = new byte[catalog.slotClasses.Length],
                colorway = new byte[catalog.colorClasses.Length],
            };

            int takenTags = 0;
            int forbiddenTags = 0;

            var candidates = new List<int>();
            var weights = new List<int>();

            for (int slot = 0; slot < catalog.slotClasses.Length; slot++)
            {
                candidates.Clear();
                weights.Clear();

                int count = catalog.PresetCount(gender, slot);
                for (int i = 0; i < count; i++)
                {
                    var candidate = catalog.Preset(gender, slot, i);
                    if (!PresetRules.IsAllowed(candidate, gender, takenTags, forbiddenTags)) continue;

                    candidates.Add(i);
                    weights.Add(candidate.weight);
                }

                if (candidates.Count == 0)
                {
                    // Falling back to index 0 would quietly break whichever rule excluded it.
                    // Leaving the class empty is visible and Report already warns about a class
                    // that can never be filled.
                    look.preset[slot] = CharacterAppearance.None;
                    continue;
                }

                int picked = candidates[PresetRules.PickWeighted(weights, rng.NextDouble())];
                look.preset[slot] = (byte)picked;

                var chosen = catalog.Preset(gender, slot, picked);
                takenTags |= chosen.tagMask;
                forbiddenTags |= chosen.conflictMask;
            }

            for (int colorClass = 0; colorClass < catalog.colorClasses.Length; colorClass++)
            {
                int count = catalog.ColorwayCount(colorClass);
                look.colorway[colorClass] = count == 0
                    ? CharacterAppearance.None
                    : (byte)rng.Next(count);
            }

            return look;
        }
    }
}
```

- [ ] **Krok 5: Spusti testy, over, že prechádzajú**

Očakávané: 6 testov PASS. Ak `ConflictingPresetsNeverAppearTogether` padne, pravidlo sa
kontroluje len jedným smerom — vráť sa k `PresetRules.IsAllowed`.

- [ ] **Krok 6: Commit**

```bash
git add Assets/_Game/Scripts/Character/CharacterAppearance.cs Assets/_Game/Scripts/Character/CharacterRandomizer.cs Assets/_Game/Editor/Character/Tests/CharacterRandomizerTests.cs && git commit -m "feat(character): roll a legal appearance from a seed"
```

---

## Úloha 9: Validácia registrov

Oddelená od Unity: `CharacterValidation.Check` berie už naskenované dáta, takže sa dá otestovať
bez prefabu. Skenovanie prefabu robí úloha 12.

**Súbory:**
- Create: `Assets/_Game/Editor/Character/CharacterValidation.cs`
- Test: `Assets/_Game/Editor/Character/Tests/CharacterValidationTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using System.Collections.Generic;
using System.Linq;
using FriWorld.Character;
using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class CharacterValidationTests
    {
        static ClassRegistry Classes() => new ClassRegistry
        {
            colorClasses = new List<ColorClassDef>
            {
                new ColorClassDef { name = "torso", mainColors = 2,
                                    shadeValue = 0.62f, shadeSaturation = 1.12f },
            },
            slotClasses = new List<string> { "torso" },
        };

        static ColorwayRegistry Colorways() => new ColorwayRegistry
        {
            colorways = new List<ColorwayDef>
            {
                new ColorwayDef { colorClass = "torso", id = "navy", displayName = "Tmavomodrá",
                                  colors = new List<string> { "#243B6B", "#C8CEDA" } },
            },
        };

        static PresetRegistry Presets() => new PresetRegistry
        {
            presets = new List<PresetDef>
            {
                new PresetDef { slotClass = "torso", objectName = "torso_hoodie_1",
                                displayName = "Mikina", gender = "any",
                                hides = new List<string> { "chest" },
                                tags = new List<string> { "casual" },
                                conflicts = new List<string>(), weight = 1 },
            },
        };

        /// A body carrying every section plus the one preset, all names well formed.
        static ScannedBody Body(Gender gender)
        {
            var body = new ScannedBody { gender = gender, prefabPath = "test.prefab" };

            string prefix = gender == Gender.Male ? "male_body_" : "female_body_";
            foreach (var entry in BodySectionNames.All)
                body.objects.Add(new ScannedObject
                {
                    name = prefix + entry.key,
                    materialNames = new[] { "char_skin_1" },
                });

            body.objects.Add(new ScannedObject
            {
                name = "torso_hoodie_1",
                materialNames = new[] { "char_torso_1", "char_torso_2", "char_torso_11" },
            });

            return body;
        }

        static List<Issue> Run(ScannedBody body) => CharacterValidation.Check(
            Classes(), Colorways(), Presets(), new[] { body });

        static bool HasError(IEnumerable<Issue> issues, string fragment) =>
            issues.Any(i => i.severity == Severity.Error && i.text.Contains(fragment));

        [Test]
        public void ACleanSetOnlyProducesNotes()
        {
            var body = Body(Gender.Male);
            // "skin" is not declared as a colour class in this fixture, so every section slot is
            // an ignored note rather than an error.
            var issues = Run(body);

            Assert.IsFalse(issues.Any(i => i.severity == Severity.Error),
                string.Join("\n", issues.Select(i => i.text)));
        }

        [Test]
        public void AMissingBodySectionIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.RemoveAll(o => o.name == "male_body_chest");

            Assert.IsTrue(HasError(Run(body), "chest"));
        }

        [Test]
        public void AMissingPresetObjectIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.RemoveAll(o => o.name == "torso_hoodie_1");

            Assert.IsTrue(HasError(Run(body), "torso_hoodie_1"));
        }

        [Test]
        public void ADuplicateObjectNameIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.Add(new ScannedObject { name = "male_body_chest", materialNames = new string[0] });

            Assert.IsTrue(HasError(Run(body), "DUPLICATE"));
        }

        [Test]
        public void AnUnparseableMaterialNameIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.Add(new ScannedObject
            {
                name = "extra_1",
                materialNames = new[] { "char_torzo" },
            });

            Assert.IsTrue(HasError(Run(body), "UNPARSED"));
        }

        [Test]
        public void AMaterialOutsideTheColourClassesIsANoteNotAnError()
        {
            var body = Body(Gender.Male);
            body.objects.First(o => o.name == "torso_hoodie_1").materialNames =
                new[] { "char_torso_1", "char_leather_1" };

            var issues = Run(body);

            Assert.IsFalse(issues.Any(i => i.severity == Severity.Error),
                string.Join("\n", issues.Select(i => i.text)));
            Assert.IsTrue(issues.Any(i => i.text.Contains("char_leather_1")));
        }

        [Test]
        public void AColourBeyondMainColorsIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.First(o => o.name == "torso_hoodie_1").materialNames =
                new[] { "char_torso_3" };

            Assert.IsTrue(HasError(Run(body), "RANGE"));
        }

        [Test]
        public void AColorwayWithTheWrongNumberOfColoursIsAnError()
        {
            var colorways = Colorways();
            colorways.colorways[0].colors = new List<string> { "#243B6B" };

            var issues = CharacterValidation.Check(
                Classes(), colorways, Presets(), new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "COUNT"));
        }

        [Test]
        public void AConflictOnATagNobodyProvidesIsAnError()
        {
            var presets = Presets();
            presets.presets[0].conflicts = new List<string> { "backpack" };

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "DEAD"));
        }

        [Test]
        public void AHidesEntryThatIsNotASectionIsAnError()
        {
            var presets = Presets();
            presets.presets[0].hides = new List<string> { "torso" };

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "SECTION"));
        }

        [Test]
        public void ASlotClassWithNoPresetForThisGenderIsAnError()
        {
            var presets = Presets();
            presets.presets[0].gender = "female";

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "EMPTY"));
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `CharacterValidation` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    public enum Severity { Error, Note }

    public struct Issue
    {
        public Severity severity;
        public string text;

        public override string ToString() =>
            (severity == Severity.Error ? "ERROR " : "note  ") + text;
    }

    /// <summary>One renderer found while scanning a base prefab.</summary>
    public sealed class ScannedObject
    {
        public string name;
        public string[] materialNames = Array.Empty<string>();
    }

    public sealed class ScannedBody
    {
        public string prefabPath;
        public Gender gender;
        public List<ScannedObject> objects = new List<ScannedObject>();
    }

    /// <summary>
    /// Everything Report checks, with no Unity scene access of its own.
    ///
    /// The split matters: scanning a prefab needs the editor and a real asset, deciding whether
    /// what was scanned is coherent does not. Keeping the decision pure is what makes the rules
    /// testable one by one instead of by opening a prefab.
    ///
    /// Errors mean the bake would produce something wrong. Notes are things worth knowing —
    /// above all a material whose keyword is not a colour class, which is a legitimate way to
    /// say "leave this slot alone" and must not read as a failure.
    /// </summary>
    public static class CharacterValidation
    {
        public static List<Issue> Check(
            ClassRegistry classes,
            ColorwayRegistry colorways,
            PresetRegistry presets,
            IReadOnlyList<ScannedBody> bodies)
        {
            var issues = new List<Issue>();

            void Error(string text) => issues.Add(new Issue { severity = Severity.Error, text = text });
            void Note(string text) => issues.Add(new Issue { severity = Severity.Note, text = text });

            // ---- classes -------------------------------------------------------------
            var colorClassByName = new Dictionary<string, ColorClassDef>(StringComparer.Ordinal);
            foreach (var def in classes.colorClasses)
            {
                if (colorClassByName.ContainsKey(def.name))
                    Error($"DUPLICATE colour class '{def.name}' appears twice in CharacterClasses.json");
                else
                    colorClassByName[def.name] = def;

                if (def.mainColors < 1 || def.mainColors > 9)
                    Error($"RANGE colour class '{def.name}' declares mainColors {def.mainColors}, must be 1..9");

                if (def.shadeValue.HasValue != def.shadeSaturation.HasValue)
                    Error($"SHADE colour class '{def.name}' sets only one of shadeValue / shadeSaturation");
            }

            var slotClasses = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in classes.slotClasses)
                if (!slotClasses.Add(name))
                    Error($"DUPLICATE slot class '{name}' appears twice in CharacterClasses.json");

            // ---- colorways -----------------------------------------------------------
            var colorwayCount = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var way in colorways.colorways)
            {
                if (!colorClassByName.TryGetValue(way.colorClass, out var def))
                {
                    Error($"UNKNOWN colorway '{way.id}' names colour class '{way.colorClass}', "
                          + "which CharacterClasses.json does not declare");
                    continue;
                }

                int given = way.colors?.Count ?? 0;
                if (given != def.mainColors)
                    Error($"COUNT colorway '{way.colorClass}/{way.id}' lists {given} colours, "
                          + $"the class declares mainColors {def.mainColors}");

                if (way.colors != null)
                    foreach (string hex in way.colors)
                        if (!ColorUtility.TryParseHtmlString(hex, out _))
                            Error($"COLOUR colorway '{way.colorClass}/{way.id}' has an unreadable colour '{hex}'");

                colorwayCount.TryGetValue(way.colorClass, out int seen);
                colorwayCount[way.colorClass] = seen + 1;
            }

            foreach (var pair in colorClassByName)
            {
                colorwayCount.TryGetValue(pair.Key, out int count);
                if (count == 0)
                    Error($"EMPTY colour class '{pair.Key}' has no colorway");
                else if (count > 254)
                    Error($"OVERFLOW colour class '{pair.Key}' has {count} colorways, the index holds 254");
            }

            // ---- tags ----------------------------------------------------------------
            var providedTags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var preset in presets.presets)
                if (preset.tags != null)
                    foreach (string tag in preset.tags)
                        providedTags.Add(tag);

            if (providedTags.Count > 32)
                Error($"OVERFLOW {providedTags.Count} distinct tags, the bitmask holds 32");

            // ---- presets -------------------------------------------------------------
            foreach (var preset in presets.presets)
            {
                string who = preset.objectName ?? "(no object)";

                if (string.IsNullOrEmpty(preset.objectName))
                    Error($"MISSING a preset in slot class '{preset.slotClass}' has no object name");

                if (!slotClasses.Contains(preset.slotClass))
                    Error($"UNKNOWN preset '{who}' names slot class '{preset.slotClass}', "
                          + "which CharacterClasses.json does not declare");

                if (ParseGender(preset.gender) == null)
                    Error($"GENDER preset '{who}' has gender '{preset.gender}', expected any / male / female");

                if (preset.weight < 1)
                    Error($"WEIGHT preset '{who}' has weight {preset.weight}, must be at least 1");

                if (preset.hides != null)
                    foreach (string section in preset.hides)
                        if (!BodySectionNames.TryParseKey(section, out _))
                            Error($"SECTION preset '{who}' hides '{section}', which is not a body section");

                if (preset.conflicts != null)
                    foreach (string tag in preset.conflicts)
                        if (!providedTags.Contains(tag))
                            Error($"DEAD preset '{who}' conflicts with tag '{tag}', which no preset provides");
            }

            // ---- per body ------------------------------------------------------------
            foreach (var body in bodies)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var scanned in body.objects)
                    if (!names.Add(scanned.name))
                        Error($"DUPLICATE {body.gender}: two objects named '{scanned.name}' in "
                              + $"{body.prefabPath}; the slot map is keyed on the name");

                // Which sections the body actually carries. Resolved through the object name
                // so male_body_chest and female_body_chest both land on Chest.
                int present = 0;
                foreach (var scanned in body.objects)
                    if (BodySectionNames.TryParseObject(scanned.name, out var found))
                        present |= (int)found;

                foreach (var entry in BodySectionNames.All)
                    if ((present & (int)entry.section) == 0)
                        Error($"MISSING {body.gender}: body section '{entry.key}' is not in {body.prefabPath}");

                foreach (var preset in presets.presets)
                {
                    var gate = ParseGender(preset.gender);
                    if (gate == null || !PresetRules.GenderAllows(gate.Value, body.gender)) continue;

                    if (!names.Contains(preset.objectName))
                        Error($"MISSING {body.gender}: preset object '{preset.objectName}' is not in {body.prefabPath}");
                }

                foreach (string slotClass in classes.slotClasses)
                {
                    int usable = 0;
                    foreach (var preset in presets.presets)
                    {
                        if (preset.slotClass != slotClass) continue;
                        var gate = ParseGender(preset.gender);
                        if (gate != null && PresetRules.GenderAllows(gate.Value, body.gender)) usable++;
                    }

                    if (usable == 0)
                        Error($"EMPTY {body.gender}: slot class '{slotClass}' has no preset — "
                              + "the NPC would be missing that part");
                    else if (usable > 254)
                        Error($"OVERFLOW {body.gender}: slot class '{slotClass}' has {usable} presets, "
                              + "the index holds 254");
                }

                foreach (var scanned in body.objects)
                {
                    foreach (string materialName in scanned.materialNames)
                    {
                        if (!MaterialSlotKey.TryParse(materialName, out var slot))
                        {
                            Error($"UNPARSED {body.gender}: '{scanned.name}' carries '{materialName}', "
                                  + "which is not char_<class>_<key>");
                            continue;
                        }

                        if (!colorClassByName.TryGetValue(slot.ColorClass, out var def))
                        {
                            Note($"IGNORED {body.gender}: '{scanned.name}' slot '{materialName}' — "
                                 + $"'{slot.ColorClass}' is not a colour class, the slot stays as authored");
                            continue;
                        }

                        if (slot.BaseKey > def.mainColors)
                            Error($"RANGE {body.gender}: '{materialName}' asks for colour {slot.BaseKey}, "
                                  + $"class '{slot.ColorClass}' declares {def.mainColors}");

                        if (slot.ShadeLevel > 1)
                            Error($"SHADE {body.gender}: '{materialName}' asks for shade level "
                                  + $"{slot.ShadeLevel}; only level 1 is supported");

                        if (slot.ShadeLevel == 1 && !def.shadeValue.HasValue)
                            Error($"SHADE {body.gender}: '{materialName}' asks for a shade, "
                                  + $"class '{slot.ColorClass}' declares no shadeValue");
                    }
                }
            }

            return issues;
        }

        public static GenderGate? ParseGender(string gender)
        {
            switch (gender)
            {
                case "any":    return GenderGate.Any;
                case "male":   return GenderGate.Male;
                case "female": return GenderGate.Female;
                default:       return null;
            }
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 11 testov PASS.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Editor/Character/CharacterValidation.cs Assets/_Game/Editor/Character/Tests/CharacterValidationTests.cs && git commit -m "feat(character): validate the registers against a scanned body"
```

---

## Úloha 10: Skenovanie prefabu a menu `Character/1 — Report`

**Súbory:**
- Create: `Assets/_Game/Editor/Character/CharacterScan.cs`
- Create: `Assets/_Game/Editor/Character/CharacterReport.cs`

Prefaby sa otvárajú cez `PrefabUtility.LoadPrefabContents` — izolovaná preview scéna nedvíha
modálne dialógy, čo cez MCP inak padne.

- [ ] **Krok 1: Napíš `CharacterScan.cs`**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Reads the two base prefabs into the plain data CharacterValidation and the baker work on.
    ///
    /// LoadPrefabContents rather than opening the prefab in the stage: the isolated preview scene
    /// raises no modal dialogs, which is what makes this runnable from a script or over MCP.
    /// </summary>
    public static class CharacterScan
    {
        public const string MalePrefabPath = "Assets/_Game/Prefabs/Character/char_base_male.prefab";
        public const string FemalePrefabPath = "Assets/_Game/Prefabs/Character/char_base_female.prefab";

        public static string PathFor(Gender gender) =>
            gender == Gender.Male ? MalePrefabPath : FemalePrefabPath;

        /// <summary>Returns null when the prefab is not there, so the caller can say which one.</summary>
        public static ScannedBody Read(Gender gender)
        {
            string path = PathFor(gender);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return null;

            var body = new ScannedBody { gender = gender, prefabPath = path };
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var materialNames = new List<string>();
                    foreach (var material in renderer.sharedMaterials)
                        materialNames.Add(material != null ? material.name : string.Empty);

                    body.objects.Add(new ScannedObject
                    {
                        name = renderer.gameObject.name,
                        materialNames = materialNames.ToArray(),
                    });
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return body;
        }

        public static List<ScannedBody> ReadBoth(List<string> missing)
        {
            var bodies = new List<ScannedBody>();

            foreach (Gender gender in new[] { Gender.Male, Gender.Female })
            {
                var body = Read(gender);
                if (body == null) missing.Add(PathFor(gender));
                else bodies.Add(body);
            }

            return bodies;
        }
    }
}
```

- [ ] **Krok 2: Napíš `CharacterReport.cs`**

```csharp
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 1 — Report. Reads everything, writes nothing.
    ///
    /// It never guesses. A name it cannot place is reported, not resolved to something similar —
    /// the same discipline the object type registry runs on, and for the same reason: a silent
    /// near-match is a bug you find months later on one NPC.
    /// </summary>
    public static class CharacterReport
    {
        public static void Run()
        {
            var missing = new List<string>();
            var bodies = CharacterScan.ReadBoth(missing);

            var issues = CharacterValidation.Check(
                CharacterRegistries.LoadClasses(),
                CharacterRegistries.LoadColorways(),
                CharacterRegistries.LoadPresets(),
                bodies);

            var report = new StringBuilder();
            report.AppendLine("Character Report");
            report.AppendLine();

            foreach (string path in missing)
                report.AppendLine($"ERROR MISSING base prefab {path} does not exist");

            int errors = missing.Count;
            int notes = 0;

            foreach (var issue in issues)
            {
                report.AppendLine(issue.ToString());
                if (issue.severity == Severity.Error) errors++;
                else notes++;
            }

            report.AppendLine();
            report.AppendLine($"{errors} errors, {notes} notes, {bodies.Count} bodies scanned.");

            if (errors > 0) Debug.LogError(report.ToString());
            else Debug.Log(report.ToString());
        }
    }
}
```

- [ ] **Krok 3: Over ručne**

`Assets/Refresh`, počkaj na dokompilovanie. Zavolaj `CharacterReport.Run()` (menu pribudne
v úlohe 13, dovtedy cez Unity MCP `Unity_RunCommand`). Bez prefabov musí vypísať dva riadky
`MISSING base prefab` a nespadnúť.

- [ ] **Krok 4: Commit**

```bash
git add Assets/_Game/Editor/Character/CharacterScan.cs Assets/_Game/Editor/Character/CharacterReport.cs && git commit -m "feat(character): scan the base prefabs and report what does not line up"
```

---

## Úloha 11: Generátor materiálov `Character/2 — Generate Shades`

**Súbory:**
- Create: `Assets/_Game/Editor/Character/ShadeMaterialGenerator.cs`

- [ ] **Krok 1: Napíš implementáciu**

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 2 — Generate Shades.
    ///
    /// Turns every colorway into real .mat assets, one per slot key. Doing it here rather than at
    /// runtime is what keeps the swap free: applying a look is then a reference assignment, the
    /// materials stay shared across every NPC wearing that colorway, and the SRP Batcher keeps
    /// batching them.
    ///
    /// The look of a material — shader, normal map, smoothness — comes from the source template
    /// extracted from the FBX. Only _BaseColor is overwritten, so re-running this never undoes
    /// art work.
    /// </summary>
    public static class ShadeMaterialGenerator
    {
        const string SourceDir = "Assets/_Game/Art/Materials/Character/_source";
        const string OutputRoot = "Assets/_Game/Art/Materials/Character";

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static void Run()
        {
            var classes = CharacterRegistries.LoadClasses();
            var colorways = CharacterRegistries.LoadColorways();

            var classByName = new Dictionary<string, ColorClassDef>();
            foreach (var def in classes.colorClasses) classByName[def.name] = def;

            int written = 0;
            var problems = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var way in colorways.colorways)
                {
                    if (!classByName.TryGetValue(way.colorClass, out var def))
                    {
                        problems.Add($"colorway '{way.id}' names unknown colour class '{way.colorClass}'");
                        continue;
                    }

                    Directory.CreateDirectory(Path.Combine(OutputRoot, way.colorClass));

                    for (int baseKey = 1; baseKey <= def.mainColors; baseKey++)
                    {
                        if (way.colors == null || way.colors.Count < baseKey)
                        {
                            problems.Add($"colorway '{way.colorClass}/{way.id}' has no colour {baseKey}");
                            continue;
                        }

                        if (!ColorUtility.TryParseHtmlString(way.colors[baseKey - 1], out var color))
                        {
                            problems.Add($"colorway '{way.colorClass}/{way.id}' colour {baseKey} "
                                         + $"'{way.colors[baseKey - 1]}' is unreadable");
                            continue;
                        }

                        if (Write(way, def, baseKey, 0, color, problems)) written++;

                        if (def.shadeValue.HasValue && def.shadeSaturation.HasValue)
                        {
                            var shade = ShadeColor.Derive(color, def.shadeValue.Value,
                                                          def.shadeSaturation.Value);
                            if (Write(way, def, baseKey, 1, shade, problems)) written++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string summary = $"Generate Shades: {written} materials written, {problems.Count} problems.";
            if (problems.Count > 0) Debug.LogError(summary + "\n" + string.Join("\n", problems));
            else Debug.Log(summary);
        }

        /// <summary>Creates or updates one material. Returns false when the template is missing.</summary>
        static bool Write(ColorwayDef way, ColorClassDef def, int baseKey, int shadeLevel,
                          Color color, List<string> problems)
        {
            string key = shadeLevel == 0 ? baseKey.ToString() : $"{baseKey}{shadeLevel}";

            // Prefer a template authored for the shade slot itself; fall back to the base slot,
            // which is the common case — the shade usually only differs in colour.
            var template = LoadTemplate($"char_{def.name}_{key}")
                           ?? LoadTemplate($"char_{def.name}_{baseKey}");

            if (template == null)
            {
                problems.Add($"no source template for char_{def.name}_{key} in {SourceDir}");
                return false;
            }

            string path = $"{OutputRoot}/{def.name}/mt_char_{def.name}_{way.id}_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                var created = new Material(template) { name = Path.GetFileNameWithoutExtension(path) };
                created.SetColor(BaseColor, color);
                AssetDatabase.CreateAsset(created, path);
            }
            else
            {
                // Keep the asset — its GUID is already in the baked catalog and in any prefab
                // that happens to reference it. Only the colour is re-derived.
                existing.shader = template.shader;
                existing.CopyPropertiesFromMaterial(template);
                existing.SetColor(BaseColor, color);
                EditorUtility.SetDirty(existing);
            }

            return true;
        }

        static Material LoadTemplate(string materialName) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{SourceDir}/{materialName}.mat");
    }
}
```

- [ ] **Krok 2: Over ručne**

Polož do `Assets/_Game/Art/Materials/Character/_source/` aspoň `char_torso_1.mat`
a `char_torso_2.mat` (URP/Lit stačí). Spusti `ShadeMaterialGenerator.Run()`.

Očakávané: v `Assets/_Game/Art/Materials/Character/torso/` pribudnú
`mt_char_torso_navy_1.mat`, `mt_char_torso_navy_2.mat`, `mt_char_torso_navy_11.mat`,
`mt_char_torso_navy_21.mat`. Odtieň `_11` musí byť **viditeľne tmavší** než `_1`.

Spusti to druhýkrát — nesmú pribudnúť duplikáty a GUID existujúcich materiálov sa nesmú zmeniť
(`git status` ukáže zmenený `.mat`, nie nový `.mat.meta`).

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Editor/Character/ShadeMaterialGenerator.cs && git commit -m "feat(character): generate colorway materials and their derived shades"
```

---

## Úloha 12: Bake katalógu `Character/3 — Bake Catalog`

**Súbory:**
- Create: `Assets/_Game/Editor/Character/CharacterCatalogBaker.cs`

- [ ] **Krok 1: Napíš implementáciu**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 3 — Bake Catalog.
    ///
    /// Compiles the three registers into CharacterCatalog.asset: names become indices, colorway
    /// ids become Material references, and every renderer's material names become a slot map.
    /// After this the game reads one asset and parses nothing.
    ///
    /// It refuses to write while Report still finds an error. A catalog baked from a broken
    /// register is worse than no catalog, because it looks like it worked.
    /// </summary>
    public static class CharacterCatalogBaker
    {
        public const string CatalogPath = "Assets/Resources/CharacterCatalog.asset";
        const string MaterialRoot = "Assets/_Game/Art/Materials/Character";

        public static void Run()
        {
            var classes = CharacterRegistries.LoadClasses();
            var colorwayRegistry = CharacterRegistries.LoadColorways();
            var presetRegistry = CharacterRegistries.LoadPresets();

            var missing = new List<string>();
            var bodies = CharacterScan.ReadBoth(missing);

            var issues = CharacterValidation.Check(classes, colorwayRegistry, presetRegistry, bodies);

            int errors = missing.Count;
            foreach (var issue in issues)
                if (issue.severity == Severity.Error) errors++;

            if (errors > 0)
            {
                Debug.LogError($"Bake Catalog refused: Report finds {errors} errors. "
                               + "Run Character > 1 — Report and fix them first.");
                return;
            }

            var catalog = LoadOrCreate();

            catalog.slotClasses = classes.slotClasses.ToArray();

            var colorClassNames = new List<string>();
            foreach (var def in classes.colorClasses) colorClassNames.Add(def.name);
            catalog.colorClasses = colorClassNames.ToArray();

            catalog.tags = CollectTags(presetRegistry);

            BakeColorways(catalog, classes, colorwayRegistry);

            foreach (var body in bodies)
            {
                var bundle = body.gender == Gender.Male ? catalog.male : catalog.female;
                bundle.basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(body.prefabPath);
                BakePresets(catalog, bundle, presetRegistry, body.gender);
                BakeSlotMaps(catalog, bundle, classes, body);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"Bake Catalog: {catalog.slotClasses.Length} slot classes, "
                      + $"{catalog.colorClasses.Length} colour classes, "
                      + $"{catalog.colorways.Length} colorways, "
                      + $"{catalog.tags.Length} tags, "
                      + $"male {catalog.male.presets.Length} presets / {catalog.male.slotMaps.Length} slot maps, "
                      + $"female {catalog.female.presets.Length} presets / {catalog.female.slotMaps.Length} slot maps.");
        }

        static CharacterCatalog LoadOrCreate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        static string[] CollectTags(PresetRegistry presets)
        {
            var tags = new List<string>();
            foreach (var preset in presets.presets)
            {
                if (preset.tags == null) continue;
                foreach (string tag in preset.tags)
                    if (!tags.Contains(tag)) tags.Add(tag);
            }
            tags.Sort(StringComparer.Ordinal);   // stable order, so a rebake gives the same masks
            return tags.ToArray();
        }

        static int MaskOf(IEnumerable<string> tags, string[] table)
        {
            int mask = 0;
            if (tags == null) return mask;

            foreach (string tag in tags)
            {
                int index = Array.IndexOf(table, tag);
                if (index >= 0) mask |= 1 << index;
            }
            return mask;
        }

        static void BakeColorways(CharacterCatalog catalog, ClassRegistry classes,
                                  ColorwayRegistry registry)
        {
            var entries = new List<ColorwayEntry>();
            var start = new int[catalog.colorClasses.Length + 1];

            for (int c = 0; c < catalog.colorClasses.Length; c++)
            {
                start[c] = entries.Count;
                string className = catalog.colorClasses[c];
                var def = classes.colorClasses.Find(d => d.name == className);

                foreach (var way in registry.colorways)
                {
                    if (way.colorClass != className) continue;

                    var materials = new Material[def.mainColors * 2];
                    for (int baseKey = 1; baseKey <= def.mainColors; baseKey++)
                    {
                        materials[(baseKey - 1) * 2] = LoadMaterial(className, way.id, baseKey.ToString());
                        if (def.shadeValue.HasValue)
                            materials[(baseKey - 1) * 2 + 1] =
                                LoadMaterial(className, way.id, $"{baseKey}1");
                    }

                    entries.Add(new ColorwayEntry
                    {
                        colorClass = c,
                        id = way.id,
                        displayName = way.displayName,
                        materials = materials,
                    });
                }
            }

            start[catalog.colorClasses.Length] = entries.Count;
            catalog.colorways = entries.ToArray();
            catalog.colorwayStart = start;
        }

        static Material LoadMaterial(string colorClass, string colorwayId, string key) =>
            AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/{colorClass}/mt_char_{colorClass}_{colorwayId}_{key}.mat");

        static void BakePresets(CharacterCatalog catalog, GenderBundle bundle,
                                PresetRegistry registry, Gender gender)
        {
            var entries = new List<PresetEntry>();
            var start = new int[catalog.slotClasses.Length + 1];

            for (int s = 0; s < catalog.slotClasses.Length; s++)
            {
                start[s] = entries.Count;
                string slotClass = catalog.slotClasses[s];

                foreach (var preset in registry.presets)
                {
                    if (preset.slotClass != slotClass) continue;

                    var gate = CharacterValidation.ParseGender(preset.gender);
                    if (gate == null || !PresetRules.GenderAllows(gate.Value, gender)) continue;

                    int hides = 0;
                    if (preset.hides != null)
                        foreach (string section in preset.hides)
                            if (BodySectionNames.TryParseKey(section, out var parsed))
                                hides |= (int)parsed;

                    entries.Add(new PresetEntry
                    {
                        slotClass = s,
                        objectName = preset.objectName,
                        displayName = preset.displayName,
                        gender = gate.Value,
                        hides = hides,
                        tagMask = MaskOf(preset.tags, catalog.tags),
                        conflictMask = MaskOf(preset.conflicts, catalog.tags),
                        weight = preset.weight,
                    });
                }
            }

            start[catalog.slotClasses.Length] = entries.Count;
            bundle.presets = entries.ToArray();
            bundle.presetStart = start;
        }

        static void BakeSlotMaps(CharacterCatalog catalog, GenderBundle bundle,
                                 ClassRegistry classes, ScannedBody body)
        {
            var maps = new List<RendererSlotMap>();

            foreach (var scanned in body.objects)
            {
                int count = scanned.materialNames.Length;
                var colorClass = new int[count];
                var materialIndex = new int[count];
                bool anythingToDo = false;

                for (int i = 0; i < count; i++)
                {
                    colorClass[i] = -1;
                    materialIndex[i] = -1;

                    if (!MaterialSlotKey.TryParse(scanned.materialNames[i], out var slot)) continue;

                    int index = Array.IndexOf(catalog.colorClasses, slot.ColorClass);
                    if (index < 0) continue;   // char_leather_1 and friends: left as authored

                    colorClass[i] = index;
                    materialIndex[i] = (slot.BaseKey - 1) * 2 + slot.ShadeLevel;
                    anythingToDo = true;
                }

                // A renderer with nothing to recolour needs no entry; SlotMap returning null is
                // already the "leave it alone" path.
                if (!anythingToDo) continue;

                maps.Add(new RendererSlotMap
                {
                    objectName = scanned.name,
                    colorClass = colorClass,
                    materialIndex = materialIndex,
                });
            }

            bundle.slotMaps = maps.ToArray();
        }
    }
}
```

- [ ] **Krok 2: Over ručne**

Spusti `CharacterCatalogBaker.Run()` s prázdnymi prefabmi.
Očakávané: `Bake Catalog refused: Report finds N errors` — bake sa nesmie zapísať, kým Report
nie je čistý.

Potom s hotovými placeholder prefabmi z úlohy 0: vznikne `Assets/Resources/CharacterCatalog.asset`
a log vypíše počty. Otvor asset v inšpektore a over, že `colorways` majú vyplnené `materials`.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Editor/Character/CharacterCatalogBaker.cs && git commit -m "feat(character): bake the registers into a catalog asset"
```

---

## Úloha 13: Menu `Character`

**Súbory:**
- Create: `Assets/_Game/Editor/Character/CharacterMenu.cs`

- [ ] **Krok 1: Napíš implementáciu**

```csharp
using UnityEditor;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// The Character menu, in the order it is run.
    ///
    /// Same shape as Routine: numbered, because a Unity menu item cannot carry a tooltip and the
    /// order is not free — shades have to exist before the catalog can reference them.
    /// </summary>
    public static class CharacterMenu
    {
        const int Scan = 100;
        const int Generate = 130;

        [MenuItem("Character/1 — Report", priority = Scan)]
        static void Step1() => CharacterReport.Run();

        [MenuItem("Character/2 — Generate Shades", priority = Generate)]
        static void Step2() => ShadeMaterialGenerator.Run();

        [MenuItem("Character/3 — Bake Catalog", priority = Generate + 1)]
        static void Step3() => CharacterCatalogBaker.Run();
    }
}
```

- [ ] **Krok 2: Over ručne**

`Assets/Refresh`. V menu baru musí pribudnúť **Character** s tromi položkami v poradí 1, 2, 3.
Klikni na každú, žiadna nesmie hodiť výnimku.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Editor/Character/CharacterMenu.cs && git commit -m "feat(character): add the Character menu"
```

---

## Úloha 14: `CharacterBuilder` — poskladanie postavy

**Súbory:**
- Create: `Assets/_Game/Scripts/Character/CharacterBuilder.cs`
- Test: `Assets/_Game/Editor/Character/Tests/CharacterBuilderTests.cs`

- [ ] **Krok 1: Napíš padajúci test**

```csharp
using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterBuilderTests
    {
        CharacterCatalog catalog;
        GameObject instance;
        Material navy;
        Material navyShade;
        Material leather;

        [SetUp]
        public void SetUp()
        {
            navy = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "navy_1" };
            navyShade = new Material(navy) { name = "navy_11" };
            leather = new Material(navy) { name = "char_leather_1" };

            catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso" };
            catalog.colorClasses = new[] { "torso" };
            catalog.tags = new string[0];

            catalog.colorways = new[]
            {
                new ColorwayEntry
                {
                    colorClass = 0, id = "navy",
                    materials = new[] { navy, navyShade },
                },
            };
            catalog.colorwayStart = new[] { 0, 1 };

            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry
                    {
                        slotClass = 0, objectName = "torso_hoodie_1", gender = GenderGate.Any,
                        hides = (int)(BodySection.Chest | BodySection.Abdomen), weight = 1,
                    },
                    new PresetEntry
                    {
                        slotClass = 0, objectName = "torso_tank_1", gender = GenderGate.Any,
                        hides = 0, weight = 1,
                    },
                },
                presetStart = new[] { 0, 2 },
                slotMaps = new[]
                {
                    new RendererSlotMap
                    {
                        objectName = "torso_hoodie_1",
                        colorClass = new[] { 0, 0, -1 },
                        materialIndex = new[] { 0, 1, -1 },
                    },
                },
            };

            instance = new GameObject("char_base_male");
            // Section objects carry the gender prefix, exactly as they come out of Blender.
            AddRenderer("male_body_chest", 1);
            AddRenderer("male_body_abdomen", 1);
            AddRenderer("male_body_hand_L", 1);
            AddRenderer("torso_hoodie_1", 3);
            AddRenderer("torso_tank_1", 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(navy);
            Object.DestroyImmediate(navyShade);
            Object.DestroyImmediate(leather);
        }

        void AddRenderer(string name, int slots)
        {
            var child = new GameObject(name);
            child.transform.SetParent(instance.transform);

            var renderer = child.AddComponent<MeshRenderer>();
            var materials = new Material[slots];
            for (int i = 0; i < slots; i++) materials[i] = leather;
            renderer.sharedMaterials = materials;
        }

        Transform Find(string name) => instance.transform.Find(name);

        static CharacterAppearance Look(byte preset, byte colorway) => new CharacterAppearance
        {
            gender = Gender.Male,
            preset = new[] { preset },
            colorway = new[] { colorway },
        };

        [Test]
        public void TheUnchosenPresetIsGone()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            Assert.IsNotNull(Find("torso_hoodie_1"));
            Assert.IsNull(Find("torso_tank_1"));
        }

        [Test]
        public void TheHiddenSectionsAreGone()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            Assert.IsNull(Find("male_body_chest"));
            Assert.IsNull(Find("male_body_abdomen"));
            Assert.IsNotNull(Find("male_body_hand_L"));
        }

        [Test]
        public void APresetThatHidesNothingLeavesTheSkinAlone()
        {
            CharacterBuilder.Apply(instance, Look(1, 0), catalog);

            Assert.IsNotNull(Find("male_body_chest"));
            Assert.IsNotNull(Find("male_body_abdomen"));
        }

        [Test]
        public void TheColourwayMaterialsLandOnTheRightSlots()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            var materials = Find("torso_hoodie_1").GetComponent<MeshRenderer>().sharedMaterials;

            Assert.AreSame(navy, materials[0]);
            Assert.AreSame(navyShade, materials[1]);
        }

        [Test]
        public void ASlotOutsideTheColourClassesIsLeftAsAuthored()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            var materials = Find("torso_hoodie_1").GetComponent<MeshRenderer>().sharedMaterials;

            Assert.AreSame(leather, materials[2]);
        }

        [Test]
        public void AnEmptySlotClassStripsEveryPresetOfThatClass()
        {
            CharacterBuilder.Apply(instance, Look(CharacterAppearance.None, 0), catalog);

            Assert.IsNull(Find("torso_hoodie_1"));
            Assert.IsNull(Find("torso_tank_1"));
        }
    }
}
```

- [ ] **Krok 2: Spusti test, over, že padá**

Očakávané: kompilačná chyba, `CharacterBuilder` neexistuje.

- [ ] **Krok 3: Napíš implementáciu**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    /// <summary>
    /// Turns a base prefab instance into one character.
    ///
    /// Everything not chosen is destroyed rather than deactivated. Twenty NPCs each carrying every
    /// preset as a disabled object is a lot of Transforms for a web build to walk every frame,
    /// and the meshes are shared assets, so destroying the objects costs no VRAM.
    ///
    /// Nothing here allocates a Material. The colorway materials are shared assets, so every NPC
    /// wearing navy points at the same one and the SRP Batcher keeps them in a batch.
    /// </summary>
    public static class CharacterBuilder
    {
        static readonly List<Renderer> Buffer = new List<Renderer>();

        public static void Apply(GameObject instance, in CharacterAppearance look,
                                 CharacterCatalog catalog)
        {
            if (instance == null || catalog == null) return;

            // 1. Which preset object survives in each slot class, and what it covers.
            var keep = new HashSet<string>(System.StringComparer.Ordinal);
            int hidden = 0;

            for (int slot = 0; slot < catalog.slotClasses.Length && slot < look.preset.Length; slot++)
            {
                byte index = look.preset[slot];
                if (index == CharacterAppearance.None) continue;

                var preset = catalog.Preset(look.gender, slot, index);
                keep.Add(preset.objectName);
                hidden |= preset.hides;
            }

            // 2. Everything the catalog knows as a preset object but that was not chosen goes,
            //    together with the skin the chosen clothing covers.
            var bundle = catalog.Bundle(look.gender);
            var drop = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (var preset in bundle.presets)
                if (!keep.Contains(preset.objectName))
                    drop.Add(preset.objectName);

            instance.GetComponentsInChildren(true, Buffer);

            for (int i = Buffer.Count - 1; i >= 0; i--)
            {
                var renderer = Buffer[i];
                if (renderer == null) continue;

                string name = renderer.gameObject.name;

                // A body section is recognised from its own name rather than by building
                // "male_body_" + key here, so the builder never has to know what the gender
                // prefix looks like.
                bool covered = BodySectionNames.TryParseObject(name, out var section)
                               && (hidden & (int)section) != 0;

                if (!covered && !drop.Contains(name)) continue;

                Object.Destroy(renderer.gameObject);
                Buffer[i] = null;
            }

            // 3. Recolour what is left.
            foreach (var renderer in Buffer)
            {
                if (renderer == null) continue;

                var map = catalog.SlotMap(look.gender, renderer.gameObject.name);
                if (map == null) continue;

                var materials = renderer.sharedMaterials;
                bool changed = false;

                int slots = Mathf.Min(materials.Length, map.colorClass.Length);
                for (int i = 0; i < slots; i++)
                {
                    int colorClass = map.colorClass[i];
                    if (colorClass < 0) continue;                       // left as authored
                    if (colorClass >= look.colorway.Length) continue;

                    byte colorwayIndex = look.colorway[colorClass];
                    if (colorwayIndex == CharacterAppearance.None) continue;
                    if (colorwayIndex >= catalog.ColorwayCount(colorClass)) continue;

                    var colorway = catalog.Colorway(colorClass, colorwayIndex);
                    int materialIndex = map.materialIndex[i];
                    var material = materialIndex >= 0 && materialIndex < colorway.materials.Length
                        ? colorway.materials[materialIndex]
                        : null;

                    if (material == null) continue;

                    materials[i] = material;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = materials;
            }

            Buffer.Clear();
        }
    }
}
```

- [ ] **Krok 4: Spusti testy, over, že prechádzajú**

Očakávané: 6 testov PASS.

`Object.Destroy` v EditMode neplatí okamžite. Ak testy hlásia, že objekt ešte existuje, zmeň
v `Apply` `Object.Destroy` na
`if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);` — v builde
sa vetva vyhodnotí na `Destroy`, v teste sa objekt zmizne hneď.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Character/CharacterBuilder.cs Assets/_Game/Editor/Character/Tests/CharacterBuilderTests.cs && git commit -m "feat(character): strip and recolour a base prefab into one character"
```

---

## Úloha 15: `NPCSpawner` spawnuje z katalógu

**Súbory:**
- Modify: `Assets/_Game/Scripts/NPC/NpcSpawner.cs`

- [ ] **Krok 1: Nahraď pole prefabov katalógom**

V `NPCSpawner` zmaž `[SerializeField] private GameObject[] npcPrefabs;` a doplň:

```csharp
using FriWorld.Character;

// …

[Header("Appearance")]
[Tooltip("Baked by Character > 3 — Bake Catalog. Falls back to Resources when left empty.")]
[SerializeField] private CharacterCatalog catalog;

[Range(0f, 1f)]
[Tooltip("Share of spawns that use the female base prefab.")]
[SerializeField] private float femaleShare = 0.5f;

private int spawnCounter;
```

- [ ] **Krok 2: Nahraď telo `SpawnNPC`**

```csharp
    private void SpawnNPC()
    {
        if (catalog == null) catalog = Resources.Load<CharacterCatalog>("CharacterCatalog");
        if (catalog == null)
        {
            Debug.LogError("[NPCSpawner] No CharacterCatalog. Run Character > 3 — Bake Catalog.");
            return;
        }

        Gender gender = Random.value < femaleShare ? Gender.Female : Gender.Male;

        GameObject basePrefab = catalog.Bundle(gender).basePrefab;
        if (basePrefab == null)
        {
            Debug.LogError($"[NPCSpawner] The catalog has no base prefab for {gender}.");
            return;
        }

        // The seed is the spawn ordinal mixed with this spawner's position, so two spawners do
        // not march through the same sequence of looks and a respawn is reproducible.
        int seed = unchecked(spawnCounter++ * 397 ^ transform.position.GetHashCode());

        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject npcObj = Instantiate(basePrefab, spawnPosition, Quaternion.identity);

        CharacterBuilder.Apply(npcObj, CharacterRandomizer.Roll(seed, catalog, gender), catalog);

        StartCoroutine(HandleNpcLifetime(npcObj, Random.Range(minLifetime, maxLifetime)));
        activeNpcs++;
    }
```

- [ ] **Krok 3: Over v play mode**

Otvor scénu s `NPCSpawner`, v inšpektore nechaj `catalog` prázdny (načíta sa z `Resources`).
Spusti play mode.

Očakávané: NPC sa spawnujú, každý má práve jeden preset na slot triedu, zakrytá koža chýba
a v konzole nie je `NullReferenceException`. Dvaja NPC z toho istého spawnera nesmú vyzerať
identicky (pokiaľ paleta nemá len jednu možnosť na triedu).

- [ ] **Krok 4: Commit**

```bash
git add Assets/_Game/Scripts/NPC/NpcSpawner.cs && git commit -m "feat(npc): spawn from the character catalog instead of a prefab list"
```

---

## Úloha 16: Meranie na webe a dokumentácia

- [ ] **Krok 1: Zmeraj počet skinned rendererov**

V play mode s 20 aktívnymi NPC spusti v konzole:

```csharp
Debug.Log(Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Length);
```

Zapíš číslo. Zbuildi web build a pozri **draw calls a frame time** so spawnerom a bez neho.

- [ ] **Krok 2: Rozhodni o zlúčení sekcií**

Ak sú čísla v poriadku, nerob nič — kapitola 8 specu hovorí jasne, že zlúčenie sa dopredu
nerobí. Ak bolia, **nezačni to opravovať v tomto commite**: založ
`docs/findings/2026-XX-XX-zlucenie-sekcii-postavy.md` s nameranými číslami, hlavičkou
`**Verzia:** … · **Dátum:** … · **Stav:** zmerané, nespravené` a riadkom v
`docs/findings/README.md`.

- [ ] **Krok 3: Riadky do `CHANGELOG.md`**

Pod `## [Unreleased]` → `### Added`:

```markdown
- NPC sa skladajú z presetov a farieb namiesto tridsiatich samostatných modelov. Telo,
  oblečenie a vlasy vyberá `CharacterRandomizer` zo seedu, takže NPC s rovnakou identitou
  vyzerá po respawne rovnako a neukladá sa nič. Pravidlá — čo s čím nejde, čo je len pre
  jedno pohlavie a ktorú kožu preset zakrýva — sa píšu ručne do troch JSON registrov
  vedľa `ObjectTypes.json`.
- Menu `Character` — `1 — Report` povie, čo v registroch alebo v prefabe nesedí a nikdy
  nič nehádá, `2 — Generate Shades` dogeneruje materiály vrátane tmavších odtieňov
  odvodených v HSV z hlavnej farby, `3 — Bake Catalog` skompiluje registre do
  `CharacterCatalog.asset`. Bake odmietne zapísať, kým Report hlási chybu.
```

Pod `### Changed`:

```markdown
- `NPCSpawner` už nedrží zoznam prefabov — vyberie základné telo podľa pohlavia
  a zvyšok doskladá z katalógu.
```

- [ ] **Krok 4: Zápis do `docs/decisions/`**

Založ `docs/decisions/2026-XX-XX-vzhlad-postavy-z-registra.md`, hlavička
`**Verzia:** <aktuálny bundleVersion> · **Dátum:** RRRR-MM-DD`, sekcie
**Kontext → Rozhodnutie → Dôsledky**, 20–40 riadkov. Patrí sem, lebo dve veci nie sú
z kódu vidieť:

- **Meno materiálu je kľúč slotu, nie farba.** Kto to nevie, začne v Blenderi maľovať
  materiály a nechápe, prečo sa to za behu prepíše.
- **Predpripravené `.mat` assety, nie `MaterialPropertyBlock`.** MPB vypína SRP Batcher,
  čo je na webe pri 20 postavách presne to, čo nechceš — a je to presne tá vec, na ktorú
  by niekto siahol ako na „zjavne lacnejšiu" možnosť.

Nespomínaj v ňom to, čo už hovorí spec — odkáž naň.

- [ ] **Krok 5: Riadok do `docs/decisions/README.md`**

Do tabuľky sekcie aktuálnej verzie, najnovšie hore. **V tom istom kroku, nie neskôr** —
index, ktorý sa dopĺňa neskôr, je do týždňa zastaraný.

- [ ] **Krok 6: Commit**

```bash
git add CHANGELOG.md docs/decisions && git commit -m "docs(character): record the appearance registry decision"
```

---

## Čo tento plán zámerne nerieši

| vec | kam patrí |
|---|---|
| Creator UI pre hráča (UI Toolkit) a uloženie `CharacterAppearance` | fáza 2 |
| Telo a ruky z prvej osoby | fáza 3 |
| Odstránenie 30 × `student_*.fbx` a ich prefabov | až keď generátor beží v scéne |
| Triedy `head`, `beard`, `eye`, `lips` | riadok v `CharacterClasses.json`, keď budú meshe — viď spec, kapitola 6 |
| Trieda pre doplnky (batoh, okuliare) | formát tagov to unesie bez zmeny |
| Dopredné `requires` medzi presetmi | až keď sa ukáže potrebné |
| Zlúčenie prežitých sekcií do jedného renderera | až po meraní, úloha 16 |

Prvé štyri triedy z tabuľky sú dôvod, prečo kód nikde nevymenúva názvy tried a nemá na ne
`switch`: `head` má pribudnúť ako jeden riadok v JSON, nie ako ďalšia vetva v `CharacterBuilder`.
Jediná štrukturálna zmena, ktorú si `head` vyžiada, je presun `male_body_head` z tela pod
`Face/Head` — inak by naraz existovala pevná hlava aj hlava z presetu.
