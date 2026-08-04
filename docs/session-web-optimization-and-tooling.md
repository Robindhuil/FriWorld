# FriWorld — Session log: Web optimization, Feature Flags, Blender→Unity tooling

Detailná dokumentácia celej pracovnej session: čo sme analyzovali, zistili, rozhodli,
pridali a aké nástroje vznikli. Slúži ako referencia pre budúcu prácu.

---

## 1. Web (WebGL) profilovanie a výkon

### 1.1 Dá sa profilovať WebGL build?
- **Áno.** Development Build + *Autoconnect Profiler*, Build & Run, a editor Profiler sa
  pripojí k bežiacemu web playeru (connection dropdown → WebGL player).
- Timeline CPU, rendering (draw calls, SetPass, triangles), GC, pamäť — fungujú.
- Memory Profiler snapshoty a niektoré deep featury sú na WebGL obmedzené.
- **Editor capture je nafúknutý** (~8–10 ms EditorLoop/profiler réžia) — rozhodnutia treba
  robiť z **build capture**, nie z editorových čísel.

### 1.2 WebAssembly vs WebGL (vyjasnenie)
- **Nie sú alternatívy.** Wasm = ako beží **kód** (CPU logika) — už ho používaš.
  WebGL = grafické **API** (GPU rendering). Tvoj build = Wasm + WebGL spolu.
- Reálne „lepšie" možnosti:
  - **WebGPU** (Unity 6 experimentálne) — náhrada WebGL na grafiku, compute, možný GPU culling.
  - **Wasm multithreading** — reálny render/worker thread, cieli priamo na single-thread hrdlo.

### 1.3 Kľúčové zistenia z profilerov
- Hra je **CPU / draw-submission bound.** Dominuje `DrawBuffersBatchMode` (submit geometrie).
- **Editor capture** (~28–32 fps): réžia + animácia/skinning rozhodené na worker vlákna.
- **WebGL single-thread build** (ťažký vonkajší pohľad):
  - AVG ~16 fps, heaviest frame ~2,1 M tris, ~67 ms.
  - Main thread: `DrawBuffersBatchMode` ~11 ms, `MeshSkinning.Skin` ~4 ms (na CPU!),
    `RenderLoop.Sort`, `SRPBatcher.Flush`, `Animator.ProcessGraph`, UI Canvas, OC query.
- **GPU skinning na WebGL NEfunguje** — `MeshSkinning.Skin` ostáva na CPU napriek zapnutému
  GPU skinningu (Unity WebGL to necháva na CPU). Potvrdené.

### 1.4 Threaded WebGL build (po zapnutí Wasm threads)
- **Threading FUNGUJE:** skinning sa presunul z main threadu (4,2 ms → 0,2 ms) na **5 workerov**
  (~3,5 ms každý, paralelne). Animácia/sort tiež čiastočne.
- **ALE ťažká draw-bound snímka sa nezrýchlila** — `DrawBuffersBatchMode` ostáva na main
  (WebGL GL cally musia na main), a pribudlo `PutGeometryJobFence` (~9 ms) = main **čaká** na
  worker joby. Na draw-bound scéne sync overhead zožral zisk.
- **Záver:** threading pomáha snímkam s veľa NPC (skinning), nie draw-bound vonkajšiemu pohľadu.

### 1.5 Zmeny nastavené v Unity
- **Animator Culling Mode → `CullCompletely`** na NPC (scénové + prefaby) — animátory mimo
  záberu prestanú počítať. (GPU skinning bol už zapnutý — na webe aj tak nezaberá.)
- **`PlayerSettings.WebGL.threadsSupport = true`** — Wasm multithreading. Prereq WebGL2 + Wasm
  splnené. **Vyžaduje COOP/COEP hlavičky na hostingu** (viď web app), inak sa build nespustí.
- **RP_Web `renderScale` 0.70 → 1.00** — to bola príčina **rozmazania** na webe (renderovalo sa
  na 70% a upscalovalo). Desktop RP assety nedotknuté.

### 1.6 Occlusion culler — overený
- `RuntimeOcclusionCuller` je na **Main Camera**, enabled, `autoFindComponentName="RoomDisplay"`
  → **169 skupín**; occluderMask self-heal na **Obstacle (7)**; **1977 colliderov** na Obstacle
  (z toho veľa okien/skla). Funkčný.
- **Pozor:** prepína `Renderer.enabled`, ale UI Canvas ide cez `CanvasRenderer` (nie `Renderer`)
  → **cedulky ten culler NEvypína** (riešené zvlášť, viď §7).

---

## 2. Geometria — census a decimácia (najväčší reálny problém)

### 2.1 Triangle census scény (~1,91 M tris, 5452 MeshFiltrov)
- **50% scény (0,95 M) sú premodelované propy**, nie budova (steny 2%, strecha 0%):
  | Kategória | Tris | Kusov | Priemer | Max |
  |---|---|---|---|---|
  | radiátory | 435k | 235 | 1 850 | 16 414 |
  | ploty | 317k | 62 | 5 115 | **129 900** (front fence!) |
  | kryty zásuviek | 51k | 31 | 1 637 | 10 602 |
  | schody | 51k | 166 | | 20 503 |
  | stromy/terasa/gazebo | ~90k | | | |
- Realistické cieľové počty: plot ~1-2k (má 130k → 65×), kryt zásuvky ~50 (má 10k → 200×).
- **235 radiátorov = 235 unikátnych meshov**, všetko v jednom `fri_building.blend`.

### 2.2 Odporúčanie
- **Decimovať v Blenderi** (Decimate modifier podľa name pattern), nie combine ani culling.
  Radiátory + ploty samé = ~40% scény.
- Exact object names vytiahnuté priamo z `.blend` binárky (grep `OB<name>`), uložené pre
  dávkovú decimáciu. **Ploty** rozdeliť: exteriérové (agresívne) vs schodiskové madlá (opatrne).

### 2.3 Lightmapy (dôležité pre pamäť aj FPS)
- **Textúrová pamäť (editor 928 MB) NIE sú albedo textúry** — materiály sú ploché RGB farby.
  Je to hlavne **lightmapy (396 MB, 115 textúr)** + editorové RT (GUIViewHDRRT ~58 MB, gizmá,
  scene view — v builde preč) + fonty (67 MB).
- **Odstránenie lightmáp (unlit realtime) = +20 fps** (editor) a −285 MB web pamäte. Príčina
  zisku: menej shader samplov (lightmap + shadowmask) + menej bandwidth. Odhalilo, že scéna je
  **aj GPU/shader-bound**, nielen CPU-draw-bound.
- Stratégia pre web: **Baked GI OFF** → žiadne lightmapy; directional Realtime bez tieňov +
  FillLight + ambient. Ploché, ale lacné.

---

## 3. Web app (FriWorld-Hub, Next.js 16)

Repo: `https://github.com/Robindhuil/FriWorld-Hub`. Servíruje build z `public/game/Build` cez
`/api/game` route (drop-in swap).

### Zmeny (pripravené ako patch `0001-...`)
- **`UnityGame.tsx`:** DPR cap `Math.min(dpr, 2)` → **`1.5`** — najväčší dnešný FPS zisk na
  hi-DPI (menej fillrate). Skús aj 1.
- **`next.config.ts` → `headers()`:**
  - `COOP: same-origin` + `COEP: require-corp` na `/play` → odomkne SharedArrayBuffer =
    **Wasm threading**. Bezpečné (stránka nemá cross-origin zdroje). **Pomôže len ak je build
    skompilovaný s threads** (to je).
  - `Cache-Control: immutable` + `CORP` na `/game/Build/*`.

### Poznatky
- Kompresia (.br) hlavičky zámerne nepridané — krehké, závisí od hostingu + build compression.
- **Hosting zdarma:** Vercel Hobby (natívny Next.js, headers fungujú, 100 GB/mes) — odporúčané.
  Alternatíva Cloudflare Pages (neobmedzený prenos, ale next-on-pages adapter). NIE GitHub Pages
  (nevie API route ani COOP/COEP).

---

## 4. Feature Flag systém (nový, `Assets/!/Scripts/FeatureFlags/`)

Cieľ: podmieniť features/objekty per platforma (desktop/web) a experimentálne toggly.

### 4.1 Komponenty
- **`FlagScope`** (enum): All / DesktopOnly / WebOnly / EditorOnly.
- **`FeatureId`** (enum): named code flagy.
- **`FeatureFlagConfig`** (ScriptableObject): centrálny zoznam flagov (`Resources/FeatureFlags.asset`).
- **`Features`** (static): `Features.On(FeatureId.X)` — runtime query, scope + enabled.
- **`FeatureGate`** (komponent): runtime `SetActive` podľa flagu (experimentálne).
- **`PlatformGate`** (komponent): **build-time strip** celého objektu/miestnosti podľa platformy.
  - `Awake` zrkadlí aktívny build target (cez `PlatformFlags`) → play mode = build.
  - **Exclude list**: deti (napr. dvere), ktoré prežijú strip miestnosti (reparent von).
  - Po aplikovaní sa **gate sám odstráni** (aj v builde).
- **`ComponentGate`** (komponent): **strip vybraných komponentov** (Door, Animator, AudioSource…)
  na jednej platforme; objekt ostane. Voliteľne **zmení layer** (dropdown, napr. 7=Obstacle).
  V play mode komponenty reálne **Destroy** (nie disable) → verné buildu.
- **`PlatformFlags`** (static): `IsWeb` (editor = active build target, build = UNITY_WEBGL),
  `Keep(target)`.
- **`LayerAttribute` + drawer**: `[Layer] int` sa rendruje ako layer dropdown.

### 4.2 Editor časť
- **`FeatureFlagBuildProcessor`** (`IProcessSceneWithReport`): pri builde na **dočasnej kópii
  scény** stripne PlatformGate objekty + ComponentGate komponenty (scripty prvé kvôli
  `RequireComponent`) + aplikuje layer change + reparent exclude. Tvojej scény sa nedotkne.
- **`FeatureFlagPreview`**: `Tools > Feature Flags > Preview Web/Desktop` (edit mode náhľad).
- **`FeatureFlagConfigCreator`**: `Tools > Feature Flags > Create Config Asset`.
- **`ComponentGateEditor`** + **`LayerAttributeDrawer`**: čisté inšpektory.

### 4.3 Dôležité rozhodnutia / poznatky
- Interakcia s dverami používa `PlayerInteract` s **`QueryTriggerInteraction.Ignore`** →
  dať dvere na trigger by **rozbilo interakciu**. Preto ComponentGate strippuje komponenty a mení
  layer (Interactable→Obstacle), nie collider na trigger.
- **Navmesh:** dvere presunuté na Obstacle (7) po stripe by mali blokovať nav, ale baked navmesh
  je statický. **Full runtime rebake (`NavMeshSurface.BuildNavMesh`) zhodil Unity** (revoxelizácia
  celej ~1,9 M tris budovy na main threade → freeze/OOM). `NavMeshRebaker` **odstránený**.
  - Správne riešenie: **`NavMeshObstacle` + Carve** na dvere (dynamická diera, žiadny bake),
    gatnuté `ComponentGate` WebOnly. (Carve = obstacle vyreže lokálny kúsok navmeshu.)
- **`DoorGateSetup`** tool: na označené „door" objekty pridá ComponentGate (DesktopOnly),
  do poľa Door+Animator+AudioSource, changeLayer → 7.

---

## 5. NPC / dvere / kolízie

- **student_1Un** koliduje s hráčom: príčina = **solídny `BoxCollider` (isTrigger=False) na
  Default**; hráč = CharacterController na Default → kolízia. Nie Rigidbody.
- Skúmané: trigger (rozbíja interakciu), player-side `Physics.IgnoreCollision` (nechcené),
  nakoniec **všetko vrátené** — `student_1Un` je neinteraktívna postava a v scéne bola
  **zámena `student_1Un.fbx` vs `student_1Un.prefab`** (dva rôzne assety, oba 48… pozn.: to bol
  iný prípad, viď §6.4).
- **Animator culling** (§1.5) rieši výkon NPC animácie mimo záberu.

---

## 6. Blender → Unity workflow a nástroje

### 6.1 Import jednotlivých objektov s live update
- Jeden `.blend` = jeden asset; objekty = **mesh sub-assety**, ktoré sa **updatnú pri reimporte**
  (uloženie `.blend` → Unity reimportne, meše/UV/normály live). Podmienky: `.blend` v Assets,
  Blender nainštalovaný, **stabilné mená objektov** (inak sa stratia GUID referencie).
- **Materiály rob v Unity**, nie Blender (import je stratový a Unity ich pri reimporte neprepíše).
  Blender = geometria + UV; oboje sa updatuje. UV0 aj UV2 (alebo Generate Lightmap UVs).

### 6.2 Lampy: instancing vs combine
- **Combine a instancing sa vylučujú.** Pre veľa identických propov (lampy) je **instancing lepší**
  (zdieľaný mesh = pamäť + GPU instancing), nie combine (duplikuje geometriu do room meshov).
- V URP **SRP Batcher dávkuje aj rôzne materiály** lacno → farebné varianty = **materiál na farbu**
  (prefab varianty / paleta), NIE unikátny materiál na inštanciu (`renderer.material` = kópia = zle).
  MaterialPropertyBlock v URP vypína SRP Batcher → zvyčajne nie výhra.

### 6.3 `PrefabReplacerWindow` (Editor tool)
Nahradí copy-paste mesh objekty inštanciami prefabu, zarovnané k originálom.
- **Align módy:** Copy transform / Match mesh center / **Match geometry** (Kabsch–Horn best-fit
  rotácia cez všetky vertexy — pre applied/baked rotácie; vyžaduje rovnaký mesh source).
- **Geometry tolerance** slider; **fallback** na Match mesh center + **Rotation offset** pole
  (na konzistentnú 90° chybu).
- **Group under original parent**: inštancie do groupy pomenovanej podľa parenta, meno
  `{parent}_{prefabBase}_{index}`, **reuse existujúcej root groupy** + pokračovanie číslovania.
- **Diagnose** tlačidlo (nič nemení): vertexCount, mirror flag, `fit(rotation)%` / `fit(mirror)%`.
- **Skip non-matching mesh**: nechá objekty s iným meshom (iný variant) tak.

### 6.4 Poznatok z diagnózy (dôležitý)
- Zlyhania geometry-align neboli bug: objekt `ra003_lamp_2` mal **mesh `lamp_1`** (nie lamp_2) —
  „_2" v mene = poradové číslo, nie variant. `fit(rotation)=55%`, `fit(mirror)=55%` → iné poradie
  vertexov / iný variant. **Geometry fit je vlastne detektor variantu.**

### 6.5 Blender hierarchy skript (review + stabilná verzia)
- Pôvodný skript mal **riziko mazania objektov** (globálne `bpy.data.objects.get(col.name)` +
  remove) a nedeterministické `users_collection[0]` + `matrix_world` bez depsgraph update.
- **Stabilná verzia**: empties **tag-uje custom property** → maže len svoje (nikdy mesh),
  collection vyberá deterministicky, transform cez `matrix_world = world` po `view_layer.update()`
  (objekt ostane na mieste). Empties nesú hierarchiu pre Unity (collections nie sú objekty).
- Ďalšie Blender snippety: **keep-filter** (z výberu nechať len mená zo zoznamu), **decimate
  modifier 0.4** (bez apply), **name-search** (select podľa object name substring).

### 6.6 Ďalšie Unity editor nástroje
- **`MoveParentKeepChildren`**: `Tools > Recenter Parent To Children (keep children)` — posunie
  pivot parenta na stred detí bez pohnutia detí.
- **`SequentialRenamer`**: modal, premenuje selected na `{meno}_{n}` (start číslo, hierarchy sort).
- **`SortSelectedByName`**: modal, zoradí selected v hierarchii **Ascending/Descending** +
  **natural sort** (lamp_2 pred lamp_10).

---

## 7. Room sign performance (cedulky)

- **~169 `RoomDisplay`** ceduliek, každá s **world-space Canvasom** ťahajúcim dáta z JSON.
  Dáta sa načítavajú **raz** (Start → `FindAndDisplayRoom`), takže FPS problém = **rendering/rebuild
  160 aktívnych Canvasov**, nie dáta.
- **Riešenie:**
  - `RoomDisplay.SetVisible(bool)` — toggle `canvas.enabled` (dáta ostávajú, len sa nekreslí).
  - **`RoomSignManager`** (nový, na scéne): na časovači (0.2 s) zapne len cedulky **v rádiuse
    (12 m) AND na ktoré kamera pozerá** (`Dot ≥ facingDot`), zvyšok vypne. 169 checkov = nič.
  - Editovanie dát cez Canvas ostáva nezmenené. Voliteľne neskôr occlusion raycast.

---

## 8. Git — cleanup, merge, commity

### 8.1 Cleanup
- `.gitignore` doplnený: `ProfilerCaptures/` (4,1 GB), `_Backup_*`, `Data/`, `Assets/_Recovery/`,
  AI-assistant settings, **lightmapy** (exr/png/hdr, ReflectionProbe, LightingData + .meta).
- Odtrackované: 8 Fantasy Skybox lightmáp + `_Recovery` scény (na disku ostali).
- Veľký balast (ProfilerCaptures, MemoryCaptures 35 GB) **nikdy nebol commitnutý** → žiadny
  history rewrite.

### 8.2 Merge (rozišli sa vetvy)
- `origin/master` mal 1 commit navyše (`Delete Assets/3Dmodels/static directory` — zámerný cleanup),
  lokál 11 commitov → divergencia → pull spustil merge s konfliktom.
- Konflikty = súbory v `3Dmodels/static/skola/` (modify vs delete). Vyriešené **`git rm --cached`**
  (odtrackovať, **nechať na disku** — GUID/materiály nerozbité; sú v .gitignore). **Nič sa z disku
  nezmazalo.**

### 8.3 Obnova
- Omylom zmazaný **`Rooms.prefab`** obnovený z commitu `910251d` cez `git checkout <commit> -- <path>`
  (aj `.meta` → pôvodné GUID → referencie fungujú).

### 8.4 Commity (autor Robindhuil, bez Claude co-authora)
- Batch 1 (11 commitov): rozdelené na feature-flags/rendering/lighting/web/building/npc/ui/scene/assets/docs.
- Batch 2 (5): feature-flags, editor tool, building lamps, secrets prefaby, scene.
- Batch 3 (4): sign culling (RoomSignManager), door gate tool, restore Rooms.prefab, TMP font.

---

## 9. Otvorené / ďalšie kroky

1. **Decimovať propy v Blenderi** (radiátory, ploty, kryty zásuviek) → reimport → zmerať web build.
2. **Web build za COOP/COEP hlavičkami** (nasadiť Next.js patch) — inak sa threaded build nespustí.
3. **Nasadiť web scénu/lighting bez lightmáp** (Baked GI off + fill light) — potvrdiť FPS zisk na builde.
4. **Dvere na webe:** `NavMeshObstacle` + Carve gatnuté WebOnly (nie runtime rebake).
5. **Nahradiť lampy inštanciami** cez PrefabReplacer (variant lamp_1 / lamp_2 podľa mesh matchu).
6. Prípadne **WebGPU** experiment (Unity 6) na grafické hrdlo.
7. Uložiť scénu s `RoomSignManager` + doladiť rádius/uhol.

---

## 10. Konvencie session
- Web build = jediný zdroj pravdy pre výkon (editor je nafúknutý).
- Desktop quality presety (RP_High/Medium/Low/Mobile) sa **nemenia** — web ide cez RP_Web + per-
  platform override.
- Commity bez Claude co-authora, po logických skupinách.
- Pred deštruktívnymi krokmi backup / evidence-first (žiadne hádanie).
