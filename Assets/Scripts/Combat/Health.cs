using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float curHealth;
    private bool isDead, damageEnabled;
    public float CurHealth => curHealth;
    public float MaxHealth => maxHealth;
    public float CurHealthPercent => curHealth / maxHealth;
    public bool IsDead => isDead;

    public event Action<DamageInfo> OnDamaged, OnDeath;
    public event Action OnHealthChanged;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        curHealth = maxHealth;
        isDead = false;
        damageEnabled = true;
    }

    public bool TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || damageInfo.Amount <= 0f || !damageEnabled)
            return false;
        curHealth = Mathf.Max(curHealth - damageInfo.Amount, 0f);
        OnDamaged?.Invoke(damageInfo);
        OnHealthChanged?.Invoke();
        if (curHealth <= 0f)
            Die(damageInfo);
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        curHealth = Mathf.Min(curHealth + amount, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void ResetHealth()
    {
        isDead = false;
        curHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    public void SetDamageEnabled(bool enabled)
    {
        damageEnabled = enabled;
    }

    private void Die(DamageInfo damageInfo)
    {
        if (isDead)
            return;
        isDead = true;
        OnDeath?.Invoke(damageInfo);
    }
}
