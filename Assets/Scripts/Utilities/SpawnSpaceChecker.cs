using UnityEngine;

public static class SpawnSpaceChecker
{
    public static bool IsCircleAreaClear(Vector2 center, float radius, LayerMask blockedLayers, PhysicsScene2D physicsScene)
    {
        Collider2D hit = physicsScene.OverlapCircle(center, radius, blockedLayers);
        return hit == null;
    }

    public static bool IsBoxAreaClear(Vector2 center, Vector2 size, LayerMask blockedLayers, PhysicsScene2D physicsScene)
    {
        Collider2D hit = physicsScene.OverlapBox(center, size, 0f, blockedLayers);
        return hit == null;
    }
}