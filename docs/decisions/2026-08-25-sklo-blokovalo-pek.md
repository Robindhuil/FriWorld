# Čo púšťa svetlo do budovy, rozhoduje materiál — nie typ

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-08-25

## Kontext

Interiér bol tmavý a hľadalo sa, kde pridať bounce. Materiály za to nemohli: albedo statickej
geometrie vážené plochou je 0.595, `mt_interior_wall_1` má 0.794, `mt_ceiling_1` 0.728. Svetlo
sa jednoducho dovnútra nedostávalo.

Progresívny lightmapper berie **každý shadow caster ako úplne nepriehľadný**, bez ohľadu na to,
čo hovorí materiál. Zasklenie, ktoré vrhá tieň, teda zamuruje miestnosť. `GlassShadowSetup.cs`
na to existoval a bol napísaný správne, aj s tým komentárom — len robil `FindObjectsByType` po
**scéne**, takže vyrábal prefab overrides, a kroky 4–8 zapisujú do `FriBuilding.prefab`. Prvý
beh pipeline ich zmietol. Tá istá pasca ako pri dverných gatoch. Preto mala v deň merania celá
budova `castShadows = On`.

## Rozhodnutie

Vrhanie tieňov rozhoduje **`GenerateLayersAndStatic`** (krok 6), rovnakým testom priehľadnosti
ako `OccluderStatic`: renderer, ktorého **všetky** materiály sú na queue ≥ 2450, tieň nehádže.
Patrí to sem preto, že tento krok píše do prefab assetu.

**Musí to zostať test na materiáli, nie na type.** Jedno okno sú tri renderery s tým istým
typovým kľúčom `window_<int>_glass`:

```
ra006_window_1          mt_plastic_1   queue 2000   rám, tieni
ra006_window_1_glass_1  glass          queue 3000   zasklenie, púšťa
ra006_window_1_glass_2  mt_glass_2     queue 2000   parapet + nadpražie, tieni
```

`mt_glass_2` nie je zasklenie. Rozloženie plochy trojuholníkov v jednej takej doske:

```
ra000_corridor_3_window_1_glass_2   19.44 x 3.85 x 1.49 m
   +0.25 m  31.7 %   parapet pod oknom
   +0.75 m  31.7 %
   +3.25 m  18.3 %   nadpražie
   +3.50 m  18.3 %
   medzi tým nič — tam je otvor
```

Je to plná stena pod parapetom a nad nadpražím, ktorá má tieniť.

## Čo sa cestou ukázalo ako nepravda

Do registra pribudlo na chvíľu voliteľné pole `shadows: yes | no` s tým, že tabule majú
vyzerať nepriehľadne a napriek tomu púšťať svetlo. Postavené to bolo na zámene: `mt_glass_2`
sa považoval za zasklenie. `shadows: no` na type `window_<int>_glass` teda otvorilo **celú
zostavu vrátane parapetu**, takže slnko svietilo cez plnú stenu pod oknom a radiátory na nej
hádzali tieň do chodby.

Pole je zmazané. Skutočné zasklenie je materiál `glass` na queue 3000 a materiálový test ho
pustí sám.

## Dôsledky

- Tieň nevrhá 333 rendererov, všetky rozhodnuté materiálom. Register do toho nehovorí.
- 202 parapetov `mt_glass_2` / `mt_glass_3` tieni, 318 zasklení `glass` púšťa.
- `GlassShadowSetup.cs` zmazaný — duplikát, ktorý navyše nefungoval trvalo.
- Ak by niekedy bolo treba, aby konkrétna plocha vyzerala plne a napriek tomu púšťala svetlo,
  nie je to práca pre register. Buď sa rozdelí mesh v Blenderi, alebo dostane vlastný
  priehľadný materiál — typový kľúč to vyjadriť nevie, lebo pokrýva celú zostavu.
