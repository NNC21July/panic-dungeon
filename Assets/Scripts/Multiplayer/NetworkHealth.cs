using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [Networked] public float CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    public float MaxHealth => maxHealth;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public bool TryTakeDamage(float amount)
    {
        if (!Object.HasStateAuthority || IsDead || amount <= 0f)
            return false;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        if (CurrentHealth <= 0f)
            IsDead = true;
        return true;
    }
}