using Fusion;
using UnityEngine;

public class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField, Min(0f)] private float shakeStrength = 0.04f;
    [SerializeField, Range(0f, 1f)] private float damagedVolume = 0.5f;
    [SerializeField] private AudioClip damagedSfx;
    private Health health;
    private NetworkHealth networkHealth;
    private NetworkObject networkObject;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        networkHealth = GetComponent<NetworkHealth>();
        if (networkHealth != null)
            networkObject = GetComponent<NetworkObject>();
        else
            health = GetComponent<Health>();
        if (networkHealth == null && health == null)
        {
            Debug.LogError(
                "PlayerDamageFeedback requires either Health or NetworkHealth.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (networkHealth != null)
            networkHealth.OnDamaged += HandleNetworkDamaged;
        else if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (networkHealth != null)
            networkHealth.OnDamaged -= HandleNetworkDamaged;
        else if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void HandleNetworkDamaged()
    {
        if (networkObject == null || !networkObject.HasInputAuthority)
            return;
        NetworkLocalPlayerUIBinder binder = GetComponent<NetworkLocalPlayerUIBinder>();
        if (binder != null && !binder.IsViewingOwnPlayer)
            return;
        PlayFeedback(networkHealth.PresentedDamageType);
    }

    // Called by the local viewer's binder for the selected remote player only.
    public void PlaySpectatorFeedback()
    {
        if (networkHealth != null && networkObject != null && networkObject.IsValid)
            PlayFeedback(networkHealth.PresentedDamageType);
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        PlayFeedback(damageInfo.Type);
    }

    private void PlayFeedback(DamageType damageType)
    {
        AudioManager.Instance?.PlaySfx(damagedSfx, damagedVolume);

        if (damageType == DamageType.Poison)
            return;
        CameraShake cameraShake = networkHealth != null
                ? CameraShake.FindInScene(gameObject.scene)
                : CameraShake.Instance;

        cameraShake?.AddShake(shakeStrength);
    }
}
