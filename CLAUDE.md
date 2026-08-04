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

## Platformové vetvenie

Máš na to systém v `_Game/Scripts/FeatureFlags/` — používaj ho, nie rozsypané `#if`.

| Potreba | Nástroj |
|---|---|
| celý GameObject/miestnosť len pre jednu platformu | `PlatformGate` (strip pri builde) |
| konkrétne komponenty na spoločnom objekte | `ComponentGate` |
| zapínateľná/experimentálna vec v kóde | `Features.On(FeatureId.X)` |
| render politika webu | `WebRenderDefaults` |

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
