using UnityEngine;

public static class SpawnSpaceChecker
{
    public static bool IsCircleAreaClear(Vector2 center, float radius, LayerMask blockedLayers)
    {
        Collider2D hit = Physics2D.OverlapCircle(center, radius, blockedLayers);
        return hit == null;
    }

    public static bool IsBoxAreaClear(Vector2 center, Vector2 size, LayerMask blockedLayers)
    {
        Collider2D hit = Physics2D.OverlapBox(center, size, blockedLayers);
        return hit == null;
    }
}