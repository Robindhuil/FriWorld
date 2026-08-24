# Nastavovanie komponentov nepatrí do custom inšpektora

## Kontext

Posuvníky hlasitosti v nastaveniach nemali na zvuk dverí žiadny vplyv. Podozrenie padlo na
`AudioSettings` — na zle pomenovaný exposed parameter alebo na nepripojený mixér.

Ani jedno. Mená sedia (`MasterVolume`, `MusicVolume`, `SFXVolume`) a mixér je priradený.

| | |
|---|---|
| `AudioSource` vo `FriBuilding.prefab` | 291 |
| z toho s vyplneným `outputAudioMixerGroup` | **14** |
| všetky sedia na `Interactable` | áno |

`AudioSource` bez output group hrá priamo do `AudioListener`. Mixér ho nevidí, takže naň
nedosiahne žiadny jeho parameter — hlasitosť sa nedá stlmiť ničím okrem hlasitosti systému.

Prečo práve 14: napojenie na skupinu existovalo, ale žilo v `InteractionEditor`, teda
v **custom inšpektore** komponentu `Interactable`. Spúšťalo sa, keď človek v inšpektore ručne
preklikol `playSoundEffect`. Tých 14 sú presne tie, ktoré niekto naklikal. Zvyšných 277 dverí
vyrobil `SetupInteractables`, a ten cez inšpektor neprechádza — inšpektor sa pri generovaní
vôbec nevykreslí.

## Rozhodnutie

Napojenie sa presunulo do `SfxMixerGroup`, ktorý používajú **obaja** — inšpektor aj generátor.
`SetupInteractables` odteraz napojí každý `AudioSource`, ktorý žiadnu skupinu nemá, a počet
hlási v sumáre.

Zdroj, ktorý už niekam ukazuje, sa nechá tak. Keby ho niekto vedome dal na `Music`, tiché
pretiahnutie na `Sfx` by mu to rozhodnutie zrušilo bez slova.

## Dôsledky

- Existujúcich 277 dverí sa opraví behom kroku 7 v `Routine`.
- Editor-only riešenie stačí: každý `AudioSource` v budove je naautorovaný, nie spawnutý za
  behu. Keby raz pribudol zdroj vytvorený skriptom v hre, potreboval by vlastnú cestu k skupine
  — `AssetDatabase` v builde neexistuje.

## Čo neskúšať znova

**Kód v custom inšpektore nie je nastavovací kód.** Beží iba vtedy, keď sa komponent kreslí
v Inspectore, čo pri hromadnom generovaní nenastane nikdy. Vyzerá to, že „to už riešime",
a pritom to platí pre pár objektov, ktoré niekto náhodou otvoril.

Rovnaká pasca číha všade, kde `OnInspectorGUI` alebo `OnValidate` niečo dopĺňa. Keď to má
platiť pre všetky objekty, musí to byť v ceste, ktorou prechádzajú všetky — u nás v generátore.
