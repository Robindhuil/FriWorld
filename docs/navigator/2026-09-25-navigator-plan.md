# Navigator — plán

**Verzia projektu pri písaní:** 0.1.2-alpha · **Dátum:** 2026-09-25 · **Stav:** fázy 3–4 funkčné v editore, Build Profile je, web build ešte nebol (2026-10-01)

Druhá hra v tom istom Unity projekte. Používateľ na externom webe (fri.uniza.sk) klikne na
miestnosť, otvorí sa nová karta a v nej kamera preletí od recepcie k dverám tej miestnosti
po schodoch. Ovláda sa ako video: play/pauza, pretáčanie, rýchlosť, zobrazená dĺžka.

Dokument je písaný tak, aby sa podľa neho dalo ísť v inej session bez tohto kontextu.

---

## 1. Čo je rozhodnuté

| Rozhodnutie | Prečo |
|---|---|
| **Samostatná hra, nie feature FriWorldu.** Do FriWorldu z nej nič nepôjde. | Iný produkt, iný build, iné publikum. |
| **Mimo FF systému.** Vlastná scéna, build profil s jedinou scénou. | Oddelenie zabezpečí scéna a profil. |
| **Zdieľa sa len budova** (`Assets/_Game/Prefabs/FriBuilding/`). Navigator ju iba číta. | Jedna budova, jeden zdroj pravdy. |
| **Živý WebGL build, nie predrenderované videá.** | Priradenie miestností príde z API a môže sa meniť; video by bolo zastarané. |
| **Animácia je čistá funkcia `pose(t)`.** | Pretáčanie a rýchlosť sú potom len nastavenie `t`, nič sa neakumuluje. |
| **Naviguje sa podľa kódu miestnosti** (`RA101`), nie podľa interného `id`. *(zmenené 2026-09-26)* | Kód ľudia poznajú a je v modeli: kontajner `ra101` = miestnosť `RA101`. Netreba register ani dohodu o `id`. |
| **Nová karta, nie modal / iframe.** | Na fri.uniza.sk stačí `<a target="_blank">`, bez ich JS a CSP dohody; na mobile celá obrazovka; odkaz sa dá zdieľať. Tá istá URL pôjde neskôr aj do iframe. |
| **Len schody, žiadny výťah.** | Zadanie. |
| **Ovládanie prehrávača v HTML, nie v Unity UI.** | Na mobile lepšie ovládateľné, natívny vzhľad. |
| **Bez Unity splashu.** | Od Unity 6 nepovinný aj na Personal. |
| **Musí bežať na mobile.** | Dnešný FriWorld web build na mobile beží — overené. |
| **Žiadne baked svetlo, žiadne tiene.** *(2026-09-26)* | Web build; lightmapy by mali stovky MB a len by robili problémy. |
| **Štart je tam, kde stojí kamera v scéne.** *(2026-09-26)* | Recepciu vyberá Robin ručne; kód pozíciu ani natočenie neodvodzuje. |

---

## 2. Štruktúra

```
Assets/
├── _Game/          ← FriWorld (bez zmeny)
│   └── Prefabs/FriBuilding/   ← ZDIEĽANÉ, Navigator len číta
├── _Navigator/     ← všetko Navigatorove
│   ├── Scenes/FriNavigator.unity
│   ├── Scripts/    ← asmdef FriWorld.Navigator
│   ├── Editor/     ← asmdef FriWorld.Navigator.Editor, menu Navigator → 1, 2, 3
│   ├── Data/       ← NavigatorNavMesh.asset, RoomAnchors.asset, NavigatorReflection.exr (generované)
│   ├── Prefabs/    ← NavBlocker (vyreže NavMesh tam, kade let nemá ísť)
│   ├── Tests/      ← asmdef FriWorld.Navigator.Tests (EditMode, `Routine → Run EditMode Tests`)
│   └── Settings/   ← NavigatorLighting.lighting, Build Profile
docs/navigator/     ← tento plán a neskôr navigatorove rozhodnutia
```

Pravidlá oddelenia:

- **Navigator nikdy nezapisuje do zdieľaného prefabu.** Kotvy, NavMesh, otvorené dvere — všetko
  žije v scéne alebo dátach Navigatora. Zápis do `FriBuilding.prefab` je výsadou krokov
  `Routine` FriWorldu.
- **FriWorld o Navigatore nevie.** Žiadna referencia z `_Game/` do `_Navigator/`.
- Oba asmdefy majú `autoReferenced: false`, takže `Assembly-CSharp` FriWorldu Navigator nevidí.
- Okrem budovy sa zdieľajú aj prefaby svetla z `_Game/Prefabs/Enviroment/` (slnko, fill,
  volume). Budova aj tie stoja na **tých istých súradniciach ako v `Demo.unity`**, nech sedí
  uhol slnka. Probes z `Enviroment` sú v Navigatore vyhodené — bez bakeu nemajú čo niesť.
- Svetlo sa **nepečie**: `NavigatorLighting.lighting` bez GI, slnko realtime bez tieňov,
  kamera tiene nekreslí. Occlusion je upečená (`Scenes/FriNavigator/OcclusionCullingData.asset`).
- **Pasca:** default reflection „Skybox" bez upečených lighting dát zhodí URP
  `ReflectionProbeManager` (NRE, celý obraz biely/čierny). Default reflection je preto
  `Custom` s cubemapou `Data/NavigatorReflection.exr`, ktorú `Navigator → 3` upečie z oblohy.
  Priamo textúra skyboxu `FS000_Day_03` nejde (2026-10-01): nemá mipmapy ani konvolúciu,
  takže aj drsná podlaha zrkadlila ostré mraky.

---

## 3. Pasce, ktoré treba vyriešiť hneď na začiatku

### 3.1 Skripty FriWorldu na prefabe bežia aj v Navigatore

Budova nesie `Door`, interaktábly, zvuky… V Navigatore nie je hráč ani `InputManager`.
`Door` s chýbajúcim hráčom počíta (`FindGameObjectWithTag("Player")` má null vetvu).
**Overiť vo fáze 1** pohľadom do konzoly WebGL buildu; riešiť len to, čo naozaj padá.

### 3.2 asmdef nemôže referencovať `Assembly-CSharp`

`Door` a `Interactable` nemajú vlastný asmdef, takže ich kód Navigatora
v asmdef-e nevidí. Nevadí:

- miestnosti a dvere sa hľadajú **podľa mena** v editorovom kroku (`NavigatorBake`, viď 5):
  kontajner `r<písmeno><3 číslice>`, dvere `<kontajner>_door_<n>` — presná zhoda, nie podreťazec.
- Za behu sa mená nečítajú; čo treba, je upečené v `_Navigator/Data/`. Web build navyše
  časť budovy podľa platformy strihá.

### 3.3 NavMesh

**Spravené.** `Navigator → 1 — Bake NavMesh` upečie vlastný NavMesh (agent Humanoid, vrstvy
`Obstacle` + `Nav` ako `PlayerNav` FriWorldu) do `Data/NavigatorNavMesh.asset`. Surfaces
FriWorldu (`FriBuilding/NavMesh`) sú v tejto scéne vypnuté override-om, prefab nedotknutý.
Výťah netreba vylučovať: jeho podlahy na poschodiach nie sú zvisle prepojené, takže trasa
ide vždy po schodoch.

Kade let viesť nemá (dvere, priestor), tam sa v scéne položí `Prefabs/NavBlocker.prefab` —
`NavMeshModifierVolume` s plochou `Not Walkable` vo vrstve `Obstacle`, bez meshu a collidera —
a natiahne škálou. Po bakeu je NavMesh v tom mieste vyrezaný. Overené v izolovanom bakei
(2026-10-01): vyreže aj otočený box a pivot 15 cm nad podlahou; vo vrstve `Default` ho bake
ignoruje a pri pivote 30 cm nad podlahou podlahu minie.

### 3.4 Build Settings

Build Profile Navigatora má v zozname **len** `FriNavigator.unity`; FriWorld build (Build Settings: Menu + Demo) ju nemá.
Splash (`m_ShowUnitySplashScreen` je dnes `1`) vypnúť cez override v profile Navigatora.

**Spravené 2026-10-01:** `Settings/NavigatorWeb.asset` — platforma Web, zoznam scén len
`FriNavigator.unity`, vlastné Player Settings so splashom vypnutým. Aktívny profil sa nemenil,
editor ostal na Windows. Vlastné Player Settings sú **kópia globálnych z 2026-10-01**: verzia,
názov produktu ani web nastavenia sa z Project Settings do Navigatora už neprenášajú — menia sa
v profile.

---

## 4. Tok dát

```
fri.uniza.sk  <a href="…/navigate/{kód}" target="_blank">
      │
friworld-web  /navigate/[kód]
      │  1. server-side: API → názov, označenie (neexistuje → chyba v HTML, Unity sa nesťahuje)
      │  2. vlastný loader: názov miestnosti + progress
      │  3. build dostane kód v URL (?room=RA101) — alebo po štarte SendMessage("Navigator", "Go", "RA101")
      ▼
Unity  kód → kotva (RoomAnchors) → NavMesh cesta z kamery → stopa kamery → duration
      │  jslib: onReady(duration) · onTime(t) (pár × za s) · onEnded() · onError(kód)
      ▼
HTML ovládanie  Play · Pause · Seek(t) · SetSpeed(x)  → SendMessage
```

API sa nevolá z Unity (žiadny CORS z WebGL, chyby rieši HTML).

**Kód miestnosti v URL** (rozhodnuté 2026-10-01): web build si pri štarte prečíta `room` z URL
stránky (`RoomLink`) a hneď letí — `…/index.html?room=RA101` funguje aj bez Hubu, len so
statickým serverom. Bez parametra čaká na `Go` zo stránky; testovacia miestnosť z inšpektora
(`roomCode`, `playOnStart`) platí len mimo web buildu. `Go` ostáva aj na prepnutie miestnosti
a ovládanie zo stránky.

---

## 5. Kódy miestností a kotvy

Miestnosti a ich kódy sú **`RoomPoints`** vo `FriBuilding` (`RoomSignManager/RoomPoints`,
inštancia `Rooms.prefab`): jeden ručne položený bod na miestnosť pri jej vchode, meno bodu je
kód. Vstup sa normalizuje (`"ra 101"` = `RA101`).

`Navigator → 2 — Bake Room Anchors` upečie `Data/RoomAnchors.asset`: pre každý bod kotvu na
NavMeshi 1.6 m pred dverami, v ktorých bod stojí (krídlo do 1 m od bodu, na jeho poschodí),
a smer k dverám. Z dvoch strán dverí vyhrá tá, ku ktorej je od kamery kratšia úplná cesta
a z ktorej dvere naozaj vidno. Bod, pri ktorom dvere nie sú, je kotvou sám a smer je smer
príletu. Krok **validuje** dosiahnuteľnosť z kamery a vypíše body bez dverí.

**Prečo body a nie kontajnery** (zmenené 2026-10-01; predtým kód = meno kontajnera a kotva pred
jeho dverami): kabinet, do ktorého sa vchádza cez inú miestnosť (RA104 cez RA105), má vlastné
dvere až za jej dverami. Let k nim viedol cez dvere susednej miestnosti, a tie sú na webe
desktopOnly, teda zatvorené — kamera nimi preletela. Bod kabinetu stojí pri vchode z chodby.
Body sú pravda aj tam, kde sa s menami kontajnerov alebo tabúľ nezhodujú (RB051–RB054,
RC019/RC029); to sa nerieši, dáta miestností prídu z API.

Stav 2026-10-01: 169 bodov, všetky dosiahnuteľné; RA204, RA304 a RC008, ktoré kontajner nemajú,
Navigator teraz pozná. Bez dverí pri bode: RA001 a RC008 (stoja pri tabuli). Žiadna zo 169
trás neprechádza dverami, ktoré web build zatvorí.

## 6. Stopa kamery

Spravené v `CameraTrack` + `NavigatorController` (2026-09-26, doladené 2026-10-01); odchýlky
od pôvodného zámeru:

1. `NavMesh.CalculatePath(bod pod kamerou → kotva)` → rohy.
2. Namiesto spline cez rohy: rovnomerné prevzorkovanie, výška z navmeshu, odtlačenie od hrán
   NavMeshu (0.6 m, kde chodba dovolí) a kĺzavý priemer, trikrát. Catmull-Rom cez rohy by
   v úzkych zákrutách lepil kameru na roh; medzi hustými bodmi (0.25 m) ho používa `Evaluate`,
   aby rovná chodba nemala zlom v každom bode.
3. **Výška sa číta z navmeshu na surovej trase**, nie z priamok medzi rohmi — rohy nevedia, kde
   začínajú schody. Prečo a čo neprešlo:
   [`2026-10-01-vyska-kamery-z-navmeshu.md`](2026-10-01-vyska-kamery-z-navmeshu.md).
4. Profil rýchlosti: lichobežník — 1.5 s rozbeh, 3 m/s, 1.5 s dobeh. Bez stropu na dĺžku:
   RB308 na 3. poschodí budovy B trvá ~34 s. Ladí sa v inspektore alebo `SetSpeed`. Čas
   pribúda po `Time.smoothDeltaTime` — surový `deltaTime` kolíše okolo obnovovacej frekvencie
   a kamera by sa posúvala nerovnomerne.
5. Kamera vo výške očí (1.6 m), nie „dron" — pohľad 4 m vpred po kópii trasy vyhladenej
   na ±2 m, sklon obmedzený na ±20°. Centimetrové vlnky trasy tak smerom pohľadu nehýbu.
6. Prvé 3 m kamera plynulo opúšťa pózu, v ktorej bola položená; posledné 3 m sa otáča k dverám.
7. Verejné metódy pre web: `Go(kód)`, `Play()`, `Pause()`, `Seek(s)`, `SetSpeed(x)`.
8. **Dvere sa otvárajú pri prelete** (2026-10-01, `NavigatorDoors` + `DoorPassage`). Na dverách
   sa nič nemení a nič sa na ne nepridáva:
   - Dvere sa nájdu podľa tagu `Door` — register typov ho dáva len skutočným dverám, rámom nie.
     Každé je samotné krídlo s pivotom v pántoch. Zmerajú sa raz pri štarte scény, kým sú
     všetky zatvorené.
   - Po `Go(kód)` sa vyberú tie, cez ktorých otvor trasa naozaj prechádza (pretne rovinu
     krídla v jeho šírke, na jeho poschodí). Dvere cieľovej miestnosti ostávajú zatvorené —
     let končí pred nimi.
   - Hýbe nimi `Animator`, ktorý dvere už majú: `DoorRotation` = ±90.9 × otvorenie. Stavy
     `Door_open` / `Door_close` sú v controlleri mŕtve, krídlom hýbe len blend tree. Otvárajú sa
     od kamery, tým istým pravidlom ako FriWorld `Door`.
   - Otvorenie je funkcia vzdialenosti na trase, nie času: 3 → 1 m pred kamerou sa otvoria,
     1,5 → 3,5 m za ňou zatvoria. Pretáčanie dozadu ich samo zatvorí.
   - Dvere bez `Animator`a (desktop-only na webe) sú neprechodné a ostanú zatvorené. Keď cez
     také let ide, konzola varuje — patrí tam `NavBlocker`.
   - Overené na 166 letoch: 0–6 dverí na let, 21 rôznych, a každé z nich sa z každého smeru
     otvára na odvrátenú stranu.

Zostáva:

- Čiara po podlahe (LineRenderer z tej istej trasy), odkrýva sa podľa `t`.
- Doladiť záver: v zábere dvere aj tabuľka z `BakedSigns`.

Všetko sa počíta **raz** po `Go(id)`. Každý snímok len `camera = pose(t)`, `dvere = door(d)`
a neskôr `line = reveal(t)`.

---

## 7. Fázy

| # | Fáza | Hotové, keď |
|---|---|---|
| 1 | Kostra: `_Navigator/`, asmdefy, scéna s budovou, Build Profile, `CLAUDE.md` | WebGL build Navigatora sa zbuildí a ukáže budovu |
| 2 | Overenie 3.1 v builde | Konzola WebGL buildu bez chýb |
| 3 | Vlastný NavMesh + kódy miestností + kotvy + validácia | Validácia hlási 0 nedosiahnuteľných miestností — **splnené 2026-09-26** |
| 4 | Stopa kamery `pose(t)` | Dobre vyzerá prízemie, 1. poschodie aj najvyššie |
| 5 | jslib bridge + `/navigate/[id]` vo `friworld-web` s HTML ovládaním | Seek, pauza a rýchlosť fungujú na mobile |
| 6 | Napojenie na API, odkazy na fri.uniza.sk | Klik na fri.uniza.sk otvorí navigáciu |

---

## 8. Kde sa na čo robí

Plán vznikol v cloudovej session bez Unity editora. Fáza 1 sa začala 2026-09-26 lokálne.

- **Fázy 1–4** vyžadujú Unity editor (scéna, Build Profile, bake NavMeshu, ladenie kamery).
  Robia sa lokálne, ideálne s Unity MCP (`Unity_RunCommand`), na vetve z `master`.
- **Fáza 5** je v repe `friworld-web` (FriWorld Hub) — route `/navigate/[id]`, loader,
  HTML ovládanie. Jslib bridge v Unity ide s ňou naraz, nech sa kontrakt správ ladí na oboch
  stranách súčasne.
- **Fáza 6** čaká na API a na dohodu s adminmi fri.uniza.sk (len odkazy, žiadny ich JS).

Začína sa fázou 1 a jej kritériom „hotové, keď"; každá ďalšia fáza až po splnení predchádzajúcej.

---

## 9. Otvorené otázky

- Čo presne vráti API a kedy bude? Dá kód miestnosti v tvare `RA101`?
- Pod akou doménou pobeží `/navigate` (FriWorld Hub)?
