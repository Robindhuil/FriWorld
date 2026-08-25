# Lightmap UV autoruje Blender, nie Unity

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-08-25

## Kontext

Oba modelové importery majú `generateSecondaryUV = False`:

```
Assets/3Dmodels/static/fri_building/interior_objects.blend   4688 meshov
Assets/3Dmodels/static/fri_building/fri_building.blend       2510 meshov
```

Na prvý pohľad to vyzerá ako zabudnuté nastavenie, ale nie je. Zmerané naprieč
2607 unikátnymi meshmi s `ContributeGI`:

| | |
|---|---|
| uv2 v rozsahu 0..1, teda platné rozbalenie | **2593** |
| uv2 mimo 0..1 | 0 |
| uv2 degenerované (všetko v jednom bode) | 9 |
| bez uv2 (lightmapper spadne na uv0) | 5 meshov / 241 inštancií |

Rozbalenia z Blenderu sú dobré. Zapnutie Unity unwrappera by ich prepísalo vlastnými,
ktoré rešpektujú hard edges menej a švíky dávajú inam.

Pasca je v tom, ako sa tie kanály do Blenderu dostávajú. **Unity nečíta mená UV kanálov,
číta ich poradie** — kanál 0 je `mesh.uv`, kanál 1 je `mesh.uv2`, a `mesh.uv2` je lightmapa.
Skript, ktorý kanál pridá len keď meno chýba, ho pripne na koniec. Mesh, čo prišiel
s `["UVMap", "UVMap.001", "Atlas"]`, skončí s `"Lightmap"` na indexe 3 a Unity pečie do
toho, čo je náhodou na indexe 1. Meno sedí, výsledok je nezmysel. Odtiaľ tých 9 + 5.

## Rozhodnutie

`generateSecondaryUV` zostáva **False** na oboch `.blend`. Lightmap UV je zodpovednosť
Blenderu.

Z toho plynie povinnosť, ktorú musí splniť každý skript, čo v Blenderi na UV siahne:
**po ňom musia byť presne dva kanály, kanál 0 textúrová UV, kanál 1 lightmapa.** Blender
kanály nevie preusporiadať, takže jediný spôsob, ako to vynútiť, je zmazať všetky
a vytvoriť odznova. `tools/blender/rebuild_uv_channels.py` to robí a na konci overí,
že lightmap kanál naozaj leží v 0..1.

## Dôsledky

- Objekt s rozbitou uv2 sa nezapečie do jednej plochej farby ticho — vyzerá to ako
  „nasvietenie nefunguje", nie ako „chýba UV". Hľadalo by sa to v peku, kde to nie je.
- Zostáva 14 meshov na opravu: 9 s degenerovanou uv2, 5 bez nej (`chair_turning_1`,
  `Quad`, `table_noble_1`, `table_break_room_1`, +1). `table_break_room_1` má
  degenerované aj uv0. Buď im dorobiť UV v Blenderi, alebo zhodiť `ContributeGI`
  a nechať ich na light proboch.
- Ak by sa niekedy `generateSecondaryUV` zaplo, treba to zapnúť na oboch `.blend` naraz
  a počítať s tým, že sa prepíše všetkých 2593 dobrých rozbalení.
