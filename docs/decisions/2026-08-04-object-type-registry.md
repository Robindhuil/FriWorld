# Collidery a vrstvy riadi register typov, nie kľúčové slová v kóde

**Verzia:** 0.1.0-alpha · **Dátum:** 2026-08-04

## Kontext

`GenerateColliders` a `GenerateLayersAndStatic` rozhodovali podľa ručne udržiavaných polí
kľúčových slov priamo v kóde. Matching hľadal **podsekvenciu tokenov** v mene objektu, z čoho
plynula chyba, ktorá sa nedala opraviť doplnením ďalšieho slova:

> Pridám objekt `lamp_sun`, ktorý sa má správať inak než `lamp`. Meno obsahuje `lamp`, takže
> dostane vlastnosti lampy. **Nikto mi to nepovie.**

Pravidlo nikdy nepovedalo „toto meno nepoznám" — vždy našlo nejakú odpoveď. Nebolo to
hypotetické: rovnaký mechanizmus postihoval `window_1_glass` (658 objektov), kde `window`
matchlo skôr a sklo dostávalo vlastnosti okna.

Namerané pred zásahom: 3484 rôznych mien nesúcich geometriu, z toho **314 nematchovalo nič**
a ticho prepadávalo — vrátane blenderovských zvyškov `Plane.005`, `shead_1.066`, `usta.015`.

## Rozhodnutie

Prechod od **„hádaj podľa podobnosti"** k **„vieš alebo nevieš"**.

Meno sa zredukuje na **typový kľúč**, ten sa vyhľadá v registri. Známy kľúč dostane svoje
vlastnosti; **neznámy alebo nevyplnený sa nedotkne ničoho a nahlási sa.**

### Odvodenie kľúča — štyri kroky

1. odstrihni najdlhší sediaci prefix zo `ObjectPrefixes.json`
2. odstrihni vodiace `<int>_`
3. odstrihni značky `UNO` / `UYO`
4. odstrihni koncové `_<int>`

```
ra100_corridor_1_door_frame_2  →  door_frame
rc000_outside_wall_28_UNO      →  outer_wall
```

**Stráž, bez ktorej sa to rozsype:** odstránenie prefixu musí nechať za sebou slovo. Inak sa
`lamp_2` strafí na prefix `lamp`, zostane `2`, a ten jeden kľúč zhltne 1550 objektov.

### Vzory

Meno typu smie obsahovať `<int>`, ktorý zastupuje **práve jeden token číslic**:
`window_<int>_glass` pokryje `window_1_glass` až `window_7_glass` a chytí aj `window_8_glass`,
keď pribudne. Bez toho vyrábalo každé nové poradové číslo ďalší riadok na údržbu.

- **Presná zhoda vyhráva nad vzorom** — jedna inštancia sa dá kedykoľvek vyňať.
- **Dva rovnako špecifické vzory** na jeden kľúč sa nahlásia ako nejednoznačné.
- Zástupný znak je **zámerne úzky**: nesedí na `window_glass`, `window_1_2_glass` ani
  `window_a_glass`. Voľný vzor by ticho pohltil veci, ktoré doňho nepatria.

### `null` znamená jedinú vec

`collider`, `layer` a `occluder` sú povinné; `null` = *nerozhodnuté*, objekt sa nedotkne
a hlási sa pri každom behu. Voliteľné `static` a `tag` sa do súboru **nezapisujú, kým nie sú
nastavené** — inak by `null` znamenal raz „nerozhodol som" a raz „odvoď to", čo sa v súbore
nedalo rozlíšiť.

Nerozhodnuté typy sa radia **na začiatok súboru**, takže prvý riadok odpovedá na otázku „mám
niečo nevybavené?" bez spúšťania hlásenia.

## Dôsledky

- **Prefixy len navrhuje sken, nikdy ich sám nepoužije na odvodenie.** Prefix, ktorý je zároveň
  typovým kľúčom, by zožral časť viacslovného typu (`wall` by pohltil `wall_edge`) a žiadna
  automatická stráž to nechytí. Takéto návrhy sa zadržia a označia `!`.
- **Kontajner sa nesmie volať ako typ.** Preto sa `ra000_outer_wall` premenoval na
  `ra000_outside_wall` a strechy podobne — inak sa ten prefix nedá schváliť.
- **Oba nástroje pracujú len s objektmi, čo nesú geometriu.** Predtým prechádzali všetky
  transformy, takže prázdny zoskupovací objekt s `wall` v mene dostal MeshCollider bez meshu.
- **`UNO` / `UYO` v mene majú prednosť pred registrom** — sú to výnimky pre jeden kus, ktoré
  register na úrovni typu vyjadriť nevie. Z typového kľúča sa ale odstrihnú, inak by
  `wall_3_UNO` bol samostatný typ.
- **Po zmene modelu treba sken zopakovať.** Zmena v `.blend` mení kľúče; hlásenie povie, čo je
  nové, čo zmizlo a čo je chyba v pomenovaní.

## Slepé uličky — neskúšať znova

- **Dry-run diff tienenie neodhalí.** Seedovanie aj diff púšťajú tie isté staré pravidlá, takže
  na kľúči `window_1_glass` aj na celom mene dajú rovnakú odpoveď. Nula rozdielov znamenala
  „nič sa nezhoršilo", nie „všetko je správne" — register staré rozhodnutia **zdedil**.
- **Odstrihávať vnútorné číslo natvrdo** namiesto vzorov. Zahodilo by to informáciu natrvalo:
  okno č. 3 by sa už nikdy nedalo vyňať a nastaviť zvlášť.

## Nameraný výsledok

| | pred | po |
|---|---|---|
| rôznych mien / typov | 3484 | **94** |
| ticho prepadajúcich | 314 | **0** |
| prefixov | — | 262 |
| objektov s rozhodnutým typom | — | **5475 / 5475** |

Prvý ostrý beh na `ra100_corridor_1` (22 objektov bez colliderov): 12 mesh, 8 box, 2 bez
collidera, 0 nedotknutých — a collider, vrstva aj static flagy sedia s registrom u všetkých.
