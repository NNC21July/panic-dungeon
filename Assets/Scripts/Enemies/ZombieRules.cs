using UnityEngine;

// Shared decisions and calculations; callers supply the target, clock and damage authority.
public static class ZombieRules
{
    public static Vector2 MoveTowards(Vector2 position, Vector2 target, float speed, float deltaTime)
    {
        return Vector2.MoveTowards(position, target, speed * deltaTime);
    }

    public static bool IsInAttackRange(Vector2 position, Vector2 target, float range)
    {
        return (target - position).sqrMagnitude <= range * range;
    }

    public static float AdvanceCooldown(float remaining, float deltaTime)
    {
        return Mathf.Max(0f, remaining - deltaTime);
    }

    public static bool ShouldSwitchTarget(float currentDistanceSquared, float candidateDistanceSquared, float margin)
    {
        return Mathf.Sqrt(candidateDistanceSquared) + Mathf.Max(0f, margin) < Mathf.Sqrt(currentDistanceSquared);
    }

    public static bool ApplyAttack(IDamageable target, float damage, GameObject source)
    {
        return target != null && target.TakeDamage(new DamageInfo(damage, source, DamageType.Zombie));
    }
}
