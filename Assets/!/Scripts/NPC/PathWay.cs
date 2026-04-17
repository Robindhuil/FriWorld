using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Spravuje cestu s bodmi a umožňuje vizualizáciu v editori.
/// </summary>
public class PathWay : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints;
    public List<Transform> Waypoints => waypoints;
    [SerializeField] private bool alwaysDrawPath;
    [SerializeField] private bool drawAsLoop;
    [SerializeField] private bool drawNumbers;
    private Color debugColour = Color.white;

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
        for (int i = 0; i < Waypoints.Count; i++)
        {
            GUIStyle labelStyle = CreateLabelStyle();

            if (drawNumbers)
            {
                Handles.Label(Waypoints[i].position, i.ToString(), labelStyle);
            }

            if (i >= 1)
            {
                Gizmos.color = debugColour;
                Gizmos.DrawLine(Waypoints[i - 1].position, Waypoints[i].position);

                if (drawAsLoop && i == Waypoints.Count - 1)
                {
                    Gizmos.DrawLine(Waypoints[i].position, Waypoints[0].position);
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