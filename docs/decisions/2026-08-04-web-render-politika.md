# Render politika web buildu

**Verzia:** 0.1.0-alpha · **Dátum:** 2026-08-04

## Kontext

Web build dával na Lenovo Legion Y720 (i7-7700HQ, **GTX 1060**, 1080p) len 30–40 fps.
Na takom stroji to nie je hardvérový strop — najpravdepodobnejšie tam prehliadač
bežal na integrovanej HD 630 namiesto dedikovanej karty (Optimus + 8 GB RAM
v jednom slote = single-channel, čiže polovičná priepustnosť pre iGPU).

To presúva ťažisko: integrované GPU sú **bandwidth-bound**. Nebolí ich zložitosť
scény (počet objektov už bol overený ako irelevantný — culling to zahodí), ale
**full-screen passy a per-sample cena**.

## Rozhodnutie

Trojvrstvové delenie podľa toho, kde sa vec dá vypnúť:

| Vrstva | Čo | Kde |
|---|---|---|
| Asset | MSAA, depth texture, LOD cross-fade, probe blending, aniso, LOD bias, skin weights | `RP_Web.asset`, quality level „Web" |
| Feature flag | Bloom, Depth of Field, Motion Blur | `FeatureId.HeavyPostProcessing` (enabled, DesktopOnly) |
| Runtime | FXAA na kamere, default quality level | `WebRenderDefaults` |

MSAA je vypnuté a nahradené **FXAA** — MSAA zdvojnásobuje bandwidth každého color
aj depth samplu a pridáva resolve pass, FXAA je jeden lacný pass. Bez tejto výmeny
by web zostal úplne bez AA.

**Color grading a tonemapping sa negatujú.** Idú v jednom uber passe za takmer nulu
a sú to vzhľad hry, nie výkonový žrút. Gate je len na tie tri, ktoré sú reálne
viacnásobné full-screen passy.

Kde je efekt vypnutý, **nezobrazuje sa ani v menu** — mŕtvy prepínač je horší než
žiadny. Rovnako quality dropdown na webe: každý iný level ukazuje na desktopový
pipeline asset, takže by jeho voľba zahodila celé web ladenie.

## Dôsledky

- **Čokoľvek, čo na webe prepne quality level, zahodí `RP_Web`.** Preto
  `DEFAULT_QUALITY_LEVEL` na webe rezolvuje na level „Web", nie na index 2.
  Toto bola tichá regresia — pred opravou by ju nikto nespojil s nastaveniami.
- Nový post efekt pre web sa musí posúdiť: full-screen pass → pod flag; súčasť
  color gradingu → môže zostať.
- **Neurobené zámerne:** `globalTextureMipmapLimit` zostáva 0 (plné rozlíšenie).
  Je to jediná páka, ktorá viditeľne zhorší obraz, a patrí skôr do samostatného
  low-spec tieru než do baseline webu.
- **Otvorené:** DPR cap v `friworld-web/src/components/UnityGame.tsx` je stále 2,
  patch na 1.5 nebol nikdy aplikovaný. Pre hi-DPI displeje je to najväčšia páka.
