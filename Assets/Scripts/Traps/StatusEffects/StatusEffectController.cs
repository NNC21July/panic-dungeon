using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class StatusEffectController : MonoBehaviour
{
    private Health health;
    private Dictionary<Type, Coroutine> activeEffects;
    public event Action<Type> EffectStarted, EffectEnded;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += StopAllEffects;
        activeEffects = new Dictionary<Type, Coroutine>();
    }

    public void Apply(StatusEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException("Status effect null");

        if (health.IsDead)
            return;

        Type effectType = effect.GetType();
        if (activeEffects.ContainsKey(effectType))
            StopCoroutine(activeEffects[effectType]);
        activeEffects[effectType] = StartCoroutine(TickEffect(effect));
        EffectStarted?.Invoke(effectType);
    }

    private void StopAllEffects(DamageInfo damageInfo)
    {
        StopAllEffects();
    }

    public void StopAllEffects()
    {
        StopAllCoroutines();
        foreach (Type effect in activeEffects.Keys)
            EffectEnded?.Invoke(effect);
        activeEffects.Clear();
    }

    private void OnDestroy()
    {
        health.OnDeath -= StopAllEffects;
    }

    public bool IsEffectActive(Type effectType)
    {
        return activeEffects.ContainsKey(effectType);
    }

    private IEnumerator TickEffect(StatusEffect effect)
    {
        float timer = effect.Duration;
        while (timer - effect.TickInterval >= 0f)
        {
            yield return new WaitForSeconds(effect.TickInterval);
            effect.ApplyTick(health);
            timer -= effect.TickInterval;
        }
        Type effectType = effect.GetType();
        activeEffects.Remove(effectType);
        EffectEnded?.Invoke(effectType);
    }
}