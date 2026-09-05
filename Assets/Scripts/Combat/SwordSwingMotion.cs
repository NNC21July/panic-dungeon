using UnityEngine;

public static class SwordSwingMotion
{
    public static float CalculateAngle(
        Vector2 direction,
        float progress,
        float swingAngle,
        AnimationCurve swingCurve)
    {
        float curvedProgress = Mathf.Clamp01(
                    swingCurve.Evaluate(Mathf.Clamp01(progress))
                ),
                baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                startAngle = baseAngle - swingAngle / 2f,
                endAngle = baseAngle + swingAngle / 2f;

        return Mathf.Lerp(startAngle, endAngle, curvedProgress);
    }
}
