using System.Collections.Generic;
using UnityEngine;

// Callers supply only safe tiles. Separation never takes priority over wall clearance.
public static class PlayerSpawnRules
{
    public static bool TryFindSpreadPosition(IReadOnlyList<Vector2> candidates, IReadOnlyList<Vector2> reserved,
        Vector2 roomCenter, float minimumDistance, out Vector2 position)
    {
        position = default;
        float bestDistanceSquared = -1f;
        foreach (Vector2 candidate in candidates)
        {
            float distanceSquared = (candidate - roomCenter).sqrMagnitude;
            if (reserved.Count > 0)
            {
                distanceSquared = float.PositiveInfinity;
                foreach (Vector2 used in reserved)
                    distanceSquared = Mathf.Min(distanceSquared, (candidate - used).sqrMagnitude);
                if (distanceSquared < minimumDistance * minimumDistance || distanceSquared <= 0f)
                    continue;
            }
            // A stable tie-break makes placement independent of the floor-list order.
            if (distanceSquared > bestDistanceSquared || (distanceSquared == bestDistanceSquared &&
                (candidate.x < position.x || (candidate.x == position.x && candidate.y < position.y))))
            {
                bestDistanceSquared = distanceSquared;
                position = candidate;
            }
        }
        return bestDistanceSquared >= 0f;
    }
}
