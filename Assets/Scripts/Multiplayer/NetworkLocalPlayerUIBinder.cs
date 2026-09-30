using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkHealth))]
public class NetworkLocalPlayerUIBinder : NetworkBehaviour
{
    private PoisonStatusUI poisonStatus;
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;
        NetworkHealth networkHealth = GetComponent<NetworkHealth>();

        HealthUI healthUI = FindInScene<HealthUI>();
        if (healthUI != null)
            healthUI.Bind(networkHealth);
        else
            Debug.LogWarning("No HealthUI found in the local player's scene.", this);

        DamageVignetteUI damageVignette = FindInScene<DamageVignetteUI>();
        if (damageVignette != null)
            damageVignette.Bind(networkHealth);

        poisonStatus = FindInScene<PoisonStatusUI>();
        if (poisonStatus != null)
        {
            poisonStatus.Bind(networkHealth);
            poisonStatus.enabled = true;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
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
