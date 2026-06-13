using System;
using UnityEngine;

public class PoisonEffect : StatusEffect
{
    private readonly float tickDamage;
    private readonly GameObject source;

    public PoisonEffect(float duration, float tickInterval, float tickDamage, GameObject source) : base(duration, tickInterval)
    {
        if (tickDamage <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tickDamage), tickDamage, "Poison tick damage must be greater than zero");
        this.tickDamage = tickDamage;
        this.source = source;
    }

    public override void ApplyTick(IDamageable target)
    {
        target.TakeDamage(new DamageInfo(tickDamage, source, DamageType.Poison));
    }
}