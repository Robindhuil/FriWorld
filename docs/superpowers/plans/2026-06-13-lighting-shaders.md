# Lighting & Shaders Visual Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Opraviť modrý nádych, pridať warm & vibrant look, zabezpečiť správne bounceovanie svetla cez Mixed GI a Reflection Probes, a vytvoriť základ pre budúci časový cyklus.

**Architecture:** PP Profile opravíme priamo v .asset súbore, Directional Light a Lighting Settings cez Unity editor, Reflection Probes pridáme manuálne do scény, DayCycleController.cs vytvoríme ako C# skript.

**Tech Stack:** Unity 6, URP (Deferred), Progressive GPU Lightmapper, C#

---

### Task 1: Opraviť PP Profile — modrý tón, bloom, saturation, exposure

**Files:**
- Modify: `Assets/Scenes/Demo/PP Profile.asset`

- [ ] **Krok 1: Opraviť ShadowsMidtonesHighlights shadows (modrý tón)**

V súbore `Assets/Scenes/Demo/PP Profile.asset` nájdi sekciu `ShadowsMidtonesHighlights` a zmeň `shadows.m_Value` z `{x: 0.85897547, y: 0.88244873, z: 1, w: 0}` na:
```yaml
    shadows:
      m_OverrideState: 1
      m_Value: {x: 1, y: 0.96, z: 0.88, w: 0}
```

- [ ] **Krok 2: Opraviť saturation**

V tej istej sekciu `ColorAdjustments` zmeň `saturation.m_Value` z `27.9` na `38`:
```yaml
  saturation:
    m_OverrideState: 1
    m_Value: 38
```

- [ ] **Krok 3: Zapnúť a nastaviť Post Exposure**

V `ColorAdjustments` nastav `postExposure`:
```yaml
  postExposure:
    m_OverrideState: 1
    m_Value: 0.2
```

- [ ] **Krok 4: Opraviť Bloom**

V sekcii `Bloom` zmeň threshold a intensity:
```yaml
  threshold:
    m_OverrideState: 1
    m_Value: 1.1
  intensity:
    m_OverrideState: 1
    m_Value: 0.7
```

- [ ] **Krok 5: Vizuálna kontrola v Unity**

Otvor Unity, otvor scénu Demo, skontroluj Game view — modrý nádych by mal byť preč, scéna teplá a jasnejšia.

---

### Task 2: Nastaviť Directional Light v editore

**Files:** (zmena v Demo.unity cez editor)

- [ ] **Krok 1: Nájsť Directional Light v Hierarchy**

V Hierarchy paneli rozklikni skupinu `----ENVIROMENT----` alebo hľadaj objekt s názvom `Directional Light` / `Sun`.

- [ ] **Krok 2: Nastaviť Mixed mode**

V Inspectore pri Directional Light:
- `Light > Mode` → zmeň z `Realtime` na **`Mixed`**

- [ ] **Krok 3: Nastaviť farbu a intenzitu**

- `Color` → klikni na color picker, zadaj hex **`FFF4D6`** (teplá denná žltá ~5500K)
- `Intensity` → **`1.2`**
- `Shadow Strength` → **`0.7`**
- `Shadow Type` → `Soft Shadows`

- [ ] **Krok 4: Vizuálna kontrola**

Scéna by mala mať teplé žlté tiene. Ak sú tiene príliš tmavé, zníž Shadow Strength na 0.65.

---

### Task 3: Lighting Settings — bounces, AO, ambient

**Files:** (Window → Rendering → Lighting v editore)

- [ ] **Krok 1: Otvoriť Lighting Settings**

`Window → Rendering → Lighting` → záložka **Scene**

- [ ] **Krok 2: Environment Lighting — teplý ambient**

V sekcii `Environment`:
- `Source` → **`Color`** (nie Skybox — dáme kontrolovaný teplý ambient)
- `Ambient Color` → hex **`FFE8C0`** (teplá oranžovkastá)
- `Intensity Multiplier` → **`1.1`**

- [ ] **Krok 3: Lightmapper nastavenia**

V sekcii `Lightmapping Settings`:
- `Lightmapper` → **`Progressive GPU`**
- `Direct Samples` → **`32`**
- `Indirect Samples` → **`512`**
- `Bounces` (Indirect Bounces) → **`3`**
- `Lightmap Resolution` → **`20`** texels/unit
- `Lightmap Size` → **`1024`**
- `Compress Lightmaps` → zaškrtnúť

- [ ] **Krok 4: Ambient Occlusion**

V sekcii `Lightmapping Settings`:
- `Ambient Occlusion` → **zaškrtnúť**
- `Max Distance` → **`1`**
- `Indirect Contribution` → **`1`**
- `Direct Contribution` → **`0.5`**

- [ ] **Krok 5: Mixed Lighting**

V sekcii `Mixed Lighting`:
- `Baked Global Illumination` → **zaškrtnúť**
- `Lighting Mode` → **`Shadowmask`**

---

### Task 4: Pridať Reflection Probes do scény

**Files:** (zmena v Demo.unity cez editor)

- [ ] **Krok 1: Pridať Exteriér Reflection Probe**

V Hierarchy klikni pravým → `Light → Reflection Probe`.
Premenuj na `ReflectionProbe_Exterior`.

V Inspectore:
- `Type` → **`Baked`**
- `Box Size` → nastaviť tak aby pokrýval celý vonkajší priestor scény (napr. `{x: 50, y: 20, z: 50}`)
- `Resolution` → **`256`**
- `HDR` → zaškrtnúť
- Umiestni ho do stredu exteriéru, výška ~5m nad zemou

- [ ] **Krok 2: Pridať Interiér Reflection Probes**

Pre každú väčšiu miestnosť/zónu v interiéri:
- V Hierarchy: pravým → `Light → Reflection Probe`
- `Type` → **`Baked`**
- `Box Projection` → **zaškrtnúť**
- `Box Size` → prispôsobiť veľkosti miestnosti
- `Resolution` → **`128`**
- Umiestni do stredu miestnosti

Minimálne 2 interiérové proby (1 per väčší priestor).

- [ ] **Krok 3: Vizuálna kontrola pred bake**

Scéna → zapni `Scene > Probe Visualization` aby si videl pokrytie. Proby by mali pokrývať všetky hlavné priestory bez veľkých medzier.

---

### Task 5: Spustiť Lightmap Bake

**Files:** (automaticky generuje `Assets/Scenes/Demo/` lightmap textúry)

- [ ] **Krok 1: Skontrolovať že všetky statické objekty majú Contribute GI**

Vyber všetky statické objekty v scéne (budovy, podlaha, steny):
- V Inspectore vpravo hore zaškrtni **`Static`** → alebo aspoň **`Contribute GI`** z dropdown

- [ ] **Krok 2: Spustiť bake**

`Window → Rendering → Lighting` → klikni **`Generate Lighting`**

Bake môže trvať 5–20 minút podľa zložitosti scény. Pokrok vidíš v progress bare dole.

- [ ] **Krok 3: Vizuálna kontrola po bake**

Po dokončení:
- Tmavé interiéry by mali mať viditeľné bounced svetlo
- Tiene by mali byť mäkké a teplé
- Žiadne čisto čierne plochy (pokiaľ nie sú zakryté)

Ak sú niektoré miesta stále príliš tmavé → zvýš `Indirect Samples` na 1024 a rebake.

---

### Task 6: Vytvoriť DayCycleController.cs

**Files:**
- Create: `Assets/Scripts/Systems/DayCycleController.cs`

- [ ] **Krok 1: Vytvoriť adresár ak neexistuje**

V Project okne: `Assets/Scripts/Systems/` — ak neexistuje, vytvor priečinok.

- [ ] **Krok 2: Vytvoriť skript**

Vytvor `Assets/Scripts/Systems/DayCycleController.cs`:

```csharp
using UnityEngine;

/// Riadi farbu a intenzitu slnka počas dňového cyklu.
/// Pripoj na GameObject v scéne a priraď DirectionalLight.
/// Rozsah: 0 = svitanie, 0.5 = poludnie, 1 = západ slnka.
public class DayCycleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;

    [Header("Day Cycle")]
    [SerializeField][Range(0f, 1f)] private float timeOfDay = 0.5f;
    [SerializeField] private bool autoAdvance = false;
    [SerializeField] private float dayDurationSeconds = 120f;

    [Header("Sun Color")]
    [SerializeField] private Gradient sunColorGradient;

    [Header("Sun Intensity")]
    [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1.2f);

    [Header("Ambient Intensity")]
    [SerializeField] private AnimationCurve ambientIntensityCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1.1f);

    private void Reset()
    {
        // Defaultný gradient: svitanie oranžové → deň žlto-biely → západ červený
        sunColorGradient = new Gradient();
        var colors = new GradientColorKey[]
        {
            new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0f),   // svitanie
            new GradientColorKey(new Color(1f, 0.953f, 0.839f), 0.5f), // poludnie #FFF4D6
            new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f),   // západ
        };
        var alphas = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f),
        };
        sunColorGradient.SetKeys(colors, alphas);
    }

    private void Update()
    {
        if (autoAdvance)
        {
            timeOfDay += Time.deltaTime / dayDurationSeconds;
            if (timeOfDay > 1f) timeOfDay = 0f;
        }

        ApplyDayTime(timeOfDay);
    }

    public void SetTimeOfDay(float t)
    {
        timeOfDay = Mathf.Clamp01(t);
        ApplyDayTime(timeOfDay);
    }

    private void ApplyDayTime(float t)
    {
        if (directionalLight == null) return;

        directionalLight.color = sunColorGradient.Evaluate(t);
        directionalLight.intensity = sunIntensityCurve.Evaluate(t);
        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(t);

        // Rotácia slnka: svitanie (východ) → západ. Rozsah 10°–170° (bez noci)
        float sunAngle = Mathf.Lerp(10f, 170f, t);
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
    }
}
```

- [ ] **Krok 3: Pridať do scény**

V Hierarchy: vyber existujúci Directional Light objekt → `Add Component → DayCycleController`.
Alebo vytvor nový prázdny GameObject `DayCycle` a priraď skript tam.

V Inspectore priraď `Directional Light` do poľa `directionalLight`.

- [ ] **Krok 4: Otestovať v editore**

- `Auto Advance` nechaj vypnuté
- Pohybuj sliderom `Time Of Day` manuálne od 0 do 1
- Slnko by sa malo pohybovať po oblohe, farba meniť od oranžovej → bielej → červenej
- `Auto Advance` zapni pre live preview (Day Duration: 30s pre rýchly test)

---

### Task 7: Záverečná vizuálna kontrola

- [ ] **Krok 1: Skontrolovať Game view pri `timeOfDay = 0.5` (poludnie)**

Scéna by mala byť:
- Teplá, žlto-biela
- Bez modrého nádechu
- Interiéry viditeľne osvetlené bounced svetlom
- Reflection Probes odrážajú prostredie

- [ ] **Krok 2: Skontrolovať prechod interiér ↔ exteriér**

Choď kamerou z exteriéru do interiéru — prechod by mal byť plynulý, bez náhleho stmavnutia.

- [ ] **Krok 3: Skontrolovať SSAO**

V PC_Renderer.asset je SSAO s Intensity: 0.5. Ak sú tiene v rohoch príliš agresívne, zníž na 0.3. Ak sú slabé, zvýš na 0.7.
`Edit → Project Settings → Graphics → PC_Renderer → SSAO → Intensity`

- [ ] **Krok 4: Uložiť scénu**

`Ctrl+S` — uloží všetky zmeny v Demo.unity.
