# FriWorld — WebGL optimalizácia (session 2026‑08‑03)

Detailný záznam celej optimalizačnej práce pre WebGL build: čo sme analyzovali, zistili,
zmenili, aké nápady vznikli a aké problémy sme cestou odhalili.

> **TL;DR výsledok:** WebGL build beží **60 fps** (capnuté vsyncom/refresh rate displeja) s
> **~4.3 ms rezervou (headroom)**, **GC prakticky nula**, **žiadne seknutia**. Hlavné žrúty
> (font atlas špičky, canvas/TMP GC, render passy) sú vyriešené. Frame‑perf je v cieľovom
> stave — ďalej sa už neoplatí tlačiť.

---

## 1. Metodika a nástroje

- Práca cez **Unity MCP** (Unity 6000.4.11f1 / URP, WebGL target).
- **Zistenie:** vstavané `Unity_Profiler_*` MCP nástroje **nefungujú** cez externý MCP bridge —
  padajú na `System.ArgumentNullException` v `ConversationCache.GetOrCreateCache`
  (`conversationContext` je `null`, lebo boli navrhnuté pre Unity Assistant chat, nie externý bridge).
- **Workaround:** capturey čítame priamo cez C# (`ProfilerDriver.LoadProfile` +
  `HierarchyFrameDataView`) spúšťané cez `Unity_RunCommand`. Agregujeme naprieč všetkými 2000
  frames per marker (self/total time, GC, calls), počítame percentily frame‑time atď.

### Dôležité upozornenia k interpretácii captureov
- **Editor PlayMode capturey** obsahujú `EditorLoop` réžiu (~6–10 ms/frame), ktorá v reálnom
  builde **neexistuje** → oddeľovali sme `PlayerLoop` (hra) od celkového času.
- **Frame 1999** (posledný) je v každom editor capture artefakt zastavenia profileru
  (viac‑sekundový `EditorLoop`) — ignorujeme jeho `max`.
- **WebGL build** je **jednovláknový** (žiadne worker thready) — v editore paralelná práca
  (`WaitForJobGroupID`) sa na webe zoraďuje na main thread.

---

## 2. Kľúčové zistenia (analýzy)

### 2.1 Počet objektov NIE je bottleneck
Séria captureov s rôznym počtom objektov v scéne:

| Capture | Objekty | PlayerLoop medián |
|---|---|---|
| 0 objektov | 0 | 7.00 ms |
| ~1000 objektov | ~1000 | 6.95 ms |
| **~1200 objektov** | ~1200 | **5.85 ms (najrýchlejšie!)** |

**Záver:** capture s najviac objektmi bol najrýchlejší → počet objektov je pre frame‑time
irelevantný. Dôvod: **occlusion culling** (Umbra `CullDynamicObjectsWithUmbra` + vlastný
`RuntimeOcclusionCuller`) zahodí objekty mimo záber. Culling markery boli ploché naprieč
všetkými captureami. Rozdiely medzi captureami pochádzali z **hernej situácie**, nie geometrie.

### 2.2 Skutočné premenné výkonu (podľa dát)
1. **Font atlas rebuild** — najväčší problém, katastrofálne špičky (245–350 ms).
2. **World overlay canvasy** (RoomDisplay štítky) — UGUI/TMP/canvas per‑frame GC.
3. **Per‑frame script GC** (`UpdateScene`, `PlayerInteract`).
4. **Raycasty** — z occlusion cullera + hráčových checkov (situačné).

### 2.3 Font atlas — koreň problému (najväčší žrút)
Recurring špičky: **245 ms** (08‑01) a **350 ms** (08‑03) — nezávisle od objektov aj canvasov
(capture bez canvasov, s minimom raycastov, aj tak vyprodukoval 350 ms; jediné, čo vyskočilo,
bol `FontEngine.Get_GPOS_Lookups`).

**Príčina:** herné fonty boli `Static`, ale **neúplné**:
- `mainFont` (bedstead.otf, 82 znakov) — chýbali **veľké slovenské diakritiky** (`Á Č Ď É Í …`).
- `Courier Prime Code`, `JetBrainsMono` — bez diakritiky.

Chýbajúce znaky padali na fallback `LiberationSans SDF - Fallback`, ktorý bol **`Dynamic` +
`clearOnBuild = True`** → **runtime rasterizácia glyphov** (`FontAsset.TryAddCharacter` →
GPOS/GSUB lookupy) = tá špička. `clearOnBuild = True` znamená, že vo WebGL builde by fallback
štartoval prázdny → ešte horšie.

### 2.4 World canvasy vs HUD (kto robí GC)
Capture (04:20) po oprave fontu odhalil **4.75 MB UGUI/Canvas/TMP GC**. Rozlíšenie:

| Systém | Technológia | Ten 4.75 MB? |
|---|---|---|
| **RoomDisplay** (169 štítkov) | **UGUI** (`TextMeshProUGUI` na `Canvas`) | ✅ ÁNO |
| HUD / PlayerUI | **UI Toolkit** (`UIElements`) | ❌ nie (iný systém) |
| PlayerWindows (Codex/Journal/…) | UGUI, event‑driven | ❌ nie |

**Mechanizmus:** `RoomSignManager` (throttle 0.2 s) prepínal `canvas.enabled` podľa vzdialenosti;
keď sa hráč hýbe medzi miestnosťami, štítky sa neustále zapínajú → **rebuild canvasu +
regenerácia TMP meshu** (`TMP Parse Text`, `UGUI.Rendering.UpdateBatches`). Preto situačné:
capture v stoji = 1.8 MB, v pohybe medzi miestnosťami = 7.5 MB.

### 2.5 Raycasty
- `PlayerInteract` robí **len 1 raycast/frame** (lacný) — jeho skutočný žrút bol per‑frame
  `Outline.ForceUpdateMaterials()` + text churn (viď 3.2).
- Hlavný zdroj raycastov je **`RuntimeOcclusionCuller`** (až **9 rayov/miestnosť/tik**, throttle
  0.15 s + gate na pohyb, **nealokuje**).
- `PlayerMotor` + `PlayerMovementSounds` — 2× ground raycast dole (rôzne parametre).
- **Žiadne NPC raycasty** (pôvodný predpoklad bol nesprávny).

---

## 3. Vykonané zmeny

### 3.1 Font atlas fix
Cez `Unity_RunCommand` (`TMP_FontAsset.TryAddCharacters` v Dynamic móde, potom späť Static):

- **`mainFont`**: 82 → **156** znakov (zapečené ASCII + slovenská diakritika + interpunkcia).
- **`Courier Prime Code SDF`**: 97 → **155** znakov.
- **`LiberationSans SDF`** (default + fallback font): 250 → **273** — staticky doplnené `Š ĺ Ĺ №`
  (tie bedstead.otf reálne nemá).
- Pridaný **explicitný fallback** `LiberationSans SDF` na mainFont aj Courier.
- **`LiberationSans SDF - Fallback`**: `clearOnBuild` → **False** (poistka).
- Zdrojové TTF: `bedstead.otf`, `Courier Prime Code.ttf`, `LiberationSans.ttf`.
- **Pozn.:** bedstead nemá veľké `Š` → renderuje sa z LiberationSans fallbacku (mierny štýlový
  nesúlad, ale žiadne zamrznutie).

**JetBrains SDF odstránený:**
- Bol referencovaný v **3 prefaboch** (`MainMenu.prefab`, `MainMenu 1.prefab`, `FakeIntelliJ.prefab`)
  — 33 TMP textov.
- Texty prepnuté na `mainFont` priamou **YAML zámenou guid** v .prefab súboroch (Unity prefab‑save
  bol blokovaný pred‑existujúcimi missing scripts — viď 5.1).
- SDF asset zmazaný (filesystem, keďže `AssetDatabase.DeleteAsset` cez MCP padal na dialógu).
- **TTF ponechaný** — používa ho `IdeTree.uxml` (UI Toolkit).

**Overenie (capture 04:20):** `FontAsset.TryAddCharacter` **157 → 1.6 ms**, `FontEngine` preč z top
markerov, najhorší PlayerLoop **350 → 49 ms** (aj v ťažšej scéne). Špičky eliminované.

### 3.2 `PlayerInteract.cs` — refactor
- **Predtým:** každý frame `UpdateText`, 2× `GetComponent`, a hlavne outline sa **každý frame**
  vypol/zapol + `ForceUpdateMaterials()` (= tie ~0.78 MB GC).
- **Teraz:** outline sa prebuilduje **len pri zmene cieľa** (cache `outlinedGO`). Text, vstup aj
  `CanCommunicate` ostávajú per‑frame → **správanie identické**, highlight vyzerá rovnako.
  `TryGetComponent` (1×).
- **Výsledok:** GC z PlayerInteract **0.78 → 0.39 MB**, per‑frame outline churn preč.

### 3.3 `RuntimeOcclusionCuller.cs` — bounds caching
- `ComputeVisible` predtým iteroval **všetky renderery každý tik** len na prepočet AABB.
- Teraz sa bounds skupiny **cachujú** (lazy, raz) — miestnosti sú statické, výsledok identický.
- Raycasty samotné sa neznižovali (počet samplov = culling správanie); optimalizoval sa sprievodný CPU.

### 3.4 Room sign bake systém (najväčší GC win) — `RoomSignBaker.cs`
**Nápad:** zapiecť world‑canvas štítky do textúry na quad → počas hry nulový canvas/TMP/rebuild cost.

**Editor tool** `Assets/!/Editor/RoomSignBaker.cs` (menu `FriWorld → Room Signs`):
- Pre každý zo 169 štítkov: naplní texty z JSON (`Resources/Rooms`), **vygeneruje QR**
  (`WebClient` na `api.qrserver.com`, raz pri bake namiesto 169 runtime downloadov), vyrenderuje
  Canvas ortho kamerou do **komprimovanej Texture2D** (~256–384 px, per štítok), vytvorí **quad**
  (URP/Unlit, **Opaque** → žiadny overdraw), **vypne Canvas**.
- Menu: `Bake All`, `Bake Selected`, `Restore Canvases` (undo).
- Render detail: kamera na `-forward` strane, tenký near/far slab (obchádza zadnú stranu),
  dočasný bake layer.

**Culling:** quady sú MeshRenderery → **`RuntimeOcclusionCuller` ich culluje automaticky**
(zbiera renderery pod každým `RoomDisplay`). `RoomSignManager` sa tým stáva **redundantný**
(odporúčané vypnúť). `RoomDisplay.SetVisible` sme **zámerne nemenili** — inak by sa bili occlusion
culler a proximity manager o ten istý renderer.

**Overenie (05:16 bez bake → 05:24 zapečené):**

| Metrika | Bez bake | Zapečené | Δ |
|---|---|---|---|
| GC total | 5.24 MB | 1.72 MB | **−67 %** |
| TMP Parse Text (GC) | 2.39 MB | 0.00 MB | eliminované |
| UGUI.UpdateBatches (self) | 32.2 ms | 3.8 ms | −88 % |
| Frames > 16.67 ms | 6.2 % | 3.2 % | −48 % |

### 3.5 `RP_Web.asset` — tuning (zachovaná ostrosť)
Renderer = `PC_Renderer` (Forward+). Ponechané ako dobré: Render Scale 1, MSAA 2x, HDR off,
tiene off (distance 0), SRP Batcher on, Depth Priming off.

Zmeny (žiadna strata ostrosti):
| Nastavenie | → | Prečo |
|---|---|---|
| Opaque Texture | **Off** | nič nesampluje `_CameraOpaqueTexture` (overené) → preč copy pass |
| Shadow support (Main/Add/Soft/Any) | **Off** | tiene už neviditeľné (distance 0) → preč shadow varianty/machinery |
| Lens Flare (data + screen‑space) | **Off** | nepoužité (overené) |
| Light Cookies | **Off** | 0 cookie svetiel → uvoľnený 2048² cookie atlas |
| Terrain Holes | **Off** | 0 terrainov |
| **SSAO** (renderer feature) | **Off** | najväčší per‑frame feature; stráca AO, nie ostrosť |
| Depth Texture | **ponechaný** | používa ho DepthOfField (viď 3.6) |

### 3.6 DoF default off na webe — `PlayerPreferenceSettingsManager.cs`
- Zistené: `PlayerPP.asset` má **DepthOfField ON (Gaussian)** + MotionBlur; oba sú **blur** (proti
  „ostrosti") a stoja výkon. MotionBlur default už bol `false`. DoF default bol `true`.
- Zmena: `DEFAULT_DEPTH_OF_FIELD` → **`false` len na `#if UNITY_WEBGL`** (desktop nezmenený, stále
  user‑toggle). Web hráč dostane ostrý obraz bez DoF passov.
- Depth texture ponechaný (DoF je stále prepínateľný). Úplné zhodenie depth by chcelo DoF na webe
  odstrániť aj z nastavení.

---

## 4. Merania (súhrn)

### Font atlas
`TryAddCharacter` 157 → 1.6 ms · worst PlayerLoop 350 → **49 ms** · špičky eliminované.

### GC (editor capturey)
Sign bake: **5.24 → 1.72 MB (−67 %)** · TMP Parse Text 2.39 → 0 MB · PlayerInteract 0.78 → 0.39 MB.

### WebGL build — pred (14:00) vs po RP tuningu (14:55)
| Metrika | 14:00 | 14:55 | Δ |
|---|---|---|---|
| Medián frame | 16.78 ms (~60 fps) | 16.69 ms (~60 fps) | capnuté |
| **Idle headroom** (`WaitForTargetFPS`) | 2.21 ms | **4.26 ms** | **+93 %** |
| **Reálna CPU práca** (median − idle) | 14.58 ms | **12.43 ms** | **−15 %** |
| Uncapped ekvivalent | ~69 fps | **~80 fps** | +16 % |
| **URP rendering total** | 7.01 ms | **5.51 ms** | **−21 %** |
| Gfx.UpdateBufferRanges | 1.24 ms | **0.13 ms** | −90 % |
| SSAO markery | 0.088 ms | **0** | preč |
| GC | ~0 | ~0 | nula |

**Poznámka k 60 fps:** to **nie je náš cap** — je to **VSync (default on) + browser
`requestAnimationFrame`** viazaný na refresh rate displeja. Cez rAF sa nad refresh nedá ísť. Dôkaz
capu: `WaitForTargetFPS = 4.26 ms/frame` = hra dorobí prácu a **čaká** → rezerva. Win z optimalizácie
je v **headroome** (na slabšom HW udržíš 60), nie vo vyššom čísle.

---

## 5. Odhalené pred‑existujúce problémy (mimo optimalizácie)

### 5.1 Missing scripts na prefaboch
Objekty `Menu` (MainMenu), `InputHolder` / `MarkdownRenderer` / `JavaExec` (FakeIntelliJ) majú
**mŕtve komponenty** — ich `.cs` skripty v projekte **neexistujú** (žiadny compile error, čiže
genuinely osirené). Blokujú `PrefabUtility.SaveAsPrefabAsset` a spúšťajú MCP dialógy. **Odporúčané
vyriešiť** (odstrániť mŕtve komponenty alebo doplniť skripty).

### 5.2 🔴 Chýbajúci Prefab Asset → WebGL build FAIL
Dev build padol: `Assets/Scenes/Demo.unity` má **9× `entryQuiz` + 9× `pc_on` inštancií** odkazujúcich
na **neexistujúci prefab** (guid `74679ac96915e914c9b1171f94469f21` — na disku ho žiadna `.meta`
nedeklaruje). Spôsobuje „Problem detected while opening the Scene file" → build Failed.
**Toto treba opraviť pred publishom** (nájsť/obnoviť prefab, alebo odstrániť/nahradiť tie inštancie).

*(Neskorší build 14:00 už prešiel — buď bol prefab medzitým doriešený, alebo build išiel z iného
stavu. Treba overiť, že Demo.unity je čistá.)*

---

## 6. Nápady / odporúčania (zatiaľ neurobené)

Zoradené podľa hodnoty pre web:

1. **Opraviť chýbajúci prefab v `Demo.unity`** (5.2) — build blocker.
2. **DPR cap na 1.5** — jediná nedoriešená render páka; patrí do **web‑layeru** (WebGL template
   alebo Next.js wrapper), nie Unity C# (`Screen.SetResolution` bojuje s auto‑canvas‑match).
   Snippet: `createUnityInstance(canvas, { ...config, matchWebGLToCanvasSize:false }, …)` + manuálne
   `canvas.width/height = clientSize × min(devicePixelRatio, 1.5)`. Over v builde: DevTools →
   `window.devicePixelRatio`. Dôležité pre **retina/slabšie zariadenia** (fill rate).
3. **Load time / veľkosť buildu** — **nikdy sme nemerali**; často najväčší reálny web UX problém.
   Pozrieť: IL2CPP/Managed stripping, Brotli kompresia, texture compression audit, nepoužité assety.
4. **Validovať na reálnom cieľovom HW** (slabší notebook, integrovaná grafika, iné prehliadače) —
   nech reálne dáta rozhodnú, či treba viac, namiesto špekulácie.
5. **Cleanup:** zmazať `*__BACKUP.asset` (mainFont / Courier / LiberationSans) po potvrdení, že
   fonty sú OK.
6. **Voliteľné:** ~200 ms editor špička (`@frame 1934`) — nesúvisí s buildom ani fontom
   (bola v pre‑ aj post‑bake editor captureoch), skôr detail.

---

## 7. Verdikt

**Frame‑performance je v cieľovom stave — ďalej sa neoplatí tlačiť.** 60 fps s ~4.3 ms rezervou,
GC nula, žiadne stuttery. Veľké výhry (font, canvas, GC, RP passy) sú spravené; zvyšok sú
diminishing returns a nadmerná optimalizácia by len zvyšovala zložitosť/riziko. Zostáva
**opraviť build blocker (5.2)**, **dokončiť DPR cap**, **pozrieť load time** a **overiť na
slabšom HW** — potom je to naozaj hotové a energia patrí do obsahu/polish.

---

## Príloha A — dotknuté súbory

**Upravené:**
- `Assets/!/Scripts/Player/PlayerInteract.cs` (refactor)
- `Assets/!/Scripts/Rendering/RuntimeOcclusionCuller.cs` (bounds cache)
- `Assets/!/Scripts/Player/UI/PlayerPreferenceSettingsManager.cs` (DoF default off na webe)
- `Assets/Settings/RP_Web.asset`, `Assets/Settings/PC_Renderer.asset` (render tuning + SSAO off)
- Fonty: `mainFont.asset`, `Courier Prime Code SDF.asset`, `LiberationSans SDF.asset`,
  `LiberationSans SDF - Fallback.asset` (bake + fallbacky + clearOnBuild)
- Prefaby: `MainMenu.prefab`, `MainMenu 1.prefab`, `FakeIntelliJ.prefab` (JetBrains → mainFont)

**Vytvorené:**
- `Assets/!/Editor/RoomSignBaker.cs` (bake tool)
- `Assets/!/BakedSigns/*.png` + `*.mat` (per štítok) + `BakedSignQuad` deti na štítkoch
- `*__BACKUP.asset` (font backupy — na zmazanie po potvrdení)

**Zmazané:**
- `Assets/!/Prefabs/UI/Fonts/JetBrainsMono-Regular SDF.asset` (TTF ponechaný pre UI Toolkit)

## Príloha B — analyzované capturey
Editor PlayMode: 08‑01 (18:21, 18:25, 20:55, 22:09), 08‑03 (03:08, 03:12, 03:29, 04:20, 05:16, 05:24).
WebGL build: 08‑03 14:00 (pred RP tuningom), 14:55 (po). Všetky 2000 frames, main thread.
