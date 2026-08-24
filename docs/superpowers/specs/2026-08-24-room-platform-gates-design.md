# Dátovo riadené platformové gaty pre miestnosti

Stav: schválený 2026‑08‑24. Dátová vrstva a synchronizácia hotové (`7aa9bbd`); zostáva
migrácia náčrtu a dva appliery — viď `docs/superpowers/plans/2026-08-24-room-platform-gates.md`.

---

## 1. Problém

Priradenie `PlatformGate` a `ComponentGate` sa dnes robí ručne v hierarchii. Nikde nie je
zapísané, ktorá miestnosť má na webe zmiznúť — jediným záznamom je samotný komponent na
objekte. Keď o komponent prídeš, prišiel si aj o rozhodnutie.

A prišlo sa oň.

### Namerané

| | |
|---|---|
| `PlatformGate` v prefab assete `FriBuilding.prefab` | 144 |
| `ComponentGate` v prefab assete | **0** |
| objektov s komponentom `Door` | 283 |
| `ComponentGate` na inštancii v scéne | 1 (`ra008_door_1`) |
| pridaných komponentov ako override na inštancii scény | 2671 |

`PlatformGate`y prežili, lebo sedia v **prefab assete**. Dverné `ComponentGate`y boli
**override na inštancii v scéne** a zmizli — zostal jediný, ten pridaný naposledy.

Z toho plynie pravidlo, ktoré drží celý návrh pohromade:

> Nástroj zapisuje do prefab assetu cez `PrefabUtility.LoadPrefabContents`, nikdy na inštanciu
> v scéne.

Vedľajší nález mimo rozsahu tohto návrhu: tých 2671 override komponentov sú prevažne
`BoxCollider` a `NavMeshModifier` od `GenerateColliders`. Tá istá pasca, zatiaľ nevystrelila.

### Druhá chyba: dvere sa hľadajú podreťazcom

`DoorGateSetup` vyberá dvere cez `name.ToLowerInvariant().Contains("door")`. To je presne ten
substring matching, ktorý register typov nahradil.

| | |
|---|---|
| mien obsahujúcich `door` | 808 |
| z nich skutočných dverí (typ so `script: "Door"`) | 283 |

Zvyšok sú `door_frame`, `door_frame_<int>_glass` a `doorstep`, ktoré dnešný nástroj konfiguruje
tiež, hoci nemajú čo stripovať.

---

## 2. Princíp riešenia

Rozhodnutie „ktorá oblasť je len pre desktop" prestáva byť vlastnosťou scény a stáva sa
**dátami v repozitári**. Komponenty v prefabe sú od toho odvodený, kedykoľvek znovu
vygenerovateľný výstup.

Rovnaká disciplína ako register typov: **presná zhoda, nikdy podreťazec; nevyplnené znamená
nedotknúť sa a nahlásiť.**

---

## 3. Model hierarchie

```
FriBuilding                        ← jeden prefab asset
├── fri_building                   ← z .blend: steny, stropy, podlahy, okná, dvere
│   └── nav, outside, ra, rb, rc, terrace
├── RoomSignManager
├── NavMesh
└── Objects                        ← inštancie prefabov nábytku
    └── ra, rb, rc, terrace, outside
```

### Oblasť

**Oblasť** je kontajner s deťmi, ktorého meno **je** schválený prefix z `ObjectPrefixes.json`.
Presná zhoda, žiadne odstrihávanie.

Schválený zoznam je jediné, čo oddeľuje miestnosti od nábytkového šumu. Bez neho by sa medzi
oblasti dostali `ra001_lamp`, `chair_classroom_1_yellow (3)` a stovky ďalších.

### Namerané

| | |
|---|---|
| oblastí v `Objects` | 246 |
| oblastí vo `fri_building` | 301 |
| oblastí len v `Objects` | **0** |
| oblastí len vo `fri_building` | 55 |
| oblastí s vlastným `MeshRenderer` | **0** |

Dva dôsledky:

- Množina mien v `Objects` je **podmnožinou** tej vo `fri_building`, bez jedinej výnimky. Jeden
  riadok dát teda obslúži obe vetvy.
- Oblasti sú **čisté empty objekty**. `PlatformGate` na oblasti sa dotkne výhradne potomkov.

Tých 55 navyše sú miesta bez nábytku: výťahy, obvodové steny, strechy, schodiská,
`rc000_kitchen`, terasy, `rb_basement_room_1` až `_14`. Uplatní sa pri nich len dverná vetva.

### Hĺbka nie je jednotná

`Objects/rc` má oblasti hneď pod sebou, `Objects/ra` má medzi tým poschodie (`ra0`…`ra3`).
Nástroj preto **nesmie predpokladať fixnú úroveň** — prechádza strom a porovnáva mená.

### Dvere patria svojej oblasti

```
ra001/ra001_door_1                  rc001/rc001_door_1 … _3
ra102/ra102_door_1                  ra000_corridor_2/ra000_corridor_2_door_1 … _2
```

Žiadne dvere nesedia „medzi" dvomi oblasťami. Priradenie je jednoznačné.

---

## 4. Dáta

### `Assets/_Game/Editor/RoomPlatforms.json`

```json
{
  "rooms": [
    { "room": "ra100_corridor_2" },
    { "room": "ra100_corridor_1", "platform": "all" },
    { "room": "rb_basement_room_7", "platform": "desktopOnly" }
  ]
}
```

Konvencie kopírujú `ObjectTypes.json`, aby sa nebolo treba učiť druhý systém:

- **`platform` sa pri nerozhodnutom vynechá celé** (`NullValueHandling.Ignore`), rovnako ako
  `static`, `tag` a `script` v registri typov. Nikdy nevzniknú dva zápisy pre to isté.
- **Nerozhodnuté plávajú navrch súboru**, zvyšok abecedne. Ten istý komparátor ako
  `TypeRegistry.Save` — čerstvo pribudnutá oblasť je prvá vec v súbore, netreba ju hľadať.
- Vyhľadávanie **presnou zhodou**.

### Význam hodnôt

| hodnota | `Objects` | `fri_building` (dvere) |
|---|---|---|
| chýba | nedotkne sa, nahlási | nedotkne sa, nahlási |
| `"all"` | **odstráni** `PlatformGate`, ak tam je | **odstráni** `ComponentGate`, ak tam je |
| `"desktopOnly"` | `PlatformGate` s `target = DesktopOnly` | `ComponentGate` na dverách |
| `"webOnly"` | `PlatformGate` s `target = WebOnly` | **odstráni** `ComponentGate`, ak tam je |

Dverná vetva teda pozná jediné pravidlo, ktoré gate pridáva — `desktopOnly`. Každá iná
*rozhodnutá* hodnota znamená „tu dverný gate nepatrí". Nie je to zvláštny prípad pre `webOnly`,
ale ten istý zápis: rozhodnuté a nie `desktopOnly` → zabezpeč, že tam gate nie je.

Odstraňovanie je cielené. V `Objects` sa ruší `PlatformGate` na samotnom kontajneri oblasti,
nie hlbšie v podstrome. Vo `fri_building` sa ruší `ComponentGate` len na objektoch, ktorých
typový kľúč má `script: "Door"`. Gate, ktorý niekto pridal ručne inde, nástroj nahlási ako
sirotu a nechá tak — mazať cudzie nastavenie nie je jeho úloha.

Rozdiel medzi `all` a chýbajúcou hodnotou je to, čo robí JSON zdrojom pravdy. `all` znamená
„viem, že tu gate nepatrí, zmaž ho". Chýbajúca hodnota znamená „ešte som sa nerozhodol,
nechaj tak". Bez toho rozdielu by sa raz nasadený gate nedal cez dáta zrušiť.

### Prečo prefixy prestali strihať koncové číslo

Zberač prefixov pôvodne z mena kontajnera odstrihával koncové `_<int>`, takže
`ra000_corridor_1` až `_4` dali jeden riadok `ra000_corridor`. Na typový kľúč to vplyv nemá:

```
dieťa   ra000_corridor_2_lamp_1
prefix "ra000_corridor"     → 2_lamp_1 → StripLeadingInt → lamp_1 → lamp
prefix "ra000_corridor_2"   →   lamp_1 →      (nič)      → lamp_1 → lamp
```

Oba varianty dajú `lamp`. Strih teda nebol otázkou správnosti, len kompakcie — 15 riadkov
namiesto 53.

Pre platformové rozhodnutie je ale tá kompakcia strata. `ra100_corridor_1` a `ra100_corridor_2`
sú dve rôzne chodby a nedá sa jedna gatnúť a druhú nechať. Preto **zberač strih zrušil** a oba
súbory zdieľajú jeden kľúčový priestor:

| súbor | kľúč | úloha |
|---|---|---|
| `ObjectPrefixes.json` | `ra100_corridor_1` | krájať mená detí |
| `RoomPlatforms.json` | `ra100_corridor_1` | pomenovať fyzickú oblasť |

Cena je 325 prefixov namiesto 262 a to, že nová `ra000_corridor_5` sa musí najprv nahlásiť cez
`Add Prefixes From Selection`. To je jeden beh nástroja a hlásenie ju vypíše.

### Poistka, ktorá s tým musela prísť

Bez strihu zberač navrhne aj nábytkové kontajnery, a niektoré z nich by **zožrali hlavičku
registrovaného typu**: prefix `cubboard_1` premení `cubboard_1_part_1` na `part` a záznam
`cubboard_1_part` zostane mŕtvy.

Pôvodná poistka `riskyPrefixes` porovnávala na presnú zhodu s typovým kľúčom, takže z ôsmich
takých prípadov chytila dva. Pribudla druhá: zadrž prefix, ktorý je **tokenovým prefixom**
mena registrovaného typu. Ani jedna nič nemaže — obe zadržia a nahlásia, rozhodnutie je človeka.

Pri prvom behu zadržali `poster`, `radiator`, `chair_corridor`, `cubboard_1`, `table_cabinet_1`,
`table_pc_1`, `table_pc_half_1`, `trash` a `outside`.

Posledné meno stojí za zmienku: `outside` je schválený **už dávno** a je tokenovým prefixom typu
`outside_e_box`. To znamená, že `outside_e_box_1` sa dnes odvodzuje na `e_box` a záznam
`outside_e_box` je mŕtvy. Existujúca chyba, ktorú poistka len odhalila; opraviť ju znamená
premenovať objekty alebo zahodiť ten typ, čo do tohto návrhu nepatrí.

---

## 5. Synchronizácia

Jeden sken hierarchie, dva výstupy. `FriWorld > Registry > Add Prefixes From Selection`
po zápise prefixov zosúladí aj `RoomPlatforms.json`:

- oblasť v hierarchii, chýba v `RoomPlatforms.json` → **pribudne bez `platform`** a vyplynie
  navrch súboru
- oblasť v `RoomPlatforms.json`, ktorá už v hierarchii nie je → **ostane a nahlási sa ako
  sirota**. Zmazať rozhodnutie preto, že niekto dočasne premenoval kontajner, je jednosmerná
  strata.
- existujúce hodnoty sa **nikdy neprepíšu**

To isté je dostupné samostatne ako `FriWorld > Registry > Sync Room Platforms`, aby sa dalo
zosúladiť bez skenovania výberu.

---

## 6. Aplikácia

Dve vetvy, dva samostatne spustiteľné nástroje. Po reimporte `.blend` stačí dverná vetva a
`Objects` sa nemusí ani otvoriť.

```
FriWorld > Feature Flags > Report Room Gates
FriWorld > Feature Flags > Apply Object Gates
FriWorld > Feature Flags > Apply Door Gates
FriWorld > Feature Flags > Apply All Room Gates
```

Obe vetvy **zosúlaďujú, nedopisujú**. Prejdú prefab asset, pre každý kontajner odstrihnú
koncové `_<int>`, hľadajú presnú zhodu v `RoomPlatforms.json` a gate doplnia, opravia alebo
odstránia podľa tabuľky v §4.

Zápis ide cez `PrefabUtility.LoadPrefabContents` → úpravy → `PrefabUtility.SaveAsPrefabAsset`
→ `UnloadPrefabContents`. Izolovaná preview scéna navyše nedvíha modálne dialógy, takže to
prejde aj cez MCP. Otvorená inštancia v scéne prevezme zmenu sama, pokiaľ na tom istom
komponente nemá vlastný override — také prípady vypíše report ako nesúlad.

### Vetva `Objects` — `PlatformGate`

Na kontajner oblasti. `exclude` ostáva prázdny; dvere v tejto vetve nie sú.

### Vetva `fri_building` — `ComponentGate`

Pre každú oblasť s `desktopOnly` sa nájdu potomkovia, ktorých **typový kľúč z registra má
`script: "Door"`**. Nie podľa mena. Na každom z nich:

```
target                   = DesktopOnly
components               = Door + Animator + AudioSource, ak sú prítomné
changeLayerWhenStripped  = true
strippedLayer            = LayerMask.NameToLayer("Obstacle")
```

Vrstva sa rezolvuje menom a pri chýbajúcej vrstve to spadne nahlas — nie natvrdo `7` ako dnes.

Dôvod pre zmenu vrstvy namiesto trigger collidera: `PlayerInteract` používa
`QueryTriggerInteraction.Ignore`, takže prepnutie na trigger by rozbilo interakciu. Podrobne v
`docs/session-web-optimization-and-tooling.md`, §4.3.

`Door` dedí z `Interactable`, takže odstránenie `Door` je odstránenie interaktability. Dvere
ostanú viditeľné a neinteraktívne.

---

## 7. Report

`Report Room Gates` vypíše:

- **nerozhodnuté** oblasti — aby nedriftovali potichu
- **nesúlad JSON ↔ prefab** — čo hovoria dáta oproti tomu, čo je nasadené
- **siroty** — gate na kontajneri, ktorý nesedí na žiadnu oblasť
- **bez efektu** — oblasť s `desktopOnly`, ktorá nemá nábytok ani dvere. Dnes `outside_gazebo`.
  Nie je to chyba; keď tam raz niečo pribudne, zaberie samo.
- **vnorenie** — oblasť, ktorá obsahuje inú oblasť

Report nahrádza jednorazový import zo scény. Keď vie kedykoľvek ukázať rozdiel, netreba
migračný nástroj na jedno použitie.

### Vnorenie

Päť kontajnerov je zároveň prefixom aj rodičom ďalších oblastí: `rb`, `outside`, `terrace`,
`rb_basement`, `rc000_cafeteria`. `rb` je celá budova B.

Applier **odmietne gatnúť oblasť, ktorá obsahuje inú gatnutú oblasť** — vonkajší strip by
vnútorné rozhodnutie ticho zneplatnil. Všetkých päť je dnes `all`, takže konflikt nenastáva.

---

## 8. Migrácia

`Assets/_Game/Editor/Platforms.json` je ručný náčrt so 262 riadkami na úrovni prefixu:
158× `desktopOnly`, 104× `all`. Nie je to platný JSON a slúžil na zachytenie zámeru.

Prevod: každej z 301 oblastí sa priradí hodnota, ktorú má jej meno **po odstrihnutí koncového
`_<int>`** v náčrte. `ra100_corridor: all` sa rozvinie na všetky tri chodby,
`rb_basement_room: desktopOnly` na všetkých 14. Odtiaľ sa jednotlivé oblasti dajú doladiť, čo
pri prefixovej zrnitosti nešlo.

Ten strih žije len v migračnom skripte. Nikde inde už nie je potrebný, a skript sa po jednom
behu zmaže spolu s náčrtom.

Náčrt sa po prevode zmaže.

Náčrt sa zámerne rozchádza s tým, čo je v prefabe — je novší:

| | |
|---|---|
| zhoda náčrtu a prefabu | 134 |
| gate v prefabe, náčrt hovorí `all` | 7 |
| náčrt hovorí `desktopOnly`, gate chýba | 24 |

Náčrt vyhráva. Po prvom `Apply All Room Gates` bude prefab zodpovedať dátam.

---

## 9. Štruktúra kódu

Asmdef `FriWorld.ObjectRegistry.Editor` má `references: []`, takže nevidí `PlatformGate`,
`ComponentGate` ani `Door`. To určuje rez medzi čistým a Unity kódom.

```
Assets/_Game/Editor/ObjectRegistry/        ← asmdef: UnityEngine + UnityEditor, nie herné typy
  RoomPlatforms.cs         load / save / reconcile / Find(room)          ✔ hotové
  RoomGateScope.cs         otvorí prefab, nájde oblasti                  ✔ hotové
  RegistryScanner.cs       navrhuje prefixy bez strihu                   ✔ hotové
  ObjectRegistryMenu.cs    Sync Room Platforms + poistka na typy         ✔ hotové
  Tests/RoomPlatformsTests.cs                                            ✔ hotové

Assets/_Game/Editor/FeatureFlags/          ← Assembly-CSharp-Editor, vidí herné typy
  ObjectsPlatformGates.cs  Objects      → PlatformGate
  DoorComponentGates.cs    fri_building → ComponentGate
  RoomGateReport.cs
  RoomGateMenu.cs
```

`RoomGateScope` je jediné spoločné: otvorenie prefabu, prechod stromom, presná zhoda mena proti
schváleným prefixom. Patrí do asmdef, hoci sa dotýka Unity — vystačí si s `Transform` a
`PrefabUtility` a nepotrebuje `PlatformGate` ani `Door`. Vďaka tomu ho vie použiť aj
`ObjectRegistryMenu` pri synchronizácii, čo by cez hranicu assembly inak nešlo.

`StripTrailingInt` je dnes napísaný dvakrát — v `RegistryScanner.cs` a v `ObjectTypeKey.cs`.
Zjednotí sa na jedno miesto, keďže sa ho návrh aj tak dotýka.

`DoorGateSetup.cs` sa zmaže; `DoorComponentGates` ho nahrádza.

---

## 10. Zámerne neurobené

- **Zrnitosť pod úrovňou oblasti.** Ak raz bude treba gatnúť jednotlivý objekt vnútri
  miestnosti, čistá cesta je voliteľný override kľúčovaný celou cestou — nie zmena významu
  oblasti.
- **`PlatformGate` na kontajnery vo `fri_building`.** Pravidlo „táto vetva sa strihá len po
  dverách" platí plošne. Výnimka pre voľne stojace stavby by ho oslabila a raz by zmizla stena.
  `outside_gazebo` teda zostáva `desktopOnly` a zatiaľ nemá čo strhnúť.
- **Oprava override komponentov od `GenerateColliders`.** Tá istá trieda problému, ale iný
  nástroj a iný rozsah.
