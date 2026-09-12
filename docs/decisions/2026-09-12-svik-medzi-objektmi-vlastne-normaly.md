# Švík medzi dvoma objektmi sa zatvára vlastnými normálami, nie modifierom

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-12

## Kontext

`cranium_1` je samostatný objekt, lebo je to preset — lebka sa má dať vymeniť bez toho, aby
sa prerábala tvár. Delí s tvárou 39 vrcholov na vlasovej línii, ktoré ležia presne na sebe
(0.000 mm). Napriek tomu bolo miesto stretu vidieť: lebka bola celá flat shaded a aj po
zapnutí smooth ostal na švíku zlom, lebo každý objekt si normály počíta zo svojich plôch.

Zvyčajné riešenie je **Data Transfer** modifier s prenosom vlastných normál. Tu sa použiť
nedá: cieľ nesie dvadsať shape keys a Blender modifier na taký mesh aplikovať nedovolí,
takže by modifier zostal a pri exporte by mesh prepísal — a kľúče padli, presne ako
v [blend shapy a modifiery](2026-09-11-blend-shapes-a-modifiery.md).

## Rozhodnutie

Normály sa spočítajú a zapíšu priamo do meshu, cez `normals_split_custom_set()`:

- na 39 spoločných vrcholoch je normála **znormalizovaný súčet** normály tváre a normály
  lebky — to, čo by vyšlo, keby to bola jedna sieť,
- obidva objekty dostanú v tom bode **tú istú** normálu; keby ju dostala len lebka, tvár by
  si držala svoju a švík by ostal vidieť,
- zvyšok oboch sietí si necháva vlastné hladké normály.

Kontrola je jednočíselná: uhol medzi normálami na spoločných vrcholoch, `max 0.0000°`.

## Dôsledky

**Unity musí mať na tomto modeli `Normals: Import`.** S `Calculate` si ich prepočíta zo
svojich plôch a švík sa vráti.

**Normály sú statické.** Pri deformácii sa neprepočítajú — pri posunoch do 5 mm to nevidno,
ale po zmene topológie treba prepočet zopakovať.

**Je to tretia cesta okolo toho istého pravidla.** Mesh s blend shapes znesie iba
`ARMATURE`, takže všetko ostatné — hladké tieňovanie, prenos normál, zrkadlenie — musí
skončiť zapísané v dátach meshu, nie v modifieri.
