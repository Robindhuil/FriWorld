# Čo stoja vlasy a kde sa to dá ušetriť

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-12 · **Stav:** čiastočne — štyri jednoduché
účesy hotové, bohaté vlasy odložené (karta *Poriadne účesy* na boarde)

## Čo sa meralo

Prvý pokus o vlasy boli NURBS cesty s bevelom cez Bézier kruh. Merané na šestnástich
prameňoch toho istého účesu, vždy ten istý tvar, menili sa len dva parametre:

| ako | trojuholníky za 16 prameňov | na prameň |
|---|---|---|
| profil 48-uholník, `resolution_u` 12 (pôvodné) | 73 344 | 4 584 |
| šesťuholníkový profil, `resolution_u` 6 | 4 488 | 281 |
| šesťuholníkový profil, `resolution_u` 4 | 2 928 | **183** |
| plochá stuha (`extrude`), `resolution_u` 6 | 748 | 47 |
| plochá stuha, `resolution_u` 4 | 488 | 31 |

Pre porovnanie: celá postava má dnes ~7 500 trojuholníkov, samotná tvár 3 198.

Šesťuholník s `resolution_u` 4 je nasadený — na renderi som medzi ním a jemnejšími
nastaveniami nenašiel rozdiel, a je to 25× lacnejšie než pôvodný stav.

## Čo z toho plynie pre bohaté vlasy

**Plná hlava z prameňov je 7–9 tisíc trojuholníkov** (40–50 prameňov po 183), teda toľko ako
zvyšok postavy. Masa — sculpt alebo box-modeling, potom retopo na ~600 vertov — stojí
800–1 500 a s desiatimi prameňmi na rozbitie obrysu vyjde účes na **2 600–3 300**.

**Pamäť blend shapeov rastie rýchlejšie než trojuholníky.** Unity drží na kľúč a vrchol
pozíciu, normálu aj tangentu, teda 36 bajtov:

```
37 440 vertov × 20 kľúčov × 36 B = 27 MB     pôvodný stav, jeden účes
   780 vertov × 20 kľúčov × 36 B = 562 KB
   600 vertov ×  5 kľúčov × 36 B = 108 KB    po oboch úsporách
```

Účes hýbe päť až sedem osí z dvadsiatich (lícne kosti, čeľusť, uši). `propagate_shape_keys.py`
dnes zapisuje všetkých dvadsať, aj tie, ktoré cieľom nehýbu — preskočiť ich je ďalšia
štvrtina až tri štvrtiny tej pamäte a je to zmena na pár riadkov. Neurobené, lebo Report
zatiaľ považuje os na menej meshoch za varovanie.

## Čo neskúšať

**Alfa karty** na vlasy nie. Šetria trojuholníky, ale platí sa prekreslovaním, a web je
limitovaný práve výplňou. Stylizovaná plná geometria bez priehľadnosti je tu lacnejšia.

**Voxel remesh na tenkú škrupinu.** Pri voxeli 3 mm a škrupine tenšej než ~9 mm sa mesh
rozstrieľa na diery. Voxel musí byť ≤ ⅓ najtenšieho miesta, alebo sa najprv zhrubne cez
Solidify.
