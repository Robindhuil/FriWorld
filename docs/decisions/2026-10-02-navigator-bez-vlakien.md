# Navigator beží bez vlákien

**Verzia:** 0.1.2-alpha · **Dátum:** 2026-10-02

## Kontext

Navigator sa bude otvárať hlavne z mobilu. Profil `NavigatorWeb` prevzal z FriWorldu
viacvláknový WebGL (`webGLThreadsSupport: 1`). Taký build potrebuje `SharedArrayBuffer`, a ten
prehliadač dá len izolovanej stránke (COOP + COEP). Hub posiela
`Cross-Origin-Embedder-Policy: credentialless`, lebo build leží na R2 a `require-corp` by chcel
na každom súbore hlavičku `Cross-Origin-Resource-Policy`, ktorú R2 bez vlastnej domény nenastaví.

Safari hodnotu `credentialless` nepodporuje, ani na iOS 26.5
([caniuse](https://caniuse.com/mdn-http_headers_cross-origin-embedder-policy_credentialless)),
a na iPhone ide cez WebKit každý prehliadač. Stránka tam izolovaná nie je, takže viacvláknový
build sa na žiadnom iPhone nespustí.

## Rozhodnutie

Profil Navigatora má vlákna vypnuté (`webGLThreadsSupport: 0`). Jednovláknový build nepotrebuje
`SharedArrayBuffer` ani izoláciu, nemá `worker.js` a Hub preň kontrolu viacvláknovosti preskočí.
Overené: zo servera bez COOP/COEP (`crossOriginIsolated` false, `SharedArrayBuffer` chýba) sa
spustí, letí a hlási čas.

Nevybraté: `require-corp` pre celý Hub. Chcelo by vlastnú doménu pre R2 s Transform Rule na
hlavičku a týka sa aj hry.

## Dôsledky

- Navigator beží aj tam, kde stránka izolovaná nie je, teda aj na iPhone. Hra FriWorld ostáva
  viacvláknová a na iPhone sa nespustí; to toto rozhodnutie nerieši.
- Všetko beží na hlavnom vlákne. V skrytom paneli prehliadača vyšiel jednovláknový build
  2,7 snímky/s a viacvláknový 3,0, oba brzdené rovnako; hovorí to len, že prepad nie je
  dramatický. Skutočné čísla ukáže mobil.
- Zmena vlákien vynúti celé prelinkovanie: build trval 20 min namiesto obvyklých dvoch.
