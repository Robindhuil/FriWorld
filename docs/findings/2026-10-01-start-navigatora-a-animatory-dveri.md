# Čo stojí štart Navigatora a čo stoja dvere každý snímok

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-01 · **Stav:** spravené v 0.1.2-alpha — Navigator
za behu vypína skript `Door` aj animátory dverí mimo trasy (výsledok na konci)

## Čo sa zmeralo

Profiler, play mode v editore, scéna `FriNavigator`, let RA323 — **nie WebGL build**.

Prvý snímok po stlačení Play trvá 3,3 s. Väčšinu tvorí editor sám a nebude vo builde:

| časť prvého snímku | čas |
|---|---|
| `EditorLoop` — vstup do play mode | 1 985 ms |
| prvé vykreslenie, skoro celé `Mono.JIT` kódu URP | 827 ms |
| `Door.Start` × 284 | 112 ms |
| — z toho 284× varovanie „GlobalSoundEffectRegistry nebol najdeny v scene" | 50 ms |
| — z toho `AddComponent` × 852 pre detektory NPC | 23 ms |
| prvé vykreslenie tabuliek miestností (`Canvas`) | 65 ms |
| `NavigatorController.Start` (`Go`, s JIT) | 40 ms |

Ďalšie snímky majú 70–150 ms `EditorLoop` a skoro žiadny `PlayerLoop`. Kamera sa v prvej
sekunde takmer nehýbe (rozbeh 1,5 s), takže pomalé snímky ju neposunú viditeľným skokom.

Ustálený let, priemer 301 snímok: `PlayerLoop` 8,7 ms, z toho vykreslenie 4,0 ms a **animátory
2,6 ms** — 284 `Animator`ov dverí má `Culling Mode: Always Animate`, takže sa vyhodnocujú
každý snímok, aj zatvorené a mimo obrazu. `Door.Update` × 284 stojí 0,1 ms, Navigator 0,03 ms.

## Čo by sa dalo spraviť

- **Vypnúť skript `Door` v Navigatore** pred jeho `Start` (za behu, asset sa nemení). Navigator
  hýbe `Animator`om sám, `Door` tam nemá hráča ani NPC. Ušetrí 112 ms na štarte a 284
  varovaní v konzole — aj v konzole prehliadača vo web builde.
- **Animátory dverí na `Cull Completely`** na prefaboch dverí, čo pomôže aj FriWorldu; alebo
  v Navigatore vypnúť animátory dverí, ktoré nie sú na trase letu. Ušetrí väčšinu z 2,6 ms
  každý snímok.
- Vo web builde bude chýbať JIT aj editor, ale WebGL prekladá shader pri prvom vykreslení —
  zmerať v kroku 2 plánu, či nie je treba warmup.

## Výsledok

Spravené za behu v `NavigatorDoors` (Robin, 2026-10-01): skript `Door` sa vypne v `Awake`, ešte
pred jeho `Start`, a animátory bežia len na dverách aktuálneho letu. Prefab dverí ostal
nezmenený; `Cull Completely` sa nerobil. Tým istým meraním:

| | predtým | potom |
|---|---|---|
| `Door.Start` na štarte | 112 ms, 284× | 0 |
| varovania v konzole na štarte | 284 | 0 |
| `PlayerLoop` počas letu | 8,7 ms | 4,7 ms |
| animátory počas letu | 2,6 ms | 0,2 ms |

Štart stále ťahá editor (vstup do play mode a JIT) a `Go` so svojimi ~40 ms.
