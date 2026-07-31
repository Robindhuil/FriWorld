# Lighting & Shaders Visual Overhaul — FriWorld

**Dátum:** 2026-06-13  
**Štýl:** Warm & Vibrant (inšpirácia: Firewatch, Journey)  
**Pipeline:** URP, Deferred Rendering, Unity 6  
**Scéna:** Demo (mix interiér + exteriér)  
**Lighting mode:** Mixed (baked statika + realtime dynamika)

---

## Identifikované problémy

| Problém | Príčina |
|---|---|
| Modrý nádych | ShadowsMidtonesHighlights shadows: `r:0.859 g:0.882 b:1.0` |
| Svetlo nebounceuje | Directional Light na Realtime — žiadne GI lightmapy |
| Tmavé interiéry | Žiadne Reflection Probes, ambient bez warmth |
| Prázdny Global Volume | Global Volume Profile v Demo scéne bez komponentov |

---

## Post-Processing (PP Profile.asset — Demo)

| Parameter | Pred | Po |
|---|---|---|
| Tonemapping | ACES | ACES (zachovať) |
| Shadows tint | `r:0.859 g:0.882 b:1.0` | `r:1.0 g:0.96 b:0.88` |
| Highlights tint | `r:1.0 g:0.968 b:0.872` | zachovať |
| Saturation | 27.9 | 38 |
| Bloom threshold | 1.66 | 1.1 |
| Bloom intensity | 0.53 | 0.7 |
| Post Exposure | 0 | +0.2 |

---

## Directional Light

- Mode: Mixed
- Color: `#FFF4D6` (~5500K, teplá žltá)
- Intensity: 1.2
- Shadow Strength: 0.7

---

## Lighting Settings (Window → Rendering → Lighting)

- Lightmapper: Progressive GPU
- Direct Samples: 32
- Indirect Samples: 512
- Bounces: 3
- Lightmap Resolution: 20 texels/unit
- Ambient Occlusion: zapnuté, Max Distance: 1m, Intensity: 0.5
- Environment Lighting Intensity Multiplier: 1.1

---

## Reflection Probes

- 1× Exteriér — veľký, Baked, pokrýva vonkajší priestor
- 1–2× per miestnosť — Box Projected, Baked

---

## Poradie implementácie

1. Opraviť `PP Profile.asset` — modrý tón, bloom, saturation, exposure
2. Nastaviť Global Volume v scéne
3. Directional Light → Mixed, teplá farba
4. Lighting Settings — bounces, AO, ambient
5. Pridať Reflection Probes
6. Spustiť Lightmap Bake
7. Vytvoriť `DayCycleController.cs`

---

## Budúci časový cyklus

`DayCycleController.cs` bude meniť:
- `sunColor` — gradient (sunrise → day → sunset)
- `sunIntensity` — AnimationCurve
- `ambientIntensity` — AnimationCurve

Baked GI zostane statická (denné svetlo). Dynamické objekty dostanú svetlo cez Light Probes automaticky.
