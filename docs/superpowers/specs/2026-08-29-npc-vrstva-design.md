# NPC vrstva — návrh

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-08-29 · **Stav:** návrh, nezačaté

Nový typ NPC pre generovaných študentov, ktorí chodia po bodoch v budove. Správanie sa
nemení; mení sa tvar, aby doň neskôr šlo zapojiť agentovú simuláciu bez prepisovania.

**Toto nie je agentová simulácia.** Rozvrhy, cvičenia, obedy a rezervácia stoličiek do tejto
verzie nepatria. Sú tu spomenuté len ako záťažový test návrhu — ak sa doň nedajú zapojiť,
návrh je zlý.

---

## 1. Kontext

Dnešná NPC vrstva sú dve cesty vedľa seba. Menovaní učitelia bežia na `Npc` + `StateMachine`
+ `WonderState`; generovaní študenti na `CharacterNpcSpawner` + `NpcWander`, ktoré vznikli
pred dvoma dňami ako dočasný kus, aby sa dalo overiť, že generovanie postáv funguje.

Prečo sa stará vrstva neprevzala, je zapísané v
[`docs/2026-08-28-npc-skripty-na-prerobenie.md`](../../2026-08-28-npc-skripty-na-prerobenie.md).
Skrátene: `Npc` je jeden objekt na štyri veci naraz, v `Start` hľadá tri závislosti v scéne,
a `WonderState` píše každý snímok do `Animator`a, ktorý generovaná postava nemá.

**Rozsah tejto zmeny:** len chodiaca kulisa. Menovaní učitelia zostávajú na starom `Npc`,
kým sa nebude prepisovať dialóg. Nič sa nemaže.

---

## 2. Jediná vec, ktorá musí platiť

**Telo NPC nesmie rozhodovať, kam ide.** Je pasívne a riadené zvonku.

Dnes ho riadi komponent s waypointmi. Neskôr ho bude riadiť simulácia, ktorá tiká stovky
študentov ako riadky v tabuľke a telo dá len tým v okolí hráča. Ak je telo pasívne, je to
výmena riadiča. Ak si telo tiká samo — ako dnes `NpcWander` aj `WonderState` — je to prepis.

Všetko ostatné v tomto dokumente je z toho odvodené.

---

## 3. Štyri kusy

| kus | zodpovednosť |
|---|---|
| `NpcActor` | pasívne telo: `GoTo(point)`, `Stop()`, hlási `Activity` a `HasArrived` |
| `INpcDirector` | povie telu, kam ďalej |
| `NpcActivity` | enum toho, čo NPC práve robí |
| `AmbientNpcSpawner` | vyrobí telo z katalógu postáv a pripne mu riadiča |

`WaypointDirector` je jediná implementácia riadiča v tejto verzii — berie `PathWay`, vyberá
bod, čaká po príchode. Je to dnešný `NpcWander` bez pohybovej časti.

### `NpcActor`

Obaľuje `NavMeshAgent` a **nič nerozhoduje**. Drží bočný odklon od hrany navmeshu prevzatý
z `WonderState` — bez neho NPC chodia po stene namiesto stredom chodby a je to jediná časť
starej vrstvy, ktorá stojí za zachovanie.

Nesie aj to, čo dnes robí `NpcWander.Cycle` zle a čo sa opravilo pri review: **cesta, ktorá
nie je `PathComplete`, sa počíta ako príchod.** Inak NPC na nedosiahnuteľnom bode zamrzne
navždy, lebo neúplná cesta necháva `remainingDistance` na `Infinity`.

### `NpcActivity`

```csharp
enum NpcActivity { Idle, Walking }
```

Dnes dve hodnoty a **nikto ich nečíta**. Je to zámerne: je to seam pre animácie.

AI nikdy nesmie volať `Animator.SetBool`. Presne to dnes robí `WonderState` a presne preto
by na generovanej postave bez animátora hádzalo `NullReferenceException` v každom snímku.
Telo publikuje **zámer**, samostatný komponent ho neskôr preloží do parametrov animátora.
Vďaka tomu postava bez animátora beží ďalej, graf sa dá prerobiť bez zásahu do AI, a kulisa
môže mať lacnejší graf než NPC, s ktorým sa dá hovoriť.

Zaviesť ten seam teraz je pár riadkov. Dodatočne sa dotkne všetkého.

**Deväť `AnimatorController`ov v `Assets/3Dmodels/` sa v tejto zmene nedotýkame.** Že sa
prehadzuje `runtimeAnimatorController` namiesto prechodov medzi stavmi, je vec na neskôr,
spolu s animáciami.

### `INpcDirector`

```csharp
interface INpcDirector
{
    bool TryGetNext(NpcActor actor, out Vector3 destination);
}
```

Jedna metóda. Telo sa spýta, keď dorazí; riadič povie kam, alebo že nikam. Simulácia neskôr
implementuje to isté rozhranie a jej odpoveď príde z rozvrhu namiesto zo zoznamu bodov.

---

## 4. Ako sa doň zapojí simulácia

Toto je jediný dôvod, prečo sa refaktor robí teraz, takže nech je napísané, čo sa vtedy
zmení a čo nie.

**Nezmení sa:** `NpcActor`, `NpcActivity`, `INpcDirector`, spawner postáv z katalógu.

**Pribudne:** `ScheduleDirector`, ktorý namiesto `PathWay` číta rozvrh; hrubý navigačný graf
miestností pre tých, čo nie sú vidieť; a materializácia — telo len pre NPC v okolí hráča.

**Jedna vec do toho zapadá zadarmo:** vzhľad sa už teraz losuje deterministicky zo seedu
naviazaného na identitu. Študent, ktorý zmizne za rohom a o pol hodiny sa objaví pri jedálni,
bude vyzerať rovnako a **nič sa preto nemusí ukladať**. Presne to materializácia potrebuje.

**Čo sa tým nerieši a vtedy to bude treba:** stav prestane patriť telu a presunie sa do
tabuľky simulácie, takže `AmbientNpcSpawner` sa zmení z „drž nažive N kusov" na „drž
viditeľných v súlade so simuláciou".

---

## 5. Rozloženie

```
Assets/_Game/Scripts/Npc/          FriWorld.Npc          runtime
Assets/_Game/Editor/Npc/Tests/     FriWorld.Npc.Tests    EditMode testy
```

Vlastný asmdef z rovnakého dôvodu ako `FriWorld.Character`: **asmdef nevie referencovať
`Assembly-CSharp`**, takže testy sa dajú písať len proti kódu, ktorý v jednom je.

Z toho zároveň plynie, prečo interaktívne NPC v tejto zmene nie sú. `NodeDialogueManager`,
`QuestManager` aj `GameState` sedia v `Assembly-CSharp`; potiahnuť ich sem by z refaktoru NPC
spravilo refaktor polovice hry.

`PathWay` zostáva ako je — je to zoznam bodov s gizmom a nová vrstva ho používa nezmenený.

---

## 6. Čo sa nahradí a čo zostane

| | |
|---|---|
| `NpcWander` | nahradí `NpcActor` + `WaypointDirector` |
| `CharacterNpcSpawner` | nahradí `AmbientNpcSpawner` |
| `NPCSpawner`, `Npc`, `StateMachine`, `WonderState`, `IdleState`, `DialogueState` | **zostávajú nedotknuté** |
| `PathWay`, `NavMeshCenteringUtility` | zostávajú, používajú sa |

Nič sa nemaže. Staré prefaby, `student_*.fbx` ani starý spawner v `Demo.unity` sa v tejto
zmene neriešia.

---

## 7. Čo do tejto verzie nepatrí

- Rozvrhy, cvičenia, obedy, rezervácia stoličiek — celá agentová simulácia.
- Animácie a ich stavový stroj. Seam áno, graf nie.
- Dialóg a questy na novom type.
- `NavMeshLink` na dvere a schody.
- Materializácia podľa vzdialenosti od hráča.
