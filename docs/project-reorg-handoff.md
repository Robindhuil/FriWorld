# FriWorld — reorganizácia projektu: HANDOFF (ako pokračovať)

Stav k 2026‑08‑03. Reorg je **~z polovice hotový** (tá štrukturálne najdôležitejšia časť).
Zvyšok viazne na pred‑existujúcom probléme (missing scripts).

---

## 1. Čo je HOTOVÉ (a overené play modom — bez errorov)

- **Third‑party izolovaný** → `Assets/ThirdParty/` (QuickOutline, `Fantasy Skybox FREE`).
- **`Assets/!` → `Assets/_Game`** (čitateľný názov pre vlastný obsah, ~742 súborov).
- **Hardcoded cesty opravené** po premenovaní:
  - 14× `.uss`/`.uxml` (URL `Assets/!/…` → `Assets/_Game/…`)
  - `RoomSignBaker.cs` (`FOLDER = "Assets/_Game/BakedSigns"`)
  - Overené: `grep -r 'Assets/!/'` = 0 zásahov.
- Vytvorený prázdny `Assets/_Game/Art/` (pripravený na Models).

**Referencie sú zachované** (guid‑based, `AssetDatabase.MoveAsset`).

---

## 2. Git stav — DÔLEŽITÉ

- **Checkpoint commit pred reorgom:** `45dcca3` „chore: remaining working-tree changes (checkpoint before folder reorg)".
- **Reorg presuny vyššie sú NEZACOMMITOVANÉ** (working tree zmeny nad checkpointom) — tak si to želal.
- Revert možnosti:
  - `git -C E:/UNITY/FriWorld checkout .` — zahodí reorg presuny (späť na checkpoint, teda `!` a bez ThirdParty)
  - `git -C E:/UNITY/FriWorld reset --hard 45dcca3` — tvrdý návrat na checkpoint
- **ODPORÚČANIE:** keďže reorg‑doteraz je overený (play mode ide), **commitni ho** ako nový checkpoint, nech sa nestratí:
  ```bash
  git -C E:/UNITY/FriWorld add -A
  git -C E:/UNITY/FriWorld commit -m "refactor(structure): isolate ThirdParty, rename ! -> _Game, fix hardcoded paths"
  ```

---

## 3. BLOCKER: missing scripts

Ďalšie presuny cez skript (MCP) padajú: **každý `AssetDatabase` presun spustí reimport → prefab s mŕtvym skriptom → modálny dialóg → MCP zlyhá** („user interactions not supported").

Mŕtve komponenty (skripty už v projekte neexistujú — žiadny compile error, genuinely osirené) sú na:
- `Menu` — v `MainMenu.prefab` / `MainMenu 1.prefab`
- `InputHolder`, `MarkdownRenderer`, `JavaExec` — vo `FakeIntelliJ.prefab`

Toto blokuje **reorg aj buildy aj ukladanie prefabov**.

---

## 4. Ako pokračovať — 2 cesty

### Cesta A (odporúčaná): najprv vyriešiť missing scripts, potom dokončiť reorg skriptom
1. Nájsť presne, ktoré GameObjecty majú mŕtve komponenty (editor skript / `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`).
2. Odstrániť ich (alebo doplniť správne skripty, ak nejaké majú byť).
3. Keď reimporty prestanú hádzať dialóg, **dokončiť presuny skriptom** (Settings, Input, UI, Scripts/Editor merge, Scenes) + **veľké binárne ručne** (viď nižšie).

### Cesta B: zvyšné presuny ručne v Unity Project okne
Drag‑drop priečinkov — Unity si reimport aj dialógy odbaví interaktívne, referencie zostanú.

---

## 5. Zostáva presunúť (mapping do `_Game/`)

| Presunúť | Kam | Pozn. / gotcha |
|---|---|---|
| `Assets/3Dmodels` (217 MB) | `_Game/Art/Models` | **VEĽKÉ** — presúvaj v **Project okne** (skript = 30 min reimport → timeout). Progress bar to zvládne. |
| `Assets/Animations` | `_Game/Animations` | ok (guid) |
| `Assets/Images` | `_Game/Art/Images` | ok |
| `Assets/Settings` | `_Game/Settings` | RP/volume assety — referencie guid (Project Settings), OK |
| `Assets/Input` | `_Game/Input` | ok |
| `Assets/UI Toolkit` | `_Game/UI` | ⚠️ `.uss`/`.uxml` sa referencujú cez URL (path). Po presune spusti fix: `sed -i 's|Assets/UI Toolkit/|Assets/_Game/UI/|g'` na všetkých `.uss`/`.uxml`, čo ich referencujú. |
| `Assets/Scenes` | `_Game/Scenes` | ⚠️ **po presune znovu pridaj scény do Build Settings** (Menu, Demo) — cesty sa zmenia, MoveAsset ich v build settings NEupdatne. |
| `Assets/Scripts` (`BlobShadow.cs`, `Systems/`) | `_Game/Scripts` | zlúčiť (duplicitný top‑level Scripts) |
| `Assets/Editor` (15 súborov) | `_Game/Editor` | zlúčiť (duplicitný top‑level Editor) |

**NECHAŤ na mieste (Unity special / package):** `Assets/Resources`, `Assets/StreamingAssets`, `Assets/TextMesh Pro`, `Assets/Plugins`, `Assets/ThirdParty`, `Assets/_Game`.

---

## 6. Cleanup po reorgu
- Zmazať prázdny `Assets/_Game/Scripts/Markdig/` (0 assetov, leftover).
- Ak zostane prázdny `Assets/_Game/Art/` (po ručnom presune Models tam), je OK.
- `Assets/_Recovery/` (32 MB, recovery záloha) — mimo Assets (do zálohy mimo projektu).

---

## 7. Ostatné otvorené veci (mimo reorgu, z web‑optimalizácie)
- 🔴 **`Demo.unity` chýbajúci prefab** (guid `74679ac96915e914c9b1171f94469f21`, inštancie `entryQuiz`/`pc_on`) = **build blocker**. Nájsť/obnoviť alebo odstrániť inštancie.
- **DPR cap 1.5** — do WebGL template / Next.js wrappera (`matchWebGLToCanvasSize:false` + canvas backing = clientSize × min(dpr,1.5)).
- **Code Optimization = Disk Size** + Brotli + Managed Stripping High pre menší/rýchlejší web build.
- Detailný web‑optimalizačný report: `docs/web-optimization-2026-08-03.md`.
- Nepoužité assety (kandidáti na zmazanie): `docs/unused-not-in-build.md` (544 súborov / 288 MB mimo build scén).

---

## 8. Cieľová štruktúra (hybrid — pripomenutie)
```
Assets/
├── _Game/          ← všetok vlastný obsah (Art, Audio, Animations, Fonts, Prefabs,
│                      Scenes, Scripts, Settings, UI, Input, BakedSigns, Editor)
├── ThirdParty/     ← QuickOutline, Fantasy Skybox (+ čokoľvek externé)
├── TextMesh Pro/   ← Unity package (nechať)
├── Plugins/        ← native / .jslib
├── Resources/      ← load podľa mena (Rooms.json) — vnútro NEmeniť
└── StreamingAssets/← special (nechať presne tu)
```
Pravidlá: vlastný obsah len v `_Game/`, third‑party len v `ThirdParty/`, PascalCase bez medzier/diakritiky, Unity special priečinky (Resources/StreamingAssets/Editor/Plugins) majú pevné mená.
