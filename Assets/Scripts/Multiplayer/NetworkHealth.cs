using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHealth : NetworkBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0.1f)] private float arrowFeedbackTimeout = 0.75f;
    [Networked, OnChangedRender(nameof(HandleCurrentHealthChanged))] public float CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] public int DamageSequence { get; private set; }
    public float MaxHealth => maxHealth;
    public event Action OnDamaged;
    public event Action OnHealthChanged;
    public DamageType PresentedDamageType { get; private set; }
    private int initialDamageSequence;
    private readonly Dictionary<int, double> feedbackWaitingForImpact = new(); // confirmed arrow hits waiting for their visual impact; each with deadline
    private readonly HashSet<int> earlyArrowImpacts = new(); // visual impacts that arrived before their damage notification
    private readonly HashSet<int> shownDamageFeedback = new(); // events already presented; so don't play twice
    private readonly List<int> feedbackReadyAfterTimeout = new();


    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            DamageSequence = 0;
        }
        initialDamageSequence = DamageSequence;
        PresentedDamageType = DamageType.Normal;
        feedbackWaitingForImpact.Clear();
        earlyArrowImpacts.Clear();
        shownDamageFeedback.Clear();
        feedbackReadyAfterTimeout.Clear();
    }

    public bool TakeDamage(DamageInfo damageInfo)
    {
        if (!Object.HasStateAuthority || IsDead || damageInfo.Amount <= 0f)
            return false;

        CurrentHealth = Mathf.Max(CurrentHealth - damageInfo.Amount, 0f);
        if (CurrentHealth <= 0f)
            IsDead = true;
        DamageSequence++;
        RPC_QueueDamageFeedback(DamageSequence, damageInfo.Type);
        return true;
    }

    private void HandleCurrentHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable)]
    private void RPC_QueueDamageFeedback(int sequence, DamageType damageType)
    {
        if (sequence <= initialDamageSequence ||
            shownDamageFeedback.Contains(sequence)) // already shown or historical
            return;
        if (damageType != DamageType.Arrow)
        {
            ShowDamageFeedback(sequence, damageType);
            return;
        }
        if (earlyArrowImpacts.Remove(sequence))
        {
            ShowDamageFeedback(sequence, damageType);
            return;
        }
        if (!feedbackWaitingForImpact.ContainsKey(sequence))
            feedbackWaitingForImpact.Add(sequence, Time.unscaledTimeAsDouble + arrowFeedbackTimeout);
    }

    private void ShowDamageFeedback(int sequence, DamageType damageType)
    {
        if (sequence <= initialDamageSequence ||
            !shownDamageFeedback.Add(sequence))
            return;

        feedbackWaitingForImpact.Remove(sequence);
        earlyArrowImpacts.Remove(sequence);
        PresentedDamageType = damageType;
        OnDamaged?.Invoke();
    }

    public void NotifyArrowImpactShown(int sequence)
    {
        if (Object == null || !Object.IsValid ||
            sequence <= initialDamageSequence ||
            shownDamageFeedback.Contains(sequence))
            return;

        if (feedbackWaitingForImpact.ContainsKey(sequence)) // can play player feedback when the arrow impact has been shown
        {
            ShowDamageFeedback(sequence, DamageType.Arrow);
            return;
        }
        earlyArrowImpacts.Add(sequence); // if not then wait for damage notif
    }

    public override void Render()
    {
        feedbackReadyAfterTimeout.Clear();
        double now = Time.unscaledTimeAsDouble;

        foreach (KeyValuePair<int, double> pending in feedbackWaitingForImpact)
            if (now >= pending.Value)
                feedbackReadyAfterTimeout.Add(pending.Key);

        foreach (int sequence in feedbackReadyAfterTimeout)
            ShowDamageFeedback(sequence, DamageType.Arrow);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        feedbackWaitingForImpact.Clear();
        earlyArrowImpacts.Clear();
        shownDamageFeedback.Clear();
        feedbackReadyAfterTimeout.Clear();
    }
}