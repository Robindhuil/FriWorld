using UnityEngine;
using UnityEngine.AI;

public class Navigation : MonoBehaviour
{
    public Transform RoomDestination { get; set; }
    public Transform QuestDestination { get; set; }
    private NavMeshPath roomPath;
    private NavMeshPath questPath;
    private LineRenderer roomLineRenderer;
    private LineRenderer questLineRenderer;
    public bool DrawQuestLine { get; set; }
    public bool DrawRoomLine { get; set; }

    [SerializeField] private float lineHeightOffset = 0.4f;
    [SerializeField] private float segmentLength = 0.1f;

    private void Start()
    {
        roomPath = new NavMeshPath();
        questPath = new NavMeshPath();

        roomLineRenderer = CreateLineRenderer("RoomLine", Color.green);
        questLineRenderer = CreateLineRenderer("QuestLine", new Color(0.984f, 0.721f, 0f));
    }

    private void Update()
    {
        if (DrawQuestLine && QuestDestination != null)
        {
            CalculatePath(QuestDestination, questPath, questLineRenderer);
        }
        if (DrawRoomLine && RoomDestination != null)
        {
            CalculatePath(RoomDestination, roomPath, roomLineRenderer);
        }
    }

    public void CalculatePath(Transform destination, NavMeshPath path, LineRenderer renderer)
    {
        if (destination == null)
        {
            renderer.positionCount = 0;
            return;
        }

        NavMeshHit startHit, endHit;
        bool startOnNavMesh = NavMesh.SamplePosition(transform.position, out startHit, 2f, NavMesh.AllAreas);
        bool endOnNavMesh = NavMesh.SamplePosition(destination.position, out endHit, 2f, NavMesh.AllAreas);

        if (startOnNavMesh && endOnNavMesh)
        {
            NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path);
            UpdateLineRendererWithSegments(path, renderer);
        }
        else
        {
            renderer.positionCount = 0;
        }
    }

    /// <summary>
    /// Vykreslí čiaru po segmentoch s výškovou korekciou.
    /// </summary>
    private void UpdateLineRendererWithSegments(NavMeshPath path, LineRenderer renderer)
    {
        if (path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
        {
            renderer.positionCount = 0;
            return;
        }

        float totalDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);
        Vector3[] segmentPoints = new Vector3[segmentCount + 1];

        segmentPoints[0] = path.corners[0] + Vector3.up * lineHeightOffset;

        float distanceCovered = 0f;
        int currentCorner = 0;

        for (int i = 1; i <= segmentCount; i++)
        {
            distanceCovered += segmentLength;

            while (currentCorner < path.corners.Length - 1 &&
                   distanceCovered > Vector3.Distance(path.corners[currentCorner], path.corners[currentCorner + 1]))
            {
                distanceCovered -= Vector3.Distance(path.corners[currentCorner], path.corners[currentCorner + 1]);
                currentCorner++;
            }

            if (currentCorner >= path.corners.Length - 1)
            {
                segmentPoints[i] = path.corners[path.corners.Length - 1] + Vector3.up * lineHeightOffset;
                break;
            }

            Vector3 direction = (path.corners[currentCorner + 1] - path.corners[currentCorner]).normalized;
            Vector3 segmentPos = path.corners[currentCorner] + direction * distanceCovered;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(segmentPos, out hit, 2f, NavMesh.AllAreas))
            {
                segmentPoints[i] = hit.position + Vector3.up * lineHeightOffset;
            }
            else
            {
                segmentPoints[i] = segmentPos;
            }

            if (i >= 1)
            {
                float heightDifference = segmentPoints[i].y - segmentPoints[i - 1].y;
                if (Mathf.Abs(heightDifference) > 0.05f)
                {
                    float slopeAngle = Mathf.Atan2(heightDifference, segmentLength) * Mathf.Rad2Deg;
                }
            }
        }

        renderer.positionCount = segmentPoints.Length;
        renderer.SetPositions(segmentPoints);
    }

    private LineRenderer CreateLineRenderer(string name, Color color)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.parent = this.transform;

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        return lineRenderer;
    }

    public void ClearQuestPath()
    {
        QuestDestination = null;
        questPath = new NavMeshPath();
        questLineRenderer.positionCount = 0;
    }

    public void ClearRoomPath()
    {
        RoomDestination = null;
        roomPath = new NavMeshPath();
        roomLineRenderer.positionCount = 0;
    }
}