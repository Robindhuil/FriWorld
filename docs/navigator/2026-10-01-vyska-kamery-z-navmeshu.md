# Výška kamery Navigatora sa číta z navmeshu na surovej trase

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-01

## Kontext

`CameraTrack` skladá let z rohov `NavMesh.CalculatePath`. Rohy však vznikajú len tam, kde cesta
zatáča v pôdoryse, nie tam, kde sa mení sklon: chodba, ktorá ide rovno do schodov, nemá roh pri
ich päte. Verzia z 2026-09-26 brala výšku z priamok medzi rohmi, lebo snap na navmesh kamerou
trhal — lenže priamka ide šikmo cez vzduch alebo cez podlahu. Na všetkých 166 letoch: RC009
letela 26 m v podlahe (úsek dlhý 30 m klesá o 4 m, kamera až 2.26 m pod navmeshom), rad RB1xx
0.8 m nízko na 4 m, RA124 0.7 m vysoko na 5 m.

## Rozhodnutie

`FollowFloor` prečíta výšku z navmeshu **na surovej trase** — na prevzorkovaných rohoch, ešte
pred odtláčaním od stien. Krokuje od štartu: každý dotaz začína vo výške predchádzajúceho bodu,
takže v schodisku chytí rameno, po ktorom cesta ide. Šum navmeshu zoberie to isté vyhladenie,
ktoré beží po každom odtlačení.

| variant | najväčšia odchýlka od navmeshu | otočky výšky > 5 mm na 100 m |
|---|---|---|
| priamky medzi rohmi (2026-09-26) | 2.26 m, 26 m v kuse | 1.4 |
| surový snap na konci, ako v `c7da996` | — | 24.1 |
| snap až na vyhladenej trase | rozíde sa o poschodie (3.75 m) | — |
| **snap na surovej trase, potom vyhladenie** | **0.30 m, 0.7 m v kuse** | **1.4** |

Snap na vyhladenej trase zlyhá v schodisku budovy B: ramená idú 1 m od seba a vyhladená
zákruta ich prereže ponad zábradlie, takže najbližší navmesh je susedné rameno.

## Dôsledky

- Pri päte a vrchu schodov kamera zaobľuje do 0.3 m — stúpať začne asi meter pred prvým
  schodom. Je to zámer.
- Plynulosť ostala: chvenie rotácie 28.9 → 28.7 °/100 m, zrýchlenie výšky 165 → 172 mm/100 m.
- Kamera letí vo výške očí aj tam, kde predtým plávala vyššie, takže zavadí o to, čo NavMesh
  nepozná — napr. `rc000_corridor_1_handicap_machine_1` vo vrstve `NoObstacle` (RC006, RC007).
  To sa rieši vrstvou objektu, nie výškou kamery.
