using UnityEngine;
using System;

public class PoisonArrow : Arrow
{
    [SerializeField, Min(0.01f)] private float duration, tickInterval, tickDamage;
    [SerializeField] private ParticleSystem poisonSplash;
    public Action<Vector2> SplashHandler { get; set; }

    protected override void OnSuccessfulHit(Collider2D other, Vector2 impactPos)
    {
        IPoisonReceiver poisonReceiver = other.GetComponentInParent<IPoisonReceiver>();

        // Enemies take the arrow's impact damage and splash, but not the poison status.
        if (poisonReceiver != null && other.gameObject.layer != GameLayers.Enemy)
            poisonReceiver.ApplyPoison(
                new PoisonEffect(duration, tickInterval, tickDamage, Shooter));

        if (SplashHandler != null)
        {
            SplashHandler(impactPos);
            return;
        }
        PlaySplash(impactPos);
    }

    public ParticleSystem PlaySplash(Vector2 position)
    {
        ParticleSystem splash = Instantiate(poisonSplash, position, Quaternion.identity);

        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(splash.gameObject, gameObject.scene);

        splash.Play();
        return splash;
    }
}
