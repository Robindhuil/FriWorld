using UnityEngine;

/// <summary>
/// Keeps only nearby, looked-at room signs (<see cref="RoomDisplay"/>) rendering, so all
/// ~160 world-space canvases aren't active at once. Data is loaded once by RoomDisplay in
/// Start; this only toggles each sign's Canvas on a timer. Put on a persistent object.
/// </summary>
public class RoomSignManager : MonoBehaviour
{
    [Tooltip("Signs closer than this (metres) may be shown.")]
    [SerializeField] private float activateDistance = 12f;

    [Tooltip("How much the camera must face the sign: 1 = straight ahead, 0 ≈ 90° cone, -1 = anything.")]
    [SerializeField, Range(-1f, 1f)] private float facingDot = 0.3f;

    [Tooltip("Seconds between visibility checks (160 checks is cheap; no per-frame cost).")]
    [SerializeField] private float checkInterval = 0.2f;

    private Transform cam;
    private RoomDisplay[] signs;
    private float timer;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        signs = FindObjectsByType<RoomDisplay>(FindObjectsSortMode.None);
        foreach (var s in signs)
            if (s != null) s.SetVisible(false);
    }

    private void Update()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        Vector3 p = cam.position, f = cam.forward;
        float d2 = activateDistance * activateDistance;

        foreach (var s in signs)
        {
            if (s == null) continue;

            Vector3 to = s.transform.position - p;
            float dist2 = to.sqrMagnitude;
            bool on = dist2 <= d2 && (dist2 < 0.04f || Vector3.Dot(f, to.normalized) >= facingDot);
            s.SetVisible(on);
        }
    }
}
