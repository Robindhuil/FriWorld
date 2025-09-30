using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    public Camera Cam { get; set; }

    [SerializeField, Tooltip("Horizontal mouse sensitivity.")]
    private float xSensitivity = 30f;

    [SerializeField, Tooltip("Vertical mouse sensitivity.")]
    private float ySensitivity = 30f;

    [SerializeField, Tooltip("Maximum upward look angle.")]
    private float maxLookUpAngle = 80f;

    [SerializeField, Tooltip("Maximum downward look angle.")]
    private float maxLookDownAngle = -80f;

    private float xRotation = 0f;
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

        xSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 30f);
        ySensitivity = xSensitivity;
        invertY = PlayerPrefs.GetInt("InvertMouseY", 0) == 1;

        Debug.Log($"[PlayerLook] PlayerLook initialized - Sensitivity: {xSensitivity}, InvertY: {invertY}");
    }

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x * xSensitivity * Time.deltaTime;
        float mouseY = input.y * ySensitivity * Time.deltaTime;

        if (invertY)
            mouseY = -mouseY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, maxLookDownAngle, maxLookUpAngle);
        Cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    public void SetSensitivity(float newSensitivity)
    {
        xSensitivity = newSensitivity;
        ySensitivity = newSensitivity;
        Debug.Log($"[PlayerLook] Sensitivity updated to: {newSensitivity}");
    }

    public void SetInvertY(bool isInverted)
    {
        invertY = isInverted;
        Debug.Log($"[PlayerLook] Invert Y updated to: {isInverted}");
    }

    public string GetCurrentSettings()
    {
        return $"Sensitivity: {xSensitivity}, InvertY: {invertY}";
    }
}