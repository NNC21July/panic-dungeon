using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class StatusEffectController : MonoBehaviour, IPoisonReceiver
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

    public void ApplyPoison(PoisonEffect effect)
    {
        Apply(effect);
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
        StatusEffectTimer timer = new StatusEffectTimer(effect);
        while (!timer.IsComplete)
        {
            yield return null;
            int ticks = timer.Advance(Time.deltaTime);
            for (int i = 0; i < ticks; i++)
            {
                if (health.IsDead)
                    yield break;
                effect.ApplyTick(health);
                if (health.IsDead)
                    yield break;
            }
        }
        Type effectType = effect.GetType();
        activeEffects.Remove(effectType);
        EffectEnded?.Invoke(effectType);
    }
}
