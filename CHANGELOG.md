# Changelog

Jeden riadok na zmenu, písaný z pohľadu hráča alebo vývojára — nie zoznam súborov.
Podrobnosti sú v commite; netriviálne rozhodnutia v `docs/decisions/`.

Formát podľa [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Verzia je `bundleVersion` z `ProjectSettings` — dvíha sa pri builde do produkcie a vtedy
sa `[Unreleased]` premenuje na to číslo. Rozhodnutia k jednotlivým verziám sú
v [`docs/decisions/`](docs/decisions/), zmerané a nespravené návrhy
v [`docs/findings/`](docs/findings/).

## [Unreleased]

_Nazbierané od poslednej produkčnej verzie. Aktuálny `bundleVersion`: **0.1.2-alpha**._

### Added
- NPC sa skladajú z presetov a farieb namiesto tridsiatich samostatných modelov. Telo,
  oblečenie a vlasy vyberá `CharacterRandomizer` zo seedu, takže NPC s rovnakou identitou
  vyzerá po respawne rovnako a neukladá sa nič. Pravidlá — čo s čím nejde, čo je len pre
  jedno pohlavie a ktorú kožu preset zakrýva — sa píšu ručne do troch JSON registrov
  vedľa `ObjectTypes.json`.
- Menu `Character` — `1 — Report` povie, čo v registroch alebo v prefabe nesedí a nikdy nič
  nehádá, `2 — Generate Shades` dogeneruje materiály vrátane tmavších odtieňov odvodených
  v HSV z hlavnej farby, `3 — Bake Catalog` skompiluje registre do `CharacterCatalog.asset`.
  Bake odmietne zapísať, kým Report hlási chybu v registri; chýbajúce telo je varovanie
  a upečú sa telá, ktoré existujú.
- `CharacterGridSpawner` — testovací nástroj, ktorý pri Play rozostaví mriežku postáv, aby
  sa dali kombinácie pozrieť naraz. Seedy idú za sebou, takže tie isté nastavenia dajú tie
  isté postavy a problém sa dá reprodukovať, kým sa opravuje.
- `NpcWander` — chodenie po `PathWay` bez dialógov, questov a animátora, aby generované NPC
  mohli chodiť po fakulte skôr, než sa NPC vrstva prepíše.
  (`docs/2026-08-28-npc-skripty-na-prerobenie.md`)
- `tools/blender/replace_material.py` — vymení jeden materiál za druhý na označených
  objektoch. Cieľový materiál nikdy nevytvára: preklep v mene by inak ticho vyrobil
  prázdny sivý materiál namiesto toho, aby povedal, že meno nesedí. Sloty naviazané na
  zdieľané mesh DATA hlási zvlášť, lebo tie zmenia aj objekty, ktoré si neoznačil.
  (`18986b1`)
- Scéna `Assets/_Game/Scenes/Character/NpcCharacterGenerator.unity` — pracovný stôl pre
  skladanie postáv, mimo Demo scény. Zatiaľ nie je v Build Settings. (`97f6e16`)
- `tools/blender/rebuild_uv_channels.py` — po ňom má mesh presne dva UV kanály, 0 textúrový
  a 1 lightmapa. Staršie skripty kanál len dopĺňali podľa mena, takže na meshi so štyrmi
  kanálmi skončila lightmapa na indexe 3 a Unity pieklo do toho, čo bolo na indexe 1.
  Unity poradie kanálov čítať musí, mená ignoruje. `DRY_RUN` najprv vypíše, čo na objektoch
  je, a beh na konci overí, že lightmap kanál leží v 0..1.
  (`docs/decisions/2026-08-25-lightmap-uv-z-blenderu.md`)
- Menu `Routine` — pipeline na pridanie objektu v poradí, v akom sa spúšťa, plus okno
  `Object Pipeline` s krátkym popisom ku každému kroku a tlačidlom Run. Označíš
  `FriBuilding` raz a klikáš zhora dole; výber sa medzi krokmi nemení.
- `FriWorld > Feature Flags > Room Gates` — okno, ktoré generuje platformové gaty
  z `RoomPlatforms.json` priamo do prefab assetu. Vetvy `Objects` a `fri_building` sa
  dajú zapísať zvlášť, Preview najprv ukáže, čo by sa zmenilo.
- `RoomPlatforms.json` — platformové rozhodnutie pre každú miestnosť žije v repozitári,
  nie ako komponent v hierarchii. `Add Prefixes From Selection` ho drží v súlade
  s budovou: nová miestnosť pribudne nerozhodnutá navrch súboru, hotové rozhodnutia
  sa nikdy neprepíšu.
- `Add Prefixes From Selection` zadrží aj prefix, ktorý by zožral hlavičku
  registrovaného typu — `cubboard_1` by z `cubboard_1_part_1` spravil `part`. Doteraz
  chytal len prefix, ktorý bol typom presne.
- `OcclusionABTest` — merací nástroj do play mode: strieda zapnuté a vypnuté
  occlusion culling a hlási draw cally, trojuholníky a čas snímku zvlášť pre oba
  stavy. Len pre editor, v builde sa nekompiluje. Nie je nikde nasadený — nasadí sa
  na hráčovu kameru, keď treba čísla, a potom sa zase odoberie.

### Fixed
- Tag `Door` a vrstva `Interactable` sedia už len na skutočných dverách. Tag sa odvodzoval
  z vrstvy, takže ho dostalo aj 302 zárubní a dva kusy `thick_door`, ktoré žiadne správanie
  dverí nenesú — spolu 304 objektov s tagom a bez komponentu `Door`. Rozhoduje o tom pole
  `script` v registri, rovnako ako v `SetupInteractables` a `DoorComponentGates`.
  Zárubne šli z `interactable` na `noObstacle`, čím sa zároveň stali statickými a začnú sa
  pri najbližšom peku zapekať do lightmapy. `thick_door` dostal `script: Door`, takže sa
  konečne otvára; jeho pánt išiel medzi hardware k zárubniam. Zárubne skončili na `obstacle`,
  lebo navmesh zbiera výhradne vrstvy `Obstacle` a `Nav`.
  (`docs/decisions/2026-08-25-door-frame-noobstacle.md`)
- Oknami svieti do interiéru slnko. Zasklenie vrhalo tieň, a progresívny lightmapper berie
  každý shadow caster ako plný múr bez ohľadu na materiál, takže miestnosti boli zamurované.
  Rozhoduje o tom materiál renderera: jedno okno sú tri renderery — rám, zasklenie a plný
  parapet s nadpražím — a púšťať má len ten prostredný.
  (`docs/decisions/2026-08-25-sklo-blokovalo-pek.md`)
- Slnko už nesvieti cez plnú stenu pod oknom. `mt_glass_2` nie je zasklenie ale parapet
  a nadpražie, takže keď sa tieň vypol na celom type `window_<int>_glass`, otvorila sa aj tá
  stena — radiátory na nej potom hádzali tieň do chodby.
- Slnko už nepresvitá stenami do interiéru. Smerové svetlo malo `shadowStrength 0.7`, čo
  púšťalo 30 % priameho slnka cez každý tieň v scéne — vrátane celej obvodovej steny budovy.
  Osvetlená bola vždy presne tá svetová strana stien, ktorá mieri na slnko, aj v miestnosti
  bez okna. V uzavretej miestnosti klesol pomer jasu stien z 3.14× na 1.02×.
  (`docs/decisions/2026-08-25-shadow-strength-a-shadowmask.md`)
- Kvalita „Nízke" má na desktope tiene. `RP_Low` mal `shadowDistance 0` prevzatú z webového
  ladenia, takže hráč na najnižšom presete videl slnko cez steny bez ohľadu na opravu vyššie.
- Posuvníky hlasitosti v nastaveniach konečne ovplyvňujú zvuk dverí. Z 291 `AudioSource`
  v budove ich bolo do mixéra napojených 14 — zvyšok hral priamo do AudioListenera, kam
  žiadny parameter mixéra nedosiahne. Napojenie žilo len v inšpektore `Interactable`, takže
  zabralo pri ručnom kliknutí a nikdy pri objektoch z generátora.
  (`docs/decisions/2026-08-24-sfx-mimo-mixera.md`)
- Dverné gaty už nežijú ako override na inštancii v scéne, takže ich reimport `.blend`
  nezmetie. Z 283 dverí predtým prežil jediný.
  (`docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`)
- Collidery, vrstvy aj interaktabilita sa generujú priamo do `FriBuilding.prefab`, nie na
  inštanciu v scéne. Doteraz to boli prefab overrides — 2671 komponentov, ktoré by prvý
  reimport `.blend` zmietol rovnako ako dverné gaty. Tie tri nástroje už výber neriešia.

- Occlusion culling konečne zakrýva to, čo má — **o polovicu menej draw callov
  a trojuholníkov** (namerané 1048 → 554 a 133 632 → 70 769). Tri štvrtiny stien
  pre neho doteraz neexistovali ako prekážka a bake navyše zahadzoval všetko menšie
  než 5 m, takže zostávali vykreslené veci, ktoré hráč nevidí. Nástroj na
  priraďovanie vrstiev teraz rozhoduje o occlusion podľa materiálu, nie len podľa
  mena — priehľadné plochy occludermi nikdy nebudú.

- Pohľad myšou už občas nešvihne do strany vo web builde. Otáčanie je teraz
  nezávislé od frame rate. (`7d51874`)
- Video nastavenia na webe už neprehodia render pipeline na desktopovú a nezahodia
  tým celé web ladenie. (`c461b76`)
- Odstránených 13 osirených komponentov na prefaboch, ktoré blokovali ukladanie
  prefabov aj buildy. (`c473606`)
- Doplnený chýbajúci `Resources/FeatureFlags.asset` — bez neho čítali všetky
  feature flagy OFF. (`c461b76`)

### Changed
- NPC v Demo scéne spawnuje `CharacterNpcSpawner` z katalógu postáv namiesto zoznamu
  tridsiatich prefabov. Polomer, životnosť aj odchod domov pred zmiznutím zostali rovnaké.
  Starý `NPCSpawner` je zatiaľ len vypnutý, nie zmazaný.
- Materiály sa volajú podľa toho, **čím povrch je**, nie podľa toho, na akom objekte sedí.
  `mt_lamp_1`, `mt_sign_2`, `mt_trash_container_4` a ďalších osemdesiat nahradili
  `mt_plastic_1..7`, `mt_wood_5..6`, `mt_fri_paint_1..5`, `mt_steel_8`, `mt_dirt_1`,
  `mt_floor_8` a `mt_fabric_1..2`. Zo **140 materiálov je 79**, každý povrch je jedna
  SRP Batcher dávka namiesto desiatich a preladenie svetla je jedna zmena, nie osemdesiat.
  71 prefabov je prepojených v tom istom commite. (`e6f8685`,
  `docs/decisions/2026-08-28-materialy-podla-substancie.md`)
- Bloom je striedmejší a farebné ladenie kontrastnejšie: prah bloomu 0.8 → 1, intenzita
  1.2 → 1 a scatter 0.7 → 0.325, takže žiara je tesnejšia a nerozlieva sa cez celý obraz;
  kontrast −16.5 → −24.4, sýtosť 25 → 53.8. (`c8d5f44`)
- Dvere dostali Light Probe Proxy Volume, takže ich nesvieti jedna vzorka v ťažisku. Prob sa
  vnútri objemu dverí mení priemerne 2.10x a najhoršie 15.86x — dvere v prahu majú na jednej
  strane denné svetlo a na druhej tmavú chodbu, a jedna vzorka to spriemerovala. Mriežka je
  2 x 1 x 4 v lokálnych osiach meshu, teda štyri vzorky po výške. Na zariadení bez podpory
  3D textúr `LightProbeProxyFallback` spadne späť na blendované proby.
- Dvere, NPC a ostatné dynamické objekty už nesvietia inak než statická geometria vedľa nich.
  Light proby sa kladú na NavMesh, nie do mriežky odvodenej od bounding boxu budovy — tá mala
  dvanásť pevných výšok pre celú budovu, ktoré s podlažiami nemali nič spoločné, takže dvere
  na jednom poschodí brali svetlo spod nôh a na inom zo vzduchu meter a pol nad podlahou.
  Namerané na dverách: najbližší prob zo 4.62 m na 1.02 m, najhorší prípad zo 4.86 m na
  1.96 m, a to pri 3698 proboch namiesto 11194, teda aj kratší pek.
- Exteriér stlmený bez toho, aby na tom stratil interiér: ambient `Trilight` z 0.93 na 0.65.
  Vonku vidí geometria celú pologuľu oblohy a berie skoro celý ten člen, vnútri ho pek
  zatieni, takže je to jediná páka, čo reže len vonku. Priame slnko na tom podiel nemá —
  s úplne vypnutým slnkom klesol jas zeme len z 0.575 na 0.519, teda ~10 %.
  **Prejaví sa až po `Generate Lighting`.**
- Volume `Post Processing` s profilom `PlayerPP` je globálny s prioritou 1. Visel na hráčovi
  v 0.75 m sfére a sedel na rovnakej priorite ako `Global Volume`, takže ktorý z nich vyhrá,
  nebolo definované — a líšili sa v expozícii aj v bloome. `PlayerPP` prestal prepisovať
  `postExposure`; nikto ho nenastavuje a len rušil expozíciu scény.
- Vrhanie tieňov rozhoduje krok 6 pipeline (`Layers And Static`) podľa materiálov, rovnakým
  testom priehľadnosti ako `OccluderStatic` — čo vidíš skrz, tým musí prejsť aj svetlo peku.
  Zapisuje sa do `FriBuilding.prefab`, takže to prežije ďalší beh pipeline. Typ o tom
  rozhodovať nemôže: `window_<int>_glass` pokrýva celú zostavu vrátane parapetu.
- Pek dostal viac odrazeného svetla: `albedoBoost` 1 → 1.6, `indirectScale` 1.5 → 2.5,
  `maxBounces` 4 → 6, `sun.bounceIntensity` 1 → 2, pečený AO vypnutý (SSAO už beží
  v `PC_Renderer`). **Prejaví sa až po `Generate Lighting`.**
- Všetky projektové nástroje sú pod jedným menu `FriWorld`, roztriedené do skupín
  `Registry`, `Generate`, `Feature Flags`, `Lighting`, `Room Signs`, `Utilities`
  a `Debug`. `Tools` je Unity vlastné menu a naše skripty tam už nie sú.
- Zberač prefixov už neodstrihuje koncové `_<číslo>` z mien kontajnerov, takže
  `ra100_corridor_1` a `ra100_corridor_2` sú dva riadky. Sú to dve rôzne chodby a každá
  si rozhoduje sama; typový kľúč sa tým nemení. Prefixov je tým pádom viac.

- Collidery, vrstvy a static flagy sa už neurčujú podľa kľúčových slov v kóde, ale
  podľa registra typov v `ObjectPrefixes.json` a `ObjectTypes.json`. Neznámy alebo
  nevyplnený typ sa nahlási a objekt zostane nedotknutý — namiesto toho, aby ticho
  prevzal vlastnosti podobne pomenovaného typu. Postup pri pridávaní objektu je
  v `CLAUDE.md`. (`522f44e`)
- Assety preusporiadané: všetok vlastný obsah je v `Assets/_Game/`, externé veci
  v `Assets/ThirdParty/`. (`c9cf423`, `7b2daa1`)
- `Features.IsWeb` sleduje aktívny build target, takže play mode v editore sedí
  s reálnym buildom. (`c461b76`)

### Performance
- Dynamic batching je vypnutý aj na desktope. Prehadzuje malé meshe na CPU každý snímok,
  aby ich zlial do jedného draw callu — lenže pod URP to isté rieši SRP Batcher bez toho,
  aby sa vrcholov dotkol, takže tá CPU práca bola zbytočná. Web ho mal vypnutý už predtým.
  (`aacb912`)
- Web build výrazne odľahčený pre integrované grafiky: vypnuté MSAA (nahradené
  lacnejším FXAA), depth texture, LOD cross-fade a blending reflection probes;
  anizotropné filtrovanie už nie je vynútené na všetky textúry a LOD sa neprepína
  na dvojnásobnú vzdialenosť. (`c461b76`)
- Bloom, Depth of Field a Motion Blur sa na webe nezapínajú a nezobrazujú sa ani
  v nastaveniach. Farebné ladenie a tonemapping zostávajú — sú prakticky zadarmo
  a tvoria vzhľad hry. (`c461b76`)

### Removed
- Prefaby `GameObject 7`, `GameObject 8` a `GameObject 9` z priečinka `cubboard` — uložili
  sa omylom a niesli Unity default meno. Nič ich nereferencovalo a medzera v mene znamená,
  že by sa aj tak nikdy nezhodli s typovým kľúčom v registri. (`7d2f5a8`)
- `FriWorld > Lighting > Glass: Disable Shadow Casting` — robilo správnu vec na nesprávnom
  mieste. Písalo vrhanie tieňov na inštanciu v scéne, takže to bol override a prvý beh
  pipeline ho zmietol. Rozhodnutie prevzal krok 6.
- `Tools > Setup Door Gates` — hľadal dvere cez `Contains("door")`, čím chytal aj 302
  zárubní a 221 prahov, a vrstvu mal natvrdo na `7`. Nahradilo ho okno Room Gates,
  ktoré berie dvere z registra typov.
- `RuntimeOcclusionCuller` — raycastové culovanie za behu. Zmerané: pokrývalo 338
  z 5867 rendererov (len cedule, nie budovu), stálo až 2,79 ms v jednom snímku
  a štvrtinu cedúľ skrývalo aj vtedy, keď boli jasne viditeľné. Odkedy occlusion
  culling funguje poriadne, nerobilo nič navyše. (`docs/decisions/`)
