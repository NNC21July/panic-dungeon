using System.Collections.Generic;
using Fusion;
using UnityEngine;

// The same arrow component. Observers preview contact; only the victim reports it.
public partial class NetworkArrow
{
    [SerializeField, Min(0.05f)] private float observerHideDuration = 0.5f;
    private struct ObservedArrowPlayer
    {
        public Vector2 Center;
        public int Correction;
    }
    private readonly TrapContactPreview<NetworkId> observedArrowContact = new();
    private readonly HashSet<NetworkId> rejectedArrowObservations = new();
    private Dictionary<NetworkId, ObservedArrowPlayer> previousArrowPlayers = new(), currentArrowPlayers = new();
    private int arrowObserverFrame = -1, arrowObserverTeleport;
    private double arrowObserverTime, arrowObserverRenderTime;
    private Vector2 arrowObserverPosition;
    private Quaternion arrowObserverRotation;
    private Vector3 arrowObserverScale;

    private void ResetArrowObservers()
    {
        observedArrowContact.Reset();
        rejectedArrowObservations.Clear();
        previousArrowPlayers.Clear();
        currentArrowPlayers.Clear();
        arrowObserverFrame = -1;
    }

    private void RefreshArrowVisibility()
    {
        SetArrowVisible(!impactShown && !localReportPending && !localReportAccepted && !observedArrowContact.IsActive);
    }

    private void CheckObservedPlayerContact()
    {
        if (Object.HasStateAuthority || impactShown || localReportPending || localReportAccepted || !Runner.IsConnectedToServer)
        {
            observedArrowContact.Release();
            arrowObserverFrame = -1;
            return;
        }
        if (!networkTransform.TryGetSnapshotsBuffers(out var from, out var to, out float alpha))
        {
            observedArrowContact.Release();
            arrowObserverFrame = -1;
            return;
        }

        double now = Time.unscaledTimeAsDouble;
        double renderTime = new ContactRenderSample { FromTick = from.Tick, ToTick = to.Tick, Alpha = alpha }.GetTime(Runner.DeltaTime);
        int teleport = from.ReinterpretState<NetworkTRSPData>().TeleportKey;
        bool stablePose = teleport == to.ReinterpretState<NetworkTRSPData>().TeleportKey &&
            (arrowObserverFrame < 0 || (teleport == arrowObserverTeleport &&
            Quaternion.Angle(arrowObserverRotation, transform.rotation) < 0.01f && arrowObserverScale == transform.lossyScale));
        bool consecutive = stablePose && arrowObserverFrame == Time.frameCount - 1 &&
            now - arrowObserverTime <= 0.1d && renderTime >= arrowObserverRenderTime && renderTime - arrowObserverRenderTime <= 0.1d;
        Vector2 position = transform.position;
        Matrix4x4 matrix = transform.localToWorldMatrix;
        currentArrowPlayers.Clear();
        bool nearLocalPlayer = false;
        float earliest = float.PositiveInfinity;
        NetworkId target = default;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid)
                continue;
            CircleCollider2D circle = obj.GetComponent<CircleCollider2D>();
            NetworkPlayerMovementSync movement = obj.GetComponent<NetworkPlayerMovementSync>();
            NetworkPlayerGameplayGate gate = obj.GetComponent<NetworkPlayerGameplayGate>();
            if (circle == null || !circle.enabled || movement == null || gate == null || !gate.CanAct)
                continue;
            ObservedArrowPlayer sample = new ObservedArrowPlayer
            {
                Center = circle.transform.TransformPoint(circle.offset),
                Correction = obj.HasInputAuthority ? movement.AppliedCorrectionCount : movement.CorrectionCount
            };
            currentArrowPlayers[obj.Id] = sample;
            Vector3 scale = circle.transform.lossyScale;
            float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 start = sample.Center;
            if (consecutive && previousArrowPlayers.TryGetValue(obj.Id, out var previous) && previous.Correction == sample.Correction)
                start = previous.Center + position - arrowObserverPosition;

            if (obj.HasInputAuthority)
            {
                // Restore the arrow before it reaches our own player; never hide a danger behind somebody else's guess.
                nearLocalPlayer = PolygonContactDetector.SweptTouchesCircle(matrix, arrowOutline, arrowCollider.offset,
                    start, sample.Center, radius + 0.25f);
            }
            else if (stablePose && !rejectedArrowObservations.Contains(obj.Id) &&
                PolygonContactDetector.TryGetContactFraction(matrix, arrowOutline, arrowCollider.offset,
                    start, sample.Center, radius, out float fraction) && fraction < earliest)
            {
                earliest = fraction;
                target = obj.Id;
            }
        }

        bool safe = stablePose && !nearLocalPlayer;
        bool targetExists = !observedArrowContact.IsActive ||
            (Runner.TryFindObject(observedArrowContact.Target, out NetworkObject heldTarget) && heldTarget != null && heldTarget.IsValid &&
            !heldTarget.HasInputAuthority && Runner.TryGetPlayerObject(heldTarget.InputAuthority, out NetworkObject activeTarget) && activeTarget == heldTarget);
        observedArrowContact.Evaluate(0, now, safe && targetExists);
        // An arrow has one attack per spawn. ResetArrowObservers starts a fresh lifetime when reused.
        if (!observedArrowContact.IsActive && safe && target.IsValid)
            observedArrowContact.TryBegin(0, target, now, observerHideDuration);

        // Keep real playback samples even while hidden. Hiding must not freeze movement or history.
        var oldPlayers = previousArrowPlayers;
        previousArrowPlayers = currentArrowPlayers;
        currentArrowPlayers = oldPlayers;
        arrowObserverFrame = Time.frameCount;
        arrowObserverTime = now;
        arrowObserverRenderTime = renderTime;
        arrowObserverTeleport = teleport;
        arrowObserverPosition = position;
        arrowObserverRotation = transform.rotation;
        arrowObserverScale = transform.lossyScale;
    }

    public void ResolveObservedReport(NetworkId target, bool accepted)
    {
        if (Object == null || !Object.IsValid || Object.HasStateAuthority)
            return;
        if (accepted)
            return; // Confirmed impact and feedback still use the existing authoritative playback path.
        rejectedArrowObservations.Add(target);
        observedArrowContact.Reject(0, target);
        RefreshArrowVisibility();
    }
}
