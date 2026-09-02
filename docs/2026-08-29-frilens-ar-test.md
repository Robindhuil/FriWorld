# FriLens — čo treba spraviť

**Verzia projektu pri písaní:** 0.1.2-alpha (FriWorld) · **Dátum:** 2026-08-29 · **Stav:** návrh, nezačaté

Samostatný Unity projekt, oddelený od `FriWorld`. Jediná otázka, na ktorú má odpovedať:
**ako presne sa upečený navmesh premietne do skutočnej fakulty a ako rýchlo to odchádza,
keď sa človek prejde.**

Nie navigačná appka. Nie prekryv miestností. Jedna značka, jedna plocha, vlastné oči.

Dokument je písaný tak, aby sa podľa neho dalo ísť bez tohto kontextu.

---

## 0. Bez čoho sa nedá začať

- [ ] **Android Build Support** do Unity Hub → Installs → Add modules, aj s SDK, NDK a OpenJDK.
      V `6000.4.11f1/Editor/Data/PlaybackEngines/` sú dnes len `WebGLSupport`
      a `windowsstandalonesupport` — Android build sa nedá spraviť vôbec.
- [ ] **Telefón s podporou ARCore.** Over ho v Googlom zozname podporovaných zariadení;
      nie je to samozrejmosť ani pri nových kusoch.
- [ ] Na telefóne zapnúť vývojárske možnosti a **USB ladenie**.

iOS by som nechal tak — potrebuje Mac a Apple Developer Program za 99 $ ročne. Na Android
stačí kábel.

---

## 1. Projekt

- [ ] Nový Unity projekt **6000.4.11f1**, rovnaká verzia ako FriWorld — nech sa nemusí riešiť,
      či sa niečo správa inak.
- [ ] Šablóna **AR**, ak je v Hube ponúkaná. Príde s nainštalovaným AR Foundation aj ARCore
      pluginom, zapnutým XR Plug-in Managementom a scénou, v ktorej už sedí `AR Session`
      a `XR Origin`. Vynechá tým skoro celý krok 2 a väčšinu kroku 3 — a hlavne tie nastavenia,
      na ktorých sa dá ticho pomýliť. V novších Unity je postavená na URP, takže zhoda
      s FriWorldom zostáva.

      Ak ju v Hube nevidíš (môže sa objaviť až po doinštalovaní Android modulu), vezmi
      **Universal 3D (URP)** a balíčky doplň ručne. Built-in pipeline by tiež stačil — kreslí
      sa jedna unlit plocha — ale je to legacy cesta a rozchádza sa s FriWorldom.
- [ ] Vlastný git repozitár, `frilens`.

## 2. Balíčky

S AR šablónou už väčšina sedí; over a doplň.

- [ ] **AR Foundation**
- [ ] **Google ARCore XR Plugin**
- [ ] **AI Navigation** (`com.unity.ai.navigation`) — `NavMeshSurface` je v Unity 6 v balíčku,
      nie v jadre. **Toto AR šablóna neprinesie**, doplň vždy.
- [ ] Project Settings → XR Plug-in Management → Android → zapnúť **ARCore**

## 3. Player settings pre Android

- [ ] **Minimum API Level 24** a vyššie — ARCore beží od Androidu 7.0
- [ ] Graphics API: nechať **OpenGLES3**, Vulkan zatiaľ vyhodiť. Novšie ARCore ho zvláda, ale na
      test netreba riskovať ovládačové prekvapenia
- [ ] Scripting backend **IL2CPP**, target architecture **ARM64**
- [ ] ARCore settings → **AR Required**

---

## 4. Navmesh

- [ ] Naimportovať model zo skenu.
- [ ] **Upiecť navmesh s `agentRadius` okolo 0.01.**

      Toto je najdôležitejší riadok v celom dokumente. Vo FriWorlde je navmesh pečený
      s polomerom **0.2**, takže je **odsadený 20 cm od každej steny** — agent so svojím
      polomerom by sa tam nezmestil. Keby si taký premietol do AR a porovnal jeho hranu
      s lištou pri stene, videl by si všade dvadsaťcentimetrovú medzeru a čítal by si to ako
      chybu modelu. Pri polomere blízkom nule hrana sadne na stenu a máš ostrú referenciu.

- [ ] Editor skriptom vyrobiť z navmeshu **obyčajný mesh asset**: `NavMesh.CalculateTriangulation()`
      vráti vrcholy a indexy, z toho sa poskladá `Mesh` a uloží.
- [ ] **Orezať na jedno podlažie.** Navmesh pokrýva všetky naraz a v AR by sa prekrývali.
- [ ] Model zo skenu do buildu **nemusí ísť** — v AR sa budova nekreslí, skutočná je render.
      Ide tam len ten jeden mesh. (Model bude treba až pri okluzii, čo do tohto testu nepatrí.)

## 5. Značka

- [ ] Vybrať miesto, ktoré vieš v modeli presne lokalizovať — roh miestnosti, zárubňa, roh schodiska.
- [ ] Vyrobiť značku s dostatkom detailu, vytlačiť **matne** (lesk rozbíja rozpoznávanie),
      nalepiť naplocho na tvrdý podklad.
- [ ] **Odmerať vytlačenú značku pravítkom** a ten rozmer zadať do Reference Image Library.
      Nie rozmer, ktorý si poslal do tlače — tlačiarne škálujú. **Chyba 5 % v rozmere značky
      je chyba 5 % v mierke celého prekryvu**, čo je na 40-metrovej chodbe dva metre.
- [ ] Zistiť **pozíciu aj rotáciu** značky v modeli. Toto je najzdĺhavejšia časť a rozhoduje
      o presnosti všetkého ostatného.

## 6. Zosúladenie

- [ ] Do modelu umiestniť prázdny objekt presne na pózu značky.
- [ ] Pri rozpoznaní obrázka posunúť koreň modelu tak, aby sa tento objekt kryl so sledovaným
      obrázkom. Model sa dá jednoducho zavesiť pod anchor a posunúť o inverznú lokálnu pózu.
- [ ] **Tlačidlo na opätovné zosúladenie** — budeš ho chcieť v teréne používať často.

## 7. Vykreslenie

- [ ] Navmesh mesh, **unlit, polopriehľadný**, obojstranný.
- [ ] Posadiť **2–3 cm nad podlahu**, inak bude blikať proti nej.
- [ ] Farba s kontrastom voči skutočnej podlahe fakulty.

## 8. Diagnostika na obrazovke

Bez tohto sa z testu stane „vyzerá to trochu mimo" namiesto čísla.

- [ ] Stav trackingu z `ARSession`
- [ ] Čas od posledného rozpoznania značky
- [ ] **Prejdená vzdialenosť od zosúladenia** — integrovaná z pozície AR kamery
- [ ] Tlačidlo re-anchor a tlačidlo na skrytie prekryvu (nech vidíš, čo je pod ním)

---

## 9. Samotný test

- [ ] Zosúladiť pri značke a **hneď pozrieť zblízka** — to je chyba modelu a značky, bez driftu.
- [ ] Prejsť známu trasu a pozerať na hranu navmeshu pri stene po **10, 25, 50 a 100 metroch**.
- [ ] Fotiť cez appku, nie spamäti.
- [ ] Na konci sa vrátiť k značke, zosúladiť znova a pozrieť, či to skočí späť.

## 10. Ako čítať výsledok

Toto je celý zmysel testu. Chyba nie je jedno číslo — podľa toho, **ako** je nesprávna, vieš,
kde je príčina:

| čo vidíš | príčina |
|---|---|
| chyba už pri značke, konštantná | pozícia alebo rotácia značky v modeli je zle určená |
| chyba **rastie s dĺžkou chodby**, prekryv sa „rozťahuje" | zlá mierka — rozmer značky alebo model nie je 1:1 |
| prekryv je **pootočený** a odchýlka rastie so vzdialenosťou | rotácia značky, alebo VIO drift v yaw |
| chyba **rastie s prejdenou vzdialenosťou**, tvar sedí | bežný VIO drift — očakávaj 1–2 % prejdenej dráhy |

Posledný riadok je normálny a nedá sa odstrániť, len opravovať ďalšími značkami. Prvé tri sú
chyby, ktoré sa dajú spraviť lepšie.

---

## Čo tento test zámerne nerieši

- Okluziu — nič sa neschováva za skutočné steny.
- Navigáciu, miestnosti, prekryv dát z `Rooms.json`.
- Viac podlaží a prechod medzi nimi.
- Správanie pri bežnom používaní — telefón vo vrecku, prechod cez dav.
- iOS.

Ak vyjde, ďalší krok je viac značiek a meranie, ako často treba opravovať drift. Ak nevyjde,
tabuľka vyššie povie prečo, a to je viac než dosť na jeden víkend.
