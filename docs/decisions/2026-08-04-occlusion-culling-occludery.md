# Occlusion culling: occluder rozhoduje materiál, nie meno

## Kontext

Pri pohľade na budovu zostávali vykreslené objekty, ktoré hráč reálne nevidel. Na to
vznikol `RuntimeOcclusionCuller` — raycastové culovanie za behu. Meranie ukázalo, že
**problém neriešil ani náhodou**:

- Zoskupuje podľa komponentu `RoomDisplay`, ktorý sedí na **cedulkách pri dverách**,
  nie na koreňoch miestností. Pokrýval **338 z 5867 rendererov (5,8 %)** — budovy
  samotnej sa nedotkol.
- Stál **až 2,79 ms v jednom snímku** (~7× za sekundu) a ~1 300 raycastov/s.
- **143 zo 169 skupín bolo off-screen** — a to Unity culluje samo, zadarmo. Raycasty
  rozhodovali len o ~26 skupinách, z ktorých priemerne **1,05** skončila viditeľná.
- **25 % cedúľ vyhodnotil ako zakryté aj z 2 m čelne.** Príčina: ceduľa visí na stene,
  ktorá je sama occluderom, takže lúč do stredu jej AABB vždy trafí tú stenu.

Skutočná príčina pôvodného príznaku bola inde: **1360 z 1808 stien (75 %) nemalo
`OccluderStatic`.** Umbra mala dáta zapečené a kamera ju mala zapnutú, ale tri
štvrtiny stien pre ňu neblokovali výhľad.

## Rozhodnutie

Opraviť to na zdroji — v `GenerateLayersAndStatic.cs`, ktorý sa spúšťa na všetkých
objektoch, nech to platí aj pre všetko, čo pribudne.

`IsOccluderName(string)` nahradené za `ShouldBeOccluder(GameObject)`:
**meno navrhne, geometria rozhodne.**

1. `OccluderKeywords` doplnené o `door_frame`, `foor`, `pillar`.
2. **Guard na priehľadnosť** — žiadny materiál v queue ≥ 2450.
3. **Guard na veľkosť** — súčet plôch bounding boxu ≥ 2 m².

Body 2 a 3 sú dôležitejšie než bod 1, lebo meno na toto nestačí. Dáta:
objekty `window` nesú na **tom istom rendereri** `mt_window_frame_1` (nepriehľadný)
aj `glass` / `Custom/URPGlass` (priehľadný). A `door_frame` má 307 nepriehľadných
materiálov, ale 30 priehľadných — presklené zárubne. Bez guardu by sa z okien stali
occludery a Umbra by začala cullovať to, čo cez sklo jasne vidno.

Výsledok: occludery na vrstve Obstacle **448 → 715**. Pribudlo 272 (najmä zárubne),
ubudlo 178 — z toho 169 cedúľ (drobné, len nafukovali bake), 4 tenké lišty `wall_edge`,
2 ploché `nav` pomôcky, 1 úzky stĺpik a **2 sklá, ktoré doteraz nesprávne zakrývali**.

## Nameraný výsledok

A/B v play mode cez `OcclusionABTest` (strieda `useOcclusionCulling` každé 2 s), 12 381
snímkov, obe strany takmer rovnako (6225 vs 6156), ~3 minúty chôdze po budove:

| | occlusion OFF | occlusion ON | Δ |
|---|---|---|---|
| draw calls | 1048 | **554** | **−47,1 %** |
| trojuholníky | 133 632 | **70 769** | **−47,0 %** |
| setPass calls | 44 | 33 | −25 % |
| ms/frame | 13,96 | 14,13 | −1,2 % |

**Frame time sa nezmenil a nie je to dôkaz, že to nepomáha.** Obe strany sedia na ~14 ms,
čo je strop daný niečím iným než vykresľovaním (`EditorLoop` réžia + GTX 1060, ktorú táto
scéna nezaťaží). Keď nie si limitovaný geometriou, zisk z jej polovice nevidno. Na
integrovanej grafike a vo WebGL — kde je réžia na draw call podstatne vyššia než natívne —
sa polovičné draw cally prejaviť musia. **Zatiaľ je to odôvodnený predpoklad, nie meranie;
potvrdí sa až vo web builde.**

Vizuálna kontrola pri tom istom prechode: žiadne prepadávanie geometrie ani blikanie.

## Dôsledky

- **`foor` nie je preklep na našej strane** — takto sa volajú podlahové meshe
  (19 objektov `*foor*`, 0 objektov `*floor*`). Premenovanie kľúčového slova by ho
  prestalo matchovať.
- Po každej zmene occluderov treba **rebake occlusion culling**, inak sa nič nezmení.
- **Slepá ulička, neskúšať znova:** ladenie `boundsPadding` v `RuntimeOcclusionCuller`.
  Je síce degenerované (9 vzorkovacích bodov sa zmestí do 5 mm, čiže 9 lúčov mieri
  na to isté miesto), ale s opraveným relatívnym paddingom vyjde nad-culling **rovnako
  — 43 zo 169**. Occluder a occludee je tá istá stena; paddingom sa to neopraví.
- **`RuntimeOcclusionCuller` bol odstránený.** Meranie prebehlo bez neho a Umbra sama dala
  −47 %; cedule sú `OccludeeStatic`, takže ich culluje tiež. Nerobil už nič, čo by Umbra
  neurobila lepšie a bez raycastov. Skript je zmazaný — ak by ho niekto niekedy chcel
  oživiť pre skutočnú geometriu miestností, žije v histórii do commitu `c3824e3`.
- **Pre rastúcu scénu je Umbra jediná škálovateľná cesta.** Raycastové culovanie je
  O(skupiny v zábere) každý tik, takže s každým pridaným objektom dražie. Umbra je
  predpočítaná a jej runtime cena od počtu objektov prakticky nezávisí. Cena sa presúva
  do bake času a do 1,68 MB dát v builde.
- **Po pridaní geometrie treba rebake** — inak nové objekty nič nezakryjú a nič ich nezakryje.
- `OcclusionABTest` zostáva v repe ako nástroj, ale **nie je na ničom nasadený**. Keď budeš
  chcieť čísla znova (napr. po zmene `smallestOccluder`), hoď ho na hráčovu kameru, prejdi
  budovu aspoň 2 minúty a potom ho zase odober.
