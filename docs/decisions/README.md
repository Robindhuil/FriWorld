# Rozhodnutia

Krátke zápisy o veciach, ktoré sa do commit message nezmestia a na ktoré by inak
niekto nabehol znova.

**Sem patrí:** voľba medzi možnosťami, kde dôvod nie je vidieť z kódu · pasca,
do ktorej sa dá spadnúť opäť · bug, ktorého príčina bola inde než prejav ·
platformovo špecifická zmena s netriviálnym dôvodom.

**Sem nepatrí:** bežný feature, chore, refactor ani rename. Tie pokryje commit
message a riadok v `CHANGELOG.md`.

Súbor: `YYYY-MM-DD-kratky-nazov.md`, sekcie **Kontext → Rozhodnutie → Dôsledky**,
20–40 riadkov.

| Dátum | Téma |
|---|---|
| 2026-08-04 | [Mouse delta sa nenásobí deltaTime](2026-08-04-mouse-delta-a-frame-time.md) |
| 2026-08-04 | [Render politika web buildu](2026-08-04-web-render-politika.md) |
| 2026-08-04 | [Occlusion culling: occluder rozhoduje materiál, nie meno](2026-08-04-occlusion-culling-occludery.md) |
