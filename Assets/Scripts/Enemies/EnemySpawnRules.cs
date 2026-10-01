using System;
using System.Collections.Generic;
using UnityEngine;

public static class EnemySpawnRules
{
    public static bool TryFindPosition(IReadOnlyList<Vector2> floor, IReadOnlyList<Vector2> players,
        float minDistance, int startIndex, Func<Vector2, bool> isClear, out Vector2 position)
    {
        position = default;
        if (floor.Count == 0 || players.Count == 0)
            return false;
        float minDistanceSquared = minDistance * minDistance;
        for (int i = 0; i < floor.Count; i++)
        {
            Vector2 candidate = floor[(startIndex + i) % floor.Count];
            bool farEnough = true;
            foreach (Vector2 player in players)
                if ((player - candidate).sqrMagnitude < minDistanceSquared)
                {
                    farEnough = false;
                    break;
                }
            if (farEnough && isClear(candidate))
            {
                position = candidate;
                return true;
            }
        }
        return false;
    }
}
