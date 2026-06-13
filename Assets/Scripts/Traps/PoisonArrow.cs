using UnityEngine;

public class PoisonArrow : Arrow
{
    [SerializeField, Min(0.01f)] private float duration, tickInterval, tickDamage;

    protected override void OnSuccessfulHit(Collider2D other)
    {
        StatusEffectController statusEffectController = other.GetComponentInParent<StatusEffectController>();
        if (statusEffectController == null)
            return;

        statusEffectController.Apply(new PoisonEffect(duration, tickInterval, tickDamage, Shooter));
    }
}