using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class StatusEffectController : MonoBehaviour
{
    private Health health;
    private Dictionary<Type, Coroutine> activeEffects = new Dictionary<Type, Coroutine>();
    public event Action<Type> EffectStarted, EffectEnded;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += StopAllEffects;
    }

    public void Apply(StatusEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        if (health.IsDead)
            return;

        Type effectType = effect.GetType();
        bool wasAlreadyActive = activeEffects.ContainsKey(effectType);
        if (wasAlreadyActive)
            StopCoroutine(activeEffects[effectType]);
        activeEffects[effectType] = StartCoroutine(TickEffect(effect));
        if (!wasAlreadyActive)
            EffectStarted?.Invoke(effectType);
    }

    private void StopAllEffects(DamageInfo damageInfo)
    {
        StopAllEffects();
    }

    public void StopAllEffects()
    {
        foreach (KeyValuePair<Type, Coroutine> effect in activeEffects)
        {
            StopCoroutine(effect.Value);
            EffectEnded?.Invoke(effect.Key);
        }
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