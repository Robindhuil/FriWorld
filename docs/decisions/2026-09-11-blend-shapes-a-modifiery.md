# Blend shapy z `.blendu` prežijú iba s Armature modifierom

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-11

## Kontext

Prvá deformačná os `nose_wide` bola v Blenderi nasochaná na tvári a skriptom prenesená na
obočie, bradu, oči, lebku a krk. V Blenderi fungovala. Do Unity neprišiel **ani jeden**
kľúč — Bake našiel jedinú shape mapu a Report nič nehlásil, lebo z jeho pohľadu bolo všetko
v poriadku.

Jedna vec ale sedela podozrivo presne:

```
beard_none_1   verts=3     blendShapes=1  [nose_wide]   ← jediný objekt BEZ Mirroru
face_1         verts=1668  blendShapes=0
cranium_1      verts=940   blendShapes=0
brow_1         verts=156   blendShapes=0
eye_brown_1    verts=272   blendShapes=0
```

Unity konvertuje `.blend` cez `Unity-BlenderToFBX.py`, ktorý exportuje s
`use_mesh_modifiers=True`. Modifier, ktorý mesh prepíše, zahodí pri tom shape keys. Mirror
mení počet vertov, takže ich zahodí vždy. Prežil práve ten jediný mesh, ktorý ho nemá.

Po aplikovaní Mirroru prišlo jedenásť z dvanástich — a chýbal presne ten, na ktorom kľúč
vznikol. `face_1` mal ešte **Smooth by Angle**, čo je geometry-nodes modifier, a ten mesh
prepisuje tiež.

## Rozhodnutie

**Na meshi, ktorý nesie blend shapes, smie zostať iba `ARMATURE`.** Ten je deformer,
vyhodnocuje sa inde a kľúče mu nevadia.

Prakticky to znamená:

- **Mirror sa aplikuje.** Blender nedovolí aplikovať modifier na mesh so shape keys, takže
  poradie je vynútené: zmazať kľúče → aplikovať Mirror → nasochať odznova → preniesť.
- **Hladké tieňovanie nesmie byť modifier.** Namiesto `shade_auto_smooth`, ktorý pridá
  Smooth by Angle, sa použije `shade_smooth_by_angle`, ktorý zapíše ostré hrany priamo do
  meshu. Na `face_1` z toho vzniklo 255 ostrých hrán a tieňovanie zostalo rovnaké.

## Dôsledky

**Symetrické modelovanie cez modifier je preč.** Namiesto neho symetria X v edit a sculpt
móde. Je to cena za to, že sa kľúče vôbec dostanú do hry.

**Report na to nemá kontrolu a ani ju mať nemôže** — registre vyzerajú správne, mesh
v Blenderi tiež, chyba vznikne až pri konverzii. Čo Report vie chytiť a chytá, je následok:
os, ktorú nesie menej meshov než inú, čiže neprebehnutý `propagate_shape_keys.py`.
Overiť sa to dá jedine na importovanom prefabe, `sharedMesh.blendShapeCount`.

**Je to tretí prípad toho istého vzoru.** Import `.blend` ticho zahodí niečo, čo v Blenderi
vyzerá v poriadku: najprv zlý Blender cez „Open with", potom `hide_viewport` a modifiery,
teraz modifiery a blend shapy. Prechod na ručne exportované `.fbx` by dal kontrolu nad
prepínačmi exportu a všetky tri by zmizli naraz.
