# Slnko svietilo cez steny: `shadowStrength` nie je „sila tieňa"

## Kontext

V interiéri bola vždy presne **jedna svetová strana stien** citeľne svetlejšia než ostatné tri —
aj v miestnosti bez okna, kam sa slnko nemá ako dostať. Podozrenie padalo na `FillLight`.
Neprávom: tá klietka je šesť smerových svetiel `±X ±Y ±Z` s rovnakou intenzitou, teda
azimutálne symetrická. Rovnako nevinné je aj ambient — režim `Trilight` má nenulový len
zvislý koeficient SH, vodorovne je konštantný.

Zmerané z bodu v miestnosti bez okna (`-24, 15.7, 40`), stredný jas štyroch pohľadov:

| | +Z | −Z | +X | −X | max/min |
|---|---|---|---|---|---|
| pôvodný stav | **0.2047** | 0.0652 | 0.0671 | 0.0755 | **3.14×** |
| `shadowStrength` 1.0 | 0.0662 | 0.0652 | 0.0667 | 0.0654 | **1.02×** |
| slnko úplne vypnuté | 0.0662 | 0.0652 | 0.0667 | 0.0654 | 1.02× |

Pri `shadowStrength = 1` je výsledok **identický s vypnutým slnkom**. Celý rozdiel teda robilo
slnko presvitajúce stenou — a svietilo presne na tú stenu, ktorej normála mieri proti nemu.

Príčinou je `Light.shadowStrength = 0.7` na smerovom svetle. To nie je „mäkkosť" ani „tmavosť"
tieňa; je to **podiel svetla, ktorý sa v tieni zrazí**. Pri 0.7 prejde každým tieňom v scéne
30 % priameho slnka — vrátane tieňa, ktorým je celá obvodová stena budovy.

Dve veci to maskovali:

- Slnko je **Mixed** a `MixedBakeMode` je **Shadowmask**, takže statická geometria berie
  zatienenie z upečenej shadowmask textúry, nie z realtime shadow mapy. `shadowStrength`
  škáluje aj ju. Preto sa jav správal rovnako pri kvalite `Web` (kde sú tiene úplne vypnuté)
  aj pri `Vysoké` — v oboch prípadoch presvitalo tých istých 30 %.
- Editor bežal na quality leveli **`Web`** (`RP_Web`, `shadowDistance 0`, žiadne tiene), hoci
  build target bol `StandaloneWindows64`. Čokoľvek, čo sa v editore pozeralo, bolo renderované
  webovou cestou.

## Rozhodnutie

`shadowStrength` slnka je **1.0**. Ak treba interiér zosvetliť, patrí to do svetiel a do peku
GI, nie do presvitania tieňov — to zosvetlí len tie plochy, ktoré náhodou mieria na slnko,
a tvar budovy pritom úplne ignoruje.

`RP_Low` mal `shadowDistance 0`, teda „Nízke" na desktope nemalo tiene vôbec a chyba sa tam
vracala v plnej sile. Nastavené na 40. Nula zostáva len v `RP_Web`, kde je zámerná.

## Dôsledky

- Miestnosť bez okna je teraz naozaj tmavá (jas ~0.065). To je správne, ale odhaľuje, že
  **v budove nie je ani jedno bodové či kužeľové svetlo** — `InteriorCeilingLight.prefab`
  existuje a v scéne nemá jedinú inštanciu. Kým sa nerozvesí, tmavé miestnosti tmavé zostanú.
- Naprieč 17 uzavretými bodmi klesol priemerný pomer jasu stien z 3.04× na 1.89×. Zvyšok je
  denné svetlo cez sklo, teda korektné.
- `shadowStrength < 1` sa už nepoužíva na ladenie jasu. Ak sa niekedy objaví, je to táto chyba.
