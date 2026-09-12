# Tri pasce pri skriptovaní Blenderu, ktoré stáli celý deň

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-12

## Kontext

Model sa dnes upravoval skriptami cez MCP: zapečenie Mirroru, zvarenie lebky do tváre,
prehnanie kľúčov. Tri veci pri tom potichu rozbili model a každá sa prejavila inde, než
vznikla. Všetky tri sú opakovateľné, preto sú tu.

## Rozhodnutie

**1. Výber spravený v object mode neplatí v edit mode.**

```python
for v in me.vertices:
    v.select = v.index in sel      # vyzerá to, že je vybraté
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.remove_doubles(...)   # beží cez CELÝ mesh
```

Takto zmizol celý krk (`remove_doubles` zvaril všetko) a neskôr dva páry vrcholov v kútikoch
úst — ich oddelené normály držali ostrý prechod, po spriemerovaní pery vyzerali nafúknuto.
Geometria sa pritom líšila o 0.084 mm, takže na meraní vzdialeností to nevidno; vidno to až
na uhle normál, kde to bolo 66°.

**Ako správne:** `bmesh.ops.weld_verts` s výslovnou mapou dvojíc, alebo `bmesh.from_edit_mesh`,
keď je objekt naozaj v edit mode. Žiadny prah, ktorý môže chytiť niečo iné.

**2. Novovytvorený shape key príde s hodnotou 1.0.**

`propagate_shape_keys.py` kľúč vyrobil a hodnotu nenastavil. Dvadsať kľúčov na jedenástich
overlayoch tak sedelo naplno naraz: obočie odskočené, oči mimo jamiek, brada vedľa čeľuste.
**V Unity to nebolo vidieť vôbec** — tam váhy píše `CharacterBuilder` a importuje sa základný
tvar — takže render z hry vyzeral v poriadku a rozbitý bol len súbor, v ktorom sa pracuje.

**3. Ručne zapečený modifier prenesie len to, čo vymenuješ.**

`apply_mirror_with_shape_keys.py` niesol pozície, polygóny, materiály, smooth flagy, ostré
hrany, UV, váhy aj vlastné normály — a `use_seam` nie. `face_1` prišla o 167 označených
hrán, obočie o 20, brady o 50, oči o 18. Na render to vplyv nemá, takže sa to zistilo až
o niekoľko commitov neskôr, keď ich niekto hľadal.

## Dôsledky

**Po každom zásahu sa meria proti zálohe, nie proti dojmu.** Záloha `.blend` pred každým
nevratným krokom a potom porovnanie vrchol po vrchole: pozície, normály, počty kľúčov.
Práve to odhalilo kútiky úst aj chýbajúce seamy.

**Čo sa nekreslí, to sa nekontroluje samo.** Seamy, váhy a hodnoty kľúčov nie sú vidieť na
renderi — a dve z týchto troch pascí prežili práve preto. Patria do kontrolného zoznamu
skriptu, nie do oka.

**Unity a Blender ukazujú iné veci.** Hodnoty kľúčov Unity ignoruje, seamy tiež. Overovať
treba v oboch: čo je rozbité v jednom, môže v druhom vyzerať bezchybne.
