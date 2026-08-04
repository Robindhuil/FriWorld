using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    public Camera Cam { get; set; }

    [SerializeField, Tooltip("Horizontal mouse sensitivity.")]
    private float xSensitivity = 800f;

    [SerializeField, Tooltip("Vertical mouse sensitivity.")]
    private float ySensitivity = 800f;

    [SerializeField, Tooltip("Maximum upward look angle.")]
    private float maxLookUpAngle = 80f;

    [SerializeField, Tooltip("Maximum downward look angle.")]
    private float maxLookDownAngle = -80f;

    [SerializeField, Tooltip("Largest mouse delta accepted in a single frame, in pixels. Guards against the one-frame spike browsers can emit when pointer lock is (re)acquired.")]
    private float maxFrameDelta = 300f;

    // DPI conversion factor: DPI / DPI_DIVISOR = degrees of rotation per pixel of mouse movement.
    // (e.g., 6000 makes 800 DPI = 0.1333 deg/px, which matches how the old
    // Time.deltaTime-scaled code felt at 60 fps)
    private const float DPI_DIVISOR = 6000f;

    private float xRotation = 0f;
    private bool invertX = false;
    private bool invertY = false;

    private void Awake()
    {
        if (Cam == null)
        {
            Cam = GetComponentInChildren<Camera>();
            if (Cam == null)
            {
                Debug.LogError("[PlayerLook] Camera not found. Please assign a Camera component.");
            }
        }

        // Web swaps MSAA for FXAA; no-op on every other platform.
        WebRenderDefaults.ApplyToCamera(Cam);

        float dpiValue = PlayerPrefs.GetFloat("MouseSensitivity", 800f);
        xSensitivity = ConvertDPIToSensitivity(dpiValue);
        ySensitivity = xSensitivity;
        invertX = PlayerPrefs.GetInt("InvertX", 0) == 1;
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;

        Debug.Log($"[PlayerLook] PlayerLook initialized - DPI: {dpiValue}, Normalized Sensitivity: {xSensitivity}, InvertX: {invertX}, InvertY: {invertY}");
    }

    public void ProcessLook(Vector2 input)
    {
        // input comes from <Mouse>/delta, which is already the movement accumulated over this
        // frame - not a rate. Scaling it by Time.deltaTime would make rotation grow with dt^2,
        // so a single long frame turns into a camera snap. Steady vsync hides that in the editor;
        // WebGL's uneven frame pacing does not.
        if (input.sqrMagnitude > maxFrameDelta * maxFrameDelta)
            input = input.normalized * maxFrameDelta;

        float mouseX = input.x * xSensitivity;
        float mouseY = input.y * ySensitivity;

        if (invertX)
            mouseX = -mouseX;
        
        if (invertY)
            mouseY = -mouseY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, maxLookDownAngle, maxLookUpAngle);
        Cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    public void SetSensitivity(float newDPI)
    {
        // Convert DPI to normalized sensitivity
        float normalizedSensitivity = ConvertDPIToSensitivity(Mathf.Clamp(newDPI, 400f, 3200f));
        xSensitivity = normalizedSensitivity;
        ySensitivity = xSensitivity;
        Debug.Log($"[PlayerLook] DPI {newDPI} set - Normalized Sensitivity: {xSensitivity}");
    }

    /// <summary>
    /// Converts DPI value to normalized sensitivity and sets it.
    /// Conversion formula: DPI / DPI_DIVISOR = normalized sensitivity
    /// </summary>
    /// <param name="dpiValue">The DPI value (e.g., 400, 800, 1600, 3200)</param>
    public void SetDPISensitivity(float dpiValue)
    {
        float normalizedSensitivity = dpiValue / DPI_DIVISOR;
        SetSensitivity(normalizedSensitivity);
        Debug.Log($"[PlayerLook] DPI value {dpiValue} converted to sensitivity {normalizedSensitivity}");
    }

    /// <summary>
    /// Converts a DPI value to normalized sensitivity without setting it.
    /// </summary>
    /// <param name="dpiValue">The DPI value to convert</param>
    /// <returns>Normalized sensitivity value</returns>
    public float ConvertDPIToSensitivity(float dpiValue)
    {
        return dpiValue / DPI_DIVISOR;
    }
    
    public void SetInvertX(bool isInverted)
    {
        invertX = isInverted;
        Debug.Log($"[PlayerLook] Invert X updated to: {isInverted}");
    }

    public void SetInvertY(bool isInverted)
    {
        invertY = isInverted;
        Debug.Log($"[PlayerLook] Invert Y updated to: {isInverted}");
    }
    
    public float GetSensitivity()
    {
        return xSensitivity;
    }
    
    public bool GetInvertX()
    {
        return invertX;
    }
    
    public bool GetInvertY()
    {
        return invertY;
    }

    public string GetCurrentSettings()
    {
        return $"Sensitivity: {xSensitivity}, InvertX: {invertX}, InvertY: {invertY}";
    }
}