using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Arrow))]
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkArrow : NetworkBehaviour
{
    [SerializeField, Min(0.1f)] private float finishedRetentionDuration = 2f;
    [SerializeField] private bool logMovement;
    [Networked] private NetworkBool HasFinished { get; set; }
    [Networked] private NetworkBool HitSomething { get; set; }
    [Networked] private NetworkBool HasPoisonSplash { get; set; }
    [Networked] private Vector2 PoisonSplashPosition { get; set; }
    private TickTimer cleanupTimer;
    private Renderer[] arrowRenderers;
    private bool impactShown;
    private Arrow arrow;
    private Rigidbody2D rb;
    private NetworkTrapDirector trapDirector;
    private Vector2 fireDirection;
    private GameObject shooter;
    // temporary fields
    private bool hasMovementSample;
    private Vector2 sampledBodyPosition;
    private double sampledTime;

    private void Awake()
    {
        arrow = GetComponent<Arrow>();
        rb = GetComponent<Rigidbody2D>();
        arrowRenderers = GetComponentsInChildren<Renderer>(true);

        arrow.SimulateLocally = false;
    }

    public void Configure(Vector2 direction, GameObject source, NetworkTrapDirector director)
    {
        fireDirection = direction;
        shooter = source;
        trapDirector = director;
    }

    public override void Spawned()
    {
        bool isHost = Object.HasStateAuthority;
        impactShown = false;
        cleanupTimer = TickTimer.None;
        SetArrowVisible(true);

        arrow.SimulateLocally = isHost;
        arrow.FinishHandler = HandleFinished;

        rb.bodyType = isHost ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.simulated = isHost;

        if (isHost)
        {
            HasFinished = false;
            HitSomething = false;
            HasPoisonSplash = false;
            PoisonSplashPosition = Vector2.zero;

            rb.position = transform.position;
            arrow.Initialize(fireDirection, shooter);
        }
        if (arrow is PoisonArrow poisonArrow)
            poisonArrow.SplashHandler = RecordPoisonSplash;

        Runner.AddVisibilityNodes(gameObject);

        if (!isHost && HasFinished)
        {
            impactShown = true;
            SetArrowVisible(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (HasFinished)
        {
            if (cleanupTimer.Expired(Runner))
                Runner.Despawn(Object);
            return;
        }

        if (trapDirector == null || !trapDirector.IsMatchPlaying)
        {
            arrow.SimulateLocally = false;
            Runner.Despawn(Object);
        }
    }

    // temporary function
    private void LateUpdate()
    {
        if (!logMovement || Object == null || !Object.IsValid ||
            !Object.HasStateAuthority)
        {
            hasMovementSample = false;
            return;
        }

        double now = Time.timeAsDouble;

        if (!hasMovementSample)
        {
            sampledBodyPosition = rb.position;
            sampledTime = now;
            hasMovementSample = true;
            return;
        }

        double elapsed = now - sampledTime;
        if (elapsed < 0.25d)
            return;

        float measuredSpeed =
            Vector2.Distance(sampledBodyPosition, rb.position) / (float)elapsed;

        Debug.Log(
            $"Arrow {Object.Id}: measured body speed = {measuredSpeed:F1}, " +
            $"body position = {rb.position}, " +
            $"display position = {(Vector2)transform.position}",
            this
        );

        sampledBodyPosition = rb.position;
        sampledTime = now;
    }

    public override void Render()
    {
        if (impactShown)
            return;

        if (Object.HasStateAuthority)
        {
            if (HasFinished)
                ShowImpact();
            return;
        }

        if (!TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out float alpha))
            return;

        var finishedReader = GetPropertyReader<NetworkBool>(nameof(HasFinished));

        if (finishedReader.Read(fromBuffer))
            ShowImpact();
    }

    private void ShowImpact()
    {
        if (impactShown)
            return;

        impactShown = true;
        SetArrowVisible(false);

        if (HitSomething)
            arrow.PlayHitSound();

        if (HasPoisonSplash && arrow is PoisonArrow poisonArrow)
        {
            ParticleSystem splash = poisonArrow.PlaySplash(PoisonSplashPosition);
            Runner.AddVisibilityNodes(splash.gameObject);
        }
    }

    private void HandleFinished(Arrow finishedArrow, bool hitSomething)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        HitSomething = hitSomething;
        HasFinished = true;

        arrow.SimulateLocally = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        cleanupTimer = TickTimer.CreateFromSeconds(Runner, finishedRetentionDuration);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        arrow.SimulateLocally = false;
        arrow.FinishHandler = null;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        trapDirector = null;
        shooter = null;

        if (arrow is PoisonArrow poisonArrow)
            poisonArrow.SplashHandler = null;
    }

    private void SetArrowVisible(bool visible)
    {
        foreach (Renderer arrowRenderer in arrowRenderers)
            arrowRenderer.forceRenderingOff = !visible;
    }

    private void RecordPoisonSplash(Vector2 position)
    {
        if (Object == null || !Object.IsValid ||
            !Object.HasStateAuthority || HasFinished)
            return;

        HasPoisonSplash = true;
        PoisonSplashPosition = position;
    }
}