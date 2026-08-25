# Čo ešte zvýši odrazené svetlo

**Verzia:** 0.1.1-alpha · **Stav:** nespravené, návrh

## Zistenie

Odrazené svetlo je násobič svetla, ktoré už existuje. Kde zdroj nie je, nemá čo násobiť.
Merané v 3650 bodoch probového poľa po peku:

| percentil | upečené svetlo |
|---|---|
| 5 % | 0.0005 |
| 25 % | 0.0353 |
| 50 % | 0.4189 |
| 75 % | 0.9964 |

| prah | podiel priestoru |
|---|---|
| pod 0.02 | **20 %** |
| pod 0.05 | 29 % |
| pod 0.10 | **36 %** |

Tretina budovy je prakticky čierna. Žiadna hodnota `albedoBoost` ani `indirectScale` to
nespraví — v tých miestnostiach nie je okno ani svietidlo.

## Čo pri fixných farbách zostáva

**Materiály sú vyčerpané.** `_BaseColor` *je* difúzne albedo, teda priamo podiel odrazeného
svetla. Keď sa farby nemenia a `_Metallic` je nula na všetkých 79 materiáloch, cez materiály
sa viac odrazu nezíska. Smoothness lightmapper nečíta vôbec, `_Metallic` bounce iba uberá.

### 1. Emisia na svietidlách — jediná páka, čo pridá svetlo

Svietidlá **už sú namodelované a rozvešané**:

```
lamp       n=1310   plocha 468 m2   mt_lamp_1 x1293, mt_lamp_2 x17
lamp_sun   n=5
```

Nie je to modelárska robota, je to jeden materiál. `mt_lamp_1` má dnes
`emisia = RGBA(0,0,0,0)` a `giFlags = EmissiveIsBlack`. Zapnúť emisiu a prehodiť GI flag na
Baked spraví z 1310 svietidiel skutočné zdroje v peku, bez runtime nákladu.

**Háčik:** `mt_lamp_1` aj `mt_lamp_2` sú vnorené v `interior_objects.blend`. Vnorený materiál
reimport `.blend` prepíše, takže sa musia najprv vyextrahovať do `_Game/Art/Materials/`
a premapovať — inak to prvý reimport zmetie. Tá istá pasca ako pri dverných gatoch.

### 2. Rozlíšenie lightmapy

`lightmapMaxSize` je 1024 pri 41 atlasoch, takže požadovaných 24 texelov na jednotku sa
reálne nedosiahne. 2048 by to pustilo. Energiu to nepridá — bounce len prestane byť škvrnitý.

### 3. Stropy sú plochy bez hrúbky

275 z 293 stropov má najtenší rozmer bounds pod 2 cm a `doubleSidedGI` je vypnuté na všetkých
79 materiáloch. Rub sa zapečie čierny. Pri správnych normálach to nikto nevidí; zapnutie
`doubleSidedGI` na `mt_ceiling_1` je lacná poistka proti otočenej normále, ktorá by miestnosti
zapiekla čierny strop.

## Čo sa overilo ako slepá ulička

- **Smoothness podľa látky — skúšané a vrátené.** Do peku nevstupuje vôbec; meranie
  s nezmenenou lightmapou dalo miestnosť 0.4117 → 0.4317 a chodbu 0.4520 → 0.4628, a tie
  percentá sú runtime odraz prostredia z reflection probov, nie GI.

  Vizuálne to bolo horšie. Materiály sú **plné farby bez textúr a bez normálových máp**, takže
  jednoliata plocha dostane rovnomerný lesk po celej ploche — drevo pri 0.22 nevyzerá lakované,
  vyzerá ako plast. V realite tú variáciu dodá textúra; tu ju nemá čo dodať. Hodnoty sú späť
  na nule (listy 0.007).

  **Kým sú materiály plné farby, smoothness nedvíhaj.** Má zmysel až s textúrami alebo aspoň
  s normálovou mapou.
- **Intenzita slnka** — v exteriéri robí ~10 % jasu. Úplné vypnutie slnka zhodilo zem len
  z 0.575 na 0.519.
- **Bloom** — po ACES tonemappingu nie je prepálený ani jeden pixel.
