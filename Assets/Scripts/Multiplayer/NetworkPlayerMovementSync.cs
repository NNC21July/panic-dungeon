using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(-1600)]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
public class NetworkPlayerMovementSync : NetworkBehaviour
{
    [SerializeField, Min(1f)] private float reportsPerSecond = 30f;
    [SerializeField, Min(1f)] private float maxReportSpeed = 40f;
    [SerializeField, Min(0.01f)] private float maxBufferedTime = 0.25f;
    [SerializeField, Min(0f)] private float positionTolerance = 0.5f, collisionTolerance = 0.05f;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private bool logCorrections = true;
    [Networked] public Vector2 AcceptedPosition { get; private set; }
    [Networked] private Vector2 CorrectionPosition { get; set; }
    [Networked] public int CorrectionCount { get; private set; }
    [Networked] public Vector2 AcceptedDashDirection { get; private set; }
    [Networked] public int AcceptedDashCount { get; private set; }
    [Networked] public int AcceptedReportSequence { get; private set; }
    public int AppliedCorrectionCount => appliedCorrectionCount;
    private Rigidbody2D rb;
    private CircleCollider2D playerCollider;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;
    private NetworkPlayerMovement networkPlayerMovement;
    private NetworkPlayerGameplayGate gameplayGate;
    private ContactFilter2D blockingFilter;
    private readonly RaycastHit2D[] blockingHits = new RaycastHit2D[1];
    private double nextReportTime, lastBudgetTime;
    private float movementBudget;
    private int reportSequence, lastReportSequence, appliedCorrectionCount, lastShownDashCount;
    private int restoreInterpolationFrame = -1;
    private bool waitingForCorrection, previousForceRemoteTimeframe;
    private struct AcceptedMovementSample
    {
        public Vector2 Position;
        public double OwnerTime, ReceivedTime;
        public int CorrectionCount;
    }
    private readonly Dictionary<int, AcceptedMovementSample> acceptedMovementHistory = new();
    private readonly List<int> expiredMovementSamples = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CircleCollider2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();
        networkPlayerMovement = GetComponent<NetworkPlayerMovement>();

        blockingFilter = new ContactFilter2D();
        blockingFilter.SetLayerMask(blockingLayers);
        blockingFilter.useTriggers = false;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Vector2 spawnPosition = transform.position;
            AcceptedPosition = spawnPosition;
            CorrectionPosition = spawnPosition;

            CorrectionCount = 0;
            AcceptedDashCount = 0;
            AcceptedDashDirection = Vector2.zero;
            AcceptedReportSequence = 0;
        }

        reportSequence = 0;
        lastReportSequence = 0;
        appliedCorrectionCount = CorrectionCount;
        waitingForCorrection = false;
        nextReportTime = 0d;
        ResetMovementBudget();
        SetBodyPosition(AcceptedPosition);
        acceptedMovementHistory.Clear();
        if (Object.HasStateAuthority)
            RecordAcceptedMovement(Runner.SimulationTime);

        previousForceRemoteTimeframe = Object.ForceRemoteRenderTimeframe;
        if (!Object.HasStateAuthority && !Object.HasInputAuthority)
            Object.ForceRemoteRenderTimeframe = true;

        lastShownDashCount = AcceptedDashCount;
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority)
            return;

        ApplyCorrection();

        if (restoreInterpolationFrame >= 0 && Time.frameCount > restoreInterpolationFrame)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            restoreInterpolationFrame = -1;
        }

        if (Time.unscaledTimeAsDouble < nextReportTime)
            return;
        if (!Runner.IsServer && !Runner.IsConnectedToServer)
            return;

        nextReportTime = Time.unscaledTimeAsDouble + 1d / reportsPerSecond;

        if (Object.HasStateAuthority)
        {
            AcceptedPosition = rb.position;
            AcceptedDashCount = networkPlayerMovement.DashCount;
            AcceptedDashDirection = networkPlayerMovement.SavedDashDirection;
            return;
        }

        reportSequence = unchecked(reportSequence + 1);
        RPC_ReportPosition(
            rb.position,
            networkPlayerMovement.SavedDashDirection,
            networkPlayerMovement.DashCount,
            reportSequence,
            appliedCorrectionCount,
            Runner.SimulationTime
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority,
        Channel = RpcChannel.Unreliable, TickAligned = false,
        HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_ReportPosition(Vector2 position, Vector2 dashDirection, int dashCount, int sequence, int correctionCount, double ownerTime, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority || info.Source != Object.InputAuthority)
            return;
        if (correctionCount != CorrectionCount || unchecked(sequence - lastReportSequence) <= 0)
            return;
        if (double.IsNaN(ownerTime) || double.IsInfinity(ownerTime) ||
            ownerTime < Runner.SimulationTime - 1d ||
            ownerTime > Runner.SimulationTime + maxBufferedTime + Runner.DeltaTime * 2d)
            return;

        lastReportSequence = sequence;

        if (waitingForCorrection)
        {
            waitingForCorrection = false;
            ResetMovementBudget();
        }

        if (!CanAcceptPosition(position, out string reason))
        {
            if (logCorrections)
                Debug.LogWarning($"Movement report rejected for {Object.InputAuthority}: {reason}", this);
            CorrectPosition(AcceptedPosition);
            return;
        }

        AcceptedPosition = position;
        AcceptedDashDirection = dashDirection;
        AcceptedDashCount = dashCount;
        SetBodyPosition(position);
        AcceptedReportSequence = sequence;
        RecordAcceptedMovement(ownerTime);
    }

    private bool CanAcceptPosition(Vector2 position, out string reason)
    {
        reason = "Invalid position";
        if (!IsFinite(position))
            return false;

        Vector2 movement = position - AcceptedPosition;
        float distance = movement.magnitude;

        if (!gameplayGate.CanAct)
        {
            reason = "Player cannot act";
            return distance <= 0.0001f;
        }

        double now = Time.unscaledTimeAsDouble;
        float elapsedTime = (float)System.Math.Max(0d, now - lastBudgetTime);
        lastBudgetTime = now;

        // Unused travel allowance is capped, so waiting cannot buy a room-sized teleport.
        movementBudget = Mathf.Min(
            movementBudget + maxReportSpeed * elapsedTime,
            maxReportSpeed * maxBufferedTime + positionTolerance
        );
        if (distance > movementBudget)
        {
            reason = "Movement exceeded the travel allowance";
            return false;
        }

        if (distance > 0.0001f)
        {
            Vector3 scale = transform.lossyScale;
            float radius = Mathf.Max(0.01f,
                playerCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) - collisionTolerance);
            Vector2 offset = transform.TransformVector(playerCollider.offset);

            PhysicsScene2D physicsScene = Runner.GetPhysicsScene2D();
            int hitCount = physicsScene.CircleCast(
                AcceptedPosition + offset,
                radius,
                movement / distance,
                distance,
                blockingFilter,
                blockingHits
            );
            if (hitCount > 0)
            {
                reason = "Movement crossed a wall or obstacle";
                return false;
            }
        }

        movementBudget -= distance;
        return true;
    }

    public void CorrectPosition(Vector2 position)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority || !IsFinite(position))
            return;

        AcceptedPosition = position;
        CorrectionPosition = position;
        CorrectionCount = unchecked(CorrectionCount + 1);
        waitingForCorrection = true;
        acceptedMovementHistory.Clear();
        ResetMovementBudget();

        if (Object.HasInputAuthority)
            ApplyCorrection();
        else
            SetBodyPosition(position);
    }

    private void ApplyCorrection()
    {
        if (appliedCorrectionCount == CorrectionCount)
            return;

        playerMovement.SetMoveInput(Vector2.zero);
        playerDash.CancelDash();
        rb.interpolation = RigidbodyInterpolation2D.None;
        SetBodyPosition(CorrectionPosition);
        restoreInterpolationFrame = Time.frameCount;
        appliedCorrectionCount = CorrectionCount;
        nextReportTime = 0d;
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
            return;
        if (Object.HasStateAuthority)
        {
            ShowRemoteDashEffect(AcceptedDashCount, AcceptedDashDirection);
            return;
        }
        if (!TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out float alpha))
            return;

        var positionReader = GetPropertyReader<Vector2>(nameof(AcceptedPosition));
        var correctionReader = GetPropertyReader<int>(nameof(CorrectionCount));
        var dashCountReader = GetPropertyReader<int>(nameof(AcceptedDashCount));
        var dashDirectionReader = GetPropertyReader<Vector2>(nameof(AcceptedDashDirection));

        Vector2 fromPosition = positionReader.Read(fromBuffer),
                toPosition = positionReader.Read(toBuffer);

        bool wasCorrected =
            correctionReader.Read(fromBuffer) != correctionReader.Read(toBuffer);

        Vector2 position = wasCorrected
            ? toPosition
            : Vector2.Lerp(fromPosition, toPosition, alpha);

        // Move the whole remote body, not a separate delayed sprite.
        SetBodyPosition(position);

        if (wasCorrected)
            ShowRemoteDashEffect(
                dashCountReader.Read(toBuffer),
                dashDirectionReader.Read(toBuffer),
                false
            );
        else if (alpha > 0f)
            ShowRemoteDashEffect(
                dashCountReader.Read(toBuffer),
                dashDirectionReader.Read(toBuffer)
            );
        else
            ShowRemoteDashEffect(
                dashCountReader.Read(fromBuffer),
                dashDirectionReader.Read(fromBuffer)
            );
    }

    private void SetBodyPosition(Vector2 position)
    {
        rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        Physics2D.SyncTransforms();
    }

    private void ResetMovementBudget()
    {
        movementBudget = maxReportSpeed * maxBufferedTime + positionTolerance;
        lastBudgetTime = Time.unscaledTimeAsDouble;
    }

    private static bool IsFinite(Vector2 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) &&
            !float.IsInfinity(position.x) && !float.IsInfinity(position.y);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Object.ForceRemoteRenderTimeframe = previousForceRemoteTimeframe;
        acceptedMovementHistory.Clear();
    }

    public PlayerContactSample CaptureContactSample(Vector2 center)
    {
        return new PlayerContactSample
        {
            Center = center,
            Time = Runner.SimulationTime,
            MovementSequence = AcceptedReportSequence,
            CorrectionCount = appliedCorrectionCount
        };
    }

    private void RecordAcceptedMovement(double ownerTime)
    {
        double now = Runner.SimulationTime;
        acceptedMovementHistory[AcceptedReportSequence] = new AcceptedMovementSample
        {
            Position = AcceptedPosition,
            OwnerTime = ownerTime,
            ReceivedTime = now,
            CorrectionCount = CorrectionCount
        };
        expiredMovementSamples.Clear();
        foreach (var entry in acceptedMovementHistory)
            if (entry.Value.ReceivedTime < now - 2d)
                expiredMovementSamples.Add(entry.Key);
        foreach (int sequence in expiredMovementSamples)
            acceptedMovementHistory.Remove(sequence);
    }

    public bool CanAcceptContactPath(PlayerContactSample start, PlayerContactSample end)
    {
        if (!Object.HasStateAuthority || !gameplayGate.CanAct || waitingForCorrection ||
            start.CorrectionCount != CorrectionCount || end.CorrectionCount != CorrectionCount ||
            !CanAcceptContactSample(start) || !CanAcceptContactSample(end))
            return false;

        double elapsed = end.Time - start.Time;
        if (elapsed < 0d || elapsed > 0.15d)
            return false;
        float allowance = maxReportSpeed * ((float)elapsed + Time.fixedDeltaTime * 2f) + positionTolerance;
        return Vector2.Distance(start.Center, end.Center) <= allowance &&
            ContactPathIsClear(start.Center, end.Center);
    }

    private bool CanAcceptContactSample(PlayerContactSample sample)
    {
        if (!IsFinite(sample.Center) || double.IsNaN(sample.Time) || double.IsInfinity(sample.Time) ||
            !acceptedMovementHistory.TryGetValue(sample.MovementSequence, out var accepted) ||
            accepted.CorrectionCount != sample.CorrectionCount)
            return false;

        double now = Runner.SimulationTime;
        double elapsed = sample.Time - accepted.OwnerTime;
        // The owner's present-time player is not on the arrow's delayed render clock.
        if (sample.Time < now - 1d || sample.Time > now + maxBufferedTime + Runner.DeltaTime * 2d ||
            elapsed < -Runner.DeltaTime * 2d || elapsed > maxBufferedTime + Runner.DeltaTime * 2d)
            return false;

        Vector2 center = accepted.Position + (Vector2)transform.TransformVector(playerCollider.offset);
        float allowance = maxReportSpeed * ((float)System.Math.Max(0d, elapsed) + Time.fixedDeltaTime * 2f) + positionTolerance;
        return Vector2.Distance(center, sample.Center) <= allowance && ContactPathIsClear(center, sample.Center);
    }

    private bool ContactPathIsClear(Vector2 start, Vector2 end)
    {
        Vector2 movement = end - start;
        float distance = movement.magnitude;
        if (distance <= 0.0001f)
            return true;
        Vector3 scale = transform.lossyScale;
        float radius = Mathf.Max(0.01f,
            playerCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) - collisionTolerance);
        return Runner.GetPhysicsScene2D().CircleCast(
            start, radius, movement / distance, distance, blockingFilter, blockingHits) == 0;
    }

    private void ShowRemoteDashEffect(int dashCount, Vector2 direction, bool showEffect = true)
    {
        if (unchecked(dashCount - lastShownDashCount) <= 0)
            return;
        lastShownDashCount = dashCount;
        if (showEffect)
            playerDash.PlayDashEffect(direction);
    }
}
