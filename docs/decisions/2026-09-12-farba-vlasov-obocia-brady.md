# Obočie a brada idú za vlasmi, brada s odchýlkou o krok

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-09-12

## Kontext

Vlasy, obočie a brada boli tri samostatné farebné triedy s vlastnými paletami a vlastným
losovaním. To znamená, že sa dala vylosovať čierna hlava s hnedým obočím a ryšavou bradou —
tri farby na jednej hlave, čo nečítaš ako variabilitu, ale ako chybu.

Zároveň tá sloboda musí zostať. Ručne nastavené NPC alebo budúci editor postavy majú mať
možnosť dať všetko inak; vzhľad je len pole indexov a nič v dátach to nezakazuje.

## Rozhodnutie

**Obmedzenie patrí do `CharacterRandomizer`, nie do katalógu.** Zakázané je to len pri
losovaní; kto index nastaví sám, nastaví si čo chce.

Farebná trieda môže mať `follows`:

- **nemá vlastnú paletu** — dedí colorways triedy, ktorú nasleduje, pod tými istými id,
- **materiály si generuje zo svojej šablóny**, zafarbené cez `followValue`/`followSaturation`,
  takže obočie si drží obočiovú textúru a berie len farbu vlasov,
- **v losovaní kopíruje** hod zdroja namiesto vlastného ťahu.

K tomu `followDrift` a `followDriftChance`: follower smie odskočiť o zadaný počet krokov
v palete, so zadanou pravdepodobnosťou. Obočie má drift 0 — je to farba vlasov, bodka.
Brada má drift 1 pri 25 %, lebo skutočná brada beží o odtieň vedľa vlasov a bez toho vyzerá
dav ako vytlačený.

Merané na 600 semenách: **83 % brád presne vo farbe vlasov, 16 % o krok, nikdy ďalej,
obočie nikdy mimo.**

## Dôsledky

**Materiál nesmie niesť cudziu triedu.** Prvý pokus bol pomenovať materiál obočia
`char_hair_11` — teda „trieda hair, kľúč 1, tmavší odtieň". Zadarmo a bez kódu, ale šablóna
materiálu sa hľadá podľa mena triedy, takže obočie by dostalo vlasovú textúru a vlasy by
prišli o svoj vlastný tmavší odtieň. Meno materiálu hovorí, **ktorý slot** sa farbí, nie
odkiaľ sa berie vzhľad.

**Krok je index, takže paleta musí byť gradient.** Report meria luminanciu a hlási `ORDER`,
keď farby nejdú od tmavej po svetlú — inak „o odtieň vedľa" znamená „o riadok nižšie v
súbore". Ryšavá sa do takého poradia nezmestí; keď pribudne, patrí jej vlastná vetva
s driftom 0.

**Brada prišla o svoju paletu.** Dedí vlasovú, takže dnes nie je ryšavá brada — vráti sa
s ryšavými vlasmi.

**Losuje sa len pre slot, ktorý drift deklaruje**, takže zapnutie driftu neprehádže farby
semenám, ktoré ho nemajú.
