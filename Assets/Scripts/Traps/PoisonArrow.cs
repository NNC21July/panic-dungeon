using UnityEngine;
using System;

public class PoisonArrow : Arrow
{
    [SerializeField, Min(0.01f)] private float duration, tickInterval, tickDamage;
    [SerializeField] private ParticleSystem poisonSplash;
    public Action<Vector2> SplashHandler { get; set; }

    protected override void OnSuccessfulHit(Collider2D other)
    {
        StatusEffectController statusEffectController = other.GetComponentInParent<StatusEffectController>();

        if (statusEffectController != null)
            statusEffectController.Apply(
                new PoisonEffect(duration, tickInterval, tickDamage, Shooter));

        Vector2 impactPos = other.ClosestPoint(transform.position);

        if (SplashHandler != null)
        {
            SplashHandler(impactPos);
            return;
        }

        ParticleSystem splash = Instantiate(poisonSplash, impactPos, Quaternion.identity);

        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(splash.gameObject, gameObject.scene);

        splash.Play();
    }
}
