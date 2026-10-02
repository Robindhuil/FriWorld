# Herný dizajn FriWorldu

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-02 · **Stav:** živý dokument — zámer zozbieraný 2026-10-02, z nového zatiaľ nič nezačaté

Čo je FriWorld ako hra: komu je určený, z čoho sa skladá a kam sa má posunúť. Technika
(svetlo, pipeline objektov, architektúra NPC) má vlastné dokumenty v `docs/`, tu sú na ne
len odkazy. Navigator je samostatná hra a sem nepatrí — `docs/navigator/`.

Každá oblasť má tri časti:

- **Dnes** — čo v hre naozaj je, overené v kóde a dátach, nie podľa starého README.
- **Zámer** — čo je rozhodnuté, že má byť.
- **Otvorené** — čo treba ešte rozhodnúť. Sem sa zbierajú požiadavky.

Keď sa niečo postaví, presunie sa zo Zámeru do Dnes v tom istom commite. Keď oblasť
narastie na vlastný návrh (napríklad jedna quest línia), dostane súbor v `docs/design/`
a tu po nej zostane odsek s odkazom.

---

## 1. Zámer hry

FriWorld je interaktívna 3D prehliadka Fakulty riadenia a informatiky UNIZA a zároveň
náborová hra. Cieľovka sú **žiaci základných a stredných škôl**: hráč sa prejde po vernom
modeli fakulty, spozná jej priestory a učiteľov a vyskúša si predmety, ktoré sa tu učia.

| | |
|---|---|
| hlavný kanál | **web** — nič sa neinštaluje, ide na školskom počítači aj na Chromebooku |
| desktop | verzia pre nadšencov cez launcher, cieľový domov je Steam |
| jazyk hráča | slovenčina |
| peniaze | projekt nezarába a nebude sa doňho investovať |

Prečo vedie web: [Bez podpisu. Launcher je most k Steamu](https://github.com/Robindhuil/FriWorld-Launcher/blob/master/docs/decisions/2026-08-26-bez-podpisu-launcher-je-most-k-steamu.md).
Hra vznikla ako bakalárska práca v roku 2025.

---

## 2. Quest línie podľa predmetov

### Dnes

Jedna lineárna línia 26 questov v `Assets/Resources/Quests/questList.txt`, celá
o programovaní v Jave. Opakuje sa v nej vzor *nájdi učiteľa → splň jeho úlohu → vráť sa*:

| učiteľ | úloha |
|---|---|
| Janech | rozlíšiť programovacie jazyky od hovorených (vstupný kvíz) |
| Meško | zabudnuté heslo — doplniť Java program |
| Gregorová | koľko stoličiek chýba v RA301 |
| Petríková | nekonečný for cyklus |
| Tóth | generovanie skúšok |
| Ďuračík | vytvoriť inštanciu triedy miestnosť |
| Kvet | zamknutá miestnosť — doplniť metódu |
| Janech | učitelia Informatiky 1, potom finálny test (výstupný kvíz) |

Posledný quest končí vetou *„Je toto koniec?"* — koniec hry neexistuje.

Riadok má formát `id meno -> cieľ -> popis`. Nepozná líniu, predmet ani predpoklady,
poradie dáva len to, ktorý quest aktivuje ktorý.

### Zámer

- **Viac quest línií, každá z jedného predmetu**, vybraného podľa toho, čo žiaka ZŠ a SŠ
  môže najviac chytiť. Kandidáti, zoznam nie je uzavretý:
  - programovanie v Jave — základom je dnešná línia,
  - Python na rovnakom princípe ako Java,
  - 3D tlač — tlač priamo v hre a úlohy k nej,
  - počítačové siete,
  - matematika.
- **Líniu zadáva učiteľ, ktorý ten predmet naozaj učí** (sekcia 4).

### Otvorené

- Sú línie paralelné a hráč si vyberá, alebo sa odomykajú za sebou? Je nejaká spoločná
  úvodná línia, akú dnes robí Janech?
- Rozlišuje sa ZŠ a SŠ — obtiažnosťou, inými líniami, alebo vôbec?
- Koľko trvá jedna línia a ako končí? Čo je koniec hry a čo za ňu hráč dostane?
- Čo s dnešnou líniou: ostane ako Java línia, alebo sa rozdelí medzi viac predmetov?
- **Ukladanie postupu.** Dnes sa ukladajú len nastavenia (`PlayerPrefs`). Questy, kódex
  ani štatistiky po zatvorení hry zmiznú. Pri jednej krátkej línii to prejde, pri
  viacerých nie. Na webe by ukladanie išlo do úložiska prehliadača, ktoré školský
  počítač môže mazať.
- 3D tlačiareň v modeli budovy nie je, `ObjectTypes.json` taký typ nepozná.
- Formát `questList.txt` na línie nestačí. Nový bude potrebovať aspoň líniu, predmet,
  zadávateľa a predpoklady.

---

## 3. Minihry

### Dnes

Dva typy, oba sa spúšťajú z počítačov v budove.

- **Programovacia** (`IdeMiniGame`, `IdeUI`) — imitácia **VS Code**, zvýrazňovanie syntaxe
  je naladené na jeho farby. Kód spúšťa vlastný Java interpreter za rozhraním
  `IInterpreter`, typ interpretera vyberá `InterpreterType` — dnes s jedinou hodnotou
  `Java`. Šablóny úloh sú v `Resources/minigames/`: `var`, `if`, `for_cycle`, `while`,
  `function`, `class`, `field`.
- **Kvíz** (`QuizMiniGame`) — `entryQuiz.json` (10 otázok: programovací, alebo hovorený
  jazyk?) a `exitQuiz.json` (15 otázok, finálny test).

### Zámer

- Typy miniher sa majú od seba **zreteľne líšiť**. Programovacia a kvíz sú prvé dva.
- Funkčnosť aj priebeh (flow) miniher sa bude rozširovať viacerými smermi.
- Python ide **na rovnakom princípe ako Java** — v tom istom VS Code, s vlastným
  interpreterom. Miesto preň je pripravené: druhá hodnota `InterpreterType` a druhá
  implementácia `IInterpreter`.

### Otvorené

- Aké ďalšie typy? Nová línia môže potrebovať vlastný — 3D tlač (model, slicer, tlač),
  siete (zapojenie, adresovanie), matematika.
- Čo presne znamená rozšírenie flow — nápovedy, viac pokusov, úloha v krokoch,
  hodnotenie, časový limit?
- Spúšťajú sa minihry aj inak než z počítača — pri tlačiarni, v sieťovom laboratóriu?

---

## 4. Učitelia

### Dnes

Sedem menovaných učiteľov: Janech, Meško, Gregorová, Petríková, Tóth, Ďuračík a Kvet.
Majú ručne robené modely a bežia na starej NPC vrstve (`Npc` + `StateMachine`). Dialógy
sú v `Resources/dialogue/Scripts/`, Kvet, Ďuračík a Tóth majú aj „Casual" variant. Quest
ich hľadá podľa miesta — *„nájdi ho niekde v okolí miestnosti RA124"*. Gregorová je
v kódexe označená ako fiktívna postava.

### Zámer

- **Zadávateľom úloh je učiteľ, ktorý daný predmet učí.**
- Učiteľ je zároveň **agent v agentovej simulácii** (sekcia 5) — pohybuje sa po fakulte
  dynamicky, nie na pevnom mieste.

### Otvorené

- **Ako hráč nájde učiteľa, ktorý sa hýbe?** Ciele typu „pri RA124" prestanú platiť.
  Možnosti: navigácia priamo k nemu, rozvrh alebo konzultačné hodiny, značka na mape.
- Čo urobí učiteľ, keď ho hráč osloví uprostred presunu?
- Skutoční učitelia v hre — meno, fotka v kódexe, podoba modelu. Potrebný je ich súhlas
  a postup pre prípad, že učiteľ odíde alebo predmet prevezme iný.
- Ostanú učitelia ručné modely, ako hovorí návrh postáv, alebo ich bude skladať aj
  generátor?

Starý `Npc` je jeden objekt na pohyb, dialóg aj questy, takže simulácia doň nezapadne.
Čo s ním: [`docs/2026-08-28-npc-skripty-na-prerobenie.md`](../2026-08-28-npc-skripty-na-prerobenie.md).

---

## 5. Agentová simulácia

### Dnes

Nie je. Generovaní študenti chodia po bodoch `PathWay`: telo (`NpcActor`) je pasívne a kam
ísť, mu hovorí `WaypointDirector`. Postavené je to zámerne tak, aby simulácia bola výmena
riadiča, nie prepis —
[`docs/superpowers/specs/2026-08-29-npc-vrstva-design.md`](../superpowers/specs/2026-08-29-npc-vrstva-design.md).
`DayCycleController` (slnko od svitania po západ) existuje, ale nie je v žiadnej scéne.

### Zámer

- Študenti aj učitelia žijú na fakulte: rozvrhy, cvičenia, obedy, obsadzovanie stoličiek.
- Simulácia tiká všetkých, telo dostanú len tí v okolí hráča. Vzhľad študenta sa už dnes
  losuje zo seedu jeho identity, takže sa preň nemusí nič ukladať.

### Otvorené

- Odkiaľ prídu rozvrhy — z tej istej univerzitnej API ako miestnosti (sekcia 8)?
- Plynie v hre čas? Ako rýchlo, a viaže sa naň deň a noc?
- Koľko agentov unesie web build.

---

## 6. Generované postavy

### Dnes

Študenti sa skladajú z presetov a farieb, nie z hotových modelov. Hotové je **len mužské
telo**:

| slot | presetov |
|---|---|
| vrch (`torso`) | 6 — štyri košele, dve tričká |
| spodok (`legs`) | 5 — troje dlhé nohavice, dvoje kraťasy |
| obuv (`feet`) | 2 |
| vlasy | 4 + holá hlava |
| obočie / brada / oči | 4 / 4 + oholený / 4 |
| pehy | 1 hotový + 3 rozrobené |

K tomu 20 tvárových osí, výška a farebná paleta. Animácie postavy nemajú.
Návrh: [`docs/superpowers/specs/2026-08-28-character-customization-design.md`](../superpowers/specs/2026-08-28-character-customization-design.md).

### Zámer

- **Ženské telo.**
- Ďalšie oblečenie a doplnky.
- Neskôr ten istý systém poslúži na **tvorbu postavy hráčom**, potom telo a ruky z prvej
  osoby — v tomto poradí, tak ho určuje návrh postáv.

### Otvorené

- Rozsah oblečenia: mikiny, bundy, sukne, šaty, okuliare, batohy?
- Animácie chôdze a státia pre generované postavy.

---

## 7. Dialógy

### Dnes

Dialógy sú stromové — uzly s podmienkami a efektmi nad stavom hry (`NodeDialogueManager`,
`GameState`). Obsah tvorí desať skriptov v `Resources/dialogue/Scripts/` plus
`TestScript.json` a texty questov v `questList.txt`.

### Zámer

Obsah sa **prepíše** — veľa informácií v ňom nedáva zmysel alebo je chybných. Príklady len
z `questList.txt`:

- `quest_5` hovorí o kóde od dverí `913A2200`, hoci `quest_4` je o hesle do Moodlu.
- „Pani Petríková ťa poslal", „páni Petru Gregorovú".
- Preklepy: „pomocť", „študenotv", „Hassovyých", „ževraj", „zdelím".

### Otvorené

- Kto obsah píše a kto ho schvaľuje — učiteľ, ktorý predmet učí?
- Rod hráča: texty ho dnes riešia lomkou („urobil/a"). S tvorbou postavy sa dá vybrať.
- Ostane tykanie hráčovi?
- Ostane s refaktorom formát skriptov, alebo sa zmení aj dialógový systém?

---

## 8. Miestnosti a dáta o fakulte

### Dnes

`Assets/Resources/Rooms.json` — 171 miestností s katedrou, kódom, funkciou, učiteľmi
a odkazom na `fri.uniza.sk/miestnost/…`. Číta ho navigácia v hre a `RoomSignBaker`
(`FriWorld > Room Signs`), ktorý z neho **upečie cedule** na dvere. Navigator má vlastné
body miestností a dáta FriWorldu nečíta.

### Zámer

Miestnosti sa menia každý semester, rovnako učitelia v nich. Dáta sa majú ťahať
z **univerzitnej API**, nie z JSONu v repe.

### Otvorené

- Existuje taká API a kto ju poskytne? Čo vracia — miestnosti, učiteľov, rozvrhy?
- Za behu, alebo pri builde? Cedule sú dnes pečené, takže zmena dát znamená nový build.
  Za behu ich treba vedieť popísať dynamicky.
- Čo keď API neodpovie — posledné známe dáta v builde ako záloha?
- Jeden zdroj aj pre Navigator, ktorý s prerábkou dát miestností cez API už počíta.

---

## 9. HUD a mapa

### Dnes

HUD tvorí panel s aktívnou úlohou a výzva na interakciu (`PlayerHUDTree.uxml`,
`PlayerUI`). Ostatné je v oknách s kartami: kódex, denník úloh, navigácia (výber
miestnosti podľa poschodí, čiara v priestore, QR kód), štatistiky a prehrávač videí.
K tomu notifikácie.

### Zámer

- **Minimálne HUD hráča.**
- **Mapa celej budovy v UI.**

### Otvorené

- Čo HUD ukazuje — aktuálny cieľ, smer alebo kompas, minimapu?
- Čo je na mape — poschodia, poloha hráča, učitelia (sekcia 4), cieľ questu?
- Nahradí mapa dnešné okno navigácie, alebo pôjdu vedľa seba?

---

## 10. Zvuk a hudba

### Dnes

- Efekty: tlačidlá, kliknutie, dvere, chôdza, šprint, skok, zdvihnutie, nájdený secret.
- Hovorené repliky NPC nie sú, v projekte nie je jediný hlasový súbor. Dialógy sú len text.
- Hudba v menu je playlist s uvedenými autormi: tri skladby Tobyho Foxa (Undertale,
  Snowdin Town, Home), cover *Zelda & Chill — Lost Woods*, dve chill skladby
  a `main_menu_music`. Leží v `Resources`, takže ide do každého buildu (~28 MB).

### Zámer

- Prerobiť zvukové efekty.
- Niektoré repliky NPC nahovoriť.
- **Vlastná hudobná téma.**

### Otvorené

- **Práva na hudbu.** Pri skladbách Tobyho Foxa a pri covere Zeldy je uvedený autor, ale
  uvedenie autora nie je licencia. Je to ten istý problém ako pri videách (sekcia 11)
  a s vlastnou témou by mali ísť preč.
- Ktoré repliky sa nahovoria a kto ich nahovorí — skutoční učitelia?

---

## 11. Videá

### Dnes

14 videí v `Assets/StreamingAssets/videos/`, spolu 202 MB:

| skupina | videá | kde |
|---|---|---|
| z webu fakulty | `historia`, `ples`, `prihlaska`, `buducnost_na_fri`, `fri_club`, `frifest`, `tlac_sutaz`, `ako_sa_nestratit`, `rozhodni_sa_spravne` | obrazovky v budove (`Demo.unity`) |
| návody k programovaniu | `programming`, `variables`, `if`, `for_loop`, `while` | kódex |

Návodové videá sú animácie iného tvorcu a slovenský hlas k nim je preložený z angličtiny
cez AI. Záznam kódexu *Pole* odkazuje na `videos/java_arrays.mp4`, ktorý v projekte nie je.

### Zámer

**Návodové videá sa spravia nanovo, vlastné** — kvôli právam na pôvodné animácie.

### Otvorené

- Forma: animácia, záznam priamo z herného VS Code, vlastný hlas — kto ich spraví
  a nahovorí?
- Pokryjú aj nové línie (Python, 3D tlač, siete, matematika)?
- Videá z webu fakulty: je na ne súhlas fakulty výslovne, alebo sa len predpokladá?
- Veľkosť pre web: návodové video má ~15 MB a vo web builde Navigatora tvorili videá
  FriWorldu 202 z 360 MB, kým ich odtiaľ nevyhodil
  ([meranie](../findings/2026-10-01-velkost-web-buildu-navigatora.md)). Pri nových stojí
  za to určiť rozpočet na minútu videa.

---

## 12. Ostatné mechaniky

Z bakalárky v hre zostávajú a zatiaľ sa nemenia:

- **Kódex** — 28 záznamov: 7 učiteľov, 9 tém z programovania, 12 secretov.
- **Secrety** — zberateľné predmety: figúrky učiteľov, káva, Black Snake, Pí, kanál…
- **Štatistiky** — nájdené secrety, chyby a prejdená vzdialenosť.
- **Interaktívne objekty** — dvere, zamknuté dvere, keypad, pamätné tabule, odkazy na web,
  počítače s minihrami.
- Voľný pohyb v prvej osobe: chôdza, šprint, skok.
