# Rozhodnutia

Krátke zápisy o veciach, ktoré sa do commit message nezmestia a na ktoré by inak
niekto nabehol znova.

**Sem patrí:** voľba medzi možnosťami, kde dôvod nie je vidieť z kódu · pasca,
do ktorej sa dá spadnúť opäť · bug, ktorého príčina bola inde než prejav ·
platformovo špecifická zmena s netriviálnym dôvodom.

**Sem nepatrí:** bežný feature, chore, refactor ani rename. Tie pokryje commit
message a riadok v `CHANGELOG.md`. Zmeraný návrh, na ktorý ešte nedošlo, patrí do
[`docs/findings/`](../findings/).

Súbor: `YYYY-MM-DD-kratky-nazov.md`, hlavička `**Verzia:** … · **Dátum:** …`,
sekcie **Kontext → Rozhodnutie → Dôsledky**, 20–40 riadkov.

Verzia je `bundleVersion` z `ProjectSettings` v čase zápisu.

## 0.1.2-alpha

| Dátum | Téma |
|---|---|
| 2026-08-29 | [Výšku postavy nesie uniformný scale, nie scale do jednej osi](2026-08-29-vyska-postavy-uniformnym-scale.md) |

## 0.1.1-alpha

| Dátum | Téma |
|---|---|
| 2026-08-28 | [Materiál sa volá podľa substancie, nie podľa objektu](2026-08-28-materialy-podla-substancie.md) |
| 2026-08-25 | [Zárubne patria na `obstacle`, lebo navmesh zbiera len tú vrstvu](2026-08-25-door-frame-noobstacle.md) |
| 2026-08-25 | [Čo púšťa svetlo do budovy, rozhoduje materiál — nie typ](2026-08-25-sklo-blokovalo-pek.md) |
| 2026-08-25 | [Lightmap UV autoruje Blender, nie Unity](2026-08-25-lightmap-uv-z-blenderu.md) |
| 2026-08-25 | [`shadowStrength` nie je „sila tieňa"](2026-08-25-shadow-strength-a-shadowmask.md) |
| 2026-08-24 | [Platformové gaty patria do prefabu, nie na inštanciu](2026-08-24-platform-gaty-v-prefabe.md) |
| 2026-08-24 | [Nastavovanie komponentov nepatrí do custom inšpektora](2026-08-24-sfx-mimo-mixera.md) |

## 0.1.0-alpha

| Dátum | Téma |
|---|---|
| 2026-08-04 | [Collidery a vrstvy riadi register typov](2026-08-04-object-type-registry.md) |
| 2026-08-04 | [Occlusion culling: occluder rozhoduje materiál, nie meno](2026-08-04-occlusion-culling-occludery.md) |
| 2026-08-04 | [Render politika web buildu](2026-08-04-web-render-politika.md) |
| 2026-08-04 | [Mouse delta sa nenásobí deltaTime](2026-08-04-mouse-delta-a-frame-time.md) |
