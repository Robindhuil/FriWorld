# Detail tváre nesie shell s alfou, nie skin textúra

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-11

## Kontext

Obočie, strnisko a riedka brada sa geometriou spraviť nedajú. Riedka brada modelovaná ako
plocha je plná brada — hustota je práve to, čo ju odlišuje, a tá vzniká tým, že medzi
chĺpkami vidno kožu. Rovnako obočie: jeden objekt vo farbe vlasov číta ako veľmi husté
obočie a distinktívnosť sa stratí.

Prvá myšlienka bola namaľovať chĺpky priamo do textúry pleti. Tá cesta má dva problémy naraz:
pleť sa losuje z desiatich odtieňov, takže by každý pár (odtieň × obočie) potreboval vlastnú
textúru, a UV island nie je vymeniteľná jednotka — vymeniteľná jednotka je material slot
alebo objekt, lebo `CharacterBuilder` vie len zmazať nevylosovaný objekt a prepísať
`sharedMaterials`.

## Rozhodnutie

**Overlay shell + biela textúra, ktorú farbí colorway.**

Detail je vlastný objekt: kópia príslušnej sekcie tváre, odsadená 1.2–1.5 mm po normále,
s materiálom `char_brow_1` alebo `char_beard_1`. Pod ním zostáva holá koža s vlastným
colorwayom, takže kombinatorika nevzniká.

Dve veci to držia pohromade:

- **RGB textúry je celé biele, tvar a hustotu nesie iba alfa.** URP Lit počíta
  `albedo = _BaseMap × _BaseColor`, takže farbu dá colorway. `N textúr + M farieb` namiesto
  `N × M`. `ShadeMaterialGenerator` na to nepotreboval ani riadok zmeny — klonuje `_source`
  materiál aj s textúrou a prepisuje len `_BaseColor`.
- **Varianty sú tily jedného atlasu, nie samostatné textúry.** Materiál je jeden na triedu,
  takže textúra je tiež jedna; variant sa mení UV, nie obrázkom. `brow_1` a `brow_2` sú
  rovnaká geometria s UV na inom tile. Pridať variant = tile v atlase + shell + riadok v JSON.

`Alpha Blend`, nie `Alpha Clip`. Na riedkych chĺpkoch clip zlyháva presne tam, kde ho
potrebuješ — tenký chlp stratí v mipoch alfu a na pár metrov zmizne. Sortovanie nie je
problém, ktorým býva: shell je tenká plocha nad opaknou kožou, sama sa neprekrýva a
s ostatnými shellmi sa nestretne.

## Dôsledky

**Preset musí mať renderer.** `CharacterScan` indexuje len objekty s `Renderer`om, takže
„bez brady" sa nedá spraviť prázdnym objektom ani vynechaním presetu — systém vyberá práve
jeden preset na slot triedu. `beard_none_1` je preto mesh s jedným trojuholníkom nulovej
plochy vo vnútri lebky. Rovnaký trik bude potrebovať každá ďalšia voliteľná trieda
(okuliare, batoh).

**Shell je snímka, nie odkaz.** Zdeformovanie sekcie pod ním s ním nepohne. Pri modelovaní
na to slúži `Surface Deform` — pridať, aplikovať, zmazať. Nechať ho tam sa nedá, shelly majú
vlastný `Armature` a deformácia by prišla dvakrát.

**Farba obočia a brady sa losuje nezávisle**, korelácia medzi slotmi v systéme nie je.
Paleta preto musí byť naplnená tak, aby každá kombinácia obstála. Svetlohnedé riedke obočie
na opálenej pleti je takmer neviditeľné — nie je to chyba, je to voľba palety.

**Atlas má dnes 2×2 tily, teda štyri sloty na triedu.** Obočie je pomer zhruba 4:1, takže
v štvorcovom tile leží ladom ~65 % plochy. Pri viac než štyroch variantoch treba prejsť na
pásy 256×128 — a UV každého existujúceho shellu je do terajšieho layoutu zapečené, takže
prerobiť to stojí toľko premapovaní, koľko je shellov.
