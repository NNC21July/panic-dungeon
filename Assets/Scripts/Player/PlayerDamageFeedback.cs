using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField, Min(0f)] private float shakeDuration = 0.1f, shakeStrength = 0.04f;
    [SerializeField, Range(0f, 1f)] private float damagedVolume = 0.5f;
    [SerializeField] private AudioClip damagedSfx;
    private Health health;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        AudioManager.Instance?.PlaySfx(damagedSfx, damagedVolume);
        if (damageInfo.Type != DamageType.Poison)
            CameraShake.Instance?.AddShake(shakeDuration, shakeStrength);
    }
}