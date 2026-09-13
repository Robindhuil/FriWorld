# Unity číta `.fbx`, `.blend` zostáva pracovný a mimo importu

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-13

## Kontext

Postava sa do Unity dostávala priamo z `.blend`. Pohodlné — uložíš a je to vnútri — ale
Unity si súbor konvertuje vlastným skriptom `Unity-BlenderToFBX.py` a ten volá exportér
s pevným zoznamom argumentov:

```python
bpy.ops.export_scene.fbx(filepath=outfile, use_selection=False,
    object_types={'ARMATURE','CAMERA','LIGHT','MESH','OTHER','EMPTY'},
    use_mesh_modifiers=True, mesh_smooth_type='OFF', use_custom_props=True, ...)
```

Chýba `use_armature_deform_only` (default `False`) aj `add_leaf_bones` (default `True`).
Do hry teda išiel celý Rigify rig a každý mesh niesol **404 bind póz** — aj obočie, ktoré
je naviazané na jedinú kosť. Deformujúcich kostí je pritom 71.

## Rozhodnutie

**Z Blenderu sa exportuje `.fbx` skriptom `tools/blender/export_character_fbx.py`** a Unity
importuje ten. Skript vyberie armatúru a 53 meshov, ktoré na nej visia, a exportuje
s `use_armature_deform_only=True`, `add_leaf_bones=False`, Unity osami a `FBX_SCALE_ALL`.

**`.blend` zostáva v projekte, ale v priečinku `Assets/3Dmodels/Npc~`.** Unity ignoruje
priečinok končiaci vlnovkou, takže pracovný súbor je stále po ruke a vo verziách, ale
neimportuje sa druhýkrát popri `.fbx`, ktorý z neho vzniká.

```
                       .blend        character_male.fbx
bind pózy na mesh        404                98
transformov v assete     619               153
```

## Dôsledky

**Export je teraz krok, na ktorý sa dá zabudnúť.** Kým sa nespustí skript, Unity vidí starý
model. To je cena za kontrolu nad prepínačmi exportu; zabudnutý export je viditeľný hneď,
tichý stratený kľúč nebol.

**Skryté objekty sa nedajú vybrať**, a čo nie je vybraté, to sa neexportuje. Skript preto
celú sadu na čas odkryje a scénu potom vráti presne do pôvodného stavu. Prvý beh bez toho
vyexportoval 22 meshov z 53 a žiadnu kostru.

**Materiály `.fbx` sú premapované na `_source/`**, rovnako ako ich mal `.blend` — inak by si
import vyrobil vlastnú sadu a `Generate Shades` by pracoval s inými assetmi než hra.

**Zmizli tým tri pasce naraz**: import si už nevyberá Blender cez „Open with", modifiery sa
neaplikujú podľa cudzieho skriptu, a `hide_viewport` nemá čo pokaziť. Zostáva jedna nová:
cesta v skripte ukazuje z `Npc~` do `Npc`, takže presun ktoréhokoľvek z tých priečinkov
treba premietnuť do `OUTPUT`.
