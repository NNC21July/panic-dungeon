using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(NetworkPlayerMovementSync))]
[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
public class NetworkPlayerArrowHitReporter : NetworkBehaviour
{
    [SerializeField, Min(0.1f)] private float maxReportAge = 1f;
    [SerializeField, Min(0.01f)] private float maxContactInterval = 0.1f;
    private const int MaxPendingReports = 32;
    private const double HistoryWaitDuration = 0.25d;
    private struct PendingReport
    {
        public ContactRenderSample Start, End;
        public PlayerContactSample PlayerStart, PlayerEnd;
        public double Deadline;
    }
    private readonly HashSet<NetworkId> reportedArrows = new(), inspectedArrows = new();
    private readonly Dictionary<NetworkId, PendingReport> pendingReports = new();
    private readonly Dictionary<NetworkId, double> awaitingResults = new();
    private readonly List<NetworkId> workList = new();
    private CircleCollider2D playerCollider;
    private NetworkPlayerMovementSync movementSync;
    private NetworkHealth health;
    private NetworkPlayerGameplayGate gameplayGate;
    private double nextCleanupTime;
    public bool CanReport => Object != null && Object.IsValid && Object.HasInputAuthority && gameplayGate.CanAct;

    private void Awake()
    {
        playerCollider = GetComponent<CircleCollider2D>();
        movementSync = GetComponent<NetworkPlayerMovementSync>();
        health = GetComponent<NetworkHealth>();
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();
    }

    public override void Spawned()
    {
        ClearReports();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ClearReports();
    }

    private void ClearReports()
    {
        reportedArrows.Clear();
        inspectedArrows.Clear();
        pendingReports.Clear();
        awaitingResults.Clear();
        workList.Clear();
        nextCleanupTime = 0d;
    }

    public bool ReportArrowContact(NetworkArrow networkArrow, ContactRenderSample startSample, ContactRenderSample endSample,
        PlayerContactSample playerStart, PlayerContactSample playerEnd)
    {
        if (!CanReport || Object.HasStateAuthority || !Runner.IsConnectedToServer ||
            networkArrow == null || networkArrow.Object == null || !networkArrow.Object.IsValid ||
            networkArrow.Runner != Runner || awaitingResults.Count >= MaxPendingReports)
            return false;

        NetworkId arrowId = networkArrow.Object.Id;
        if (!reportedArrows.Add(arrowId))
            return false;

        awaitingResults[arrowId] = Time.unscaledTimeAsDouble + maxReportAge + HistoryWaitDuration + 0.5d;
        RPC_ReportArrowContact(arrowId, startSample, endSample, playerStart, playerEnd);
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority,
    Channel = RpcChannel.Reliable, TickAligned = false,
    HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_ReportArrowContact(NetworkId arrowId, ContactRenderSample startSample, ContactRenderSample endSample,
        PlayerContactSample playerStart, PlayerContactSample playerEnd, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority || info.Source != Object.InputAuthority ||
            !Runner.TryGetPlayerObject(info.Source, out NetworkObject playerObject) || playerObject != Object)
            return;
        if (inspectedArrows.Contains(arrowId) || pendingReports.ContainsKey(arrowId))
            return;

        if (pendingReports.Count >= MaxPendingReports ||
            !CanInspectReport(arrowId, startSample, endSample, playerStart, playerEnd))
        {
            RPC_ResolveArrowContact(arrowId, false, 0);
            return;
        }

        pendingReports.Add(arrowId, new PendingReport
        {
            Start = startSample,
            End = endSample,
            PlayerStart = playerStart,
            PlayerEnd = playerEnd,
            Deadline = Time.unscaledTimeAsDouble + HistoryWaitDuration
        });
    }

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (Object.HasStateAuthority)
            ProcessReports();

        if (Object.HasInputAuthority)
        {
            workList.Clear();
            foreach (var entry in awaitingResults)
                if (Time.unscaledTimeAsDouble >= entry.Value)
                    workList.Add(entry.Key);
            foreach (NetworkId arrowId in workList)
                ResolveLocalArrow(arrowId, false, 0);
        }

        if (Time.unscaledTimeAsDouble >= nextCleanupTime)
        {
            nextCleanupTime = Time.unscaledTimeAsDouble + 1d;
            reportedArrows.RemoveWhere(id => !Runner.TryFindObject(id, out _));
            inspectedArrows.RemoveWhere(id => !Runner.TryFindObject(id, out _));
        }
    }

    private void ProcessReports()
    {
        workList.Clear();
        workList.AddRange(pendingReports.Keys);
        foreach (NetworkId arrowId in workList)
        {
            PendingReport report = pendingReports[arrowId];
            bool accepted = false, resolved = true;

            if (CanInspectReport(arrowId, report.Start, report.End, report.PlayerStart, report.PlayerEnd) &&
                movementSync.CanAcceptContactPath(report.PlayerStart, report.PlayerEnd) &&
                Runner.TryFindObject(arrowId, out NetworkObject arrowObject))
            {
                NetworkArrow networkArrow = arrowObject.GetComponent<NetworkArrow>();
                resolved = networkArrow.TryResolveReportedContact(health, playerCollider,
                    report.Start, report.End, report.PlayerStart, report.PlayerEnd, out accepted);
            }

            if (!resolved && Time.unscaledTimeAsDouble < report.Deadline)
                continue;

            pendingReports.Remove(arrowId);
            inspectedArrows.Add(arrowId);
            RPC_ResolveArrowContact(arrowId, accepted, accepted ? health.DamageSequence : 0);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority,
    Channel = RpcChannel.Reliable, TickAligned = false)]
    private void RPC_ResolveArrowContact(NetworkId arrowId, bool accepted, int damageSequence)
    {
        ResolveLocalArrow(arrowId, accepted, damageSequence);
    }

    private void ResolveLocalArrow(NetworkId arrowId, bool accepted, int damageSequence)
    {
        awaitingResults.Remove(arrowId);
        if (Runner.TryFindObject(arrowId, out NetworkObject arrowObject))
            arrowObject.GetComponent<NetworkArrow>()?.ResolveLocalReport(accepted);

        // This player already observed contact; feedback need not wait for the arrow's later snapshots.
        // The player RPC also survives an arrow disappearing before its confirmation arrives.
        if (accepted && damageSequence > 0)
            health.NotifyArrowImpactShown(damageSequence);
    }

    private bool CanInspectReport(NetworkId arrowId, ContactRenderSample start, ContactRenderSample end,
        PlayerContactSample playerStart, PlayerContactSample playerEnd)
    {
        if (!arrowId.IsValid || !gameplayGate.CanAct || !playerCollider.enabled ||
            !ContactRenderSample.CanCheckPath(start, end, playerStart, playerEnd,
                Runner.SimulationTime, Runner.DeltaTime, maxReportAge, maxContactInterval))
            return false;

        double startTime = start.GetTime(Runner.DeltaTime);

        if (!Runner.TryFindObject(arrowId, out NetworkObject arrowObject) ||
            arrowObject == null || !arrowObject.IsValid || !arrowObject.HasStateAuthority)
            return false;

        NetworkArrow networkArrow = arrowObject.GetComponent<NetworkArrow>();
        return networkArrow != null && networkArrow.Runner == Runner &&
            startTime >= networkArrow.HostSpawnTime - Runner.DeltaTime * 2d;
    }

}
