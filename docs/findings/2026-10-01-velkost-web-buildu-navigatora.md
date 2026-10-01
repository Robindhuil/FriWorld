# Čo je vo web builde Navigatora a čo tam nemusí byť

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-01 · **Stav:** čiastočne — videá z výstupu maže
`Navigator → Build Web`; obsah `Resources` a statický batching čakajú na rozhodnutie

## Čo sa zmeralo

Prvý build profilom `NavigatorWeb` (kompresia vypnutá, ako pri FriWorlde): výstup **360 MB**.

| časť | veľkosť |
|---|---|
| `StreamingAssets/videos` | 202 MB |
| `Build/Web.data` | 119 MB |
| `Build/Web.wasm` | 45 MB |

Videá sú pre tabule `MemorySign` a prehrávač v UI hráča. Navigator scéna nemá ani jeden
`VideoPlayer` ani `MemorySign`. `StreamingAssets` ide do každého buildu celý a build profil z neho
nevie vynechať priečinok — preto ich `Build Web` po builde z výstupu zmaže (projekt sa nemení).
Po tom má výstup **157 MB**.

Build report, nekomprimované užívateľské assety 136,7 MB: meshe 56,9 MB (z toho budova
`fri_building.blend` 40,8 MB), textúry 33,9 MB, zvuky 28,5 MB, ostatné 15,3 MB.

## Čo Navigator nepotrebuje a prečo tam je

Všetko z priečinkov `Resources` ide do **každého** buildu bez ohľadu na scény:

| čo | veľkosť | prečo je v builde |
|---|---|---|
| `Assets/Resources/sounds/music/*.mp3` | ~28 MB | `Resources` |
| `character_male.fbx` | 14 MB | `character_male.prefab` cez `Resources` |
| compute shadery balíka `com.unity.ai.inference` (Sentis) | ~7,5 MB | `Resources` v balíku |
| `Resources/secrets`, `ideImages`, `title.png` | ~5 MB | `Resources` |
| `TextMesh Pro/Examples & Extras/Resources` | ~3 MB | `Resources` |
| NavMesh dáta Dema (`NavMesh-PlayerNav`, `NpcNav`) | 1,5 MB | odkaz z `FriBuilding/NavMesh`, ktorý je v Navigatore vypnutý |

Spolu približne **59 MB** zo 137 MB dát.

## Čo by sa dalo spraviť

- **FriWorld presunie hudbu a ostatné z `Resources`** a bude ich načítavať cez odkazy alebo
  Addressables. Najčistejšie a zmenší aj web build FriWorldu, ale je to zásah do FriWorldu.
- **Build Navigatora dočasne skryje tieto priečinky** (premenovaním na `…~`, ktoré Unity
  ignoruje) a po builde ich vráti. Rýchle, ale krehké: pád buildu by ich nechal skryté aj pre
  FriWorld.
- **Odstrániť balík `com.unity.ai.inference`**, ak ho za behu nič nepoužíva — ušetrí ~7,5 MB
  v každom builde, je to však rozhodnutie o celom projekte.
- **Vypnúť statický batching v profile `NavigatorWeb`** (Player Settings profilu, WebGL):
  výstup 157 → 127 MB, lebo budova sa do scény už neukladá druhýkrát ako zlúčené meshe.
  Zmerané na builde, ktorému ešte chýbali varianty Lit (tie pridali 0,1 MB, rozdiel teda
  platí). Čo to urobí s plynulosťou letu, sa nemeralo, preto profil zostal ako FriWorld.
- Prenesená veľkosť bude menšia než tieto čísla — server komprimuje. Zmerať, až pobeží na Hube.
