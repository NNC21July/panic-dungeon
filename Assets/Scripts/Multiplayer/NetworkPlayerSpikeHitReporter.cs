using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(NetworkPlayerMovementSync))]
[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
public class NetworkPlayerSpikeHitReporter : NetworkBehaviour
{
    private const int MaxPendingReports = 32;
    private const double HistoryWaitDuration = 0.25d;
    private struct PendingReport
    {
        public int Cycle;
        public ContactRenderSample Start, End;
        public PlayerContactSample PlayerStart, PlayerEnd;
        public double Deadline;
    }
    private readonly Dictionary<int, PendingReport> pendingReports = new();
    private readonly List<int> workList = new();
    private NetworkTrapDirector director;
    private CircleCollider2D playerCollider;
    private NetworkPlayerMovementSync movementSync;
    private NetworkHealth health;
    private NetworkPlayerGameplayGate gameplayGate;
    private NetworkSpikeState[] previousSpikes;
    private bool[] previousValid;
    private int[] reportedCycles, inspectedCycles;
    private ContactRenderSample previousSample;
    private PlayerContactSample previousPlayer;
    private int previousFrame = -1;

    private void Awake()
    {
        playerCollider = GetComponent<CircleCollider2D>();
        movementSync = GetComponent<NetworkPlayerMovementSync>();
        health = GetComponent<NetworkHealth>();
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();
    }

    public override void Spawned()
    {
        pendingReports.Clear();
        previousFrame = -1;
        FindDirector();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority && director != null && reportedCycles != null)
            for (int i = 0; i < reportedCycles.Length; i++)
                director.ResolveSpikeReport(i, reportedCycles[i], false);
        pendingReports.Clear();
        workList.Clear();
        director = null;
        previousSpikes = null;
        previousFrame = -1;
    }

    private bool FindDirector()
    {
        if (director == null)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (NetworkTrapDirector candidate in root.GetComponentsInChildren<NetworkTrapDirector>(true))
                {
                    if (candidate.Object != null && candidate.Object.IsValid && candidate.Runner == Runner)
                    {
                        director = candidate;
                        break;
                    }
                }
                if (director != null)
                    break;
            }
        }
        if (director == null || director.Object == null || !director.Object.IsValid || director.SpikeCount == 0)
            return false;
        if (previousSpikes == null || previousSpikes.Length != director.SpikeCount)
        {
            previousSpikes = new NetworkSpikeState[director.SpikeCount];
            previousValid = new bool[director.SpikeCount];
            reportedCycles = new int[director.SpikeCount];
            inspectedCycles = new int[director.SpikeCount];
            for (int i = 0; i < director.SpikeCount; i++)
                reportedCycles[i] = inspectedCycles[i] = -1;
        }
        return true;
    }

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid || !FindDirector())
            return;
        if (Object.HasStateAuthority)
            ProcessReports();
        if (!Object.HasInputAuthority || Object.HasStateAuthority || !Runner.IsConnectedToServer ||
            !gameplayGate.CanAct || !playerCollider.enabled || !director.IsMatchPlaying)
        {
            previousFrame = -1;
            return;
        }

        Vector2 center = playerCollider.transform.TransformPoint(playerCollider.offset);
        Vector3 scale = playerCollider.transform.lossyScale;
        float radius = playerCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        PlayerContactSample player = movementSync.CaptureContactSample(center);
        bool consecutive = previousFrame == Time.frameCount - 1 &&
            previousPlayer.CorrectionCount == player.CorrectionCount;
        ContactRenderSample lastSample = default;
        for (int i = 0; i < director.SpikeCount; i++)
        {
            if (!director.TryGetDisplayedSpike(i, out var state, out var sample))
            {
                previousValid[i] = false;
                continue;
            }
            lastSample = sample;
            bool sweep = consecutive && previousValid[i] &&
                previousSpikes[i].PatternSequence == state.PatternSequence &&
                previousSpikes[i].ImpactCount == state.ImpactCount &&
                sample.GetTime(Runner.DeltaTime) >= previousSample.GetTime(Runner.DeltaTime);
            NetworkSpikeState startState = sweep ? previousSpikes[i] : state;
            ContactRenderSample startSample = sweep ? previousSample : sample;
            PlayerContactSample startPlayer = sweep ? previousPlayer : player;
            previousSpikes[i] = state;
            previousValid[i] = true;

            if (reportedCycles[i] == state.PatternSequence ||
                !ContactRenderSample.CanCheckPath(startSample, sample, startPlayer, player, Runner.SimulationTime, Runner.DeltaTime) ||
                !director.CheckSpikeContact(i, startState.Position, state.Position, startPlayer.Center, player.Center, radius, out float fraction))
                continue;

            reportedCycles[i] = state.PatternSequence;
            director.HoldReportedSpike(i, state.PatternSequence, Vector2.Lerp(startState.Position, state.Position, fraction));
            RPC_ReportSpikeContact(director.Object.Id, i, state.PatternSequence, startSample, sample, startPlayer, player);
        }
        previousSample = lastSample;
        previousPlayer = player;
        previousFrame = Time.frameCount;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority,
    Channel = RpcChannel.Reliable, TickAligned = false,
    HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_ReportSpikeContact(NetworkId directorId, int index, int cycle,
        ContactRenderSample start, ContactRenderSample end, PlayerContactSample playerStart, PlayerContactSample playerEnd,
        RpcInfo info = default)
    {
        if (!Object.HasStateAuthority || info.Source != Object.InputAuthority ||
            !Runner.TryGetPlayerObject(info.Source, out NetworkObject player) || player != Object ||
            !FindDirector() || director.Object.Id != directorId || index < 0 || index >= inspectedCycles.Length)
            return;
        if (inspectedCycles[index] >= cycle || pendingReports.ContainsKey(index))
            return;
        if (cycle != director.SpikeCycle || pendingReports.Count >= MaxPendingReports || !CanInspect(start, end, playerStart, playerEnd))
        {
            RPC_ResolveSpikeContact(index, cycle, false);
            return;
        }
        pendingReports.Add(index, new PendingReport
        {
            Cycle = cycle,
            Start = start,
            End = end,
            PlayerStart = playerStart,
            PlayerEnd = playerEnd,
            Deadline = Time.unscaledTimeAsDouble + HistoryWaitDuration
        });
    }

    private bool CanInspect(ContactRenderSample start, ContactRenderSample end, PlayerContactSample playerStart, PlayerContactSample playerEnd)
    {
        return gameplayGate.CanAct && playerCollider.enabled && director.IsMatchPlaying &&
            ContactRenderSample.CanCheckPath(start, end, playerStart, playerEnd, Runner.SimulationTime, Runner.DeltaTime);
    }

    private void ProcessReports()
    {
        workList.Clear();
        workList.AddRange(pendingReports.Keys);
        foreach (int index in workList)
        {
            PendingReport report = pendingReports[index];
            bool resolved = true, accepted = false;
            if (CanInspect(report.Start, report.End, report.PlayerStart, report.PlayerEnd) &&
                movementSync.CanAcceptContactPath(report.PlayerStart, report.PlayerEnd))
            {
                resolved = director.TryResolveSpikeContact(index, report.Cycle, report.Start, report.End,
                    report.PlayerStart, report.PlayerEnd, health, playerCollider, out accepted);
            }
            if (!resolved && Time.unscaledTimeAsDouble < report.Deadline)
                continue;
            pendingReports.Remove(index);
            inspectedCycles[index] = report.Cycle;
            RPC_ResolveSpikeContact(index, report.Cycle, accepted);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable, TickAligned = false)]
    private void RPC_ResolveSpikeContact(int index, int cycle, bool accepted)
    {
        if (director != null)
        {
            if (Object.HasInputAuthority)
                director.ResolveSpikeReport(index, cycle, accepted);
            else
                director.ResolveObservedSpike(index, cycle, Object.Id, accepted);
        }
    }
}
