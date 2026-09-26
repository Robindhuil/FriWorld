# Navigator — plán

**Verzia projektu pri písaní:** 0.1.2-alpha · **Dátum:** 2026-09-25 · **Stav:** fázy 3–4 funkčné v editore, chýba Build Profile a web build (2026-09-26)

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
│   ├── Editor/     ← asmdef FriWorld.Navigator.Editor, menu Navigator → 1, 2
│   ├── Data/       ← NavigatorNavMesh.asset, RoomAnchors.asset (generované)
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
  `Custom` s cubemapou skyboxu `FS000_Day_03`.

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

### 3.4 Build Settings

Build Profile Navigatora má v zozname **len** `FriNavigator.unity`; FriWorld build (Build Settings: Menu + Demo) ju nemá.
Splash (`m_ShowUnitySplashScreen` je dnes `1`) vypnúť cez override v profile Navigatora.

---

## 4. Tok dát

```
fri.uniza.sk  <a href="…/navigate/{kód}" target="_blank">
      │
friworld-web  /navigate/[kód]
      │  1. server-side: API → názov, označenie (neexistuje → chyba v HTML, Unity sa nesťahuje)
      │  2. vlastný loader: názov miestnosti + progress
      │  3. po štarte: SendMessage("Navigator", "Go", "RA101")
      ▼
Unity  kód → kotva (RoomAnchors) → NavMesh cesta z kamery → stopa kamery → duration
      │  jslib: onReady(duration) · onTime(t) (pár × za s) · onEnded() · onError(kód)
      ▼
HTML ovládanie  Play · Pause · Seek(t) · SetSpeed(x)  → SendMessage
```

API sa nevolá z Unity (žiadny CORS z WebGL, chyby rieši HTML).

---

## 5. Kódy miestností a kotvy

Kód miestnosti je meno jej kontajnera veľkými písmenami: `ra101` → `RA101`, `rb308` → `RB308`.
Miestnosť je každý kontajner v tvare `r<písmeno><3 číslice>`; jej dvere sú deti
`<kontajner>_door_<n>`. Vstup sa normalizuje (`"ra 101"` = `RA101`).

`Navigator → 2 — Bake Room Anchors` upečie `Data/RoomAnchors.asset`: pre každý kód bod na
NavMeshi 1.6 m pred dverami a smer k dverám. Pri viacerých dverách a oboch stranách vyhrá
bod, ku ktorému je od kamery najkratšia úplná cesta, a z ktorého dvere naozaj vidno.
Krok **validuje**: miestnosť bez dverí, miestnosť nedosiahnuteľná z kamery.

Stav 2026-09-26: 166 kontajnerov, všetky dosiahnuteľné. Tabuľky `RA204`, `RA304` a `RC008`
v budove visia, ale kontajner s tým menom model nemá — tie kódy Navigator nepozná.

## 6. Stopa kamery

Spravené v `CameraTrack` + `NavigatorController` (2026-09-26); odchýlky od pôvodného zámeru:

1. `NavMesh.CalculatePath(bod pod kamerou → kotva)` → rohy.
2. Namiesto spline: rovnomerné prevzorkovanie, odtlačenie od hrán NavMeshu (0.6 m, kde chodba
   dovolí) a kĺzavý priemer, trikrát. Catmull-Rom by v úzkych zákrutách lepil kameru na roh.
3. Profil rýchlosti: lichobežník — 1.5 s rozbeh, 4 m/s, 1.5 s dobeh. Bez stropu na dĺžku:
   RB308 na 3. poschodí budovy B trvá ~29 s. Ladí sa v inspektore alebo `SetSpeed`.
4. Kamera vo výške očí (1.6 m), nie „dron" — pohľad na priemer bodov 1–4 m vpred, sklon
   obmedzený na ±20°. Na schodoch stačí, do stien nenaráža.
5. Prvé 3 m kamera plynulo opúšťa pózu, v ktorej bola položená; posledné 3 m sa otáča k dverám.
6. Verejné metódy pre web: `Go(kód)`, `Play()`, `Pause()`, `Seek(s)`, `SetSpeed(x)`.

Zostáva:

- Čiara po podlahe (LineRenderer z tej istej trasy), odkrýva sa podľa `t`.
- Doladiť záver: v zábere dvere aj tabuľka z `BakedSigns`.
- **Odložené (2026-09-26), kamera zatiaľ preletí cez zatvorené dvere.** Karta na boarde
   „Navigator: dvere sa otvárajú pri prelete kamery". Pôvodný zámer nižšie, s opravou:
   stavy `Door_open` / `Door_close` sú v controlleri mŕtve (default je `Blend Tree` bez
   prechodov) a klipy sú jednosnímkové pózy, takže `animator.Play(stav, 0, norm)` nič
   nenainterpoluje. Krídlom hýbe blend tree cez `DoorRotation` (−90.9 … 0 … 90.9) — správne je
   `SetFloat("DoorRotation", ±90.9 * open01(t))`, bez `Play` a bez `speed = 0`.

   Pôvodne: **dvere sa otvárajú pri prelete kamery** — každé dvere na trase (chodbové, vstupné) aj
   cieľové na konci. Robí to **vlastný skript Navigatora `NavigatorDoor`**, nie `Door` z FriWorldu:
   funguje len s lietajúcou kamerou a **prehráva animáciu dverí z existujúceho
   `Assets/_Game/Animations/Door_Interaction.controller`** (stavy `Door_open` / `Door_close`,
   parameter `DoorRotation` = smer otvárania). Vlastnú animáciu nerobí.
   - Dvere na trase sa zistia **raz** po `Go(id)`: krídla (podľa typového kľúča, viď 3.2) blízko
     spline, s bodom prechodu `s` na trase a smerom od kamery. `NavigatorDoor` sa na ne pridá
     za behu — do zdieľaného prefabu sa nezapisuje.
   - **Pretáčanie:** nespúšťať animáciu cez `IsOpen` (to beží v čase a nedá sa vrátiť).
     Normalizovaný čas klipu sa odvodí z `t` — `door(t)` podľa vzdialenosti kamery k dverám po
     trase (otvárať pár metrov pred kamerou, zatvoriť za ňou) — a nastaví cez
     `animator.Play(stav, 0, norm)` pri `animator.speed = 0`. Pri pretočení dozadu sa dvere samé
     vrátia do správneho stavu.

Všetko sa počíta **raz** po `Go(id)`. Každý snímok len `camera = pose(t)`, `line = reveal(t)`
(a neskôr `dvere = door(t)`).

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
