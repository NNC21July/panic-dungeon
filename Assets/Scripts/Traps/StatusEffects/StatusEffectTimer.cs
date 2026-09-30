using System;

// Shared timing math. The caller chooses the clock and applies the returned number of ticks.
public class StatusEffectTimer
{
    private readonly double duration, interval;
    private double elapsed;
    private int appliedTicks;
    public bool IsComplete => elapsed >= duration;

    public StatusEffectTimer(StatusEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        duration = effect.Duration;
        interval = effect.TickInterval;
    }

    public int Advance(double deltaTime)
    {
        if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime < 0d)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (IsComplete)
            return 0;
        elapsed = Math.Min(duration, elapsed + deltaTime);
        int dueTicks = (int)Math.Floor(elapsed / interval + 0.0000001d);
        int ticks = dueTicks - appliedTicks;
        appliedTicks = dueTicks;
        return ticks;
    }
}
