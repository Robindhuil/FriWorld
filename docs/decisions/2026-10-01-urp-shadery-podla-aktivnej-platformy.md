# URP orezáva shadery podľa aktívnej platformy editora, nie podľa buildu

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-01

## Kontext

Prvé web buildy Navigatora (`Navigator → Build Web`, čiže `BuildPipeline.BuildPlayer` s profilom
`NavigatorWeb`) sa spúšťali z editora, ktorý bol prepnutý na Windows. Build prešiel bez chyby,
v prehliadači však chýbalo všetko s URP Lit: steny, podlahy, schody aj dvere. Kreslilo sa len
sklo (vlastný shader), Unlit tabuľky a obloha.

Prejav ťahal na zlé miesta. Statický batching, occlusion, lightmapy ani `RP_Web` to neboli,
všetko overené a vylúčené. Renderery boli zapnuté a prešli cullingom (`isVisible`). Až záznam
GL volaní jedného snímku ukázal, že nepriehľadný pas nakreslí 5 štvorcov a nič viac, takže
o Lit objekty sa ani nepokúsi.

Príčina je v `Editor.log` z buildu:

    3 URP assets included in build
    - RP_Low … - RP_Medium … - RP_High …
    Compiling shader "Universal Render Pipeline/Lit"
      Pass "ForwardLit" (vp) … After built-in stripping: 36 · After scriptable stripping: 0

`URPPreprocessBuild` a `ShaderBuildPreprocessor` vyberajú URP assety podľa
`EditorUserBuildSettings.activeBuildTarget`, teda podľa úrovní kvality **aktívnej** platformy.
Pre Windows sú to Nízke, Stredné a Vysoké, kým `RP_Web` patrí len WebGL. URP preto orezal
varianty podľa desktopových assetov a z `ForwardLit` pre gles3 nenechal nič. Build s profilom
editor na web síce prepne, ale URP si assety zoberie ešte pred tým, z Windows.

## Rozhodnutie

`Build Web` pred buildom prepne editor na WebGL a aktivuje `NavigatorWeb`. Ak sa to nepodarí,
nebuildí a zapíše chybu. Po builde vráti profil aj platformu, na ktorej editor bol, lebo FriWorld
v play mode na webovej platforme strihá desktopOnly obsah.

Po oprave log ukazuje `1 URP assets included in build - RP_Web` a `ForwardLit` 36 → 6.
Budova sa kreslí a kamera doletí k RA101.

## Dôsledky

- Každý build prepína platformu tam a späť. Prvý build po oprave trval jednorazovo 13:20,
  ďalší 0:48 a spolu s prepínaním asi dve minúty, teda toľko ako predtým.
- Pasca platí pre každý build cez `BuildPipeline` na inú než aktívnu platformu, nielen pre
  Navigator. Web build FriWorldu spustený skriptom z Windows by dopadol rovnako.
- Overenie: v `Editor.log` vyhľadať `URP assets included in build` a skontrolovať, že je tam
  `RP_Web`.
