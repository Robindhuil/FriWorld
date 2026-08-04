# Changelog

Jeden riadok na zmenu, písaný z pohľadu hráča alebo vývojára — nie zoznam súborov.
Podrobnosti sú v commite; netriviálne rozhodnutia v `docs/decisions/`.

Formát podľa [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- `RuntimeOcclusionCuller` vie na požiadanie logovať, čo reálne robí — koľko skupín
  skrýva, koľko to stojí raycastov a milisekúnd. Zapína sa `logStats` v inšpektore,
  v shipnutom builde má zostať vypnuté.

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
- Assety preusporiadané: všetok vlastný obsah je v `Assets/_Game/`, externé veci
  v `Assets/ThirdParty/`. (`c9cf423`, `7b2daa1`)
- `Features.IsWeb` sleduje aktívny build target, takže play mode v editore sedí
  s reálnym buildom. (`c461b76`)
