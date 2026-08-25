# Do interiéru sa nedostávalo svetlo, lebo okná neboli sklo

## Kontext

Interiér bol tmavý a hľadalo sa, kde pridať bounce. Prvé podozrenie padlo na materiály —
že sú príliš tmavé na to, aby čokoľvek odrazili. Zmerané albedo statickej geometrie budovy,
vážené plochou:

| materiál | plocha | albedo luma |
|---|---|---|
| `mt_interior_wall_1` | 48 516 | 0.794 |
| `mt_fri_paint_1` | 44 417 | 0.792 |
| `mt_ceiling_1` | 23 406 | 0.728 |
| `mt_floor_1` | 13 170 | 0.617 |
| **vážený priemer** | | **0.595** |

Materiály sú v poriadku. Problém je, že sa svetlo dovnútra vôbec nedostane:

- **196 okenných tabúľ s `mt_glass_2`** (+ 10 s `mt_glass_3`) boli `URP/Lit` so `_Surface = 0`,
  teda **Opaque**, render queue 2000, plná modrá bez textúry. Sedeli na objektoch pomenovaných
  `ra006_window_1_glass_2`. Pre progresívny lightmapper je nepriehľadný tieňovač plný múr bez
  ohľadu na to, čo hovorí materiál — cez tie okná neprešiel ani fotón.
- Rovnaký queue 2000 znamenal, že `ShouldBeOccluder` (test `renderQueue >= 2450`) ich vyhodnotil
  ako occludery. **180 z 196** malo `OccluderStatic`, takže Umbra cullovala aj to, čo je za nimi.
- `Custom/URPGlass` (385 tabúľ) má jediný pass, `UniversalForward`. Žiadny `SHADOWCASTER`,
  žiadny `META`. Tieň teda nehádže — overené meraním, vypnutie `shadowCastingMode` na nich
  zmenilo jas presne o 0.0000 — ale ani do GI neprispieva.

`GlassShadowSetup.cs` na to existoval a bol napísaný správne, aj s komentárom o tom, že
lightmapper berie každý shadow caster ako nepriehľadný. Len robil `FindObjectsByType` po
**scéne**, takže vyrábal prefab overrides na inštancii — a kroky 4–8 pipeline zapisujú do
`FriBuilding.prefab`, takže to prvý beh zmietol. Preto mali v deň merania všetky renderery
`castShadows = On`. Tá istá pasca ako pri dverných gatoch.

## Rozhodnutie

Vrhanie tieňov rozhoduje **`GenerateLayersAndStatic`** (krok 6), rovnakým testom priehľadnosti
ako `OccluderStatic`: renderer, ktorého **všetky** materiály sú na queue ≥ 2450, tieň nehádže.
Nie je to v `ObjectTypes.json` z rovnakého dôvodu ako occluder — meno nepovie, či je plocha
priesvitná, materiál áno. A patrí to sem preto, že tento krok píše do prefab assetu; čokoľvek,
čo zapíše vrhanie tieňov na inštanciu v scéne, je override a ďalší beh ho zahodí.

`GlassShadowSetup.cs` zmazaný — duplikát, ktorý navyše nefungoval trvalo.

Materiálový test ale nestačí. `mt_glass_2` a `mt_glass_3` **majú vyzerať nepriehľadne** — je to
štýlové rozhodnutie, nie chyba. „Nepriehľadné pre oko" a „nepriehľadné pre lightmapper" sú
však dve rôzne veci: tabuľa má čítať ako plné sklo, ale miestnosť za tabuľou, ktorá zožerie
každý fotón, je jednoducho čierna.

Preto má `ObjectTypes.json` voliteľné pole **`shadows: yes | no`**, tvarom presne ako
`occluder`. Keď chýba, rozhodujú materiály. Nastavené na `no` pre `window_<int>_glass`,
`big_window_<int>_glass`, `roof_window_<int>_glass` a `roof_<int>_glass`.

`OccluderStatic` zostáva na `auto`, teda tie tabule occludujú — a je to správne. Cez
nepriehľadné sklo naozaj nevidno, takže Umbra smie cullovať, čo je za ním. Nefyzikálne je len
to, že cez ne prejde svetlo peku; to je zámerný podvod, aby miestnosti mali denné svetlo.

Pekové páky, keď už svetlo môže dnu: `albedoBoost` 1 → 1.6, `indirectScale` 1.5 → 2,
`maxBounces` 4 → 6, pečený AO vypnutý (SSAO beží v `PC_Renderer` na 0.5, tmaviť rohy dvakrát
nemá zmysel), `sun.bounceIntensity` 1 → 2.

## Dôsledky

- Tieň nevrhá 530 rendererov: 333 preto, že ich materiál je priehľadný, 197 preto, že to hovorí
  register. V tej druhej skupine je 26 kusov navyše — 25 `mt_plastic_1` a jeden
  `mt_interior_wall_1`, ktoré zdieľajú typový kľúč s tabuľami. Sú to okenné kovania na okenných
  objektoch, takže je v poriadku, že tieň nehádžu.
- `shadows` je zámerne na úrovni **typu**, nie materiálu, hoci `occluder` sa rozhoduje podľa
  materiálu. Materiál tu odpoveď nepozná: `mt_glass_2` je nepriehľadný naschvál a nič v ňom
  nepovie, že cez neho má napriek tomu prejsť svetlo. To je rozhodnutie o objekte, a tie žijú
  v registri.
- **Pek je od tejto zmeny neaktuálny.** Kým nebeží `Generate Lighting`, statická geometria drží
  staré lightmapy aj starú shadowmask, takže v realtime sa jas takmer nezmení (namerané x1.00
  až x1.01). Všetok výnos je v prepečení.
- `albedoBoost 1.6` je materiálová páka aplikovaná globálne v peku, nie prepisovaním 200
  materiálov. Pri albede 0.595 a 6 odrazoch stúpne celková energia odrazu zhruba z 1.3 na 2.4.
