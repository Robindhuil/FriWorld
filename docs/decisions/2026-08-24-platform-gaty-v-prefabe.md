# Platformové gaty patria do prefab assetu

## Kontext

Dverné `ComponentGate`y z budovy zmizli. Podozrenie padlo na samotný FF systém — že si gate
odstraňuje sám, keďže `ComponentGate.Awake` naozaj končí `Destroy(this)`.

Meranie ukázalo niečo iné:

| | |
|---|---|
| `PlatformGate` v `FriBuilding.prefab` | 144 |
| `ComponentGate` v tom istom assete | **0** |
| objektov s komponentom `Door` | 283 |
| `ComponentGate` na inštancii v scéne | 1 |
| komponentov pridaných ako override na inštancii | 2671 |

`PlatformGate`y prežili, lebo ich niekto pridal do **prefab assetu**. Dverné gaty boli pridané
na **inštanciu v scéne**, kde sú to prefab overrides. Reimport `.blend` vymenil podstrom
`fri_building` aj s tým, čo na ňom viselo. Zostal jediný gate — ten pridaný po poslednom
reimporte.

`Awake` s tým nemal nič spoločné. Zmeny z play mode sa aj tak vracajú.

## Rozhodnutie

Nástroje, ktoré nasadzujú gaty, zapisujú do prefab assetu cez
`PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset`, nikdy na inštanciu v scéne.

Samotné rozhodnutie „ktorá miestnosť je len pre desktop" sa presunulo do
`Assets/_Game/Editor/RoomPlatforms.json`. Komponenty sú odvodený výstup, kedykoľvek znovu
vygenerovateľný z `FriWorld > Feature Flags > Room Gates`.

## Dôsledky

- Aj keby sa gaty znova stratili, dajú sa vrátiť jedným behom nástroja. Strata komponentu
  prestala byť stratou informácie.
- Ručná úprava gatu priamo v hierarchii sa pri najbližšom behu prepíše. Zmena patrí do JSON‑u.
- **`GenerateColliders`, `GenerateLayersAndStatic` a `SetupInteractables` majú ten istý problém
  a zatiaľ nevystrelil.** Čítajú `Selection` a píšu tam, kde je výber — pri označení v scéne
  teda na inštanciu. Tých 2671 override komponentov sú prevažne `BoxCollider`
  a `NavMeshModifier` od nich. Prvý reimport `.blend` ich zmetie rovnako ako dverné gaty.

  Kým sa to nespraví poriadne, obchádzka je označiť koreň v **Prefab Mode**, nie v scéne —
  výber potom ukazuje na obsah prefabu a komponenty pristanú v assete.

  Poriadne riešenie znamená dať tým trom nástrojom koreň parametrom namiesto `Selection`,
  aby ich vedel obslúžiť rovnaký kód, čo otvára prefab pre Room Gates. Neurobilo sa to teraz,
  lebo to je refaktor troch veľkých súborov a nesúvisí s tým, prečo zmizli dvere.

## Čo neskúšať znova

Vysvetlenie „gate sa odstránil sám v `Awake`" vyzerá presvedčivo, lebo ten riadok tam naozaj
je. Rozhodlo až porovnanie počtov v assete oproti inštancii — bez neho by sa hľadalo v kóde
komponentu, kde chyba nie je.
