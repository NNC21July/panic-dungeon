using System;

internal static class PoisonEffectChecks
{
    private static int checks;
    private class Target : IDamageable
    {
        public float Damage;
        public int Hits;
        public DamageType LastType;
        public bool TakeDamage(DamageInfo info)
        {
            Damage += info.Amount;
            Hits++;
            LastType = info.Type;
            return true;
        }
    }

    public static void Run()
    {
        PoisonEffect poison = new PoisonEffect(5f, 1.5f, 4f, null);
        var timer = new StatusEffectTimer(poison);
        Check(timer.Advance(0d) == 0 && !timer.IsComplete, "poison has no immediate extra tick");
        Check(timer.Advance(1d) == 0, "wait for first tick");
        Check(timer.Advance(0.5d) == 1, "first tick at interval boundary");
        Check(timer.Advance(1.5d) == 1, "second tick");
        Check(timer.Advance(1.5d) == 1 && !timer.IsComplete, "third tick before five-second expiry");
        Check(timer.Advance(0.5d) == 0 && timer.IsComplete, "remaining half second adds no extra damage");
        Check(timer.Advance(20d) == 0, "expired poison never ticks again");

        timer = new StatusEffectTimer(poison);
        Check(timer.Advance(9d) == 3 && timer.IsComplete, "long frame catches up without exceeding duration");
        timer = new StatusEffectTimer(new PoisonEffect(3f, 1.5f, 4f, null));
        Check(timer.Advance(3d) == 2, "tick exactly at expiry is included");
        timer = new StatusEffectTimer(new PoisonEffect(1f, 1.5f, 4f, null));
        Check(timer.Advance(1d) == 0 && timer.IsComplete, "short effect expires without an early tick");

        foreach (double step in new[] { 1d / 30d, 1d / 60d, 1d / 120d })
        {
            timer = new StatusEffectTimer(poison);
            int count = 0;
            for (int i = 0; i < 1000 && !timer.IsComplete; i++)
                count += timer.Advance(step);
            Check(count == 3 && timer.IsComplete, "same tick count with different clocks: " + step);
        }

        timer = new StatusEffectTimer(poison);
        timer.Advance(1d);
        timer = new StatusEffectTimer(poison); // another hit replaces the timer, it does not add a second one
        Check(timer.Advance(0.5d) == 0 && timer.Advance(1d) == 1, "refresh restarts the tick interval");
        var target = new Target();
        timer = new StatusEffectTimer(poison);
        int ticks = timer.Advance(5d);
        for (int i = 0; i < ticks; i++)
            poison.ApplyTick(target);
        Check(target.Hits == 3 && target.Damage == 12f, "shared poison damage applied three times");
        Check(target.LastType == DamageType.Poison, "poison type uses ordinary network feedback, not arrow-impact waiting");

        CheckThrows(() => new PoisonEffect(float.NaN, 1f, 4f, null), "reject NaN duration");
        CheckThrows(() => new PoisonEffect(5f, float.PositiveInfinity, 4f, null), "reject infinite interval");
        CheckThrows(() => new PoisonEffect(5f, 1f, float.NaN, null), "reject NaN damage");
        CheckThrows(() => new StatusEffectTimer(poison).Advance(-1d), "reject backwards clock");
        CheckThrows(() => new StatusEffectTimer(poison).Advance(double.NaN), "reject invalid clock");
        Console.WriteLine($"Passed {checks} shared poison checks.");
    }

    private static void CheckThrows(Action action, string name)
    {
        bool threw = false;
        try { action(); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, name);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        checks++;
    }
}
