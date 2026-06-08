using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead;

    public event Action<DamageInfo> OnDamaged, OnDeath;


    void Awake()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || damageInfo.Amount <= 0f)
            return;
        currentHealth = Mathf.Max(currentHealth - damageInfo.Amount, 0f);
        OnDamaged?.Invoke(damageInfo);

        if (currentHealth <= 0f)
            Die(damageInfo);
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

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}
