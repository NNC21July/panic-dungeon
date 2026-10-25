using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public partial class NetworkHealth : NetworkBehaviour, IDamageable, IPoisonReceiver
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
    public int PresentedDamageSequence { get; private set; }
    public bool SuppressPresentedFlash { get; private set; }
    private readonly HashSet<(NetworkId, int)> shownSwordContacts = new();
    private readonly Queue<(NetworkId, int)> swordContactOrder = new();
    private int initialDamageSequence;
    private NetworkMatchManager resultsManager;
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
        InitializePoison();
        resultsManager = null;
        shownSwordContacts.Clear();
        swordContactOrder.Clear();
        SuppressPresentedFlash = false;
        initialDamageSequence = DamageSequence;
        PresentedDamageSequence = DamageSequence;
        PresentedDamageType = DamageType.Normal;
        feedbackWaitingForImpact.Clear();
        earlyArrowImpacts.Clear();
        shownDamageFeedback.Clear();
        feedbackReadyAfterTimeout.Clear();
    }

    public bool TakeDamage(DamageInfo damageInfo)
    {
        return ApplyDamage(damageInfo, default, 0);
    }

    public bool TakeSwordDamage(DamageInfo damageInfo, NetworkId attacker, int swingTick)
    {
        return ApplyDamage(damageInfo, attacker, swingTick);
    }

    private bool ApplyDamage(DamageInfo damageInfo, NetworkId attacker, int swingTick)
    {
        if (!Object.HasStateAuthority || IsDead || damageInfo.Amount <= 0f)
            return false;

        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - damageInfo.Amount, 0f);
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            ClearPoison();
        }
        // Only the server records actual damage; visual feedback never changes match statistics.
        if (GetComponent<NetworkPlayerIdentity>() != null)
        {
            if (resultsManager == null || resultsManager.Object == null || !resultsManager.Object.IsValid)
                foreach (NetworkMatchManager manager in FindObjectsByType<NetworkMatchManager>())
                    if (manager.Runner == Runner && manager.Object != null && manager.Object.IsValid)
                    {
                        resultsManager = manager;
                        break;
                    }
            resultsManager?.RecordPlayerDamage(Object, attacker, previousHealth - CurrentHealth, IsDead);
        }
        DamageSequence++;
        RPC_QueueDamageFeedback(DamageSequence, damageInfo.Type, attacker, swingTick);
        return true;
    }

    private void HandleCurrentHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable)]
    private void RPC_QueueDamageFeedback(int sequence, DamageType damageType, NetworkId attacker, int swingTick)
    {
        if (sequence <= initialDamageSequence ||
            shownDamageFeedback.Contains(sequence)) // already shown or historical
            return;
        if (damageType != DamageType.Arrow)
        {
            SuppressPresentedFlash = damageType == DamageType.Melee && attacker.IsValid &&
                !RememberSwordContact(attacker, swingTick);
            ShowDamageFeedback(sequence, damageType);
            SuppressPresentedFlash = false;
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
        PresentedDamageSequence = sequence;
        OnDamaged?.Invoke();
    }

    public void PreviewSwordContact(NetworkId attacker, int swingTick)
    {
        if (Object == null || !Object.IsValid || IsDead || !attacker.IsValid ||
            !RememberSwordContact(attacker, swingTick))
            return;
        // Cosmetic only: do not raise OnDamaged or change health for an unconfirmed hit.
        GetComponent<DamageFlash>()?.PlayFlash();
    }

    private bool RememberSwordContact(NetworkId attacker, int swingTick)
    {
        var key = (attacker, swingTick);
        if (!shownSwordContacts.Add(key))
            return false;
        swordContactOrder.Enqueue(key);
        // Bound rejected/unconfirmed previews too; no confirmation is required for cleanup.
        if (swordContactOrder.Count > 128)
            shownSwordContacts.Remove(swordContactOrder.Dequeue());
        return true;
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
        poisonEffect = null;
        shownSwordContacts.Clear();
        swordContactOrder.Clear();
        poisonTimer = null;
        feedbackWaitingForImpact.Clear();
        earlyArrowImpacts.Clear();
        shownDamageFeedback.Clear();
        feedbackReadyAfterTimeout.Clear();
    }
}
