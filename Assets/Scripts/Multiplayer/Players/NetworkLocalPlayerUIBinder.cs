using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkHealth))]
public class NetworkLocalPlayerUIBinder : NetworkBehaviour
{
    private PoisonStatusUI poisonStatus;
    private HealthUI healthUI;
    private DamageVignetteUI damageVignette;
    private NetworkHealth viewedHealth;
    private PlayerDamageFeedback viewedFeedback;
    private int firstDamageSequence;
    public bool IsViewingOwnPlayer => viewedHealth != null && viewedHealth.gameObject == gameObject;
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;
        NetworkHealth networkHealth = GetComponent<NetworkHealth>();

        healthUI = FindInScene<HealthUI>();
        if (healthUI == null)
            Debug.LogWarning("No HealthUI found in the local player's scene.", this);

        damageVignette = FindInScene<DamageVignetteUI>();

        poisonStatus = FindInScene<PoisonStatusUI>();
        if (poisonStatus != null)
        {
            poisonStatus.enabled = true;
        }
        BindView(networkHealth);
    }

    public void BindView(NetworkHealth target)
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority)
            return;
        if (target != null && (target.Object == null || !target.Object.IsValid || target.Runner != Runner))
            return;
        UnsubscribeViewedDamage();
        viewedHealth = target;
        viewedFeedback = target != null ? target.GetComponent<PlayerDamageFeedback>() : null;
        firstDamageSequence = target != null ? target.DamageSequence : 0;
        healthUI?.Bind(target);
        damageVignette?.Bind(target);
        poisonStatus?.Bind(target);
        GetComponent<NetworkLocalPlayerCamera>()?.ResetFeedback();
        // Our own player already plays feedback through PlayerDamageFeedback.
        if (viewedHealth != null && !IsViewingOwnPlayer)
            viewedHealth.OnDamaged += HandleViewedDamage;
    }

    private void HandleViewedDamage()
    {
        if (viewedHealth == null || viewedHealth.Object == null || !viewedHealth.Object.IsValid ||
            viewedHealth.PresentedDamageSequence <= firstDamageSequence || !Runner.GetVisible())
            return;
        viewedFeedback?.PlaySpectatorFeedback();
    }

    private void UnsubscribeViewedDamage()
    {
        if (viewedHealth != null)
            viewedHealth.OnDamaged -= HandleViewedDamage;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeViewedDamage();
        viewedHealth = null;
        viewedFeedback = null;
        healthUI?.Bind(null);
        damageVignette?.Bind(null);
        if (poisonStatus != null)
            poisonStatus.Bind(null);
        poisonStatus = null;
    }

    private T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            T component =
                root.GetComponentInChildren<T>(
                    includeInactive: true
                );
            if (component != null)
                return component;
        }
        return null;
    }
}
