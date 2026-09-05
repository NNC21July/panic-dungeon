using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkTrapDirector : NetworkBehaviour
{
    [SerializeField] private NetworkMatchManager matchManager;
    [SerializeField] private TrapController trapController;
    [SerializeField] private TrapSetup trapSetup;
    [Networked] private NetworkBool TrapsRunning { get; set; }
    [Networked] private TrapPatternId ActivePattern { get; set; }
    [Networked] private float ActiveWarningDuration { get; set; }
    [Networked] private int PatternSequence { get; set; }
    [Networked] private TickTimer NextPatternTimer { get; set; }
    [Networked] private TickTimer DifficultyTimer { get; set; }
    private int handledPatternSequence = -1;
    private bool locallyRunning;

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

        if (!Object.HasStateAuthority)
            return;

        TrapsRunning = false;
        ActivePattern = TrapPatternId.None;
        NextPatternTimer = TickTimer.None;
        DifficultyTimer = TickTimer.None;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
            UpdateAuthorityState();
        ApplyNetworkStateLocally();
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
    }

    private void StartNextPattern()
    {
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
}