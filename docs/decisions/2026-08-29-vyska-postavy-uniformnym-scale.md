# Výšku postavy nesie uniformný scale, nie scale do jednej osi

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-08-29

## Kontext

Postavy sa generujú z jedného modelu, takže výška musela pribudnúť ako ďalšia losovaná
vlastnosť. Prvá myšlienka bola scale do osi Y — výšku predsa definuje dĺžka nôh a trupu,
hlava a dlane sú u nízkeho aj vysokého človeka skoro rovnaké.

Tá úvaha je anatomicky správna. Technicky sa nedá vykonať.

**Nerovnomerný scale zložený s rotáciou nie je natiahnutie, ale skosenie.** Každá kosť má
vlastnú rotáciu, takže scale do Y sa na nej neprejaví ako predĺženie v jej smere. Noha
mieri po Y a predĺži sa správne. Ruka v T-póze mieri po X, takže sa nepredĺži — iba zvislo
zhrubne. Hlava sa natiahne do vajca. Nie je to chyba hierarchie a nedá sa to obísť
parentovaním ani prázdnym objektom medzi kostrou a meshom; je to vlastnosť lineárnej
algebry.

## Rozhodnutie

**Uniformný scale na koreni postavy**, `CharacterBuilder` ho nastaví z `CharacterAppearance.height`.

Uniformný scale nič neskosí, ale zmenší aj hlavu. A hlava je jediná časť tela, ktorá je
naprieč výškami skoro konštantná — dospelý ju má 22–24 cm, či meria 165 alebo 190 cm.
Preto uniformne zmenšená postava číta detsky: dieťa má okolo 6 hláv na výšku, dospelý 7,5.
(Dlane a chodidlá naopak s výškou rastú, tie uniformný scale trafí správne.)

Kľúč je preto **nie v scale, ale v tom, aby model stál blízko priemeru svojho pohlavia**:

| model | postava | scale | hlava 23 cm sa zmení na |
|---|---|---|---|
| 1.878 m | muž 1.79 | 0.953 | 21.9 cm — nevidno |
| 1.878 m | žena 1.66 | 0.884 | 20.3 cm — **vidno, číta detsky** |
| 1.803 m | muž 1.70 | 0.943 | 21.7 cm — nevidno |
| 1.803 m | muž 1.90 | 1.054 | 24.2 cm — nevidno |

Model bol preto v Blenderi upravený na **1.803 m**, čo je pri priemere 1.80 m scale okolo
1.0, a celé pásmo 1.70–1.90 m sa zmestí do **0.943–1.054**. Ženské telo bude vlastný model
blízko 1.67 m, nie ten istý mesh zmenšený.

Register to drží oddelene zámerne: `modelHeight` je fakt o meshi, `heightMean` fakt
o populácii. Keď sa model v Blenderi znova zmení, prepíše sa jedno číslo a rozdelenie
výšok zostane, kde bolo.

## Dôsledky

**Výška sa losuje normálnym rozdelením a mimo pásma sa prevzorkuje, neoreže.** Orezanie
vyzerá neškodne a nie je: pásmo 1.70–1.90 je pri odchýlke 7 cm len ±1.43 σ, takže
**15 % davu skončilo presne na 1.70 a presne na 1.90**. Zmerané na 2000 losoch: ~300 na
každom konci. Po prevzorkovaní 1 z 2000. Takú kopu v reálnej populácii nikto nemá a na
mriežke dvadsiatich postáv je vidieť ako dvaja rovnako malí a dvaja rovnako veľkí.

**`NavMeshAgent` sa neškáluje.** Výška agenta ovplyvňuje obchádzanie prekážok, ale navmesh
je upečený pre typ s výškou 1 a polomerom 0.2, takže per-agent hodnota s pečením aj tak
nesúvisí. Pri ±6 % je rozdiel ~10 cm a menenie polomeru by len rozišlo avoidance s tým, čo
je upečené.

**Keď prídu animácie, rýchlosť chôdze by mala škálovať s výškou.** Nižšia postava kráčajúca
rovnakou rýchlosťou vyzerá, že sa vezie. Teraz to nevidno, lebo kĺžu všetci rovnako.

**Ak by pásmo ±6 % bolo raz málo**, presnejšia cesta je uniformný scale koreňa na celkovú
veľkosť plus samostatný uniformný scale na stehenné kosti na dĺžku nôh, s protiscale na
chodidlá. Uniformný per kosť neskosí nič. Platí sa za to tým, že runtime scale na kostiach
je krehký voči animáciám — preto sa to teraz nerobí.
