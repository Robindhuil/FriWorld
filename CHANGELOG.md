# Changelog

Jeden riadok na zmenu, písaný z pohľadu hráča alebo vývojára — nie zoznam súborov.
Podrobnosti sú v commite; netriviálne rozhodnutia v `docs/decisions/`.

Formát podľa [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
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

### Removed
- `Tools > Setup Door Gates` — hľadal dvere cez `Contains("door")`, čím chytal aj 302
  zárubní a 221 prahov, a vrstvu mal natvrdo na `7`. Nahradilo ho okno Room Gates,
  ktoré berie dvere z registra typov.
- `RuntimeOcclusionCuller` — raycastové culovanie za behu. Zmerané: pokrývalo 338
  z 5867 rendererov (len cedule, nie budovu), stálo až 2,79 ms v jednom snímku
  a štvrtinu cedúľ skrývalo aj vtedy, keď boli jasne viditeľné. Odkedy occlusion
  culling funguje poriadne, nerobilo nič navyše. (`docs/decisions/`)

### Changed
- Všetky projektové nástroje sú pod jedným menu `FriWorld`, roztriedené do skupín
  `Registry`, `Generate`, `Feature Flags`, `Lighting`, `Room Signs`, `Utilities`
  a `Debug`. `Tools` je Unity vlastné menu a naše skripty tam už nie sú.
- Zberač prefixov už neodstrihuje koncové `_<číslo>` z mien kontajnerov, takže
  `ra100_corridor_1` a `ra100_corridor_2` sú dva riadky. Sú to dve rôzne chodby a každá
  si rozhoduje sama; typový kľúč sa tým nemení. (325 prefixov namiesto 262.)

### Fixed
- Dverné gaty už nežijú ako override na inštancii v scéne, takže ich reimport `.blend`
  nezmetie. Z 283 dverí predtým prežil jediný.
  (`docs/decisions/2026-08-24-platform-gaty-v-prefabe.md`)

### Fixed
- Occlusion culling konečne zakrýva to, čo má — **o polovicu menej draw callov
  a trojuholníkov** (namerané 1048 → 554 a 133 632 → 70 769). Tri štvrtiny stien
  pre neho doteraz neexistovali ako prekážka a bake navyše zahadzoval všetko menšie
  než 5 m, takže zostávali vykreslené veci, ktoré hráč nevidí. Nástroj na
  priraďovanie vrstiev teraz rozhoduje o occlusion podľa materiálu, nie len podľa
  mena — priehľadné plochy occludermi nikdy nebudú.

### Performance
- Web build výrazne odľahčený pre integrované grafiky: vypnuté MSAA (nahradené
  lacnejším FXAA), depth texture, LOD cross-fade a blending reflection probes;
  anizotropné filtrovanie už nie je vynútené na všetky textúry a LOD sa neprepína
  na dvojnásobnú vzdialenosť. (`c461b76`)
- Bloom, Depth of Field a Motion Blur sa na webe nezapínajú a nezobrazujú sa ani
  v nastaveniach. Farebné ladenie a tonemapping zostávajú — sú prakticky zadarmo
  a tvoria vzhľad hry. (`c461b76`)

### Fixed
- Pohľad myšou už občas nešvihne do strany vo web builde. Otáčanie je teraz
  nezávislé od frame rate. (`7d51874`)
- Video nastavenia na webe už neprehodia render pipeline na desktopovú a nezahodia
  tým celé web ladenie. (`c461b76`)
- Odstránených 13 osirených komponentov na prefaboch, ktoré blokovali ukladanie
  prefabov aj buildy. (`c473606`)
- Doplnený chýbajúci `Resources/FeatureFlags.asset` — bez neho čítali všetky
  feature flagy OFF. (`c461b76`)

### Changed
- Collidery, vrstvy a static flagy sa už neurčujú podľa kľúčových slov v kóde, ale
  podľa registra typov v `ObjectPrefixes.json` a `ObjectTypes.json`. Neznámy alebo
  nevyplnený typ sa nahlási a objekt zostane nedotknutý — namiesto toho, aby ticho
  prevzal vlastnosti podobne pomenovaného typu. Postup pri pridávaní objektu je
  v `CLAUDE.md`. (`522f44e`)
- Assety preusporiadané: všetok vlastný obsah je v `Assets/_Game/`, externé veci
  v `Assets/ThirdParty/`. (`c9cf423`, `7b2daa1`)
- `Features.IsWeb` sleduje aktívny build target, takže play mode v editore sedí
  s reálnym buildom. (`c461b76`)
