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
        sunColorGradient = new Gradient();
        var colors = new GradientColorKey[]
        {
            new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0f),
            new GradientColorKey(new Color(1f, 0.953f, 0.839f), 0.5f),
            new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f),
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

        float sunAngle = Mathf.Lerp(10f, 170f, t);
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
    }
}
