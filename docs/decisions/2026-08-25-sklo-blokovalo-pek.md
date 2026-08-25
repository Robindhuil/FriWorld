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

`mt_glass_2` a `mt_glass_3` prehodené na Transparent (alfa 0.35 a 0.25). Tým padnú na queue
3000 a krok 6 im sám odoberie `OccluderStatic` aj vrhanie tieňov.

Pekové páky, keď už svetlo môže dnu: `albedoBoost` 1 → 1.6, `indirectScale` 1.5 → 2,
`maxBounces` 4 → 6, pečený AO vypnutý (SSAO beží v `PC_Renderer` na 0.5, tmaviť rohy dvakrát
nemá zmysel), `sun.bounceIntensity` 1 → 2.

## Dôsledky

- Krok 6 nahlásil `ShadowCastingChanged: 505`, `StaticChanged: 187`. Z 202 tabúľ `mt_glass_2/_3`
  ich 172 prestalo vrhať tieň; zvyšných 30 zdieľa mesh s nepriehľadným rámom, a tam je vrhanie
  tieňa správne. `OccluderStatic` na nich klesol z 190 na 0.
- **Pek je od tejto zmeny neaktuálny.** Kým nebeží `Generate Lighting`, statická geometria drží
  staré lightmapy aj starú shadowmask, takže v realtime sa jas takmer nezmení (namerané x1.00
  až x1.01). Všetok výnos je v prepečení.
- `albedoBoost 1.6` je materiálová páka aplikovaná globálne v peku, nie prepisovaním 200
  materiálov. Pri albede 0.595 a 6 odrazoch stúpne celková energia odrazu zhruba z 1.3 na 2.4.
