# Objekt skrytý monitorom stratí pri exporte do Unity modifiery

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-11

## Kontext

`beard_1` sa v Blenderi vykresľoval správne, v Unity nebol vidieť vôbec. Materiál sedel,
renderer bol `enabled`, textúra mala alfu. Nesedelo jedno číslo:

```
brow_1    tPos=(0.00, 1.61, 0.00)   mesh šírka 0.11   boundsC=(0.00, -0.16,  0.73)
beard_1   tPos=(0.00, 0.00, 0.00)   mesh šírka 0.06   boundsC=(-0.03, -0.13, -1.03)
beard_2   tPos=(0.00, 1.61, 0.00)   mesh šírka 0.13   boundsC=(0.00, -0.14,  0.65)
```

`beard_1` prišiel ako **polovica** meshu, teda bez Mirroru, a s bounds pri nohách. Frustum
culling ho potom zahodil vždy, keď sa kamera pozerala na hlavu. V Blenderi bol pritom
Mirror na objekte a scéna vyzerala správne.

Rozdiel bol jediný: `beard_1` bol skrytý, aby bolo v okne vidieť len jeden variant.

## Rozhodnutie

Unity konvertuje `.blend` cez `Unity-BlenderToFBX.py`, ktorý exportuje s
`use_mesh_modifiers=True`. Modifiery sa vyhodnocujú cez depsgraph — a objekt s
**`hide_viewport = True` (ikona monitora) v depsgraphe nie je**, takže sa exportuje jeho
základná geometria. Skinning to prežije, lebo váhy sa čítajú z vertex groups priamo;
Mirror, Solidify a všetko ostatné zmizne.

**Skrývať sa smie len okom (`H`, `hide_set`), nikdy monitorom.** Oko objekt z depsgraphu
nevyradí a export je správny — overené na tom istom páre objektov.

Presety oblečenia v `npc.blend` to tak mali od začiatku: `hide_viewport = False`,
`eye_hidden = True`. Konvenciu porušil až skript, ktorý prepínal viditeľnosť variantov.

## Dôsledky

**Prejaví sa to ako neviditeľný objekt, nie ako chyba.** Nič sa nevypíše. Pri symetrickom
meshi je výsledok pol objektu, ktorý navyše sedí inde, než by mal — a v Unity to vyzerá
ako problém materiálu alebo cullingu, čo je presne to, kde sa hľadá najdlhšie.

**Rýchla kontrola po každej zmene viditeľnosti:** porovnaj `transform.localPosition`
a `sharedMesh.bounds` presetu s jeho zdrojovou sekciou. Ak sa líšia, objekt bol pri
poslednom uložení skrytý monitorom.

**Nezávisí to od verzie Blenderu ani od nastavení importu.** Je to vlastnosť toho, ako
Blender vyhodnocuje depsgraph, takže prechod na ručne exportované `.fbx` to nevyrieši sám
od seba — rovnaký export s rovnakým prepínačom spraví to isté.
