using UnityEngine;

public static class PolygonContactDetector
{
    public static bool TouchesCircle(Matrix4x4 arrowMatrix, Vector2[] outline, Vector2 offset, Vector2 center, float radius)
    {
        bool inside = false;
        float radiusSquared = radius * radius;

        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = arrowMatrix.MultiplyPoint3x4(outline[i] + offset);
            Vector2 b = arrowMatrix.MultiplyPoint3x4(
                outline[(i + 1) % outline.Length] + offset
            );

            Vector2 edge = b - a;
            float fraction = edge.sqrMagnitude > 0f
                ? Mathf.Clamp01(Vector2.Dot(center - a, edge) / edge.sqrMagnitude)
                : 0f;

            Vector2 closestPoint = a + edge * fraction;
            if ((center - closestPoint).sqrMagnitude <= radiusSquared)
                return true;

            if ((a.y > center.y) != (b.y > center.y))
            {
                float crossingX =
                    a.x + (center.y - a.y) * (b.x - a.x) / (b.y - a.y);

                if (center.x < crossingX)
                    inside = !inside;
            }
        }
        return inside;
    }

    private static float PointSegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;

        float fraction = direction.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector2.Dot(point - start, direction) / direction.sqrMagnitude) : 0f;

        Vector2 closestPoint = start + direction * fraction;
        return (point - closestPoint).sqrMagnitude;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static float SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 firstDirection = b - a;
        Vector2 secondDirection = d - c;
        Vector2 difference = c - a;

        float denominator = Cross(firstDirection, secondDirection);

        if (denominator != 0f)
        {
            float firstFraction = Cross(difference, secondDirection) / denominator;
            float secondFraction = Cross(difference, firstDirection) / denominator;

            if (firstFraction >= 0f && firstFraction <= 1f &&
                secondFraction >= 0f && secondFraction <= 1f)
                return 0f;
        }

        float firstDistance = Mathf.Min(
            PointSegmentDistanceSquared(a, c, d),
            PointSegmentDistanceSquared(b, c, d)
        );

        float secondDistance = Mathf.Min(
            PointSegmentDistanceSquared(c, a, b),
            PointSegmentDistanceSquared(d, a, b)
        );

        return Mathf.Min(firstDistance, secondDistance);
    }

    public static bool SweptTouchesCircle(Matrix4x4 arrowMatrix, Vector2[] outline, Vector2 offset, Vector2 start, Vector2 end, float radius)
    {
        if (TouchesCircle(arrowMatrix, outline, offset, start, radius))
            return true;

        float radiusSquared = radius * radius;

        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = arrowMatrix.MultiplyPoint3x4(outline[i] + offset);
            Vector2 b = arrowMatrix.MultiplyPoint3x4(
                outline[(i + 1) % outline.Length] + offset
            );

            if (SegmentDistanceSquared(start, end, a, b) <= radiusSquared)
                return true;
        }

        return false;
    }

    public static bool TouchesCircle(Transform arrowTransform, Vector2[] outline, Vector2 offset, Vector2 center, float radius)
    {
        return TouchesCircle(arrowTransform.localToWorldMatrix, outline, offset, center, radius);
    }

    public static bool TryGetContactFraction(Matrix4x4 arrowMatrix, Vector2[] outline, Vector2 offset, Vector2 start, Vector2 end, float radius, out float fraction)
    {
        fraction = 0f;
        if (TouchesCircle(arrowMatrix, outline, offset, start, radius))
            return true;
        if (!SweptTouchesCircle(arrowMatrix, outline, offset, start, end, radius))
            return false;

        // Find the first touching part of the path, not just its ending position.
        float before = 0f, after = 1f;
        for (int i = 0; i < 12; i++)
        {
            float middle = (before + after) * 0.5f;
            if (SweptTouchesCircle(arrowMatrix, outline, offset, start, Vector2.Lerp(start, end, middle), radius))
                after = middle;
            else
                before = middle;
        }
        fraction = after;
        return true;
    }

    public static bool SweptTouchesCircle(Transform arrowTransform, Vector2[] outline, Vector2 offset, Vector2 start, Vector2 end, float radius)
    {
        return SweptTouchesCircle(arrowTransform.localToWorldMatrix, outline, offset, start, end, radius);
    }
}
