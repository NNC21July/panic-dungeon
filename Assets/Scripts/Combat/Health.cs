using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<DamageInfo> OnDamaged, OnDeath;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    public bool TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || damageInfo.Amount <= 0f)
            return false;
        currentHealth = Mathf.Max(currentHealth - damageInfo.Amount, 0f);
        OnDamaged?.Invoke(damageInfo);

        if (currentHealth <= 0f)
            Die(damageInfo);
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die(DamageInfo damageInfo)
    {
        if (isDead)
            return;
        isDead = true;
        OnDeath?.Invoke(damageInfo);
    }
}
