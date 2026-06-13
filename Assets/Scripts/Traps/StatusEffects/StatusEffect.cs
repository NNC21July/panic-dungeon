using System;

public abstract class StatusEffect
{
    public float Duration { get; }
    public float TickInterval { get; }

    protected StatusEffect(float duration, float tickInterval)
    {
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Status effect duration must be greater than zero");
        Duration = duration;

        if (tickInterval <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tickInterval), tickInterval, "Status effect tick interval must be greater than zero");
        TickInterval = tickInterval;
    }

    public abstract void ApplyTick(IDamageable target);
}