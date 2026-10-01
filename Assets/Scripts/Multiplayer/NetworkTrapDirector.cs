using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public partial class NetworkTrapDirector : NetworkBehaviour
{
    [SerializeField] private NetworkMatchManager matchManager;
    [SerializeField] private TrapController trapController;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private NetworkObject networkArrowPrefab, networkPoisonArrowPrefab;
    [SerializeField] private NetworkEnemySpawner enemySpawner;
    [Networked] private NetworkBool TrapsRunning { get; set; }
    [Networked] private TrapPatternId ActivePattern { get; set; }
    [Networked] private float ActiveWarningDuration { get; set; }
    [Networked] private int PatternSequence { get; set; }
    [Networked] private TickTimer NextPatternTimer { get; set; }
    [Networked] private TickTimer DifficultyTimer { get; set; }
    [Networked, Capacity(MaxSpikesPerRow * 2)]
    private NetworkArray<NetworkSpikeState> SpikeStates => default;
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
        InitializeSpikePlayback();

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
        CaptureSpikeStates();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (enemySpawner != null)
            enemySpawner.StopSpawning(runner);
        ClearSpikePlayback();
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
        if (Object.HasStateAuthority)
        {
            CaptureSpikeStates();
            enemySpawner.Tick(Runner, IsMatchPlaying, trapController.EnemySpawningPaused, trapController.DifficultyScaler.CurEnemySpawnInterval);
        }
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

        if (!Object.HasStateAuthority &&
            (ActivePattern == TrapPatternId.TopSpikes || ActivePattern == TrapPatternId.BottomSpikes))
        {
            trapController.CancelCurrentPattern();
            handledPatternSequence = PatternSequence;
            return;
        }

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
        if (!Object.HasStateAuthority)
            spike.SetPlaybackControlled(true);
    }

    private void UnsubscribeSpike(Spike spike)
    {
        spike.DamageContactHandler = null;
        spike.ResolveContactsLocally = true;
        if (!Object.HasStateAuthority)
            spike.SetPlaybackControlled(false);
    }

    private bool ResolveSpikeDamage(IDamageable damageable, DamageInfo damageInfo)
    {
        if (!Object.HasStateAuthority || !IsMatchPlaying)
            return false;
        if (damageable is NetworkHealth enemyHealth && enemyHealth.GetComponent<NetworkEnemy>() != null)
        {
            // Zombies are server-controlled and have no player owner to report contact.
            if (enemyHealth.Object == null || !enemyHealth.Object.IsValid || enemyHealth.Runner != Runner ||
                !enemyHealth.Object.HasStateAuthority || enemyHealth.IsDead)
                return false;
            return enemyHealth.TakeDamage(damageInfo);
        }
        // A remote player's owner reports visible contact instead of using the host's older player copy.
        if (damageable is NetworkHealth target && !target.Object.HasInputAuthority)
            return false;
        return damageable.TakeDamage(damageInfo);
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
