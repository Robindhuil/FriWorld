# Varianty hlavy sú presety, nie shape keys

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-09

## Kontext

Mužský model má vychádzať z jednej nafotenej skutočnej tváre a z nej majú vzniknúť variácie.
Systém postáv pozná dnes dva mechanizmy a hlava sa dá postaviť na ktorýkoľvek z nich:

- **Preset slot** — v `CharacterClasses.json` je `slotClasses`, pre každý slot sa vylosuje
  jeden preset a `CharacterBuilder` ostatné zmaže. Takto fungujú vlasy, tričko, nohavice
  aj topánky.
- **Shape keys s losovanými váhami** — plynulá deformácia jedného meshu.

Shape keys dávajú nekonečno variácií, ale `CharacterAppearance` je dnes `byte[] preset`,
`byte[] colorway` a `byte height` — pole plynulých hodnôt v ňom nie je. Doplniť ho znamená
zmeniť štruktúru, baker aj randomizér, a blendshapes stoja pamäť a čas na každom rendereri;
pri dvadsiatich NPC vo web builde to nie je zadarmo.

## Rozhodnutie

**Varianty celej hlavy ako ďalší preset slot.** Žiadne shape keys.

Podmienka, ktorá to celé drží: **línia vlasov je na všetkých variantoch na tom istom mieste
v priestore**, nielen s rovnakou topológiou. To isté platí pre očné jamky, ústa, nos a uši —
tie sedia na každom variante rovnako. Bez toho by každý pár hlava × vlasy potreboval vlastnú
verziu vlasov a počet kombinácií by sa vynásobil namiesto sčítal.

`head` sa do `slotClasses` pridáva **na koniec**, nie na začiatok. `CharacterAppearance.preset`
je indexované poradím slotu, takže vloženie na začiatok by posunulo indexy a každý existujúci
seed by vyzeral inak. Tá istá úvaha už raz padla pri výške — losuje sa posledná z rovnakého
dôvodu.

## Dôsledky

- Variácií je toľko, koľko sa ich namodeluje. Prijaté vedome; keď to bude málo, shape keys sa
  dajú pridať neskôr ako druhá vrstva nad presetmi, nie namiesto nich.
- **Hlavy musia prestať byť sekciou tela.** Dnes sú v `npc.blend` ako `male_body_head`,
  `male_body_head.001` a `male_body_head.004`. Sekcia tela a preset sú dve rôzne veci:
  sekcia sa maže, keď ju preset zakrýva, preset sa maže, keď nie je vylosovaný. Kým sú hlavy
  pomenované ako telo, vykreslia sa všetky tri naraz. Premenovať na `head_1`, `head_2`, … tak,
  ako sa volajú `hair_1` alebo `shirt_2` — a to platí aj pre pôvodnú `male_body_head`.
- Farbu hlavy netreba riešiť zvlášť. Pleť nesú materiály `char_skin_*` a tie už farebná trieda
  `skin` pokrýva; `head` je preset slot, nie farebná trieda.
- `hides` zostáva na hlavách prázdne — hlava nezakrýva žiadnu sekciu tela.
- Keď všetky tváre vyjdú z jednej skutočnej, NPC budú pôsobiť ako príbuzní, kým nebudú rozdiely
  medzi variantmi výrazné. Pri jednom zdroji je to väčšie riziko než pri viacerých.
