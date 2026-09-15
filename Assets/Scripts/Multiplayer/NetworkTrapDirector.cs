using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkTrapDirector : NetworkBehaviour
{
    [SerializeField] private NetworkMatchManager matchManager;
    [SerializeField] private TrapController trapController;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private NetworkObject networkArrowPrefab, networkPoisonArrowPrefab;
    [Networked] private NetworkBool TrapsRunning { get; set; }
    [Networked] private TrapPatternId ActivePattern { get; set; }
    [Networked] private float ActiveWarningDuration { get; set; }
    [Networked] private int PatternSequence { get; set; }
    [Networked] private TickTimer NextPatternTimer { get; set; }
    [Networked] private TickTimer DifficultyTimer { get; set; }
    [Networked, Capacity(MaxSpikesPerRow)]
    private NetworkArray<NetworkSpikeState> ActiveSpikeStates => default;
    private const int MaxSpikesPerRow = 64;
    private int handledPatternSequence = -1;
    private bool locallyRunning;
    public bool IsMatchPlaying =>
    matchManager != null && matchManager.State == NetworkMatchState.Playing;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);

        handledPatternSequence = -1;
        locallyRunning = false;
        RegisterTrapVisibility();
        RegisterSpikeCallbacks();

        foreach (ArrowShooter shooter in trapSetup.LeftArrowShooters)
            RegisterShooter(shooter);
        foreach (ArrowShooter shooter in trapSetup.RightArrowShooters)
            RegisterShooter(shooter);

        if (!Object.HasStateAuthority)
            return;

        TrapsRunning = false;
        ActivePattern = TrapPatternId.None;
        NextPatternTimer = TickTimer.None;
        DifficultyTimer = TickTimer.None;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        foreach (Spike spike in trapSetup.TopSpikes)
            UnsubscribeSpike(spike);
        foreach (Spike spike in trapSetup.BottomSpikes)
            UnsubscribeSpike(spike);

        foreach (ArrowShooter shooter in trapSetup.LeftArrowShooters)
            UnsubscribeShooter(shooter);
        foreach (ArrowShooter shooter in trapSetup.RightArrowShooters)
            UnsubscribeShooter(shooter);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
            UpdateAuthorityState();
        ApplyNetworkStateLocally();
        ApplyAuthoritativeSpikeStates();
    }

    private void UpdateAuthorityState()
    {
        if (matchManager.State != NetworkMatchState.Playing)
        {
            if (TrapsRunning)
                StopNetworkTraps();
            return;
        }
        if (!TrapsRunning)
        {
            BeginNetworkTraps();
            return;
        }
        if (DifficultyTimer.Expired(Runner))
        {
            trapController.IncreaseDifficulty();
            DifficultyTimer = TickTimer.CreateFromSeconds(Runner, trapController.DifficultyScaleInterval);
        }
        if (NextPatternTimer.Expired(Runner))
            StartNextPattern();
    }

    private void BeginNetworkTraps()
    {
        trapController.ResetForNewRound();
        locallyRunning = true;
        TrapsRunning = true;

        DifficultyTimer = TickTimer.CreateFromSeconds(Runner, trapController.DifficultyScaleInterval);
        StartNextPattern();
    }

    private void StopNetworkTraps()
    {
        TrapsRunning = false;
        ActivePattern = TrapPatternId.None;
        NextPatternTimer = TickTimer.None;
        DifficultyTimer = TickTimer.None;
        if (Object.HasStateAuthority)
            ClearSpikeStates();
    }

    private Arrow SpawnNetworkArrow(ArrowShooter shooter, Arrow selectedPrefab, Vector2 direction)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return null;
        if (matchManager.State != NetworkMatchState.Playing)
            return null;

        NetworkObject prefab = selectedPrefab is PoisonArrow
            ? networkPoisonArrowPrefab
            : networkArrowPrefab;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        NetworkObject spawnedArrow = Runner.Spawn(
            prefab,
            shooter.transform.position,
            Quaternion.Euler(0f, 0f, angle),
            onBeforeSpawned: (runner, obj) =>
            {
                obj.GetComponent<NetworkArrow>().Configure(
                    direction,
                    shooter.gameObject,
                    this
                );
            }
        );

        return spawnedArrow != null
            ? spawnedArrow.GetComponent<Arrow>()
            : null;
    }

    private void StartNextPattern()
    {
        if (Object.HasStateAuthority)
            ClearSpikeStates();

        TrapPatternId selectedPattern = trapController.SelectRandomPatternId();
        float warningDuration = trapController.CurrentWarningDuration;

        ActivePattern = selectedPattern;
        ActiveWarningDuration = warningDuration;
        PatternSequence++;

        float patternDelay = trapController.GetPatternDuration(selectedPattern, warningDuration) + trapController.CurrentPatternInterval;

        NextPatternTimer = TickTimer.CreateFromSeconds(Runner, patternDelay);
    }

    private void ApplyNetworkStateLocally()
    {
        if (!TrapsRunning) // traps not should run
        {
            if (locallyRunning)
            {
                trapController.CancelCurrentPattern();
                trapController.ResetTraps();
                locallyRunning = false;
            }
            return;
        }
        if (!locallyRunning)
        {
            trapController.ResetForNewRound();
            locallyRunning = true;
        }
        if (handledPatternSequence == PatternSequence)
            return;
        if (ActivePattern == TrapPatternId.None)
            return;

        if (trapController.StartPattern(ActivePattern, ActiveWarningDuration))
            handledPatternSequence = PatternSequence;
    }

    private void RegisterTrapVisibility()
    {
        foreach (Spike spike in trapSetup.TopSpikes)
            Runner.AddVisibilityNodes(spike.gameObject);
        foreach (Spike spike in trapSetup.BottomSpikes)
            Runner.AddVisibilityNodes(spike.gameObject);
        foreach (ArrowShooter shooter in trapSetup.LeftArrowShooters)
            Runner.AddVisibilityNodes(shooter.gameObject);
        foreach (ArrowShooter shooter in trapSetup.RightArrowShooters)
            Runner.AddVisibilityNodes(shooter.gameObject);
    }

    private void RegisterSpikeCallbacks()
    {
        foreach (Spike spike in trapSetup.TopSpikes)
            RegisterSpike(spike);
        foreach (Spike spike in trapSetup.BottomSpikes)
            RegisterSpike(spike);
    }

    private void RegisterSpike(Spike spike)
    {
        spike.ResolveContactsLocally = Object.HasStateAuthority;
        spike.DamageContactHandler = ResolveSpikeDamage;
        spike.CollisionStopped += HandleSpikeStopped;
    }

    private void UnsubscribeSpike(Spike spike)
    {
        spike.CollisionStopped -= HandleSpikeStopped;
        spike.DamageContactHandler = null;
        spike.ResolveContactsLocally = true;
    }

    private bool ResolveSpikeDamage(IDamageable damageable, DamageInfo damageInfo)
    {
        return Object.HasStateAuthority ? damageable.TakeDamage(damageInfo) : false;
    }

    private IReadOnlyList<Spike> GetActiveSpikeRow()
    {
        return ActivePattern switch
        {
            TrapPatternId.TopSpikes => trapSetup.TopSpikes,
            TrapPatternId.BottomSpikes => trapSetup.BottomSpikes,
            _ => null
        };
    }

    private void HandleSpikeStopped(Spike spike, float blockedPathProgress)
    {
        if (!Object.HasStateAuthority)
            return;

        IReadOnlyList<Spike> activeSpikeRow = GetActiveSpikeRow();
        if (activeSpikeRow == null)
            return;
        if (activeSpikeRow.Count > MaxSpikesPerRow)
            return;

        int spikeIdx = -1;
        for (int i = 0; i < activeSpikeRow.Count; i++)
        {
            if (spike.Equals(activeSpikeRow[i]))
            {
                spikeIdx = i;
                break;
            }
        }
        if (spikeIdx < 0)
            return;

        ActiveSpikeStates.Set(spikeIdx, new NetworkSpikeState { IsStopped = true, StopProgress = blockedPathProgress });
    }

    private void ClearSpikeStates()
    {
        for (int i = 0; i < ActiveSpikeStates.Length; i++)
            ActiveSpikeStates.Set(i, default);
    }

    private void ApplyAuthoritativeSpikeStates()
    {
        IReadOnlyList<Spike> activeSpikeRow = GetActiveSpikeRow();
        if (activeSpikeRow == null)
            return;
        for (int i = 0; i < Mathf.Min(activeSpikeRow.Count, ActiveSpikeStates.Length); i++)
        {
            NetworkSpikeState spikeState = ActiveSpikeStates[i];
            if (spikeState.IsStopped)
                activeSpikeRow[i].ApplyAuthoritativeStop(spikeState.StopProgress);
        }

    }

    private void RegisterShooter(ArrowShooter shooter)
    {
        shooter.CanSpawnArrows = Object.HasStateAuthority;
        shooter.SpawnHandler = SpawnNetworkArrow;
    }

    private void UnsubscribeShooter(ArrowShooter shooter)
    {
        shooter.CanSpawnArrows = false;
        shooter.SpawnHandler = null;
    }
}