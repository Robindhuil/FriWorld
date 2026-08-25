# Zárubne patria na `obstacle`, lebo navmesh zbiera len tú vrstvu

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-08-25

## Kontext

`door_frame` a `door_frame_<int>_glass` mali v `ObjectTypes.json` od seedu vrstvu
`interactable`. Dôsledok: 302 zárubní dostalo tag `Door` (odvádzal sa z vrstvy) a sedelo na
vrstve `Interactable`, hoci nenesú žiadne správanie dverí. Ako `interactable` boli navyše
nestatické, takže sa nezapekali do lightmapy.

Pred registrom hovorili keyword pravidlá niečo iné:

```csharp
InteractableKeywords = { "door", "door_slide" };
ObstacleKeywords     = { "door_frame", "thick_door", "foor", "wall", ... };
```

Zámer bol `obstacle`. Nikdy sa nepoužil — `door` tienil `door_frame`, takže zárubne reálne
padali na Interactable. Seed registra to zdedil a commit, ktorý ho vyrobil, to sám zapísal:
*„Seeding and diffing both run the same keyword rules… The registry has inherited the old
decisions, including the wrong ones."* Dry run ukázal 0 zmien práve preto, že obe strany
porovnania púšťali tie isté chybné pravidlá.

## Rozhodnutie

**`obstacle`**, teda pôvodný zámer.

Dôvod je v nastavení navmesh povrchov:

```
PlayerNav  layerMask = Obstacle, Nav
NpcNav     layerMask = Obstacle, Nav
```

Do navmeshu sa zbiera **len** geometria na `Obstacle` alebo `Nav`. Zárubňa je pevná
prekážka, ktorú má navmesh vidieť, takže inam nepatrí. Na `interactable` ani `noObstacle`
by pre navmesh neexistovala.

## Čo sa cestou ukázalo ako nepravda

Medzitým boli zárubne krátko na `noObstacle` s odôvodnením, že `obstacle` pripína
`NavMeshModifier` s `Not Walkable` a vyrezal by 291 z 302 priechodov. **To odôvodnenie
neplatilo** — meralo prekryv *bounding boxov*, nie geometrie. Zárubňa je len ostenie:

```
zo 40 vzorkovaných zárubní
   geometria prekrýva priechod pri podlahe (prah):   0
   len ostenia, stred priechodu prázdny:            40
```

Carve teda oreže ostenia, nie priechod. Agent má polomer 0.2 a najužší priechod 0.50 m.

## Dôsledky

- Statickosť je rovnaká pri `obstacle` aj `noObstacle` (`targetStatic = true`); nestatické
  bolo len `interactable`. Zárubne sa teda pri najbližšom peku prvýkrát zapečú do lightmapy
  a prestanú brať svetlo z light probov — nezávisle od toho, ktorá z tých dvoch vrstiev sa
  zvolí.
- Kolízie na vrstve nezávisia. Matica kolízií je plne priepustná a zárubne majú `collider:
  mesh`, takže hráč do nich narazí na ktorejkoľvek vrstve.
- `PlayerInteract.mask = Interactable, Obstacle`, takže interakčný raycast na zárubniach
  zastaví. Prompt sa neukáže — nemajú `Interactable` komponent — ale cez zárubňu sa nedá
  interagovať s tým za ňou.
- Tag už na vrstve nezávisí, rozhoduje `script`. Viď commit `fix(registry): the Door tag
  follows the script field, not the layer`.
