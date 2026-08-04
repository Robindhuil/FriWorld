# Register typov objektov pre GenerateColliders a GenerateLayersAndStatic

Stav: návrh schválený 2026‑08‑04, pripravený na implementačný plán.

---

## 1. Problém

Oba nástroje dnes rozhodujú podľa **ručne udržiavaných polí kľúčových slov** priamo v kóde
(`MeshColliderKeywords`, `ObstacleKeywords`, `NoObstacleKeywords`, …). Matching hľadá
*podsekvenciu tokenov* v mene objektu.

Z toho plynie chyba, ktorá sa nedá opraviť doplnením ďalšieho slova do zoznamu:

> Pridám objekt `sun_lamp`, ktorý sa má správať inak než `lamp`. Meno obsahuje `lamp`,
> takže dostane vlastnosti lampy. **Nikto mi to nepovie.**

Pravidlo nikdy nepovie „toto meno nepoznám" — vždy nájde nejakú odpoveď. To je systémová
vlastnosť, nie chyba v dátach.

Nie je to hypotetické. Rovnaký mechanizmus už dnes postihuje `window_1_glass` (658 objektov):
`window` v ňom matchne skôr, takže sklo dostáva vlastnosti okna.

### Namerané

| | |
|---|---|
| rôznych mien objektov nesúcich geometriu | 3484 |
| z toho dnes nematchuje **nič** (ticho prepadne) | 314 |
| mesh objektov celkovo | 11 866 |

Medzi tichými prepadmi sú aj blenderovské zvyšky, ktoré sa dostali do projektu:
`Plane.005`, `shead_1.066`, `usta.015`, `oci.010`, `Circle`.

---

## 2. Princíp riešenia

Prechod od **„hádaj podľa podobnosti"** k **„vieš alebo nevieš"**.

Meno objektu sa zredukuje na **typový kľúč**. Kľúč sa vyhľadá v registri. Známy kľúč dostane
svoje vlastnosti; **neznámy kľúč sa nedotkne ničoho a nahlási sa.**

Čokoľvek, čo systémom neprejde, je chyba v pomenovaní — a práve preto musí byť vidieť.

---

## 3. Odvodenie typového kľúča

Pre každý objekt s mesh rendererom:

1. **Odstrihni najdlhší sediaci prefix** zo zoznamu prefixov (tvar `<prefix>_`).
2. **Odstrihni vodiace `<int>_`**, ak zostalo.
3. **Odstrihni koncové `_<int>`.**
4. Čo zostane, je typový kľúč.

```
ra000_cleaners_room_ceiling_1
  – prefix "ra000_cleaners_room"   → ceiling_1
  – suffix "_1"                    → ceiling

ra100_corridor_1_door_frame_2
  – prefix "ra100_corridor"        → 1_door_frame_2
  – vodiace "1_"                   → door_frame_2
  – suffix "_2"                    → door_frame
```

Krok 2 nie je kozmetika. Bez neho zostáva číslo inštancie miestnosti na začiatku a ten istý
typ sa rozpadne na desiatky kľúčov: namerané `door_frame` ×233, ale zároveň `1_door_frame` ×21,
`2_door_frame` ×9, … až `14_door_frame`. Rovnako `wall_edge` vs `1_wall_edge`, `2_wall_edge`,
`3_wall_edge` a `window_1_glass` vs `1_window_1_glass`, `2_window_1_glass`.

### Stráž, bez ktorej sa to rozsype

**Odstránenie prefixu musí nechať za sebou slovo.** Bez tejto podmienky sa `lamp_2` strafí na
prefix `lamp`, zostane `2`, a kľúč „2" zhltne 1550 objektov. Ak kandidát na prefix nechá za
sebou len číslo, preskočí sa a skúsi sa kratší.

### Overené na dátach

| | |
|---|---|
| mesh objektov | 11 866 |
| potrebných prefixov | **258** |
| vzniknutých typových kľúčov | **559** |
| objektov, ktoré sa nerozlúštia | **170 (1,4 %)** |

Najčastejšie kľúče: `lamp` ×2632, `window_1_glass` ×658, `radiator` ×608, `window` ×564,
`nav` ×532, `wall` ×472, `door_frame` ×466, `ceiling` ×444.

Tých 170 nerozlúštených sú objekty pod funkčnými skupinami (napr. `Nav`), kde prefix existuje
len v mene a v hierarchii sa ho niet čoho chytiť. Skončia v hlásení.

---

## 4. Dátové súbory

Oba v `Assets/_Game/Editor/`, takže sa **nedostanú do buildu**. Zámerne oddelené — prefixy sú
štruktúra budovy, typy sú správanie.

### `ObjectPrefixes.json`

```json
{
  "prefixes": [
    "ra000_cleaners_room",
    "ra100_women_restroom",
    "rc000_corridor_2"
  ]
}
```

Zoznam **navrhne sken** z mien kontajnerov v hierarchii; človek ho potvrdí. Ručné písanie 258
prefixov sa nevyžaduje.

### `ObjectTypes.json`

```json
{
  "types": [
    { "name": "wall",           "collider": "mesh", "layer": "obstacle",   "occluder": "auto" },
    { "name": "window",         "collider": "box",  "layer": "obstacle",   "occluder": "auto" },
    { "name": "window_1_glass", "collider": "box",  "layer": "noObstacle", "occluder": "no"   },
    { "name": "lamp",           "collider": "none", "layer": "noObstacle", "occluder": "no"   },
    { "name": "sun_lamp",       "collider": "box",  "layer": "obstacle",   "occluder": "auto" },
    { "name": "Cedulka",        "collider": "none", "layer": "keep",       "occluder": "no"   }
  ]
}
```

| pole | hodnoty | význam |
|---|---|---|
| `name` | typový kľúč | presná zhoda, žiadne podreťazce |
| `collider` | `none` `mesh` `box` `sphere` | čo generuje GenerateColliders |
| `layer` | `interactable` `obstacle` `noObstacle` `nav` `keep` | `keep` = nechaj vrstvu tak |
| `occluder` | `auto` `yes` `no` | `auto` = kontrola materiálu a veľkosti (§5) |

**Static flagy a tag `Door` sa naďalej odvodzujú z vrstvy**, ako dnes — aby sa nemuseli
vypĺňať štyri polia tam, kde stačí jedno. Voliteľné polia `static` a `tag` ich vedia prebiť.

### Nerozhodnuté záznamy — kritické

Sken dopĺňa nové typy s `null` hodnotami, **nie s prázdnymi alebo defaultnými**:

```json
{ "name": "sun_lamp", "collider": null, "layer": null, "occluder": null }
```

Rozdiel medzi *„rozhodol som, že nič"* (`"collider": "none"`) a *„ešte som nerozhodol"*
(`null`) musí byť v dátach vidieť, a nástroj sa musí správať odlišne:

| stav | správanie |
|---|---|
| vyplnené | priradí vlastnosti |
| `null` | **objekt sa nedotkne a hlási sa pri každom spustení**, kým sa nevyplní |
| chýba v registri | objekt sa nedotkne a hlási sa |

Keby sa nové záznamy zakladali s defaultmi, tichá chyba by sa len presťahovala: objekt by
v registri **bol** (takže by z hlásenia zmizol), ale nedostal by nič. To je presne tá trieda
zlyhania, ktorú celý systém odstraňuje.

---

## 5. `occluder: auto`

Použije pravidlo zavedené v commite `3f7813b`: occluderom nie je nič, čo má materiál v queue
≥ 2450 (priehľadné), ani nič s plochou bounding boxu < 2 m². Dôvod je zdokumentovaný v
`docs/decisions/2026-08-04-occlusion-culling-occludery.md` — jeden `window` renderer nesie
nepriehľadný rám **aj** priehľadné sklo, takže meno o zakrývaní výhľadu nevypovedá.

---

## 6. Výnimky v mene objektu

`UNO` a `UYO` v mene **zostávajú v platnosti** a majú prednosť pred registrom. Sú to výnimky
pre jeden konkrétny kus, ktoré register (pracujúci na úrovni typu) vyjadriť nevie.

---

## 7. Hlásenie

Po každom spustení, na tri sekcie:

1. **Neznáme typy** — kľúč, počet, a **cesty k ukážkovým objektom** v hierarchii, aby sa dal
   nájsť a premenovať. Toto je hlavný výstup: čokoľvek tu je, je chyba v pomenovaní.
2. **Nevyplnené typy** — v registri sú, ale majú `null` polia.
3. **Nerozlúštené objekty** — kľúč v sebe stále nesie kód miestnosti, čiže chýba prefix.
4. **Mŕtve položky registra** — typy v JSON‑e, ktoré už nič nepoužíva.

Neznámy ani nevyplnený typ **nedostane žiadne vlastnosti** a objekt zostane nedotknutý.
Nikdy nedostane vlastnosti iného typu.

Hlásenie sa drží krátke a konkrétne — zoznam kľúčov s počtami a nanajvýš pár ukážkových ciest
na kľúč, nie výpis všetkých zásahov. Celý systém funguje len dovtedy, kým sa to hlásenie číta;
výpis na tisíc riadkov sa začne preskakovať a tým sa stráca jediná vec, kvôli ktorej vznikol.

---

## 8. Migrácia

1. Sken vygeneruje `ObjectPrefixes.json` a **návrh** `ObjectTypes.json` naplnený z dnešných
   keyword polí — tam, kde dnešné pravidlá matchujú, správanie zostane rovnaké.
2. Dry‑run porovná staré a nové rozhodnutie pre každý objekt a vypíše **rozdiely**. Tie sú
   dvojaké: opravené tienenia (napr. `window_1_glass`) a typy, ktoré treba doplniť.
3. Až po prejdení rozdielov sa keyword polia z kódu odstránia.

Bez kroku 2 sa nedá odlíšiť oprava od regresie.

---

## 9. Známe obmedzenia

- **Systém chyby v pomenovaní odhalí, nezabráni im.** Keď sa typ premenuje v Blenderi, všetky
  jeho inštancie spadnú naraz medzi neznáme. Je to správne, ale prvýkrát to vyzerá poplašne.
- **Fragmentácia kľúčov nezmizne úplne** ani po kroku s vodiacim číslom. Zvyšok sa objaví
  v hlásení a rieši sa buď doplnením prefixu, alebo premenovaním v zdroji.
- **Register nevie vyjadriť výnimku pre jeden kus.** Na to slúžia `UNO`/`UYO` v mene.

## 10. Mimo rozsah

- Premenovanie objektov v Blenderi. Systém chyby v pomenovaní **hlási**, neopravuje.
- Zmena toho, čo jednotlivé vrstvy a collidery znamenajú. Mení sa len zdroj rozhodnutia.
- `Objects/` verzus `FriBuilding` oddelenie prefixmi — dáta ukázali, že netreba: prefixy sa
  odvodzujú z hierarchie a obe vetvy fungujú rovnako.
