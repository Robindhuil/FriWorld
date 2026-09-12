# Deformačné osi sa sochajú ručne, jedna po druhej, a limit určuje pohľad

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-12

## Kontext

Prvý pokus o os bol generovaný vzorcom: `AMOUNT = TARGET / f_widest`. Čísla sedeli, nos
vyzeral ako nos prestal. Vrcholy blízko stredu doznievania sa rozšírili o 51 %, krídla
o 20 % — presne naopak než pri skutočne širšom nose. Rovnaký vzorec padol aj na dĺžke.

Druhý pokus, ten funkčný, bol opačný: oblasť sa vyreže z reálnej geometrie, kľúč sa
posunie o malý krok a **výsledok sa pozerá**. Takto vzniklo dvadsať osí na `face_1`.
Ten istý postup čaká telo, preto je tu zapísaný.

## Rozhodnutie

Jedna os = jedna vertex group + jeden shape key, v tomto poradí:

1. **Oblasť z geometrie, nie z čísel.** Váha je smoothstep cez tie súradnice, ktoré vec
   naozaj vymedzujú: vzdialenosť od bodu (brada), pásmo v `z` (lícna kosť), vzdialenosť
   dozadu k závesu čeľuste. Skupina sa uloží (`chin_shift`, `jaw_width`, `ear_size`…),
   takže sa dá zopakovať aj opraviť.
2. **Stredová os sa nesmie hnúť v `x`.** Pri každej šírkovej osi ide váha `x` cez rampu
   `0 → 6 mm`. Bez nej sa otvorí švík Mirroru.
3. **Kľúč vždy z `Basis`, nikdy `from_mix`.** Rozsah posuvníka `−3…3`; jeden kľúč nesie
   obidva smery, lebo `legacyClampBlendShapeWeights = 0`.
4. **Krok na jednotku je malý a rovnaký:** 3 mm pre posuny, 4 mm tam, kde to sedelo
   (nos, obočie), 8° pre vyklopenie ucha, 10 % pre veľkosť ucha.
5. **Limit určuje pohľad, nie miera.** Hodnota sa dvíha po `0.5`, po každom kroku render;
   hodnota, pri ktorej padne „toto je limit", je limit. Čísla sú kontrola, nie dôkaz.
6. **Po každej zmene kľúča `propagate_shape_keys.py`**, inak obočie, brada, pehy, oči
   a lebka zostanú stáť a odlepia sa.

Meno je `<časť>_<smer>` a kladná hodnota robí to, čo meno hovorí: `jaw_wide +1` je širšia
čeľusť. `lip_thin` je výnimka zdedená z toho, že sa pery len zužovali.

## Limity, ako boli schválené

Sú v jednotkách posuvníka; do `CharacterShapes.json` idú ×100 (`rangeMax = 150` pre `+1.5`).

| os | horný | spodný | čo to robí pri plnej hodnote |
|---|---|---|---|
| `nose_wide` | +1 | −0.5 | krídla 3.85 mm na stranu |
| `nose_long` | +1 | −1 | špička 5 mm dopredu |
| `nose_tall` | +1 | −1 | celý nos 4 mm hore |
| `brow_tall` | +1 | −1 | obočnicový pás 4 mm hore |
| `brow_long` | +0.5 | −0.5 | 4 mm dopredu na jednotku |
| `eye_tall` | +1 | −1 | jamky 3 mm hore |
| `eye_wide` | +1 | −1 | 3 mm na stranu, rozostup +6 mm |
| `eye_long` | +2 | −1 | 3 mm dopredu na jednotku |
| `mouth_tall` | +1 | −1 | ústa 3 mm hore |
| `mouth_wide` | +1 | −1 | kútiky 3 mm von |
| `mouth_long` | +2 | −1 | 3 mm dopredu na jednotku |
| `lip_thin` | — | — | zúženie pier v `z`; limity sa po vrátení zapečenia do `Basis` neschválili |
| `chin_tall` | +1 | −1 | brada 3 mm dole |
| `chin_long` | +1.5 | −1.5 | 3 mm dopredu na jednotku |
| `chin_wide` | +1 | −1 | šírka brady 63.6 → 67.5 / 59.7 mm |
| `jaw_wide` | +1.5 | −1.5 | šírka v uhle 121.8 → 130.8 / 112.8 mm |
| `jaw_tall` | +3 | −2 | uhol čeľuste 1627.8 → 1618.8 / 1633.8 mm |
| `cheek_wide` | +2 | −1 | šírka v lícnej kosti 120.0 → 132.0 / 114.0 mm |
| `ear_big` | +1 | −2 | výška ucha 54.3 → 59.3 / 44.3 mm |
| `ear_out` | +1.5 | −1 | vyklopenie +12° / −8° |

## Dôsledky

**Os, ktorá sa nedá vyrezať z jednej sekcie, je zlá os.** `jaw_long` (čeľusť dopredu)
padol práve na tom: hýbal celou spodnou tvárou vrátane pier a nevyzeral ako čeľusť.
Zahodil sa; skupina `jaw_shift` po ňom zostala nepoužitá.

**Register `CharacterShapes.json` je stále prázdny.** Limity žijú v tejto tabuľke, kým sa
nezapíšu. Kým tam nie sú, Unity o osiach nevie.

**Pred importom treba aplikovať Mirror na všetkých 21 objektov.** To je pasca zapísaná
v [blend shapy a modifiery](2026-09-11-blend-shapes-a-modifiery.md) a Blender ju nevie
spraviť sám — modifier sa na mesh so shape keys aplikovať nedá. Robí to
`tools/blender/apply_mirror_with_shape_keys.py`; na `face_1` bol overený proti výstupu
modifieru na 0.000 mm v pozíciách aj v kľúčoch. Spustený na ostrých dátach ešte nebol.

**Pri tele to bude to isté, len s inými sekciami.** Postup sa nemení: skupina zo
skutočnej geometrie, malý krok, render po každom kroku, limit od oka, potom propagate.
