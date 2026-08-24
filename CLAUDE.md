# FriWorld — pokyny pre Clauda

Unity 6000.4.11f1, URP. Dva build targety: **Web (WebGL)** a desktop/standalone.
Sprievodná web aplikácia (Next.js wrapper okolo buildu) žije v samostatnom repe
`ROBIN/dev/friworld-web` → GitHub `FriWorld-Hub`.

---

## Dokumentácia zmien — POVINNÉ, rob to bez vyzvania

Po každej dokončenej zmene (feature, fix, perf, chore, refactor) urob **oboje**:

### 1. Riadok do `CHANGELOG.md`
Vždy. Jeden riadok pod `## [Unreleased]`, do sekcie podľa typu
(`Added` / `Fixed` / `Changed` / `Performance` / `Removed`).
Píš, čo to znamená **pre hráča alebo pre vývojára**, nie ktoré súbory sa dotkli.

```
- Pohľad myšou už nešvihne pri zaseknutom snímku vo web builde. (`7d51874`)
```

### 2. Zápis do `docs/decisions/` — ale len keď platí aspoň jedno
- rozhodlo sa medzi viacerými možnosťami a *prečo* nie je zrejmé z kódu,
- narazilo sa na pascu, na ktorú by niekto nabehol znova,
- príčina bugu bola inde, než kde sa prejavoval,
- zmena má dosah len na jednu platformu a dôvod je netriviálny.

Formát: `docs/decisions/YYYY-MM-DD-kratky-nazov.md`, sekcie **Kontext → Rozhodnutie →
Dôsledky**. Krátko, 20–40 riadkov stačí. Bežný feature ani chore sem **nepatrí** —
commit message a changelog to pokryjú.

**Nepíš dokument len preto, že si niečo urobil.** Duplikovaný obsah zhnije a potom
mätie viac, než keby nebol.

---

## Git

- Conventional commits: `feat|fix|perf|refactor|chore|docs(scope): …`
- **Nikdy `git add -A`** — repo mieva rozrobené zmeny od používateľa a zmetieš ich
  do svojho commitu. Stageuj konkrétne cesty.
- Vetva je `master`. Commituj priebežne, pushuj len na vyžiadanie.

---

## Štruktúra projektu

```
Assets/
├── _Game/          ← VŠETOK vlastný obsah (Art, Animations, BakedSigns, Editor,
│                      Input, Prefabs, Scenes, Scripts, Settings, UI)
├── ThirdParty/     ← čokoľvek externé
├── TextMesh Pro/   ← Unity package — nechať, jeho Examples majú vlastné missing
│                      scripts, to je známy šum
├── Plugins/ Resources/ StreamingAssets/   ← Unity special, nechať
```
Vlastný kód patrí **len** do `_Game/`. PascalCase, bez medzier a diakritiky.

---

## Collidery, vrstvy a static flagy — register typov

Neurčujú sa kľúčovými slovami v kóde, ale registrom v `Assets/_Game/Editor/`:

| súbor | obsah |
|---|---|
| `ObjectPrefixes.json` | prefixy na odstrihnutie — **plné mená kontajnerov**, `ra100_corridor_1` |
| `ObjectTypes.json` | `typ → collider / layer / occluder / script` |
| `RoomPlatforms.json` | `oblasť → all / desktopOnly / webOnly` |

Meno objektu sa zredukuje na **typový kľúč**: odstrihne sa najdlhší sediaci prefix, vodiace
`<int>_`, značky `UNO`/`UYO` a koncové `_<int>`. Kľúč sa vyhľadá **presnou zhodou** — nikdy nie
podreťazcom. Neznámy alebo nevyplnený typ **objekt nedotkne a nahlási sa**; nikdy nedostane
vlastnosti podobne pomenovaného typu.

Prefixy sa berú **celé, aj s číslom inštancie**. `ra100_corridor_1` a `ra100_corridor_2` sú dve
chodby a v `RoomPlatforms.json` rozhodujú samostatne. Na typový kľúč to vplyv nemá — čo z čísla
zostane, odstrihne `ObjectTypeKey`.

### Pridanie nového objektu

Menu **`Routine`** drží celý postup v poradí, v akom sa spúšťa; `Routine → Object Pipeline…`
je to isté v okne, s popisom ku každému kroku. Ručne to vyzerá takto:

1. Pomenuj ho `<kontajner>_<typ>_<číslo>` a naimportuj.
2. Vyber koreň (napr. `FriBuilding`) → `Routine → 1 — Report On Selection`.
3. Podľa hlásenia:
   - **UNKNOWN** → krok 2 `Seed Missing Types`, potom vyplň tri polia. Nový typ je
     **prvý v súbore**, netreba ho hľadať.
   - **UNSTRIPPED** → pribudol kontajner, krok 3 `Add Prefixes`. Ten zároveň doplní nové
     oblasti do `RoomPlatforms.json` — vyplň im `platform`, sú tiež navrchu súboru.
   - **WITHHELD** → prefix by zožral hlavičku registrovaného typu. Buď objekt premenuj, alebo
     prefix schváľ ručne, ale vedz, čo tým rozbiješ.
4. Krok 1 znova — chceš „every scanned object resolved to a decided type".
5. Kroky **5 až 8**: `Generate Colliders`, `Layers And Static`, `Setup Interactables`,
   `Room Gates`. Poradie nie je ľubovoľné — vrstvy pred interaktáblami, interaktábly pred gatmi.
6. Ak sa zmenili occludery, **rebake occlusion culling**. To zo zoznamu nespraví nič.

Kroky 1–3 čítajú výber v hierarchii. **Kroky 4–8 výber ignorujú a vždy zapisujú do
`FriBuilding.prefab`** — do prefab assetu, nie na inštanciu v scéne, inak by to bol override
a prvý reimport `.blend` by to zmietol.

Objekt existujúceho typu (ďalšia lampa, ôsme okno) nevyžaduje nič — stačia kroky 5 až 8.

### Pravidlá pomenovania

- **Kontajner sa nesmie volať ako typ.** `ra000_roof` obsahujúci `roof` znamená, že ten prefix
  sa nedá schváliť. Preto sú steny `ra000_outside_wall`, nie `ra000_outer_wall`.
- Žiadne blenderovské `.001`, žiadne čiarky, medzery ani zátvorky — každé z toho vyrobí
  samostatný typ.
- Číslo inštancie v strede mena je v poriadku, pokryje ho vzor `window_<int>_glass`.

Podrobne aj s tým, čo neskúšať znova: `docs/decisions/2026-08-04-object-type-registry.md`.

---

## Platformové vetvenie

Máš na to systém v `_Game/Scripts/FeatureFlags/` — používaj ho, nie rozsypané `#if`.

| Potreba | Nástroj |
|---|---|
| **miestnosť v budove len pre jednu platformu** | `RoomPlatforms.json` + `Routine → 8 — Room Gates` |
| celý GameObject len pre jednu platformu, mimo budovy | `PlatformGate` (strip pri builde) |
| konkrétne komponenty na spoločnom objekte | `ComponentGate` |
| zapínateľná/experimentálna vec v kóde | `Features.On(FeatureId.X)` |
| render politika webu | `WebRenderDefaults` |

- **Gaty na miestnostiach nepridávaj ručne.** Sú generovaný výstup: rozhodnutie patrí do
  `RoomPlatforms.json`, ručná úprava v hierarchii sa pri najbližšom behu prepíše. Vetva
  `Objects` dostane `PlatformGate` na celý kontajner, vetva `fri_building` len `ComponentGate`
  na dvere — steny a okná sa nestrihajú nikdy.
  Podrobne: `docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`.
- Flagy sa konfigurujú v `Assets/Resources/FeatureFlags.asset`.
- Na „je toto web?" použi **`PlatformFlags.IsWeb`**, nie `Application.platform` —
  v editore sleduje aktívny build target, takže play mode sedí s buildom.
- Ak má flag bezpečný default ON, čítaj ho cez `Features.On(id, fallback: true)`,
  nech chýbajúci config feature potichu neodstrihne.

---

## Web build — pasce, na ktoré sa už nabehlo

- **`RP_Web.asset` + quality level „Web"** nesú celé web ladenie. Čokoľvek, čo
  prepne quality level na iný, ho zahodí — preto `DEFAULT_QUALITY_LEVEL` na webe
  rezolvuje na level „Web".
- **Mouse delta (`<Mouse>/delta`) sa NIKDY nenásobí `Time.deltaTime`.** Je to už
  posun za snímok, nie rýchlosť; násobenie robí rotáciu kvadratickou vo frame time
  a jeden dlhý snímok = švihnutie kamerou.
- **DPR cap patrí do web vrstvy** (`friworld-web`, `UnityGame.tsx`), nie do Unity —
  `Screen.SetResolution` sa bije s auto canvas matchom.
- **`.uss`/`.uxml` sa referencujú cez cestu, nie guid.** Po presune assetu treba
  cesty prepísať ručne, `AssetDatabase.MoveAsset` ich nechá visieť.
- **Build Settings scény sa po presune neupdatnú** — prepoj ich cez guid.

---

## Unity MCP

- Preferuj `Unity_RunCommand` (vracia logy priamo) pred menu itemom + čítaním konzoly.
- **`AssetDatabase.DeleteAsset` na priečinku otvára dialóg a cez MCP padne.**
  Overene prázdne priečinky maž na disku (aj `.meta`) a daj `Assets/Refresh`.
- Prefaby s missing scripts otváraj cez `PrefabUtility.LoadPrefabContents` —
  izolovaná preview scéna nedvíha modálne dialógy.
- Po zmene skriptu na disku daj `Assets/Refresh` a počkaj na dokompilovanie
  (`Unity_ManageEditor` → `GetState` → `IsCompiling`).
