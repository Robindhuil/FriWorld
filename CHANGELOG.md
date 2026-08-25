# Changelog

Jeden riadok na zmenu, písaný z pohľadu hráča alebo vývojára — nie zoznam súborov.
Podrobnosti sú v commite; netriviálne rozhodnutia v `docs/decisions/`.

Formát podľa [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
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
- Oknami svieti do interiéru slnko. 206 tabúľ bolo pre lightmapper plný múr, takže cez ne
  neprešiel ani fotón, a 180 z nich bolo navyše označených ako occluder — Umbra cullovala
  všetko za nimi. Tabule vyzerajú nepriehľadne aj naďalej, to je zámer; svetlo cez ne púšťa
  nové voliteľné pole `shadows` v `ObjectTypes.json`.
  (`docs/decisions/2026-08-25-sklo-blokovalo-pek.md`)
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
- Vrhanie tieňov rozhoduje krok 6 pipeline (`Layers And Static`) podľa materiálov, rovnakým
  testom priehľadnosti ako `OccluderStatic` — čo vidíš skrz, tým musí prejsť aj svetlo peku.
  Zapisuje sa do `FriBuilding.prefab`, takže to prežije ďalší beh pipeline. Nahlásilo
  `ShadowCastingChanged: 505`.
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
- Web build výrazne odľahčený pre integrované grafiky: vypnuté MSAA (nahradené
  lacnejším FXAA), depth texture, LOD cross-fade a blending reflection probes;
  anizotropné filtrovanie už nie je vynútené na všetky textúry a LOD sa neprepína
  na dvojnásobnú vzdialenosť. (`c461b76`)
- Bloom, Depth of Field a Motion Blur sa na webe nezapínajú a nezobrazujú sa ani
  v nastaveniach. Farebné ladenie a tonemapping zostávajú — sú prakticky zadarmo
  a tvoria vzhľad hry. (`c461b76`)

### Removed
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
