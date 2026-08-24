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
- **To isté platí pre `GenerateColliders`, `GenerateLayersAndStatic` a `SetupInteractables`.**
  Mali presne tú istú chybu, len ešte nevystrelila: čítali `Selection` a písali tam, kde bol
  výber, čiže pri označení v scéne na inštanciu. Tých 2671 override komponentov bolo od nich.
  Teraz idú všetky tri cez `PrefabTarget`, ktorý otvorí prefab, spustí prechod a uloží.

  Výber už neriešia vôbec, takže ich menu položky nemajú validátor a sú dostupné vždy.
- **Undo v týchto nástrojoch skončilo.** Preview scéna prefabu ho nepodporuje, takže volania
  `Undo.AddComponent` a `Undo.RecordObject` boli nahradené priamymi. Vrátiť beh znamená
  `git checkout` prefabu — čo je aj tak spoľahlivejšie než undo cez 15 586 objektov.

## Čo neskúšať znova

Vysvetlenie „gate sa odstránil sám v `Awake`" vyzerá presvedčivo, lebo ten riadok tam naozaj
je. Rozhodlo až porovnanie počtov v assete oproti inštancii — bez neho by sa hľadalo v kóde
komponentu, kde chyba nie je.
