# Zárubne sú `noObstacle`, hoci pôvodné pravidlo hovorilo `obstacle`

## Kontext

`door_frame` a `door_frame_<int>_glass` mali v `ObjectTypes.json` od seedu vrstvu
`interactable`. Dôsledok: 302 zárubní dostalo tag `Door` (odvádzal sa z vrstvy) a sedelo na
vrstve `Interactable`, hoci nenesú žiadne správanie dverí. Zároveň boli ako `interactable`
nestatické, takže sa nezapekali do lightmapy.

História toho čísla je poučná. Pred registrom hovorili keyword pravidlá niečo iné:

```csharp
InteractableKeywords = { "door", "door_slide" };
ObstacleKeywords     = { "door_frame", "thick_door", "foor", "wall", ... };
```

Zámer bol **`obstacle`**. Nikdy sa nepoužil — `door` tienil `door_frame`, takže zárubne
reálne padali na Interactable. Seed registra to zdedil a commit, ktorý ho vyrobil, to sám
zapísal: *„Seeding and diffing both run the same keyword rules… The registry has inherited
the old decisions, including the wrong ones."* Dry run ukázal 0 zmien práve preto, že obe
strany porovnania púšťali tie isté chybné pravidlá.

Takže `obstacle` bolo napísané, ale nikdy nebežalo.

## Rozhodnutie

**`noObstacle`**, teda tretia hodnota — nie tá, čo bola v registri, ani tá, čo mala byť
podľa pôvodného zámeru.

`obstacle` pripína `NavMeshModifier` s oblasťou `Not Walkable`. Robil to aj predregistrový
nástroj, takže keby pravidlo niekedy zabralo, prejavilo by sa to. Zmerané:

| | |
|---|---|
| `door_frame` rendererov | 302 |
| z toho stojacich nad pochôdznym navmeshom | **291** |
| rozmer zárubne, medián | 0.89 × 2.05 × 0.21 m |

Zárubňa je široká presne ako priechod a navmesh cez ňu vedie. `obstacle` by vyrezal
`Not Walkable` cez 291 dverí a NPC by cez ne neprešli.

`noObstacle` dá zárubniam statickosť (a teda lightmapu), zoberie im vrstvu `Interactable`
aj tag `Door`, a navmeshu sa nedotkne — overené, `NavMeshModifierAdded: 0`.

## Dôsledky

- Zárubne prestali byť dynamické. 302 objektov sa pri najbližšom peku prvýkrát zapečie do
  lightmapy namiesto brania svetla z light probov.
- Ak by sa niekedy `obstacle` znova zvažovalo, treba najprv preveriť, či sa medzitým
  nezmenil model zárubne. Dôvod proti nie je „zárubňa nie je prekážka" — je, a collider
  `mesh` jej ostáva — ale to, že jej **geometria vypĺňa priechod**, takže carve navmeshu
  by zavrel dvere pre NPC.
- Tag už na vrstve nezávisí, rozhoduje `script`. Viď commit `fix(registry): the Door tag
  follows the script field, not the layer`.
