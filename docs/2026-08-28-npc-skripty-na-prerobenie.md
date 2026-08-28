# NPC skripty — čo sa bude prerábať

**Verzia projektu pri písaní:** 0.1.2-alpha · **Dátum:** 2026-08-28 · **Stav:** čiastočne spravené 2026-08-29 — viď tabuľku nižšie

Poznámka, aby sa nezabudlo, čo je dočasné a prečo. **Teraz sa na tom nepracuje.**

Prechod na generované postavy vyžiadal nový spawner, a ten obišiel celú existujúcu NPC
vrstvu — nie preto, že je zlá, ale preto, že chodiace NPC potrebovali fungovať skôr, než sa
tá vrstva prepíše. Výsledok je, že v repozitári teraz stoja dve NPC cesty vedľa seba a jedna
z nich je slepá.

---

## Čo beží dnes

| | |
|---|---|
| `AmbientNpcSpawner` + `NpcActor` + `WaypointDirector` | **nové**, v `FriWorld.Crowd`. Telo je pasívne, riadič mu hovorí kam |
| `CharacterNpcSpawner` + `NpcWander` | **vypnuté**, nahradilo ich to vyššie |
| `NPCSpawner` | **vypnutý** v `Demo.unity`, ostáva do overenia nového |
| `Npc`, `StateMachine`, `BaseState`, `IdleState`, `WonderState`, `DialogueState` | **nepoužité** generovanými NPC, stále ich používajú ručné prefaby menovaných učiteľov |

Generované NPC teda **nevedia hovoriť, nemajú questy a nemajú animácie**. Vedia chodiť po
`WanderPath` a to je celý ich repertoár.

## Prečo sa stará vrstva neprevzala

- **`Npc` je jeden objekt na štyri veci naraz** — pohyb, dialóg, questy a stav. Generované NPC
  potrebovali z toho jednu. Zapojiť celý `Npc` by znamenalo dať každému chodiacemu študentovi
  `NodeDialogueManager`, `QuestComplete` a `TextAsset` s dialógom, ktoré nikdy nepoužije.
- **`Npc.Start` volá `FindFirstObjectByType<Player>()`, `GameState.Instance` a `QuestManager`.**
  Pri dvadsiatich spawnoch je to dvadsať prehľadaní scény a tvrdá závislosť na tom, že tie tri
  veci v scéne sú.
- **`WonderState` píše priamo do `Animator`a** (`SetBool("IsWalking", …)`). Generovaná postava
  animátor zatiaľ nemá, takže by to bola `NullReferenceException` v každom snímku.
- **`StateMachine.Initialise` natvrdo vyrába `WonderState`**, takže „NPC, ktoré len stojí"
  alebo „NPC, ktoré sedí" sa nedá poskladať bez zásahu do samotného stroja.

## Čo sa s tým má spraviť

Návrh, nie rozhodnutie — rozhodne sa, až keď na to dôjde:

1. **Rozbiť `Npc` na kusy.** Pohyb, dialóg a questy ako samostatné komponenty, ktoré si NPC
   berie podľa toho, čím je. Chodiaci študent má pohyb; učiteľ má všetky tri.
2. **Závislosti podávať, nehľadať.** `Player`, `GameState` a `QuestManager` nech prídu zvonku
   pri spawne, nie cez `FindFirstObjectByType` v `Start`.
3. **Animátor oddeliť od stavu.** Stav hovorí „idem", nie „nastav mi bool". Inak sa NPC bez
   animátora nedá spustiť — presne to, o čo sme teraz zakopli.
4. **`NpcWander` je zámerne hlúpy** a má taký zostať, kým nebude jasné, čo od chodenia
   chceme. Nesie waypointy, čakanie a bočný odklon od steny prevzatý z `WonderState`, lebo bez
   neho sa NPC lepia na múry.
5. **`NPCSpawner` zmazať**, keď `CharacterNpcSpawner` odbehne dosť dlho bez prekvapení.
   S ním padnú aj prefaby v `Assets/_Game/Prefabs/npcSpawn/` a modely `student_*.fbx`.

## Čo sa nemá stratiť

`WonderState` obsahuje **bočný odklon od hrany navmeshu** — bez neho NPC chodia po stene
namiesto stredom chodby. Je prevzatý do `NpcWander` aj s konštantami. Keď sa stará vrstva
bude mazať, toto je jediná časť, ktorá stojí za zachovanie.

Rovnako `PathWay` zostáva — je to len zoznam bodov s gizmom a nová vrstva ho používa
nezmenený.
