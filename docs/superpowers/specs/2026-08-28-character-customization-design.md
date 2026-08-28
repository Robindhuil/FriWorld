# Character customization — návrh

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-08-28 · **Stav:** schválený návrh, nezačaté

Jedna postava skladaná z presetov a farieb namiesto samostatného modelu na každé NPC.
Ten istý systém neskôr obslúži aj tvorbu postavy hráča.

Dokument je písaný tak, aby sa podľa neho dalo ísť v inej session bez tohto kontextu.
Implementačný plán je v [`docs/superpowers/plans/`](../plans/).

---

## 1. Kontext

Dnes je každé NPC vlastný model. V `Assets/3Dmodels/Npc/` leží 30 × `student_NUn.fbx`
a k tomu sedem menovaných (`duracikUn`, `gregorovaUn`, `janechUn`, `kvetUn`, `meskoUn`,
`petrikovaUn`, `tothUn`). Prefaby v `Assets/_Game/Prefabs/npc/` a `npcSpawn/` sú ich
varianty. Každá nová podoba študenta znamená nový export z Blenderu, nový import
a nový prefab.

Cieľ je opačný: dve základné telá, sada presetov na triedu a paleta farieb. Z toho sa dá
vyskladať rádovo viac podôb než 30 a pribudnutie novej mikiny je jeden objekt v Blenderi
plus jeden riadok v JSON.

**Poradie prác:** najprv NPC vrstva (tento dokument), potom tvorba postavy hráčom
vrátane UI, nakoniec telo a ruky z prvej osoby. Staré `student_*.fbx` zostávajú nedotknuté,
kým generátor nebeží; potom idú preč. Menovaní učitelia zostávajú ručné modely — je ich
málo a musia vyzerať presne tak, ako vyzerajú.

---

## 2. Slovník

Systém pracuje s dvoma zoznamami tried, ktoré sa prekrývajú, ale nie sú to isté.
Zamieňať ich je najľahšia chyba v celom návrhu.

| pojem | čo to je | hodnoty |
|---|---|---|
| **slot trieda** | čo sa vymieňa ako mesh preset | `head`, `hair`, `beard`, `torso`, `legs`, `feet` |
| **farebná trieda** | čo sa dá prefarbiť | `torso`, `legs`, `feet`, `hair`, `skin`, `eye`, `lips`, `beard` |
| **preset** | jedna konkrétna varianta slot triedy | `torso_hoodie_1`, `hair_short_2` |
| **colorway** | pomenovaná sada farieb pre farebnú triedu | `navy`, `rust` |
| **sekcia tela** | kus nahej kože, ktorý sa dá skryť | `chest`, `forearm_L`, … |

`skin`, `eye` a `lips` sa nikdy nevymieňajú ako mesh — patria telu a hlave a len sa farbia.
Preto nemôžu byť v jednom zozname so slot triedami.

### Sekcie tela

Šestnásť pevných sekcií nahého tela. Kľúč sekcie:

```
neck   chest   abdomen   hips
upperarm_L  upperarm_R   forearm_L  forearm_R   hand_L  hand_R
thigh_L     thigh_R      calf_L     calf_R      foot_L  foot_R
```

Každá sekcia je vlastný objekt so `SkinnedMeshRenderer`. V Blenderi aj v Unity sa volá
**`<pohlavie>_body_<kľúč>`** — `male_body_upperarm_L`, `female_body_upperarm_L`. Meno je
na oboch stranách to isté; kľúč je len to, čo z neho zostane po odstrihnutí prefixu.

Prefix v Blenderi treba, aby sa obe telá dali držať v jednom súbore bez kolízie mien.
Kľúč ho mať nesmie, aby maska `hides` platila pre obe telá bez toho, aby sa
`CharacterPresets.json` písal dvakrát. Je to ten istý postup, akým `ObjectTypeKey` robí
typový kľúč z mena objektu v budove: **meno je to, čo vidí autor, kľúč je to, čo hľadá
register.**

Strana je veľkým písmenom, `_L` a `_R`, a porovnáva sa ordinálne. `_l` sa nezhoduje —
dva objekty by inak mohli sadnúť na tú istú sekciu a druhý by ticho vyhral.

Šestnásť kľúčov sa vojde do jedného `int` ako bitmask.

**`head` medzi sekciami nie je**, hoci dnes v prefabe `male_body_head` existuje. Tvar
hlavy je slot trieda, takže hlava má prísť z presetu a niesť sloty `char_skin_1`,
`char_eye_1`, `char_lips_1`; `hides` ju preto nikdy neobsahuje. Kým presety na hlavu
nie sú, `male_body_head` je dočasná pevná hlava. **Keď pribudnú, presunie sa z
`male_skeleton` pod `Face/Head` a premenuje na `head_<názov>_<číslo>`** — inak by
existovala pevná hlava aj hlava z presetu naraz. Je to jediná štrukturálna zmena, ktorú
si pridanie tejto triedy vyžiada; všetko ostatné je riadok v JSON.

---

## 3. Pomenovanie materiálov

Meno materiálu na meshi **nie je farba, ktorú hráč uvidí** — je to deklarácia slotu.
V Blenderi sa farby neriešia vôbec, stačí dôsledné pomenovanie.

```
char_<farebná trieda>_<kľúč>

kľúč   1, 2, 3…      základné farby triedy
       11, 21, 31…   tmavší odtieň k farbe 1, 2, 3
```

Príklad na jednom presete mikiny:

| slot | význam |
|---|---|
| `char_torso_1` | hlavná farba mikiny |
| `char_torso_2` | sekundárna farba (lem, kapuca) |
| `char_torso_11` | tmavší odtieň k hlavnej |
| `char_leather_1` | **nedotkne sa** — `leather` nie je farebná trieda |

Kľúč sa hľadá **presnou zhodou**, nikdy podreťazcom. Neznámy keyword sa nedotkne
a nahlási sa — rovnaká disciplína ako `ObjectTypes.json`. Preklep `char_torzo_1` teda
neskončí ako náhodne prefarbený kus, ale ako hlásenie v Reporte.

Parsovanie a príslušnosť k triede sú **dve oddelené veci** a v kóde sa nemajú miešať:
`char_leather_1` je syntakticky v poriadku a rozparsuje sa na `(leather, 1, základná)`.
Až katalóg povie, že `leather` nie je farebná trieda, a slot preto nechá tak.
`char_torso_1x` sa nerozparsuje vôbec — to je chyba mena.

**Limit:** maximálne 9 základných farieb na triedu. `char_torso_10` sa nedá odlíšiť
od tmavšieho odtieňa k farbe 1. Deväť slotov na triedu je aj tak veľa, ale nech to
nie je prekvapenie neskôr.

### Odkiaľ berie tmavší odtieň vzhľad

Tmavší odtieň sa **neráta za behu**. Materiálové assety sú predpripravené a swap za behu
je len priradenie referencie, takže odtieň vygeneruje editor krok pri tvorbe assetu.
Autor teda pipetuje len základné farby.

Zdrojové materiály (`char_<trieda>_<kľúč>` extrahované z FBX pri importe) žijú
v `Assets/_Game/Art/Materials/Character/_source/`. Definujú shader, normálovú mapu
a smoothness. Generátor ich klonuje a prepisuje len `_BaseColor`.

---

## 4. JSON registre

Ručne editované, priamo v `Assets/_Game/Editor/` vedľa `ObjectTypes.json`,
`ObjectPrefixes.json` a `RoomPlatforms.json`. Tvar kopíruje `ObjectTypes.json`:
**plochý zoznam objektov pod jedným kľúčom**, aby sa nový riadok dal dopísať bez
hľadania správneho zanorenia. Parsuje sa cez Newtonsoft.Json, ktorý už v projekte je
(`com.unity.nuget.newtonsoft-json` 3.2.2).

Runtime ich **nečíta** — číta bake (kapitola 7), lebo z reťazca `"navy"` musí niekto
spraviť skutočný `Material`.

### `CharacterClasses.json`

```jsonc
{
  "colorClasses": [
    { "name": "torso", "mainColors": 2, "shadeValue": 0.62, "shadeSaturation": 1.12 },
    { "name": "legs",  "mainColors": 1, "shadeValue": 0.62, "shadeSaturation": 1.12 },
    { "name": "feet",  "mainColors": 2, "shadeValue": 0.55, "shadeSaturation": 1.10 },
    { "name": "hair",  "mainColors": 1, "shadeValue": 0.70, "shadeSaturation": 1.05 },
    { "name": "skin",  "mainColors": 1, "shadeValue": 0.80, "shadeSaturation": 1.08 },
    { "name": "eye",   "mainColors": 1, "shadeValue": null, "shadeSaturation": null },
    { "name": "lips",  "mainColors": 1, "shadeValue": null, "shadeSaturation": null },
    { "name": "beard", "mainColors": 1, "shadeValue": 0.70, "shadeSaturation": 1.05 }
  ],
  "slotClasses": ["head", "hair", "beard", "torso", "legs", "feet"]
}
```

`mainColors: 2` znamená, že nástroj čaká kľúče `1, 2` a vygeneruje `11, 21`.
Faktor odtieňa je **per trieda** — tmavší záhyb látky a tmavší prameň vlasov nie sú
to isté číslo. `shadeValue: null` znamená, že trieda tmavší odtieň nemá.

### `CharacterColorways.json`

```jsonc
{
  "colorways": [
    { "colorClass": "torso", "id": "navy", "displayName": "Tmavomodrá",
      "colors": ["#243B6B", "#C8CEDA"] },
    { "colorClass": "torso", "id": "rust", "displayName": "Hrdzavá",
      "colors": ["#A6482B", "#E8D9C0"] },
    { "colorClass": "skin",  "id": "light", "displayName": "Svetlá",
      "colors": ["#F2CDB4"] }
  ]
}
```

Dĺžka `colors` sa musí rovnať `mainColors` tej triedy. Kontroluje Report.

### `CharacterPresets.json`

```jsonc
{
  "presets": [
    { "slotClass": "torso", "object": "torso_hoodie_1", "displayName": "Mikina",
      "gender": "any",
      "hides": ["chest","abdomen","upperarm_L","upperarm_R","forearm_L","forearm_R"],
      "tags": ["bulky_torso","casual"], "conflicts": ["backpack"], "weight": 3 },

    { "slotClass": "torso", "object": "torso_tank_1", "displayName": "Tielko",
      "gender": "any",
      "hides": [], "tags": ["casual","bare_arms"], "conflicts": [], "weight": 1 },

    { "slotClass": "beard", "object": "beard_full_1", "displayName": "Plnovous",
      "gender": "male",
      "hides": [], "tags": [], "conflicts": [], "weight": 1 }
  ]
}
```

`displayName` nesie rovno slovenský text. Ak neskôr príde lokalizácia, ten istý reťazec
sa dá použiť ako kľúč.

**Tagy sú bitmask**, takže ich je maximálne 32. Report to kontroluje. Pri tridsiatich
presetoch je to ďaleko, ale nech to nie je tichý strop.

---

## 5. Pravidlá presetov

Tri rôzne veci s tromi rôznymi mechanizmami. Miešať ich do jedného poľa by bola chyba.

### 5.1 Exkluzivita v rámci triedy — zadarmo

Jedna slot trieda, jeden preset. Nie je čo konfigurovať.

### 5.2 Krížové konflikty — cez tagy, nie cez dvojice

Preset **poskytuje** tagy a **zakazuje** tagy. Sako dá `bulky_torso`, batoh ho zakáže.

Zoznam zakázaných dvojíc bol zamietnutý: pri 40 presetoch je to rádovo 800 riadkov,
ktoré nikto neudržiava, a pridanie 41. presetu znamená prejsť všetkých 40. Tag je
konštantná práca na preset bez ohľadu na to, koľko ich je.

Konflikty sú **symetrické**. Dopredné `requires` v prvej verzii nie sú — vďaka tomu
je výber jeden priechod bez backtrackingu (kapitola 8).

### 5.3 Gate na pohlavie

`"gender": "male" | "female" | "any"`. Preset označený `female` sa nikdy neobjaví
na mužskom tele.

### 5.4 Zakrytie kože

`hides` je bitmask sekcií tela, nie bool.

| preset | `hides` | prečo |
|---|---|---|
| mikina | `chest, abdomen, upperarm_*, forearm_*` | telo je pod ňou celé zakryté |
| tričko | `chest, abdomen` | ruky zostávajú holé |
| tielko | `[]` | koža okolo tielka musí byť vidieť |

Z tielka plynie pravidlo pre Blender: **oblečenie, ktoré nič neskrýva, musí byť
odsadené od kože**, inak z-fighting. Mikina to riešiť nemusí, tam koža zmizne úplne.

Pri viacerých vybraných presetoch sa masky zjednotia (`OR`).

---

## 6. Čo sa dá pridať bez zásahu do kódu

Toto je hlavná vlastnosť, ktorú návrh musí uniesť: dnes nie sú hotové hlavy, brady, ani
ženské telo, a systém sa kvôli žiadnemu z nich nemá prepisovať.

| pridať | čo to stojí |
|---|---|
| ďalšiu mikinu, vlasy, topánky | objekt v Blenderi + riadok v `CharacterPresets.json` |
| ďalšiu farbu triedy | riadok v `CharacterColorways.json` + `2 — Generate Shades` |
| druhú farbu existujúcej triedy (`char_torso_2`) | zdvihnúť `mainColors` v `CharacterClasses.json` |
| **novú slot triedu** (`head`, `beard`, `glasses`) | riadok v `slotClasses` + kontajner v Blenderi + presety v JSON |
| **novú farebnú triedu** (`eye`, `lips`) | riadok v `colorClasses` + materiály `char_eye_1` na meshi |
| nové pravidlo medzi presetmi | `tags` a `conflicts` v JSON |
| ženské telo | druhý prefab; kľúče sekcií a celý `CharacterPresets.json` platia bez zmeny |

**Žiadny z týchto riadkov nie je v C#.** Slot triedy aj farebné triedy sú dáta v
`CharacterClasses.json`; kód nikde nevymenúva „torso, legs, feet" a nikde nemá `switch`
na názov triedy. Pridanie triedy `head` je preto jeden riadok v poli `slotClasses`,
jeden kontajner v Blenderi a toľko riadkov v `CharacterPresets.json`, koľko je tvarov
hlavy — plus tá jedna štrukturálna zmena z kapitoly 2, presun `male_body_head`.

Dve veci naopak **rozšíriteľné nie sú** a treba o nich vedieť dopredu:

- **16 sekcií tela.** Zmeniť delenie tela znamená prerezať mesh v Blenderi, prepísať
  `BodySection` a prebakovať. Nie je to katastrofa, ale nie je to riadok v JSON.
- **Limity bitmasiek:** 9 základných farieb na triedu, 32 tagov, 254 presetov na triedu
  a 254 colorwayov na triedu. Report na každý z nich upozorní skôr, než sa prekročí.

---

## 7. Assety a editor kroky

### Rozloženie

```
Assets/_Game/Prefabs/Character/
    char_base_male.prefab       16 sekcií tela + všetky presety pre muža
    char_base_female.prefab     to isté pre ženu, spoločný rig

Assets/_Game/Art/Materials/Character/
    _source/                    šablóny z FBX — shader, normálky, smoothness
    torso/  legs/  feet/  hair/  skin/  eye/  lips/  beard/
                                vygenerované mt_char_<trieda>_<colorway>_<kľúč>.mat

Assets/_Game/Editor/
    CharacterClasses.json  CharacterColorways.json  CharacterPresets.json

Assets/_Game/Editor/Character/       editor kód, asmdef FriWorld.Character.Editor
Assets/_Game/Editor/Character/Tests/ EditMode testy, asmdef FriWorld.Character.Tests
Assets/_Game/Scripts/Character/      runtime kód, asmdef FriWorld.Character

Assets/Resources/
    CharacterCatalog.asset      výstup bake, jediné, čo runtime číta
```

Dve telá sú dva prefaby, nie jeden ťažký. Spawner vyberie prefab podľa pohlavia
a mesh nesie len to, čo môže potrebovať.

Presety ležia pod kontajnerom podľa triedy — `Clothes/Torso`, `Clothes/Legs`,
`Clothes/Foot`, `Face/Hair`. **Meno objektu presetu je voľné** (`shirt_1`, `t-shirt_2`,
`boots_1`): triedu určuje pole `slotClass` v `CharacterPresets.json`, nie prefix mena.
Kontajner je pre človeka, register pre systém, a nemusia sa volať rovnako — trieda `feet`
býva v kontajneri `Foot`.

**Mená objektov musia byť v rámci jedného base prefabu jedinečné** — bake podľa nich
kľúčuje mapu slotov. Kontroluje Report.

Kód dostáva vlastné asmdefy z jediného dôvodu: EditMode testy sa nedajú napísať proti
`Assembly-CSharp`, lebo asmdef nevie referencovať predefinovanú assembly. Rovnaké
rozdelenie má už `FriWorld.ObjectRegistry.Editor` a jeho testy. `NPCSpawner` zostáva
v `Assembly-CSharp`, ktorá referencuje všetky asmdefy automaticky, takže sa nemusí sťahovať.

### Menu `Character`

```
1 — Report            čo nesedí; nič nezapisuje
2 — Generate Shades   dogeneruje .mat pre kľúče 11/21 z farieb 1/2
3 — Bake Catalog      JSON → CharacterCatalog.asset s vyriešenými referenciami
```

**Report hlási a nikdy nehádá.** Kontroluje:

- `object` presetu, ktorý v príslušnom base prefabe neexistuje
- meno materiálu, ktoré sa nedá rozparsovať na `char_<trieda>_<kľúč>`
- keyword, ktorý nie je farebná trieda → **nie je to chyba**, len sa vypíše ako ignorovaný
- `colors` nesedí s `mainColors` triedy
- `hides` s neznámou sekciou
- `conflicts` na tag, ktorý žiadny preset nedáva — mŕtve pravidlo
- slot trieda bez jediného presetu pre dané pohlavie — dalo by nahé NPC
- sekcia tela chýbajúca v base prefabe

Bake zapíše okrem referencií aj **mapu slotov**: `renderer → index materiálu →
(farebná trieda, kľúč)`. Za behu sa vďaka nej neparsuje ani jeden reťazec.

---

## 8. Runtime

### `CharacterAppearance` — celý vzhľad na ~16 bajtoch

```csharp
struct CharacterAppearance {
    Gender gender;
    byte[] preset;    // index podľa slot triedy
    byte[] colorway;  // index podľa farebnej triedy
}
```

Toto je jediné, čo sa ukladá: v savote hráča aj ako seed pre NPC.

### `CharacterRandomizer.Roll(seed, catalog, gender)`

Prechádza slot triedy v **pevnom poradí**, filtruje kandidátov podľa pohlavia a podľa
tagov už vybraných kusov, potom váhový výber podľa `weight`. Keďže konflikty sú
symetrické a dopredné `requires` neexistujú, stačí jeden priechod bez backtrackingu.

Deterministické: NPC s rovnakým seedom vyzerá po respawne rovnako a nemusí sa nič ukladať.
Seed sa odvodí z identity NPC.

### `CharacterBuilder.Apply(instance, appearance, catalog)`

1. `Destroy` nevybraných preset objektov
2. zjednotenie `hides` všetkých vybraných → `Destroy` tých sekcií tela
3. prepis `sharedMaterials` podľa mapy slotov z katalógu
4. koniec

Žiadna alokácia okrem poľa materiálov. Strip pri spawne bol zvolený nad `SetActive(false)`
práve preto, že 20 NPC × všetky presety je zbytočne veľa `Transform`ov na webe;
mesh assety sa zdieľajú, takže VRAM tým nerastie.

### Zmena v `NPCSpawner`

Z „vyber jeden z 30 prefabov" sa stane „vyber pohlavie → base prefab → `Roll` → `Apply`".
Pole `npcPrefabs` odchádza.

---

## 9. Výkon

Materiály sú **zdieľané assety**, nie inštancie — 20 NPC ťahajúcich z tej istej palety
zdieľa tie isté `.mat` súbory, takže SRP Batcher ich batchuje naprieč všetkými postavami.

Strip pri spawne drží počet rendererov dole. NPC v mikine, nohaviciach a topánkach
skončí zhruba na štyroch kusoch kože (hlava, krk, dve dlane) plus štyroch kusoch
oblečenia.

**Jedno číslo treba zmerať, nie odhadnúť:** 20 NPC × počet prežitých
`SkinnedMeshRenderer`ov, na webe. Ak to bolí, ďalší krok je zlúčiť prežité sekcie
do jedného renderera pri spawne — rig je spoločný, takže `CombineMeshes` s `bindposes`
prejde. **Nerobiť to dopredu.** Ak sa to zmeria a nespraví, patrí to do `docs/findings/`.

---

## 10. Čo bolo zvážené a zamietnuté

| možnosť | prečo nie |
|---|---|
| `MaterialPropertyBlock` na farbu | vypína SRP Batcher pre ten renderer — presne to, čo na webe pri 20 postavách nechceš |
| `Material` inštancia na NPC za behu | zbytočná alokácia a čistenie, keď predpripravený asset spraví to isté zadarmo |
| Maska sekcií v shaderi (vertex color / UV2) | bitmaska je per-NPC → rozbije zdieľanie materiálov a pridá Shader Graph, ktorý inak netreba |
| Presety ako samostatné prefaby bindované na rig | runtime bone binding, ktorý pri jednom spoločnom rigu nič nerieši |
| Zoznam zakázaných dvojíc presetov | kvadratická údržba; tagy sú konštantná |
| `.mat` asset na každú kombináciu ručne | kombinatorika — odtiene preto generuje nástroj |
| Runtime parsovanie mien materiálov | string práca pri každom spawne a tichý fail pri preklepe |

---

## 11. Otvorené otázky

- **Koľko colorwayov na triedu.** Rozhodne autor pri plnení palety; systém na počte
  nezávisí, len na limite 9 základných farieb.
- **Trieda pre doplnky** (batoh, okuliare) v prvej verzii nie je. Tagy sú navrhnuté tak,
  aby ju uniesli bez zmeny formátu — `conflicts: ["backpack"]` v príklade ju už predpokladá.
- **Dopredné `requires`.** Ak sa ukáže potrebné, doplní sa s retry limitom. Zatiaľ by to
  bola zložitosť bez použitia.
- **Zlúčenie sekcií pri spawne** — až po meraní, viď kapitola 9.
