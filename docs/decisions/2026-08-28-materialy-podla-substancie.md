# Materiál sa volá podľa substancie, nie podľa objektu

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-08-28

## Kontext

Každý prop mal vlastný materiál: `mt_lamp_1`, `mt_lamp_2`, `mt_sign_1`, `mt_sign_2`,
`mt_sign_3`, `mt_trash_container_1` až `_5`, `mt_calcetto_1` až `_5`, `mt_whiteboard_1`,
`mt_e_plug_cover_1`. Spolu 140 materiálov, z ktorých veľká časť bol ten istý plast
s inou farbou.

Dve veci to stálo:

- **Dávkovanie.** SRP Batcher dávkuje podľa materiálu. Desať plastových propov s desiatimi
  materiálmi sú desiatky dávok tam, kde by stačila jedna.
- **Údržbu.** Keď sa zmení svetlo alebo tonemapping, preladiť treba každý materiál zvlášť.
  Pri osemdesiatich takmer identických plastoch to nikto neurobí dôsledne, takže sa
  postupne rozídu.

## Rozhodnutie

Meno materiálu hovorí, **čím povrch je**, nie na akom objekte sedí:
`mt_plastic_1..7`, `mt_wood_1..6`, `mt_steel_1..8`, `mt_fri_paint_1..5`, `mt_fabric_1..2`,
`mt_dirt_1`. Zo 140 materiálov zostalo 79.

Priečinok je substancia. Čo sa do žiadnej nezmestí — cedule, strop, interiérová stena,
dlažba — je v `Other/`, nie natlačené do `Plastic/`, kam nepatrí. Násilné zaradenie je
horšie než priznané „iné": po pol roku už nikto nevie, prečo je strop plast.

Výmenu v Blenderi robí `tools/blender/replace_material.py`. Cieľový materiál nikdy
nevytvára — preklep v mene by inak vyrobil prázdny sivý materiál namiesto hlásenia.

## Dôsledky

**Prerobiť materiál nie je to isté ako upraviť ho.** `mt_concrete_4` a `mt_wood_1` až `_4`
vznikli nanovo, takže dostali **nové GUID**. Všetko, čo ukazovalo na staré, by ukazovalo
do prázdna — a Unity to nenahlási, len vykreslí ružovú. Pred commitom sa to preto overilo
naprieč `Assets`: na starých piatich GUID a na 86 GUID zmazaných materiálov neukazuje ani
jeden súbor. Kto sa do materiálov pustí znova, nech to overí tiež; samotné „vyzerá to
dobre v scéne" nestačí, lebo prefab, ktorý práve nie je v scéne, sa neprejaví.

`barrier.mat` sa presunul z `Plastic/` do `Glass/` a **GUID si podržal** — to je skutočný
presun a referencie prežili. Rozdiel oproti predchádzajúcemu odstavcu je presne v tom
GUID, nie v tom, ako to vyzerá v Projecte.

Materiály a 71 prepojených prefabov sú **v jednom commite**. Rozdelené by medzi nimi
existovala revízia, v ktorej prop ukazuje na zmazaný materiál — a taká revízia sa
nedá zbuildovať ani bisectovať.

Occlusion dáta sa museli prepiecť, lebo occluder rozhoduje materiál
([2026-08-04-occlusion-culling-occludery.md](2026-08-04-occlusion-culling-occludery.md))
a konsolidácia teda posunula, ktoré renderery occludermi sú.
