using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [Networked, OnChangedRender(nameof(HandleCurrentHealthChanged))] public float CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    public float MaxHealth => maxHealth;
    public event Action OnDamaged;
    public event Action OnHealthChanged;
    private float previouslyRenderedHealth;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
        }
        previouslyRenderedHealth = CurrentHealth;
    }

    public bool TakeDamage(DamageInfo damageInfo)
    {
        if (!Object.HasStateAuthority || IsDead || damageInfo.Amount <= 0f)
            return false;

        CurrentHealth = Mathf.Max(CurrentHealth - damageInfo.Amount, 0f);
        if (CurrentHealth <= 0f)
            IsDead = true;
        return true;
    }

    private void HandleCurrentHealthChanged()
    {
        bool tookDamage = CurrentHealth < previouslyRenderedHealth;
        previouslyRenderedHealth = CurrentHealth;
        OnHealthChanged?.Invoke();
        if (tookDamage)
            OnDamaged?.Invoke();
    }
}