# Mouse delta sa nenásobí `Time.deltaTime`

**Verzia:** 0.1.0-alpha · **Dátum:** 2026-08-04

## Kontext

Vo web builde sa občas stalo, že sa pohľad hráča sám švihol do strany. FPS pritom
neklesli a v editor play mode sa to nedialo vôbec.

`PlayerLook.ProcessLook` násobil vstup sensitivitou **aj `Time.deltaTime`**. Ten
vstup ale prichádza z akcie `Look` bindnutej na `<Mouse>/delta` — a delta je **už
naakumulovaný posun za daný snímok**, nie rýchlosť.

Takže: `delta ≈ rýchlosť × dt`, a po vynásobení `rotácia ≈ rýchlosť × sens × dt²`.
Rotácia bola **kvadratická vo frame time** — jeden trikrát dlhší snímok dal za
rovnaký pohyb ruky deväťnásobné otočenie.

Editor to schoval za rovnomerný vsync pacing. WebGL má nepravidelný pacing
(browser rAF, GC, dekódovanie shaderov), takže tam to vyskočilo.

Bug bol v projekte **od initial commitu**. Nespôsobili ho úpravy `PlayerMotor` ani
`PlayerInteract` — `PlayerMotor.cs` sa odvtedy vôbec nezmenil. Tie zmeny len
posunuli frame pacing a odkryli latentnú chybu.

## Rozhodnutie

`Time.deltaTime` z `ProcessLook` preč. `DPI_DIVISOR` zo `100` na `6000`, takže
sensitivita je teraz priamo **stupne na pixel** a pri 60 fps dáva presne to isté
otočenie ako predtým. Uložené `MouseSensitivity` (DPI 400–3200) zostávajú platné.

Pridaný `maxFrameDelta` (300 px) — poistka proti jednosnímkovému výstrelu, ktorý
browsery vedia poslať pri (re)aktivácii pointer locku.

## Dôsledky

- Otáčanie je nezávislé od frame rate: 30 aj 144 fps dá rovnaké otočenie.
- **Delta vstupy (`/delta`, scroll) sa nikdy nenásobia `deltaTime`.** Platí to aj
  pre akýkoľvek ďalší delta binding, ktorý pribudne.
- Ak by sa lag objavil znova, ďalším podozrivým je pointer-lock spike v prehliadači
  — to sa dá zmerať až priamo vo web builde.
