using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Arrow))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PolygonCollider2D))]
public class NetworkArrow : NetworkBehaviour
{
    [SerializeField, Min(0.1f)] private float finishedRetentionDuration = 2f;
    [SerializeField, Min(0.1f)] private float historyDuration = 2f;
    [Networked] private NetworkBool HasFinished { get; set; }
    [Networked] private NetworkBool HitSomething { get; set; }
    [Networked] private NetworkBool HasPoisonSplash { get; set; }
    [Networked] private Vector2 PoisonSplashPosition { get; set; }
    [Networked] private NetworkId DamagedTargetId { get; set; }
    [Networked] private int DamagedTargetSequence { get; set; }
    [Networked] private double ContactTime { get; set; }
    public double HostSpawnTime { get; private set; }
    private TickTimer cleanupTimer;
    private Renderer[] arrowRenderers;
    private bool impactShown, splashShown, localReportPending;
    private int presentedDamageSequence;
    private double finishedTime;
    private Arrow arrow;
    private Rigidbody2D rb;
    private NetworkTrapDirector trapDirector;
    private Vector2 fireDirection;
    private GameObject shooter;
    private Vector2 prevArrowPosition;
    private PolygonCollider2D arrowCollider;
    private Vector2[] arrowOutline;
    private int prevContactFrame = -1, prevContactCorrection;
    private NetworkId prevContactPlayer;
    private Quaternion prevArrowRotation;
    private double prevArrowRenderTime;
    private NetworkTransform networkTransform;
    private ContactRenderSample prevArrowSample;
    private PlayerContactSample prevPlayerSample;
    private Vector3 arrowScale;
    private readonly ContactHistory<NetworkTRSPData> hostArrowHistory = new();

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        arrow = GetComponent<Arrow>();
        rb = GetComponent<Rigidbody2D>();
        arrowRenderers = GetComponentsInChildren<Renderer>(true);
        arrowCollider = GetComponent<PolygonCollider2D>();
        arrowOutline = arrowCollider.GetPath(0);
        networkTransform = GetComponent<NetworkTransform>();

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
        splashShown = false;
        localReportPending = false;
        presentedDamageSequence = 0;
        finishedTime = double.PositiveInfinity;
        cleanupTimer = TickTimer.None;
        SetArrowVisible(true);

        arrow.SimulateLocally = isHost;
        arrow.FinishHandler = HandleFinished;
        arrow.ContactDamageFilter = CanDamageOnContact;
        arrow.DamageDealt += RecordDamagedTarget;

        rb.bodyType = isHost ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.simulated = isHost;

        if (isHost)
        {
            HasFinished = false;
            HitSomething = false;
            HasPoisonSplash = false;
            PoisonSplashPosition = Vector2.zero;
            DamagedTargetId = default;
            DamagedTargetSequence = 0;
            ContactTime = 0d;
            HostSpawnTime = Runner.SimulationTime;

            rb.position = transform.position;
            arrow.Initialize(fireDirection, shooter);
        }
        if (arrow is PoisonArrow poisonArrow)
            poisonArrow.SplashHandler = RecordPoisonSplash;

        Runner.AddVisibilityNodes(gameObject);

        if (!isHost && HasFinished)
        {
            impactShown = true;
            splashShown = true;
            presentedDamageSequence = DamagedTargetSequence;
            SetArrowVisible(false);
        }
        prevContactFrame = -1;
        hostArrowHistory.Clear();
        arrowScale = transform.localScale;
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

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid)
            return;
        RecordHostArrowHistory();
        // NetworkTransform has rendered; check the final visible interval before hiding.
        CheckLocalPlayerContact();
        PresentFinishedState();
    }

    private void PresentFinishedState()
    {
        if (DamagedTargetSequence > presentedDamageSequence && DamagedTargetId.IsValid)
        {
            bool isLocalTarget = Runner.TryFindObject(DamagedTargetId, out NetworkObject target) && target.HasInputAuthority;
            if (Object.HasStateAuthority || isLocalTarget || Object.RenderTime >= ContactTime)
                ShowImpact();
            return;
        }

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
        if (!impactShown && HitSomething)
            arrow.PlayHitSound();
        impactShown = true;
        localReportPending = false;
        SetArrowVisible(false);

        if (!splashShown && HasPoisonSplash && arrow is PoisonArrow poisonArrow)
        {
            splashShown = true;
            ParticleSystem splash = poisonArrow.PlaySplash(PoisonSplashPosition);
            Runner.AddVisibilityNodes(splash.gameObject);
        }

        NotifyTargetOfArrowImpact();
    }

    private void HandleFinished(Arrow finishedArrow, bool hitSomething)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        HitSomething = hitSomething;
        if (!HasFinished)
            finishedTime = Runner.SimulationTime;
        HasFinished = true;

        arrow.SimulateLocally = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        cleanupTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(finishedRetentionDuration, historyDuration + 0.25f));
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        arrow.SimulateLocally = false;
        arrow.FinishHandler = null;
        arrow.ContactDamageFilter = null;
        arrow.DamageDealt -= RecordDamagedTarget;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        trapDirector = null;
        shooter = null;

        if (arrow is PoisonArrow poisonArrow)
            poisonArrow.SplashHandler = null;
        prevContactFrame = -1;
        hostArrowHistory.Clear();
    }

    private void SetArrowVisible(bool visible)
    {
        foreach (Renderer arrowRenderer in arrowRenderers)
            arrowRenderer.forceRenderingOff = !visible;
    }

    private bool CanDamageOnContact(Collider2D other)
    {
        NetworkHealth target = other.GetComponentInParent<NetworkHealth>();
        // Host-owned players use local physics. Remote owners report their visible contact.
        return target == null || (target.Object != null && target.Object.IsValid && target.Object.HasInputAuthority);
    }

    public void ResolveLocalReport(bool accepted)
    {
        localReportPending = false;
        if (!accepted && !impactShown)
            SetArrowVisible(true);
        prevContactFrame = -1;
    }

    public bool TryResolveReportedContact(NetworkHealth target, CircleCollider2D targetCollider,
        ContactRenderSample start, ContactRenderSample end, PlayerContactSample playerStart, PlayerContactSample playerEnd,
        out bool accepted)
    {
        accepted = false;
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority ||
            trapDirector == null || !trapDirector.IsMatchPlaying || DamagedTargetId.IsValid ||
            target == null || target.Object == null || !target.Object.IsValid || target.Runner != Runner || target.IsDead)
            return true;

        // A report can arrive before this frame's snapshot recorder has run.
        if (!hostArrowHistory.ContainsKey(start.FromTick) || !hostArrowHistory.ContainsKey(start.ToTick) ||
            !hostArrowHistory.ContainsKey(end.FromTick) || !hostArrowHistory.ContainsKey(end.ToTick))
            return false;

        Vector3 scale = targetCollider.transform.lossyScale;
        float radius = targetCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (!TryCheckReportedContact(start, end, playerStart.Center, playerEnd.Center, radius,
            out bool touched, out float fraction, out Vector2 impactPosition) || !touched)
            return true;

        double startTime = GetSampleTime(start), endTime = GetSampleTime(end);
        double impactTime = startTime + (endTime - startTime) * fraction;
        // A retained wall-hit arrow is evidence, not a stationary hazard.
        if (HasFinished && impactTime > finishedTime + Runner.DeltaTime)
            return true;

        if (!arrow.TryApplyDamage(targetCollider, impactPosition))
            return true;

        ContactTime = impactTime;
        HandleFinished(arrow, true);
        accepted = true;
        return true;
    }

    private double GetSampleTime(ContactRenderSample sample)
    {
        return sample.GetTime(Runner.DeltaTime);
    }

    private void RecordDamagedTarget(IDamageable target)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;
        if (target is not NetworkHealth targetHealth)
            return;
        if (targetHealth.Object == null || !targetHealth.Object.IsValid)
            return;

        DamagedTargetId = targetHealth.Object.Id;
        DamagedTargetSequence = targetHealth.DamageSequence;
        ContactTime = Runner.SimulationTime;
    }

    private void RecordPoisonSplash(Vector2 position)
    {
        if (Object == null || !Object.IsValid ||
            !Object.HasStateAuthority)
            return;

        HasPoisonSplash = true;
        PoisonSplashPosition = position;
    }

    private void NotifyTargetOfArrowImpact()
    {
        if (!DamagedTargetId.IsValid || DamagedTargetSequence <= presentedDamageSequence)
            return;

        if (!Runner.TryFindObject(
            DamagedTargetId,
            out NetworkObject targetObject))
            return;

        NetworkHealth targetHealth = targetObject.GetComponent<NetworkHealth>();

        if (targetHealth != null)
        {
            targetHealth.NotifyArrowImpactShown(DamagedTargetSequence);
            presentedDamageSequence = DamagedTargetSequence;
        }
    }

    private void CheckLocalPlayerContact()
    {
        if (Object == null || !Object.IsValid || impactShown || localReportPending || Object.HasStateAuthority)
        {
            prevContactFrame = -1;
            return;
        }

        if (!Runner.TryGetPlayerObject(
            Runner.LocalPlayer, out NetworkObject playerObject))
        {
            prevContactFrame = -1;
            return;
        }

        if (playerObject == null ||
            !playerObject.IsValid || !playerObject.HasInputAuthority)
        {
            prevContactFrame = -1;
            return;
        }

        NetworkPlayerArrowHitReporter reporter =
            playerObject.GetComponent<NetworkPlayerArrowHitReporter>();

        CircleCollider2D playerCollider =
            playerObject.GetComponent<CircleCollider2D>();

        NetworkPlayerMovementSync movementSync =
            playerObject.GetComponent<NetworkPlayerMovementSync>();

        if (reporter == null || playerCollider == null ||
            movementSync == null || !playerCollider.enabled || !reporter.CanReport)
        {
            prevContactFrame = -1;
            return;
        }

        Vector2 center =
            playerCollider.transform.TransformPoint(playerCollider.offset);

        Vector3 scale = playerCollider.transform.lossyScale;
        float radius = playerCollider.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

        if (!networkTransform.TryGetSnapshotsBuffers(
            out var fromBuffer, out var toBuffer, out float alpha))
        {
            prevContactFrame = -1;
            return;
        }
        ContactRenderSample curArrowSample = new()
        {
            FromTick = fromBuffer.Tick,
            ToTick = toBuffer.Tick,
            Alpha = alpha
        };
        Vector2 arrowPosition = transform.position;
        double arrowRenderTime = Object.RenderTime;
        PlayerContactSample curPlayerSample = movementSync.CaptureContactSample(center);

        bool canSweep =
            prevContactFrame == Time.frameCount - 1 &&
            prevContactPlayer == playerObject.Id &&
            prevContactCorrection == movementSync.AppliedCorrectionCount &&
            Quaternion.Angle(prevArrowRotation, transform.rotation) < 0.01f &&
            arrowRenderTime >= prevArrowRenderTime;

        Vector2 start = center;
        PlayerContactSample reportStartPlayer = curPlayerSample;
        ContactRenderSample reportStartSample = curArrowSample;

        if (canSweep)
        {
            Vector2 arrowMovement = arrowPosition - prevArrowPosition;
            start = prevPlayerSample.Center + arrowMovement;

            reportStartPlayer = prevPlayerSample;
            reportStartSample = prevArrowSample;
        }

        bool touched = PolygonContactDetector.SweptTouchesCircle(transform, arrowOutline, arrowCollider.offset, start, center, radius);

        prevContactFrame = Time.frameCount;
        prevContactPlayer = playerObject.Id;
        prevContactCorrection = movementSync.AppliedCorrectionCount;
        prevArrowPosition = arrowPosition;
        prevPlayerSample = curPlayerSample;
        prevArrowRotation = transform.rotation;
        prevArrowRenderTime = arrowRenderTime;
        prevArrowSample = curArrowSample;

        if (touched && reporter.ReportArrowContact(this, reportStartSample, curArrowSample, reportStartPlayer, curPlayerSample))
        {
            localReportPending = true;
            SetArrowVisible(false);
        }
    }

    private void StoreHostArrowSnapshot(NetworkBehaviourBuffer buffer)
    {
        if (!buffer.Valid)
            return;
        Tick tick = buffer.Tick;
        NetworkTRSPData state = buffer.ReinterpretState<NetworkTRSPData>();
        hostArrowHistory[tick] = state;
    }

    private void RecordHostArrowHistory()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;

        if (!networkTransform.TryGetSnapshotsBuffers(
            out var fromBuffer, out var toBuffer, out float alpha))
            return;

        StoreHostArrowSnapshot(fromBuffer);
        StoreHostArrowSnapshot(toBuffer);

        if (hostArrowHistory.Count == 0)
            return;

        hostArrowHistory.Prune(Mathf.CeilToInt(historyDuration / Runner.DeltaTime));
    }

    private bool TryGetHostArrowState(
    int tick, out NetworkTRSPData state)
    {
        state = default;
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        return hostArrowHistory.TryGetValue(tick, out state);
    }

    private bool TryReconstructArrowPose(ContactRenderSample sample, out Vector3 position, out Quaternion rotation, out int teleportKey)
    {
        position = default;
        rotation = Quaternion.identity;
        teleportKey = 0;

        if (sample.FromTick < 0 || sample.ToTick < sample.FromTick ||
            float.IsNaN(sample.Alpha) || float.IsInfinity(sample.Alpha) ||
            sample.Alpha < 0f || sample.Alpha > 1f)
            return false;

        if (!TryGetHostArrowState(sample.FromTick, out var fromState) ||
            !TryGetHostArrowState(sample.ToTick, out var toState))
            return false;

        if (fromState.TeleportKey != toState.TeleportKey)
            return false;

        if (Quaternion.Angle(fromState.Rotation, toState.Rotation) > 0.01f)
            return false;

        position = Vector3.Lerp(
            fromState.Position,
            toState.Position,
            sample.Alpha
        );

        rotation = Quaternion.Slerp(
            fromState.Rotation,
            toState.Rotation,
            sample.Alpha
        );

        teleportKey = fromState.TeleportKey;
        return true;
    }

    private bool TryCheckReportedContact(ContactRenderSample startSample, ContactRenderSample endSample, Vector2 startPlayerCenter, Vector2 endPlayerCenter, float playerRadius, out bool touched, out float fraction, out Vector2 impactPosition)
    {
        touched = false;
        fraction = 0f;
        impactPosition = default;

        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
        {
            return false;
        }

        Transform parent = transform.parent;
        if (parent != null && !parent.localToWorldMatrix.isIdentity)
        {
            return false;
        }

        if (float.IsNaN(playerRadius) || float.IsInfinity(playerRadius) ||
            playerRadius <= 0f)
        {
            return false;
        }

        if (!TryReconstructArrowPose(
            startSample,
            out Vector3 startArrowPosition,
            out Quaternion startArrowRotation,
            out int startTeleportKey))
        {
            return false;
        }

        if (!TryReconstructArrowPose(
            endSample,
            out Vector3 endArrowPosition,
            out Quaternion endArrowRotation,
            out int endTeleportKey))
        {
            return false;
        }

        if (startTeleportKey != endTeleportKey ||
            Quaternion.Angle(startArrowRotation, endArrowRotation) > 0.01f)
        {
            return false;
        }

        Matrix4x4 arrowMatrix = Matrix4x4.TRS(
            endArrowPosition,
            endArrowRotation,
            arrowScale
        );

        Vector2 arrowMovement =
            (Vector2)(endArrowPosition - startArrowPosition);

        Vector2 relativePlayerStart =
            startPlayerCenter + arrowMovement;

        touched = PolygonContactDetector.TryGetContactFraction(
            arrowMatrix,
            arrowOutline,
            arrowCollider.offset,
            relativePlayerStart,
            endPlayerCenter,
            playerRadius,
            out fraction
        );

        Vector2 playerCenter = Vector2.Lerp(startPlayerCenter, endPlayerCenter, fraction);
        Vector2 arrowPosition = Vector3.Lerp(startArrowPosition, endArrowPosition, fraction);
        impactPosition = playerCenter + (arrowPosition - playerCenter).normalized * playerRadius;

        return true;
    }
}
