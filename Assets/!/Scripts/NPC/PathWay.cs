using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Spravuje cestu s bodmi a umožňuje vizualizáciu v editori.
/// </summary>
public class PathWay : MonoBehaviour
{
    public List<Transform> waypoints;
    [SerializeField] private bool alwaysDrawPath;
    [SerializeField] private bool drawAsLoop;
    [SerializeField] private bool drawNumbers;
    public Color debugColour = Color.white;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (alwaysDrawPath)
        {
            DrawPath();
        }
    }

    /// <summary>
    /// Kreslí cestu medzi waypointmi.
    /// </summary>
    public void DrawPath()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            GUIStyle labelStyle = CreateLabelStyle();

            if (drawNumbers)
            {
                Handles.Label(waypoints[i].position, i.ToString(), labelStyle);
            }

            if (i >= 1)
            {
                Gizmos.color = debugColour;
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);

                if (drawAsLoop && i == waypoints.Count - 1)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                }
            }
        }
    }

    /// <summary>
    /// Kreslí cestu, keď je objekt vybraný v editore.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!alwaysDrawPath)
        {
            DrawPath();
        }
    }

    /// <summary>
    /// Vytvorí štýl pre označenie bodu.
    /// </summary>
    /// <returns>Vytvorený štýl.</returns>
    private GUIStyle CreateLabelStyle()
    {
        GUIStyle labelStyle = new GUIStyle
        {
            fontSize = 30,
            normal = { textColor = debugColour }
        };
        return labelStyle;
    }
#endif
}