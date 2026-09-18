using Fusion;
using UnityEngine;

// The same director component; kept in a separate file so trap scheduling stays readable.
public partial class NetworkTrapDirector
{
    private const float SpikeHistoryDuration = 2f;
    private readonly ContactHistory<NetworkSpikeState[]> spikeHistory = new();
    private Spike[] spikes;
    private Vector2[][] spikeOutlines;
    private Matrix4x4[] spikeMatrices;
    private NetworkSpikeState[] displayedSpikes;
    private bool[] hasDisplayedSpike;
    private int[] pendingSpikeCycles, pendingSpikeImpacts;
    private Vector2[] pendingSpikePositions;
    private double[] pendingSpikeDeadlines;
    private ContactRenderSample displayedSpikeSample;
    private bool hasSpikeSample;
    private bool previousSpikeRemoteTimeframe;

    private void InitializeSpikePlayback()
    {
        previousSpikeRemoteTimeframe = Object.ForceRemoteRenderTimeframe;
        if (!Object.HasStateAuthority)
            Object.ForceRemoteRenderTimeframe = true;
        if (trapSetup.TopSpikes.Count > MaxSpikesPerRow || trapSetup.BottomSpikes.Count > MaxSpikesPerRow)
            Debug.LogError("Network spikes support at most 64 spikes per row. Reduce the room width or increase spike width.", this);

        spikes = new Spike[MaxSpikesPerRow * 2];
        for (int i = 0; i < Mathf.Min(trapSetup.TopSpikes.Count, MaxSpikesPerRow); i++)
            spikes[i] = trapSetup.TopSpikes[i];
        for (int i = 0; i < Mathf.Min(trapSetup.BottomSpikes.Count, MaxSpikesPerRow); i++)
            spikes[MaxSpikesPerRow + i] = trapSetup.BottomSpikes[i];

        spikeOutlines = new Vector2[spikes.Length][];
        spikeMatrices = new Matrix4x4[spikes.Length];
        displayedSpikes = new NetworkSpikeState[spikes.Length];
        hasDisplayedSpike = new bool[spikes.Length];
        pendingSpikeCycles = new int[spikes.Length];
        pendingSpikeImpacts = new int[spikes.Length];
        pendingSpikePositions = new Vector2[spikes.Length];
        pendingSpikeDeadlines = new double[spikes.Length];
        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] == null)
                continue;
            spikeOutlines[i] = spikes[i].DamageCollider.GetPath(0);
            // Keep the authored scale/rotation (including parents), but supply recorded world translations later.
            spikeMatrices[i] = spikes[i].transform.localToWorldMatrix;
        }
        spikeHistory.Clear();
        hasSpikeSample = false;
    }

    private void ClearSpikePlayback()
    {
        spikeHistory.Clear();
        hasSpikeSample = false;
        trapController.PresentSpikeWarning(false, false, 0f);
        if (Object != null)
            Object.ForceRemoteRenderTimeframe = previousSpikeRemoteTimeframe;
    }

    private void CaptureSpikeStates()
    {
        if (spikes == null)
            return;
        for (int i = 0; i < spikes.Length; i++)
        {
            Spike spike = spikes[i];
            if (spike == null)
                continue;
            SpikePhase phase = spike.IsWarning ? SpikePhase.Warning :
                !spike.IsCycleActive ? SpikePhase.Idle :
                spike.IsRetracting ? SpikePhase.Retracting :
                spike.IsBlocked ? SpikePhase.Stopped :
                spike.IsAttacking ? SpikePhase.Advancing : SpikePhase.Holding;
            SpikeStates.Set(i, new NetworkSpikeState
            {
                Position = spike.BodyPosition,
                PatternSequence = PatternSequence,
                ImpactCount = spike.ImpactCount,
                Phase = phase,
                IsSolid = spike.IsSolid,
                WarningBlend = spike.WarningBlend
            });
        }
    }

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid || spikes == null)
            return;
        if (Object.HasStateAuthority)
        {
            if (TryGetSnapshotsBuffers(out var from, out var to, out _))
            {
                StoreSpikeSnapshot(from);
                StoreSpikeSnapshot(to);
                spikeHistory.Prune(Mathf.CeilToInt(SpikeHistoryDuration / Runner.DeltaTime));
            }
        }
        else
        {
            PresentSpikePlayback();
        }
    }

    private void StoreSpikeSnapshot(NetworkBehaviourBuffer buffer)
    {
        if (!buffer.Valid || spikeHistory.ContainsKey(buffer.Tick))
            return;
        var reader = GetArrayReader<NetworkSpikeState>(nameof(SpikeStates));
        var states = reader.Read(buffer);
        NetworkSpikeState[] copy = new NetworkSpikeState[spikes.Length];
        for (int i = 0; i < copy.Length; i++)
            copy[i] = states[i];
        spikeHistory[buffer.Tick] = copy;
    }

    private void PresentSpikePlayback()
    {
        hasSpikeSample = false;
        if (!TryGetSnapshotsBuffers(out var from, out var to, out float alpha))
            return;
        var reader = GetArrayReader<NetworkSpikeState>(nameof(SpikeStates));
        var fromStates = reader.Read(from);
        var toStates = reader.Read(to);
        displayedSpikeSample = new ContactRenderSample { FromTick = from.Tick, ToTick = to.Tick, Alpha = alpha };
        hasSpikeSample = true;
        bool warning = false, isTop = false;
        float warningBlend = 0f;
        for (int i = 0; i < spikes.Length; i++)
        {
            if (spikes[i] == null)
                continue;
            NetworkSpikeState state = NetworkSpikeState.Interpolate(fromStates[i], toStates[i], alpha);
            // Avoid placing scene-created spikes at default zero before their first host state arrives.
            if (state.PatternSequence == 0 && state.Position == Vector2.zero)
                continue;
            bool playImpact = hasDisplayedSpike[i] && state.ImpactCount > displayedSpikes[i].ImpactCount;
            Vector2 position = state.Position;
            if (pendingSpikeDeadlines[i] > 0d)
            {
                bool stillWaiting = Time.unscaledTimeAsDouble < pendingSpikeDeadlines[i] &&
                    state.PatternSequence == pendingSpikeCycles[i] &&
                    state.ImpactCount == pendingSpikeImpacts[i] &&
                    (state.Phase == SpikePhase.Advancing || state.Phase == SpikePhase.Holding);
                if (stillWaiting)
                    position = pendingSpikePositions[i];
                else
                    pendingSpikeDeadlines[i] = 0d;
            }
            spikes[i].ApplyPlayback(position, state.IsSolid, state.WarningBlend, playImpact);
            displayedSpikes[i] = state;
            hasDisplayedSpike[i] = true;
            if (state.Phase == SpikePhase.Warning)
            {
                warning = true;
                isTop = i < MaxSpikesPerRow;
                warningBlend = state.WarningBlend;
            }
        }
        trapController.PresentSpikeWarning(warning, isTop, warningBlend);
    }

    public int SpikeCount => spikes != null ? spikes.Length : 0;
    public int SpikeCycle => PatternSequence;

    public bool TryGetDisplayedSpike(int index, out NetworkSpikeState state, out ContactRenderSample sample)
    {
        state = default;
        sample = displayedSpikeSample;
        if (!hasSpikeSample || index < 0 || index >= SpikeCount || spikes[index] == null ||
            !hasDisplayedSpike[index] || pendingSpikeDeadlines[index] > 0d)
            return false;
        state = displayedSpikes[index];
        return state.Phase == SpikePhase.Advancing;
    }

    public bool CheckSpikeContact(int index, Vector2 startPosition, Vector2 endPosition,
        Vector2 playerStart, Vector2 playerEnd, float radius, out float fraction)
    {
        fraction = 0f;
        if (index < 0 || index >= SpikeCount || spikes[index] == null)
            return false;
        Matrix4x4 matrix = spikeMatrices[index];
        matrix.m03 = endPosition.x;
        matrix.m13 = endPosition.y;
        return PolygonContactDetector.TryGetContactFraction(matrix, spikeOutlines[index],
            spikes[index].DamageCollider.offset, playerStart + endPosition - startPosition, playerEnd, radius, out fraction);
    }

    public void HoldReportedSpike(int index, int cycle, Vector2 position)
    {
        if (index < 0 || index >= SpikeCount)
            return;
        pendingSpikeCycles[index] = cycle;
        pendingSpikePositions[index] = position;
        pendingSpikeImpacts[index] = displayedSpikes[index].ImpactCount;
        pendingSpikeDeadlines[index] = Time.unscaledTimeAsDouble + 1.5d;
        spikes[index].ApplyPlayback(position, displayedSpikes[index].IsSolid, 0f, false);
    }

    public void ResolveSpikeReport(int index, int cycle, bool accepted)
    {
        if (index < 0 || index >= SpikeCount || pendingSpikeCycles[index] != cycle)
            return;
        if (!accepted)
            pendingSpikeDeadlines[index] = 0d;
    }

    public bool TryResolveSpikeContact(int index, int cycle, ContactRenderSample start, ContactRenderSample end,
        PlayerContactSample playerStart, PlayerContactSample playerEnd, NetworkHealth target, CircleCollider2D targetCollider,
        out bool accepted)
    {
        accepted = false;
        if (!Object.HasStateAuthority || !IsMatchPlaying || cycle != PatternSequence ||
            index < 0 || index >= SpikeCount || spikes[index] == null || target == null || target.IsDead ||
            target.Runner != Runner || !targetCollider.enabled)
            return true;
        if (!spikeHistory.TryGetValue(start.FromTick, out var startFrom) ||
            !spikeHistory.TryGetValue(start.ToTick, out var startTo) ||
            !spikeHistory.TryGetValue(end.FromTick, out var endFrom) ||
            !spikeHistory.TryGetValue(end.ToTick, out var endTo))
            return false;

        NetworkSpikeState first = NetworkSpikeState.Interpolate(startFrom[index], startTo[index], start.Alpha);
        NetworkSpikeState last = NetworkSpikeState.Interpolate(endFrom[index], endTo[index], end.Alpha);
        if (!NetworkSpikeState.CanCheckContact(first, last, cycle))
            return true;

        Vector3 scale = targetCollider.transform.lossyScale;
        float radius = targetCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (!CheckSpikeContact(index, first.Position, last.Position,
            playerStart.Center, playerEnd.Center, radius, out float fraction))
            return true;
        accepted = spikes[index].TryApplyReportedDamage(target, Vector2.Lerp(first.Position, last.Position, fraction));
        return true;
    }
}
