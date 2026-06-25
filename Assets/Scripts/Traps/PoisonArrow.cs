using UnityEngine;

public class PoisonArrow : Arrow
{
    [SerializeField, Min(0.01f)] private float duration, tickInterval, tickDamage;
    [SerializeField] private ParticleSystem poisonSplash;

    protected override void Awake()
    {
        base.Awake();
        SerializedFieldValidator.Validate(this);
    }

    protected override void OnSuccessfulHit(Collider2D other)
    {
        StatusEffectController statusEffectController = other.GetComponentInParent<StatusEffectController>();
        if (statusEffectController == null)
            return;

        statusEffectController.Apply(new PoisonEffect(duration, tickInterval, tickDamage, Shooter));

        Vector2 impactPos = other.ClosestPoint(transform.position);
        Instantiate(poisonSplash, impactPos, Quaternion.identity).Play();
    }
}
