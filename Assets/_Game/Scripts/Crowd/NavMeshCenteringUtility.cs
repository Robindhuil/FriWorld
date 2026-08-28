using UnityEngine;
using UnityEngine.AI;

public enum NavMeshPathRenderMode
{
    Default = 0,
    Centered = 1,
}

public static class NavMeshCenteringUtility
{
    private const float MinClearanceSum = 0.001f;

    public static bool TryCalculateNormalizedEdgeBias(
        Vector3 worldPoint,
        Vector3 forward,
        float probeOffset,
        float startDistance,
        float deadZone,
        out float normalizedBias,
        out Vector3 right
    )
    {
        normalizedBias = 0f;
        right = Vector3.right;

        Vector3 flattenedForward = forward;
        flattenedForward.y = 0f;
        if (flattenedForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        flattenedForward.Normalize();
        right = Vector3.Cross(Vector3.up, flattenedForward).normalized;

        Vector3 leftProbe = worldPoint - right * probeOffset;
        Vector3 rightProbe = worldPoint + right * probeOffset;

        if (!TryGetEdgeClearance(leftProbe, out float leftClearance))
        {
            return false;
        }

        if (!TryGetEdgeClearance(rightProbe, out float rightClearance))
        {
            return false;
        }

        if (leftClearance >= startDistance && rightClearance >= startDistance)
        {
            normalizedBias = 0f;
            return true;
        }

        float sum = Mathf.Max(MinClearanceSum, leftClearance + rightClearance);
        normalizedBias = (rightClearance - leftClearance) / sum;

        if (Mathf.Abs(normalizedBias) < deadZone)
        {
            normalizedBias = 0f;
        }

        return true;
    }

    public static bool TryGetCenteredPoint(
        Vector3 worldPoint,
        Vector3 forward,
        float probeOffset,
        float startDistance,
        float deadZone,
        float maxLateralOffset,
        out Vector3 centeredPoint
    )
    {
        centeredPoint = worldPoint;

        if (
            !TryCalculateNormalizedEdgeBias(
                worldPoint,
                forward,
                probeOffset,
                startDistance,
                deadZone,
                out float normalizedBias,
                out Vector3 right
            )
        )
        {
            return false;
        }

        Vector3 offsetPoint = worldPoint + right * (normalizedBias * maxLateralOffset);

        if (NavMesh.SamplePosition(offsetPoint, out NavMeshHit sampledHit, 1f, NavMesh.AllAreas))
        {
            centeredPoint = sampledHit.position;
            return true;
        }

        return false;
    }

    public static bool TryGetEdgeClearance(Vector3 worldPoint, out float clearance)
    {
        clearance = 0f;

        if (!NavMesh.SamplePosition(worldPoint, out NavMeshHit sampledHit, 1f, NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMesh.FindClosestEdge(sampledHit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            return false;
        }

        clearance = edgeHit.distance;
        return true;
    }
}
