# Import `.blend` do Unity si vyberá Blender cez „Open with"

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-11

## Kontext

Po uložení `npc.blend` v Blenderi 5.2 začal Unity import padať na hláške:

```
Blender could not convert the .blend file to FBX file.
You need to use Blender 2.45-2.49 or 2.58 and later versions for direct Blender import to work.
```

Hláška ukazuje na verziu Blenderu a je zavádzajúca — Blender 5.2 je dávno „2.58 a novší".
Konverzia z príkazového riadku pritom prešla:

```
"…\Blender 5.2\blender.exe" npc.blend --background --python "…\Unity-BlenderToFBX.py" -- out.fbx
```

21 MB FBX, exit 0. Cez `blender-launcher.exe` tiež. Chyba teda nebola vo verzii ani v skripte.

Rozdiel bol v tom, **ktorý** Blender Unity spustí. Na stroji boli nainštalované tri: 4.2, 4.3
a 5.2. Staršie dva súbor uložený v 5.2 neprečítajú:

```
4.2:  Failed to read blend file 'npc.blend', not a blend file
4.3:  Cannot read blend file 'npc.blend', incomplete header, may be from a newer version of Blender
```

## Rozhodnutie

Unity nemá pre Blender žiadny konfigurovateľný kľúč — v `Unity.dll` sú vedľa tej hlášky
reťazce *„Make sure that Blender is installed and the .blend file has Blender as its
'Open with' application!"* a *„…in your PATH"*. Rozhoduje teda **asociácia `.blend`**
a PATH, nič iné. `HKLM\SOFTWARE\BlenderFoundation` neexistuje a Blender na PATH nebol.

Príčina bola v per-user zozname:

```
HKCU\…\Explorer\FileExts\.blend\OpenWithProgids
    blender.4.2
    blender.4.3          ← blender.5.2 tam chýbal
```

HKLM síce hovoril `.blend → blender.5.2`, ale tento zoznam ho prebil. Riešenie je nastaviť
default „Open with" pre `.blend` na `Blender 5.2\blender-launcher.exe` cez Windows UI.

Registrovo sa to spraviť nedá: `UserChoice` je od Windows 8 chránený hashom, ktorý shell
overuje, a zápis bez platného hashu Windows ignoruje.

## Dôsledky

**Odinštalovať staré Blendery netreba** a nebolo to ani riešenie — 4.2 a 4.3 zostávajú
použiteľné cez vlastné skratky. Mení sa len to, čo sa spustí dvojklikom na `.blend`.

**Môže sa to vrátiť.** V `OpenWithProgids` naďalej sedia aj 4.2 a 4.3. Keď sa niektorá
z nich stane defaultom, import padne s tou istou hláškou o verziách — a hláška opäť bude
ukazovať inam, než kde je príčina.

**Trvalejšia cesta je ručne exportovaný `.fbx`.** Plán character customization to aj tak
predpokladá: orez kostí na `DEF-` only sa cez `.blend` spraviť nedá, lebo Unity nevie
Blenderu podať `Armature → Only Deform Bones`. Vtedy táto pasca zmizne úplne.

**Vedľajší nález:** Unity importuje aj `.blend1` zálohy, ktoré ležia v `Assets/`. Každá
znamená ďalší beh Blenderu pri importe.
