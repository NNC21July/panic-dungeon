using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour
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

    public bool TryTakeDamage(float amount)
    {
        if (!Object.HasStateAuthority || IsDead || amount <= 0f)
            return false;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
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