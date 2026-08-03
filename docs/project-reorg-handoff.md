# FriWorld — reorganizácia projektu: HANDOFF (ako pokračovať)

Stav k 2026‑08‑03 (2. session). Reorg je **takmer hotový** — zostávajú 2 ručné kroky.

---

## 1. Čo je HOTOVÉ

### Session 1 (commit `c9cf423`)
- **Third‑party izolovaný** → `Assets/ThirdParty/` (QuickOutline, `Fantasy Skybox FREE`).
- **`Assets/!` → `Assets/_Game`** (~742 súborov).
- Opravené hardcoded cesty: 14× `.uss`/`.uxml`, `RoomSignBaker.cs`.

### Session 2
- **Blocker vyriešený** (commit `c473606`) — viď sekcia 2.
- **Presuny dokončené** (commit `7b2daa1`):
  - `Assets/Settings` → `_Game/Settings`
  - `Assets/Input` → `_Game/Input`
  - `Assets/UI Toolkit` → `_Game/UI`
  - `Assets/Scenes` → `_Game/Scenes`
  - `Assets/Scripts` → zlúčené do `_Game/Scripts`
  - `Assets/Editor` → zlúčené do `_Game/Editor`
- **Follow‑up fixy:**
  - 8× `.uxml` prepísané URL referencie (`Assets/UI%20Toolkit/` → `Assets/_Game/UI/`,
    `Assets/Images/` → `Assets/_Game/Art/Images/`). Tieto sa resolvujú **cez cestu, nie guid**,
    takže `MoveAsset` ich nechá visieť.
  - **Build Settings** znovu napojené cez guid (`MoveAsset` ich neupdatne).
  - Zmazané vyprázdnené `Assets/Scripts`, `Assets/Editor` a leftover `_Game/Scripts/Markdig`.

**Overené:** 19 `uxml`/`uss` force‑reimport bez jediného warningu; 0 errorov v konzole;
obe build scény sa načítajú z nových ciest.

---

## 2. Vyriešený blocker: missing scripts

**Príčina:** 13 osirených `MonoBehaviour` komponentov, ktorých skripty boli **zámerne zmazané**
v commitoch `289ea6e` („old files clean up") a `6fdcdd1` („Files structuralization").
Každý reimport kvôli nim otvoril modálny dialóg → MCP zlyhalo („user interactions not supported"),
čo blokovalo reorg aj buildy aj ukladanie prefabov.

| Prefab | Chýbajúci skript | Počet |
|---|---|---|
| `_Game/Prefabs/Minigames/Minigames 1 1.prefab` | `Scripts/Minigames/MiniGame.cs` | 7 |
| `_Game/Prefabs/UI/Canvases/FakeIntelliJ.prefab` | `MarkdownRenderer.cs`, `CodeInput.cs`, `JavaCodeExecutor.cs` | 3 |
| `_Game/Prefabs/UI/MainMenu/MainMenu.prefab` | `Scripts/Player/UI/MainMenu.cs` | 1 |
| `_Game/Prefabs/UI/MainMenu/MainMenu 1.prefab` | `Scripts/Player/UI/MainMenu.cs` | 1 |
| `_Game/Prefabs/Background.prefab` | `MarkdownInputHighlight.cs` | 1 |

Odstránené cez `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` nad
`PrefabUtility.LoadPrefabContents` (izolovaná preview scéna → žiadny dialóg).
Po znovunačítaní z disku: **0 zostávajúcich**.

---

## 3. Zostáva dokončiť (2 ručné kroky)

| Presunúť | Kam | Prečo ručne |
|---|---|---|
| `Assets/3Dmodels` (217 MB) | `_Game/Art/Models` | Skriptom = ~30 min reimport → MCP timeout. **Drag‑drop v Project okne**, progress bar to zvládne. |
| `Assets/_Recovery` (32 MB) | mimo projektu | Recovery záloha, nepatrí do `Assets/`. Presunúť do zálohy mimo repa. |

**NECHAŤ na mieste:** `Assets/Resources`, `Assets/StreamingAssets`, `Assets/TextMesh Pro`,
`Assets/Plugins`, `Assets/ThirdParty`, `Assets/_Game`.

---

## 4. Známy šum (neriešiť)

- **`Assets/TextMesh Pro/Examples & Extras`** — ~260 missing‑script referencií na TMP example
  skripty. Package examples, v builde nie sú, reorg sa ich nedotýka.
- **`guid 0000000000000000e000000000000000`** — Unity built‑in resources guid, nie missing script.
- **`_Game/Input/PlayerInput.cs`** — cesta `Assets/Input/` je len v generovanom komentári,
  prepíše sa pri regenerácii Input Actions.

---

## 5. Ostatné otvorené veci (mimo reorgu)

- 🔴 **`Demo.unity` chýbajúci prefab** (guid `74679ac96915e914c9b1171f94469f21`, inštancie
  `entryQuiz`/`pc_on`) = **build blocker**. Iný problém než missing scripts — chýba prefab asset,
  nie skript. Nájsť/obnoviť alebo odstrániť inštancie.
- **DPR cap 1.5** — do WebGL template / Next.js wrappera
  (`matchWebGLToCanvasSize:false` + canvas backing = clientSize × min(dpr,1.5)).
- **Code Optimization = Disk Size** + Brotli + Managed Stripping High.
- Detailný web‑optimalizačný report: `docs/web-optimization-2026-08-03.md`.
- Nepoužité assety: `docs/unused-not-in-build.md` (544 súborov / 288 MB mimo build scén).

---

## 6. Git

```
7b2daa1  refactor(structure): move Settings, Input, UI, Scenes, Scripts, Editor into _Game
c473606  fix(prefabs): remove 13 orphaned MonoBehaviours with missing scripts
c9cf423  refactor(structure): isolate ThirdParty, rename ! -> _Game, fix hardcoded paths
45dcca3  chore: remaining working-tree changes (checkpoint before folder reorg)
```

Working tree čistý. Revert ktoréhokoľvek kroku = `git revert <sha>`.

---

## 7. Cieľová štruktúra
```
Assets/
├── _Game/          ← všetok vlastný obsah (Art, Animations, BakedSigns, Editor, Input,
│                      Prefabs, Scenes, Scripts, Settings, UI)
├── ThirdParty/     ← QuickOutline, Fantasy Skybox (+ čokoľvek externé)
├── TextMesh Pro/   ← Unity package (nechať)
├── Plugins/        ← native / .jslib
├── Resources/      ← load podľa mena (Rooms.json) — vnútro NEmeniť
└── StreamingAssets/← special (nechať presne tu)
```
Pravidlá: vlastný obsah len v `_Game/`, third‑party len v `ThirdParty/`, PascalCase bez medzier
a diakritiky, Unity special priečinky (Resources/StreamingAssets/Editor/Plugins) majú pevné mená.
