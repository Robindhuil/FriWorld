using System.Linq;
using TMPro;
using UnityEngine;

public class FpsUI : MonoBehaviour
{
    private TextMeshProUGUI fps;
    private Canvas fpsCanvas;
    private float updateInterval = 0.5f;
    private float timeSinceLastUpdate = 0f;

    void Start()
    {
        fpsCanvas = GetComponent<Canvas>();
        fps = fpsCanvas.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(b => b.name == "Fps");
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            UpdateFPS();
            timeSinceLastUpdate = 0f;
        }
    }

    public void UpdateFPS()
    {
        float currentFPS = 1.0f / Time.deltaTime;
        fps.text = currentFPS.ToString("F0");
    }
}
