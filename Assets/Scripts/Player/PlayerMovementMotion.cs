using UnityEngine;

public static class PlayerMovementMotion
{
    public static Vector2 CalculateVelocity(
        Vector2 currentVelocity,
        Vector2 moveInput,
        float moveSpeed,
        float acceleration,
        float deceleration,
        float deltaTime)
    {
        Vector2 direction = moveInput.normalized,
                targetVelocity = direction * moveSpeed;
        float changeRate = direction == Vector2.zero
                ? deceleration
                : acceleration;

        return Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            changeRate * deltaTime
        );
    }

    public static Vector2 CalculateDashVelocity(
        Vector2 direction,
        float dashSpeed,
        float progress,
        AnimationCurve dashSpeedCurve)
    {
        float speedMultiplier = Mathf.Max(
            0f,
            dashSpeedCurve.Evaluate(Mathf.Clamp01(progress))
        );

        return direction * dashSpeed * speedMultiplier;
    }
}
