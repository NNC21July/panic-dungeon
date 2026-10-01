using System;
using UnityEngine;

internal static class EnemyRulesChecks
{
    private static int checks;
    private class Target : IDamageable
    {
        public DamageInfo LastDamage;
        public bool TakeDamage(DamageInfo damage)
        {
            LastDamage = damage;
            return true;
        }
    }

    public static void Run()
    {
        Check(ZombieRules.MoveTowards(Vector2.zero, new Vector2(10f, 0f), 2f, 0.5f) == new Vector2(1f, 0f), "shared move speed and clock");
        Check(ZombieRules.MoveTowards(Vector2.zero, new Vector2(0.5f, 0f), 2f, 1f) == new Vector2(0.5f, 0f), "movement does not overshoot target");
        Check(ZombieRules.IsInAttackRange(Vector2.zero, new Vector2(0.8f, 0f), 0.8f), "attack range includes boundary");
        Check(!ZombieRules.IsInAttackRange(Vector2.zero, new Vector2(0.9f, 0f), 0.8f), "outside attack range");
        Check(ZombieRules.AdvanceCooldown(1f, 0.25f) == 0.75f, "cooldown uses supplied clock");
        Check(ZombieRules.AdvanceCooldown(0.1f, 1f) == 0f, "cooldown clamps at ready");
        Check(!ZombieRules.ShouldSwitchTarget(9f, 2.9f * 2.9f, 0.3f), "small distance advantage keeps target");
        Check(ZombieRules.ShouldSwitchTarget(9f, 2.6f * 2.6f, 0.3f), "clear distance advantage allows switch");
        Check(!ZombieRules.ShouldSwitchTarget(9f, 9f, 0f), "equal distance keeps target");
        Check(!ZombieRules.ShouldSwitchTarget(4f, 1.75f * 1.75f, 0.25f), "exact margin keeps target");
        Check(!ZombieRules.ShouldSwitchTarget(4f, 9f, 0.3f), "farther player never steals target");
        Check(ZombieRules.ShouldSwitchTarget(25f, 4f, 0.3f), "dash away makes nearer player eligible");
        Check(!ZombieRules.ShouldSwitchTarget(4f, float.PositiveInfinity, 0.3f), "missing challenger cannot steal target");
        var target = new Target();
        Check(ZombieRules.ApplyAttack(target, 15f, null) && target.LastDamage.Amount == 15f && target.LastDamage.Type == DamageType.Zombie,
            "shared attack preserves damage and type");
        Check(!ZombieRules.ApplyAttack(null, 15f, null), "missing target cannot be attacked");

        Vector2[] floor = { new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(8f, 0f) };
        Vector2[] players = { Vector2.zero };
        Check(EnemySpawnRules.TryFindPosition(floor, players, 3f, 0, _ => true, out Vector2 position) && position.x == 4f,
            "single-player clearance skips nearby tile");
        players = new[] { Vector2.zero, new Vector2(4f, 0f) };
        Check(EnemySpawnRules.TryFindPosition(floor, players, 3f, 0, _ => true, out position) && position.x == 8f,
            "multiplayer clearance checks every player");
        Check(!EnemySpawnRules.TryFindPosition(floor, players, 3f, 0, _ => false, out _), "blocked floor never forces a spawn");
        Check(!EnemySpawnRules.TryFindPosition(floor, Array.Empty<Vector2>(), 3f, 0, _ => true, out _), "no living players means no spawn");
        Check(!EnemySpawnRules.TryFindPosition(Array.Empty<Vector2>(), players, 3f, 0, _ => true, out _), "empty room has no spawn");
        players = new[] { new Vector2(20f, 0f) };
        Check(EnemySpawnRules.TryFindPosition(floor, players, 3f, 2, candidate => candidate.x == 0f, out position) && position.x == 0f,
            "random start wraps through all tiles");
        Check(EnemySpawnRules.TryFindPosition(new[] { new Vector2(3f, 0f) }, new[] { Vector2.zero }, 3f, 0, _ => true, out _),
            "minimum spawn distance includes boundary");
        Check(!EnemySpawnRules.TryFindPosition(floor, new[] { Vector2.zero, new Vector2(4f, 0f), new Vector2(8f, 0f) },
            3f, 0, _ => true, out _), "players covering every tile prevents spawning");
        Console.WriteLine($"Passed {checks} shared enemy checks.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        checks++;
    }
}
