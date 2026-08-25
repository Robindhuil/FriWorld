# Launcher — implementačný plán

**Verzia projektu pri písaní:** 0.1.1-alpha · **Dátum:** 2026-08-25 · **Stav:** návrh, nezačaté

Samostatná aplikácia, ktorá si sama dotiahne posledný desktop build hry a spustí ho, aby
používateľ nemusel ručne sťahovať nové verzie a mazať staré. Cieľ sú **Windows, Linux
aj macOS**. Launcher sa stiahne z FriWorld Hubu.

Dokument je písaný tak, aby sa podľa neho dalo ísť v inej session bez tohto kontextu.

---

## 0. Čo treba vyriešiť skôr, než sa začne

Tri veci zistené pri prieskume. Prvé dve sú blokujúce.

### 0.1 Chýbajú build moduly

V `6000.4.11f1/Editor/Data/PlaybackEngines/` sú len:

```
WebGLSupport
windowsstandalonesupport
```

Linux ani macOS sa dnes zbuildovať nedajú. Doinštalovať cez Unity Hub → Installs →
Add modules: **Linux Build Support (IL2CPP)** a **Mac Build Support (IL2CPP)**.

### 0.2 macOS sa bez Macu a bez peňazí nedá spraviť poriadne

Unity vie macOS player skrížiť z Windows, ale:

- Gatekeeper nepodpísanú appku blokuje **tvrdšie než Windows SmartScreen** — hlásenie
  „cannot be opened because the developer cannot be verified" a v niektorých verziách
  bez viditeľnej cesty ďalej.
- Notarizácia vyžaduje **Mac** a **Apple Developer Program, 99 $/rok**.
- Bez nej musí používateľ spustiť `xattr -dr com.apple.quarantine /cesta/FriWorld.app`
  z terminálu, prípadne Ctrl+klik → Open.

**Rozhodni skôr, než sa do macOS pustíš:** buď to akceptuješ s návodom v Hube, alebo
macOS z prvej verzie vypadne. Plán ďalej počíta s tým, že sa akceptuje.

### 0.3 Build bude veľký

`Assets/` má **1.2 GB**. Reálny build vyjde rádovo na stovky MB až cez 1 GB **na platformu**.
Dôsledky:

- GitHub Releases majú limit **2 GB na súbor** — zmestí sa, ale nie s veľkou rezervou.
- Tri platformy na release = ~3 GB. GitHub nemá tvrdý strop na súčet, ale je to „fair use".
- **Používateľ pri každej aktualizácii stiahne celý balík.** Delta patchovanie je vo v1
  mimo rozsahu, ale toto je prvá vec, ktorú tam neskôr budeš chcieť.

---

## 1. Architektúra

Dve repozitáre, jeden kontrakt medzi nimi.

```
Robindhuil/FriWorld            hra + build pipeline, vyrába release
        │
        │  manifest.json + archívy ako release assets
        ▼
GitHub Releases (verejné, bez tokenu — overené)
        ▲
        │  HTTPS GET
        │
Robindhuil/FriWorld-Launcher   samostatná Avalonia appka
        │
        │  odkaz na stiahnutie
        ▼
FriWorld Hub (friworld-web)    detekuje OS, ponúkne správny súbor
```

**Prečo samostatné repo:** launcher má vlastnú kadenciu vydávania, vlastné platformy
a nesmie sa zamotať do Unity projektu. Jediné, čo zdieľajú, je tvar manifestu.

**Technológia:** **Avalonia UI + .NET 10**. Jeden kód pre všetky tri OS, C# ako zvyšok
projektu, .NET SDK 10.0.101 je už nainštalovaný. WPF nepripadá do úvahy, je len pre Windows.

---

## 2. Kontrakt: `manifest.json`

Publikuje sa ako asset pri každom release. Launcher číta **len jeho**, nie názvy assetov —
tie sa dajú preklepnúť, manifest nie.

```json
{
  "version": "0.1.2-alpha",
  "released": "2026-08-26T10:00:00Z",
  "notes": "Krátky text do launchera.",
  "platforms": {
    "win-x64": {
      "archive": "FriWorld-0.1.2-alpha-win-x64.zip",
      "url": "https://github.com/Robindhuil/FriWorld/releases/download/v0.1.2-alpha/FriWorld-0.1.2-alpha-win-x64.zip",
      "sha256": "…",
      "size": 812934144,
      "exec": "FriWorld.exe"
    },
    "linux-x64": { "…": "…", "exec": "FriWorld" },
    "osx-arm64": { "…": "…", "exec": "FriWorld.app" }
  }
}
```

Launcher ho berie z `https://api.github.com/repos/Robindhuil/FriWorld/releases/latest`,
z poľa `assets` nájde `manifest.json` a stiahne ho. Bez autentifikácie — repo je verejné.

### Formát archívu je per platforma, nie jeden pre všetky

| platforma | formát | prečo |
|---|---|---|
| Windows | `.zip` | natívne, práva netreba |
| Linux | `.tar.gz` | zip stráca **execute bit** na binárke |
| macOS | `.tar.gz` | `.app` bundle obsahuje **symlinky**, zip ich rozbije |

Toto je typická pasca: zip všade a potom sa hra na Linuxe nespustí, lebo nie je spustiteľná.

### Verzie sa neporovnávajú, len sa zisťuje rozdiel

`bundleVersion` je `0.1.1-alpha`. Triediť predvydania podľa SemVer je zbytočná pasca —
launcher vždy chce to najnovšie. Pravidlo je **„tag v manifeste ≠ lokálne zapísaný tag →
aktualizuj"**. Nič viac.

---

## 3. Rozloženie na disku

| OS | koreň |
|---|---|
| Windows | `%LOCALAPPDATA%\FriWorld\` |
| macOS | `~/Library/Application Support/FriWorld/` |
| Linux | `${XDG_DATA_HOME:-~/.local/share}/FriWorld/` |

```
FriWorld/
  launcher/        samotný launcher
  game/            aktuálna inštalácia
  game.new/        rozbaľuje sa sem, kým sa nedokončí
  game.old/        predošlá, maže sa po úspešnej výmene
  cache/           rozstiahnuté súbory
  installed.json   { version, platform, installedAt }
  launcher.log
```

**Nikdy nie do Program Files** — vyžadovalo by to práva správcu pri každej aktualizácii.

---

## 4. Fázy

### Fáza 1 — build pipeline na strane hry · ~1 deň

Bez nej nemá launcher čo konzumovať. Užitočná aj po Steame.

Editor skript `Assets/_Game/Editor/BuildRelease.cs`, menu `FriWorld → Build → Release`:

1. prečíta `PlayerSettings.bundleVersion`
2. zbuildí tri targety do `Build/<verzia>/<platforma>/`
3. zabalí každý do správneho formátu podľa tabuľky vyššie
4. spočíta SHA256
5. vygeneruje `manifest.json`
6. vypíše prehľad — verzia, veľkosti, hashe

**Hotovo, keď:** jeden klik vyrobí tri archívy a manifest, a `bundleVersion` sa nikde
neprepisuje ručne.

**Nezabudni:** doplniť riadok do `CHANGELOG.md` a dodržať rituál z `CLAUDE.md` — pri zdvihnutí
`bundleVersion` sa `[Unreleased]` premenuje na to číslo.

### Fáza 2 — jadro launchera, bez UI · ~1 deň

Nové repo `FriWorld-Launcher`, konzolová appka, aby sa dala testovať bez okna.

- detekcia platformy → kľúč `win-x64` / `linux-x64` / `osx-arm64`
- stiahnutie manifestu
- porovnanie s `installed.json`
- sťahovanie s progresom a **podporou pokračovania** (HTTP Range)
- overenie SHA256 — **pri nezhode zmaž a skonči chybou**, nikdy nerozbaľuj neoverený archív
- rozbalenie do `game.new/` so zachovaním práv a symlinkov
- **atomická výmena:** `game` → `game.old`, `game.new` → `game`, zmaž `game.old`
- spustenie `exec` z manifestu

**Hotovo, keď:** na čistom stroji stiahne, nainštaluje a spustí hru, a keď to pustíš druhýkrát
bez novej verzie, hru len spustí.

### Fáza 3 — UI · ~1 deň

Avalonia okno. Zámerne málo: názov, **aktuálna verzia**, poznámky z manifestu, progress bar,
tlačidlo Hrať. Stavy: kontrolujem → sťahujem X % → rozbaľujem → pripravené → chyba.

**Hotovo, keď:** človek, čo nikdy nevidel terminál, si hru nainštaluje a spustí.

### Fáza 4 — self-update launchera · ~1 deň

Launcher sa musí vedieť aktualizovať sám. Rieši sa **inak na každom OS**:

- **Windows** — bežiace `.exe` sa nedá prepísať, ale **dá sa premenovať**. Premenuj seba na
  `.old`, zapíš nové na pôvodné miesto, `.old` zmaž pri ďalšom štarte.
- **Linux/macOS** — bežiacu binárku možno odlinkovať a nahradiť; proces beží ďalej zo starého
  inode. Jednoduchšie než na Windows.
- **macOS `.app`** — vymieňa sa celý priečinok bundlu.

**Hotovo, keď:** stará verzia launchera sa sama vymení za novú a používateľ o tom nevie.

### Fáza 5 — distribúcia z Hubu · ~pol dňa

V `friworld-web` stránka na stiahnutie: detekcia OS z `navigator.userAgent`, ponuka správneho
súboru, ostatné pod „iné platformy".

Musí tam byť aj text o prvom spustení:

- **Windows** — SmartScreen ukáže modrú obrazovku **raz**, pri prvom stiahnutí launchera.
  „More info → Run anyway". Ďalšie aktualizácie idú ticho, lebo ich sťahuje launcher, nie
  prehliadač, a značku „z internetu" pridáva prehliadač.
- **macOS** — Ctrl+klik → Open, prípadne príkaz `xattr`.
- **Linux** — `chmod +x` na AppImage.

---

## 5. Pasce podľa platformy

| platforma | pasca | riešenie |
|---|---|---|
| Windows | SmartScreen pri prvom stiahnutí | text v Hube; certifikát netreba |
| Windows | bežiace `.exe` sa nedá prepísať | premenuj seba, nahraď, zmaž pri štarte |
| Windows | Defender skenuje veľký archív dlho | rozbaľuj do `game.new`, nie cez existujúcu inštaláciu |
| macOS | Gatekeeper blokuje nepodpísané | `xattr -dr com.apple.quarantine` + návod |
| macOS | zip rozbije symlinky v `.app` | `.tar.gz` |
| macOS | notarizácia chce Mac + 99 $/rok | mimo rozsahu |
| Linux | zip stráca execute bit | `.tar.gz` |
| Linux | distribúcia launchera | AppImage |
| všetky | prerušené sťahovanie ~1 GB | HTTP Range a pokračovanie |
| všetky | hra beží počas aktualizácie | zisti bežiaci proces, odmietni s hláškou |
| všetky | málo miesta na disku | over voľné miesto **pred** sťahovaním |

---

## 6. Mimo rozsahu v1

- **Delta patchovanie.** Pri ~1 GB na build to bude chýbať skoro hneď, ale v1 to
  neskomplikuj. Pozri `Velopack`, keď na to príde.
- **Podpis a notarizácia.** Stojí peniaze, projekt nezarába.
- **Web build.** Launcher je čisto desktop; web sa aktualizuje sám tým, že sa nahrá.
- **Vetvy a staré verzie.** Vždy len posledný release.
- **Automatické CI buildy.** Fáza 1 je editor skript, nie pipeline. Až keď bude bolieť.

---

## 7. Otvorené, treba rozhodnúť

1. **macOS áno alebo nie v prvej verzii?** Viď 0.2 — bez Macu a bez 99 $/rok to bude
   fungovať len s návodom na obídenie Gatekeepera.
2. **`osx-arm64`, `osx-x64`, alebo univerzálny?** Apple Silicon vs staršie Intel Macy.
   Univerzálny build je väčší, dva samostatné znamenajú štvrtý archív.
3. **Meno a ikona launchera.**
4. **Ostane repo hry verejné?** Celý plán stojí na tom, že GitHub Releases sú dostupné
   bez tokenu. Ak sa repo niekedy zavrie, hosting sa musí riešiť odznova.

---

## 8. Odhad celkom

| fáza | čas |
|---|---|
| 1 — build pipeline | 1 deň |
| 2 — jadro launchera | 1 deň |
| 3 — UI | 1 deň |
| 4 — self-update | 1 deň |
| 5 — Hub | 0.5 dňa |
| **spolu** | **~4.5 dňa** sústredenej roboty |

Plus chvost — prerušené sťahovanie, chybové stavy, testovanie na troch OS. Realisticky
**týždeň až dva** do stavu, ktorý dáš cudziemu človeku.

**Steam to celé nahradí.** Steam robí aktualizácie, delta patche aj verzie sám. Launcher je
jednorazovka na obdobie pred ním, tak doňho neinvestuj viac, než je nutné.
