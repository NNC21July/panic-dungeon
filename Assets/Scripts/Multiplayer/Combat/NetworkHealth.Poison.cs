using System;
using Fusion;
using UnityEngine;

public partial class NetworkHealth
{
    [Networked, OnChangedRender(nameof(HandlePoisonChanged))]
    public NetworkBool IsPoisoned { get; private set; }
    public event Action OnPoisonChanged;
    private PoisonEffect poisonEffect;
    private StatusEffectTimer poisonTimer;
    private NetworkMatchManager poisonMatchManager;

    private void InitializePoison()
    {
        poisonEffect = null;
        poisonTimer = null;
        poisonMatchManager = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            NetworkMatchManager candidate = root.GetComponentInChildren<NetworkMatchManager>(true);
            if (candidate != null && candidate.Runner == Runner)
            {
                poisonMatchManager = candidate;
                break;
            }
        }
        if (Object.HasStateAuthority)
            IsPoisoned = false;
    }

    public void ApplyPoison(PoisonEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority || IsDead ||
            poisonMatchManager == null || poisonMatchManager.State != NetworkMatchState.Playing)
            return;
        // Like single-player: another poison hit refreshes the effect instead of stacking extra timers.
        poisonEffect = effect;
        poisonTimer = new StatusEffectTimer(effect);
        IsPoisoned = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !Runner.IsForward)
            return;
        if (IsDead || poisonMatchManager == null || poisonMatchManager.State != NetworkMatchState.Playing)
        {
            ClearPoison();
            return;
        }
        if (poisonEffect == null || poisonTimer == null)
            return;
        int ticks = poisonTimer.Advance(Runner.DeltaTime);
        for (int i = 0; i < ticks && !IsDead; i++)
            poisonEffect.ApplyTick(this);
        if (poisonTimer != null && poisonTimer.IsComplete)
            ClearPoison();
    }

    private void ClearPoison()
    {
        poisonEffect = null;
        poisonTimer = null;
        IsPoisoned = false;
    }

    private void HandlePoisonChanged()
    {
        OnPoisonChanged?.Invoke();
    }
}
