using System.Collections.Generic;
using Fusion;
using UnityEngine;

public partial class NetworkTrapDirector
{
    [SerializeField, Min(0.05f)] private float observerHoldDuration = 0.5f;
    private struct ObservedPlayer
    {
        public NetworkId Id;
        public Vector2 Center;
        public float Radius;
        public int Correction;
    }
    private SpikeObserverHold[] observedSpikeHolds;
    private readonly List<ObservedPlayer> observedPlayers = new();
    private readonly Dictionary<NetworkId, ObservedPlayer> previousObservedPlayers = new();
    private readonly HashSet<(NetworkId Target, int Index, int Cycle)> rejectedObservations = new();
    private int observerFrame = -1, observerCycle;
    private double observerTime, observerRenderTime;
    private bool hasLocalObserverPlayer;
    private ObservedPlayer localObserverPlayer;

    private void InitializeSpikeObservers()
    {
        observedSpikeHolds = new SpikeObserverHold[SpikeCount];
        for (int i = 0; i < SpikeCount; i++)
            observedSpikeHolds[i] = new SpikeObserverHold();
        ClearSpikeObservers();
    }

    private void ClearSpikeObservers()
    {
        observedPlayers.Clear();
        previousObservedPlayers.Clear();
        rejectedObservations.Clear();
        observerFrame = -1;
        hasLocalObserverPlayer = false;
        if (observedSpikeHolds != null)
            foreach (SpikeObserverHold hold in observedSpikeHolds)
                hold.Release();
    }

    private void PrepareSpikeObservers()
    {
        observedPlayers.Clear();
        hasLocalObserverPlayer = false;
        if (observerCycle != PatternSequence)
        {
            rejectedObservations.Clear();
            observerCycle = PatternSequence;
        }
        if (!IsMatchPlaying)
            return;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid)
                continue;
            CircleCollider2D circle = obj.GetComponent<CircleCollider2D>();
            NetworkHealth health = obj.GetComponent<NetworkHealth>();
            NetworkPlayerMovementSync movement = obj.GetComponent<NetworkPlayerMovementSync>();
            if (circle == null || !circle.enabled || health == null || health.IsDead || movement == null)
                continue;
            Vector3 scale = circle.transform.lossyScale;
            ObservedPlayer sample = new ObservedPlayer
            {
                Id = obj.Id,
                Center = circle.transform.TransformPoint(circle.offset),
                Radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)),
                Correction = movement.CorrectionCount
            };
            if (obj.HasInputAuthority)
            {
                hasLocalObserverPlayer = true;
                localObserverPlayer = sample;
            }
            else
            {
                observedPlayers.Add(sample);
            }
        }
    }

    private Vector2 GetObservedSpikePosition(int index, NetworkSpikeState state)
    {
        SpikeObserverHold hold = observedSpikeHolds[index];
        double now = Time.unscaledTimeAsDouble;
        Vector2 startPosition = hasDisplayedSpike[index] ? displayedSpikes[index].Position : state.Position;
        Vector2 safetyStart = hold.IsActive ? hold.Position : startPosition;
        bool safe = IsMatchPlaying && !LocalPlayerNearSpike(index, safetyStart, state.Position);
        if (hold.IsActive && !HasObservedTarget(hold.Target))
            safe = false;
        Vector2 position = hold.Evaluate(state, now, safe);
        if (hold.IsActive || !safe || state.Phase != SpikePhase.Advancing)
            return position;

        double renderTime = displayedSpikeSample.GetTime(Runner.DeltaTime);
        bool consecutive = observerFrame == Time.frameCount - 1 && now - observerTime <= 0.1d &&
            renderTime >= observerRenderTime && renderTime - observerRenderTime <= 0.1d &&
            hasDisplayedSpike[index] && NetworkSpikeState.CanCheckContact(displayedSpikes[index], state, state.PatternSequence);
        float earliest = float.PositiveInfinity;
        Vector2 contactPosition = default;
        NetworkId target = default;
        foreach (ObservedPlayer player in observedPlayers)
        {
            if (rejectedObservations.Contains((player.Id, index, state.PatternSequence)))
                continue;
            bool sweep = consecutive && previousObservedPlayers.TryGetValue(player.Id, out var previous) &&
                previous.Correction == player.Correction;
            Vector2 spikeStart = sweep ? startPosition : state.Position;
            Vector2 playerStart = sweep ? previousObservedPlayers[player.Id].Center : player.Center;
            if (!CheckSpikeContact(index, spikeStart, state.Position, playerStart, player.Center, player.Radius, out float fraction) ||
                fraction >= earliest)
                continue;
            earliest = fraction;
            target = player.Id;
            contactPosition = Vector2.Lerp(spikeStart, state.Position, fraction);
        }
        if (target.IsValid && hold.TryBegin(state, contactPosition, now, observerHoldDuration, target))
        {
            return hold.Position;
        }
        return state.Position;
    }

    private bool LocalPlayerNearSpike(int index, Vector2 from, Vector2 to)
    {
        if (!hasLocalObserverPlayer)
            return false;
        Vector2 start = localObserverPlayer.Center;
        if (observerFrame == Time.frameCount - 1 &&
            previousObservedPlayers.TryGetValue(localObserverPlayer.Id, out var previous) &&
            previous.Correction == localObserverPlayer.Correction)
            start = previous.Center;
        // Do not let a guessed remote hit suppress the owner's real contact check or pin a tip beside them.
        return CheckSpikeContact(index, from, to, start, localObserverPlayer.Center,
            localObserverPlayer.Radius + 0.25f, out _);
    }

    private bool HasObservedTarget(NetworkId target)
    {
        // Health can reach zero before the delayed stop is displayed. Death alone must not release the hold early.
        return Runner.TryFindObject(target, out NetworkObject obj) && obj != null && obj.IsValid &&
            Runner.TryGetPlayerObject(obj.InputAuthority, out NetworkObject player) && player == obj;
    }

    private void FinishSpikeObservers()
    {
        previousObservedPlayers.Clear();
        foreach (ObservedPlayer player in observedPlayers)
            previousObservedPlayers[player.Id] = player;
        if (hasLocalObserverPlayer)
            previousObservedPlayers[localObserverPlayer.Id] = localObserverPlayer;
        observerFrame = Time.frameCount;
        observerTime = Time.unscaledTimeAsDouble;
        observerRenderTime = displayedSpikeSample.GetTime(Runner.DeltaTime);
    }

    public void ResolveObservedSpike(int index, int cycle, NetworkId target, bool accepted)
    {
        if (Object == null || !Object.IsValid || Object.HasStateAuthority || index < 0 || index >= SpikeCount)
            return;
        if (accepted)
            return; // Authoritative stop snapshots end the provisional hold on the playback timeline.
        rejectedObservations.Add((target, index, cycle));
        observedSpikeHolds[index].Reject(cycle, target);
    }
}
