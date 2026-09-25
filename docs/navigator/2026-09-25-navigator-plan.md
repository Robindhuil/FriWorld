# Navigator — plán

**Verzia projektu pri písaní:** 0.1.2-alpha · **Dátum:** 2026-09-25 · **Stav:** návrh, nezačaté

Druhá hra v tom istom Unity projekte. Používateľ na externom webe (fri.uniza.sk) klikne na
miestnosť, otvorí sa nová karta a v nej kamera preletí od recepcie k dverám tej miestnosti
po schodoch. Ovláda sa ako video: play/pauza, pretáčanie, rýchlosť, zobrazená dĺžka.

Dokument je písaný tak, aby sa podľa neho dalo ísť v inej session bez tohto kontextu.

---

## 1. Čo je rozhodnuté

| Rozhodnutie | Prečo |
|---|---|
| **Samostatná hra, nie feature FriWorldu.** Do FriWorldu z nej nič nepôjde. | Iný produkt, iný build, iné publikum. |
| **Zdieľa sa len budova** (`Assets/_Game/Prefabs/FriBuilding/`). Navigator ju iba číta. | Jedna budova, jeden zdroj pravdy. |
| **Živý WebGL build, nie predrenderované videá.** | Priradenie miestností príde z API a môže sa meniť; video by bolo zastarané. |
| **Animácia je čistá funkcia `pose(t)`.** | Pretáčanie a rýchlosť sú potom len nastavenie `t`, nič sa neakumuluje. |
| **Naviguje sa podľa `id` z API.** Označenie a názov sú len na zobrazenie. | `id` identifikuje fyzickú miestnosť, názov sa môže meniť bez buildu. |
| **Nová karta, nie modal / iframe.** | Na fri.uniza.sk stačí `<a target="_blank">`, bez ich JS a CSP dohody; na mobile celá obrazovka; odkaz sa dá zdieľať. Tá istá URL pôjde neskôr aj do iframe. |
| **Len schody, žiadny výťah.** | Zadanie. |
| **Ovládanie prehrávača v HTML, nie v Unity UI.** | Na mobile lepšie ovládateľné, natívny vzhľad. |
| **Bez Unity splashu.** | Od Unity 6 nepovinný aj na Personal. |
| **Musí bežať na mobile.** | Dnešný FriWorld web build na mobile beží — overené. |

---

## 2. Štruktúra

```
Assets/
├── _Game/          ← FriWorld (bez zmeny)
│   └── Prefabs/FriBuilding/   ← ZDIEĽANÉ, Navigator len číta
├── _Navigator/     ← všetko Navigatorove
│   ├── Scenes/Navigator.unity
│   ├── Scripts/    ← asmdef FriWorld.Navigator
│   ├── Editor/     ← asmdef FriWorld.Navigator.Editor, register RoomIds.json
│   ├── Data/       ← upečené kotvy a mapovanie id → miesto (generované)
│   └── Settings/   ← Build Profile, prípadne vlastný quality/URP asset
docs/navigator/     ← tento plán a neskôr navigatorove rozhodnutia
```

Pravidlá oddelenia:

- **Navigator nikdy nezapisuje do zdieľaného prefabu.** Kotvy, NavMesh, otvorené dvere — všetko
  žije v scéne alebo dátach Navigatora. Zápis do `FriBuilding.prefab` je výsadou krokov
  `Routine` FriWorldu.
- **FriWorld o Navigatore nevie.** Žiadna referencia z `_Game/` do `_Navigator/`.
- Pri založení `_Navigator/` treba doplniť `CLAUDE.md` (sekcia Štruktúra projektu hovorí „vlastný
  kód patrí len do `_Game/`").

---

## 3. Pasce, ktoré treba vyriešiť hneď na začiatku

### 3.1 Platform gaty by Navigatoru odstrihli miestnosti

`FeatureFlagBuildProcessor` (`_Game/Scripts/FeatureFlags/Editor/`) beží na **každej** scéne
v builde. Navigator je WebGL, takže z budovy zmiznú všetky `desktopOnly` miestnosti
z `RoomPlatforms.json` — a tie môžu byť cieľom navigácie.

**Riešenie bez zásahu do FriWorldu:** vlastný `IProcessSceneWithReport` v
`FriWorld.Navigator.Editor` s nižším `callbackOrder` (napr. `-100`), ktorý v scéne Navigatora
zničí všetky `PlatformGate` a `ComponentGate` **komponenty** (objekty ponechá). Procesor FriWorldu
potom nemá čo strihať. Obmedziť ho na scény z `_Navigator/`.

### 3.2 Skripty FriWorldu na prefabe bežia aj v Navigatore

Budova nesie `Door`, interaktábly, zvuky… V Navigatore nie je hráč ani `InputManager`.
`Door` s chýbajúcim hráčom počíta, ale ostatné treba prejsť. Ten istý procesor z 3.1 môže
v scéne Navigatora odstrániť všetky `MonoBehaviour` z `_Game/` — Navigator z budovy potrebuje
len geometriu, materiály a svetlá. Menší build ako bonus.

### 3.3 asmdef nemôže referencovať `Assembly-CSharp`

`Door`, `Interactable`, `PlatformGate` nemajú vlastný asmdef, takže ich kód Navigatora
v asmdef-e nevidí. Nevadí:

- dvere a miestnosti sa hľadajú **podľa mena cez register typov** — `FriWorld.ObjectRegistry.Editor`
  má asmdef a `ObjectTypeKey` z neho sa dá použiť v editorovom kroku;
- procesor z 3.1 a 3.2 vie pracovať cez `GetType().FullName` / cestu skriptu, bez typovej referencie.

### 3.4 NavMesh

V prefabe je NavMesh pre NPC (`agentTypeID -334000983`) aj surface s agentom `0`. Navigator
si **upečie vlastný** NavMesh v svojej scéne s vylúčeným výťahom (area / modifier), nech zmena
NPC navmeshu vo FriWorlde nerozbije trasy. Overiť, že poschodia spájajú schodiská.

### 3.5 Build Settings

Build Profile Navigatora má v zozname **len** `Navigator.unity`; FriWorld profily ju nemajú.
Splash (`m_ShowUnitySplashScreen` je dnes `1`) vypnúť cez override v profile Navigatora.

---

## 4. Tok dát

```
fri.uniza.sk  <a href="…/navigate/{id}" target="_blank">
      │
friworld-web  /navigate/[id]
      │  1. server-side: API → názov, označenie (neexistuje → chyba v HTML, Unity sa nesťahuje)
      │  2. vlastný loader: názov miestnosti + progress
      │  3. po štarte: SendMessage("Navigator", "Go", id)
      ▼
Unity  id → miesto (RoomIds) → kotva → NavMesh cesta → spline → stopa kamery → duration
      │  jslib: onReady(duration) · onTime(t) (pár × za s) · onEnded() · onError(kód)
      ▼
HTML ovládanie  Play · Pause · Seek(t) · SetSpeed(x)  → SendMessage
```

API sa nevolá z Unity (žiadny CORS z WebGL, chyby rieši HTML).

---

## 5. Register id

`Assets/_Navigator/Editor/RoomIds.json` — `{ "room": "ra101", "id": "…" }`, `room` je plné meno
kontajnera ako v `RoomPlatforms.json`. Kým API nie je, `id` = kód `raXXX`.

Editorový krok (menu `Navigator → …`) z neho upečie `Data/RoomAnchors.asset`:
`id → kotva pred dverami (pozícia, smer)` a **zvaliduje**:

- `id` bez miestnosti, miestnosť bez `id`,
- kotva, ku ktorej z recepcie nevedie úplná cesta len po schodoch (`PathPartial` / `PathInvalid`).

Dohoda s autorom API: **`id` sa nemení pri premenovaní miestnosti.**

Ak neskôr FriWorld (ktorý bude API používať tiež) potrebuje to isté mapovanie, register sa
presunie do zdieľaného miesta — zatiaľ patrí Navigatoru.

---

## 6. Stopa kamery

1. `NavMesh.CalculatePath(recepcia → kotva)` → rohy.
2. Spline cez rohy (Catmull-Rom alebo balík Splines), prevzorkovať podľa dĺžky oblúka.
3. Profil rýchlosti: ease-in/out, spomalenie v zákrutách podľa zakrivenia. Celková dĺžka
   obmedzená (orientačne 8–15 s) bez ohľadu na vzdialenosť.
4. Kamera „dron" nad a za bodom trasy, pohľad na bod 3–5 m vpred, výška vyhladená zvlášť.
5. **Schodiská:** kamera sa pritiahne k trase (skoro výška očí), inak narazí do stien.
   Najviac ladenia bude tu.
6. Čiara po podlahe (LineRenderer z tej istej spline), odkrýva sa podľa `t`.
7. Záver: zastavenie pred dverami, v zábere tabuľka z `BakedSigns`.
8. Dvere na trase: otvoriť alebo skryť krídla v scéne Navigatora — rozhodnúť vo fáze 4.

Všetko sa počíta **raz** po `Go(id)`. Každý snímok len `camera = pose(t)`, `line = reveal(t)`.

---

## 7. Fázy

| # | Fáza | Hotové, keď |
|---|---|---|
| 1 | Kostra: `_Navigator/`, asmdefy, scéna s budovou, Build Profile, `CLAUDE.md` | WebGL build Navigatora sa zbuildí a ukáže budovu |
| 2 | Scene processor (3.1, 3.2) | V builde sú `desktopOnly` miestnosti a žiadny skript z `_Game/` |
| 3 | Vlastný NavMesh + register id + kotvy + validácia | Validácia hlási 0 nedosiahnuteľných miestností |
| 4 | Stopa kamery `pose(t)` | Dobre vyzerá prízemie, 1. poschodie aj najvyššie |
| 5 | jslib bridge + `/navigate/[id]` vo `friworld-web` s HTML ovládaním | Seek, pauza a rýchlosť fungujú na mobile |
| 6 | Napojenie na API, odkazy na fri.uniza.sk | Klik na fri.uniza.sk otvorí navigáciu |

---

## 8. Otvorené otázky

- Čo presne vráti API a kedy bude? Je `id` stabilné pri premenovaní?
- Kde presne je „recepcia" ako štart — jeden pevný bod, alebo hlavný vchod?
- Dvere na trase — otvoriť, alebo skryť?
- Pod akou doménou pobeží `/navigate` (FriWorld Hub)?
