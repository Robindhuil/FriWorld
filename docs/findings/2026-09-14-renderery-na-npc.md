# Koľko rendererov stojí jedno generované NPC

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-14 · **Stav:** nespravené — výkon NPC na webe je odložený mimo 0.1.2-alpha

## Čo sa zmeralo

50 seedov, `character_male.prefab` po `CharacterBuilder.Apply`, v editore — **nie vo WebGL
builde**.

| na jedno NPC | min | priemer | max |
|---|---|---|---|
| `SkinnedMeshRenderer` | 13 | 16,2 | 19 |
| trojuholníky | 7 130 | 8 130 | 9 414 |
| materiálové sloty | | 19,1 | |
| renderery s blend shapes | | 7,0 | |

Prefab pred orezaním má 53 rendererov, každý mesh 98 kostí. V `Demo.unity` sú dva
`AmbientNpcSpawner`y po `maxActiveNPCs = 20`, teda **až 40 NPC**: okolo 650 skinned
rendererov, 325 000 trojuholníkov a 760 materiálových slotov naraz.

## Prečo nie osem, ako rátal spec

Spec (`docs/superpowers/specs/2026-08-28-character-customization-design.md`, kap. 9) počítal
štyri kusy kože a štyri kusy oblečenia. Nepočítal s hlavou: `face_1` plus obočie, brada, pehy,
oči a vlasy je šesť rendererov na každej postave, lebo každý overlay je trieda a vždy sa z nej
vyberie práve jeden preset.

Z toho sú tri presety „nič" — trojuholník nulovej plochy, ale plnohodnotný renderer:
`freckle_none_1` prežil v 44 z 50 postáv, `beard_none_1` v 26, `hair_none_1` v 3. Priemerne
**1,5 renderera na NPC, ktoré nič nekreslia**.

## Čo by sa dalo spraviť

1. **Zahodiť presety „nič" po výbere.** Najlacnejšie. `CharacterScan` ich v modeli potrebuje,
   lebo indexuje len objekty s rendererom, ale v hre po `Apply` nemajú čo robiť. Pri 40 NPC je
   to okolo 60 rendererov.
2. **Zlúčiť prežité sekcie do jedného renderera pri spawne**, ako navrhuje spec. Pasca:
   `Mesh.CombineMeshes` blend shapes nezachová a priemerne sedem rendererov ich nesie. Bez
   straty sa dá zlúčiť len telo a oblečenie, alebo treba snímky poskladať ručne cez
   `AddBlendShapeFrame`. Zlúčenie samo počet draw callov nezníži — submesh na materiál ostáva.

## Čo sa nezmeralo

Frame time vo WebGL builde. Bez neho sa nevie, či 40 NPC vôbec bolí; čísla vyššie hovoria len,
čo sa bude merať a kde hľadať.
