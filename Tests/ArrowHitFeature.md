# Reported arrow hits

## Workflow

- Single-player arrows still damage through their normal physics callbacks.
- The host's own player still uses host-local arrow physics.
- A client detects arrow contact against its own visible player and reports two arrow snapshot references plus two player samples.
- The player samples identify movement reports that the host already acknowledged. The host checks age, correction generation, travel allowance, and blocking geometry. Player sample times describe the owner's current simulation; arrow samples describe delayed playback. These are deliberately separate clocks.
- The host reconstructs the arrow's recorded outline, finds the first contact fraction, checks terminal wall timing, and applies shared arrow damage once. One accepted target consumes an arrow; competing reports are resolved in processing order, not by a global rewind tournament.
- While awaiting the result, the reporting client hides its local arrow without changing health. Rejection or timeout restores an arrow that is still active. Accepted damage uses the existing health and feedback events.
- Finished arrows retain evidence briefly; flying arrows do not expire on a timer. A late pre-wall contact can still be accepted. A stationary retained arrow is not intended to create new hits.
- Final visible contact is sampled in LateUpdate before hiding. Feedback notification is independent of whether a wall impact was already displayed.

## Observer arrow preview

`NetworkArrow.Observers.cs` is part of the existing `NetworkArrow` component, not a component to attach.
Both normal and poison network arrows use it automatically. `NetworkInputProvider` and damage rules are unchanged.

- Each client checks its rendered arrow outline against other players' rendered circles using `PolygonContactDetector`.
  Relative-motion sweeps cover consecutive frames (at most 0.1s), including a player dashing in the arrow's direction.
  Gaps remain misses. Corrections, teleports, pose changes and frame discontinuities do not invent a swept path.
- Visible contact opens a local `TrapContactPreview<NetworkId>` ticket. The arrow's renderers are hidden, but it is
  NOT despawned or stopped. Its snapshots and the victim's ordinary contact reporting continue.
- The victim alone reports their own hit. Server history checks and shared arrow damage are unchanged. The result
  RPC now also reaches observers so a rejected claim can cancel a matching preview. Rejection of another target
  does not release this target's preview. A rejection received before contact prevents a later preview for that target.
- Hiding does not call `ShowImpact`, subtract health, notify `NetworkHealth`, or play sound/splash. Confirmed impact
  and damage feedback still follow the existing server-driven path. A preview is not confirmation of a hit.
- `Observer Hide Duration` defaults to 0.5 seconds. Timeout, rejection, missing playback, target departure, or proximity
  to the local player releases the preview. One preview per arrow lifetime prevents repeated hide/show loops.
  Spawn/despawn clears tickets and samples, including when a NetworkObject is reused.
- The local player's checks run after observer safety checks. An arrow approaching the owner's circle (plus a 0.25-unit
  safety margin) cannot remain hidden by an observer guess. The margin only vetoes hiding; it is not damage reach.
  Owner samples keep updating while an observer preview is active, but an active preview cannot itself submit an owner hit.
- Visibility combines three reasons to hide: confirmed impact, the owner's pending/accepted claim, or the observer ticket.
  Rejecting an observer guess cannot accidentally show an arrow already hidden for a real confirmed impact or own claim.

The shared `TrapContactPreview<TTarget>` stores the target, attack/lifetime number, deadline, and whether this attempt
already happened. `SpikeObserverHold` delegates those rules to it while retaining spike-specific position, phase and impact
checks. Arrows use the same ticket for hiding. Neither the helper nor observer checks can authorize damage.

### Observer tests in Unity

Use a non-playing server plus two clients with fresh matching code (the result RPC targets changed). No new Inspector wiring.

1. On A, watch B stand in an arrow's path, then swap A/B. The arrow should hide at observed contact, before deep penetration.
2. Repeat while B moves/dashes in the arrow's direction, against it, and past it with a small visible gap. Test normal and poison arrows.
3. Observe a rejected/timed-out preview under added latency: a still-live arrow may reappear, but must not flicker repeatedly
   or leave false sounds, poison splashes, damage, or feedback. Reappearance is an intentional fallback, not a guaranteed invisible rewind.
4. Put A behind B in the arrow path; also try both players together. Observer hiding must not conceal a danger approaching A
   or prevent A's own valid report. An observer preview must never damage B on its own.
5. Test wall hits, fatal hits, B disconnecting, round end/restart, and simultaneous arrows. No old hidden state may leak into a new arrow.
6. Re-test spike preview/stopping/retraction and the single-player scene. Spike lifetime rules should behave as before.

This reduces delayed disappearance in an observer's view, not differences in server-approved damage timing. A server rejection
or different approved target can still require a visible correction. No physics body/artwork separation was introduced.

## Automated geometry and preview checks

The shared geometry helper is now named `PolygonContactDetector` (formerly `ArrowContactDetector`).
Arrows and spikes also share `ContactHistory<T>`, `ContactRenderSample`, and `PlayerContactSample`.
The command below runs both the arrow regression checks and spike/shared-history checks.

Run from the project root with a .NET 8+ SDK installed:

```powershell
.\Tests\Run-ArrowContactChecks.ps1
```

Pass `-UnityEditor 'path\to\Editor'` for a different Unity installation. This uses the actual shared detector and Unity's managed math types, produces files only in ignored Temp, and does not start Unity. It does not exercise Fusion's weaving, RPC transport, physics scenes, or presentation.

## Unity play-test checklist

Use fresh, matching host/client builds after the RPC layout changes. Editor multi-peer is also suitable for the first pass.

1. Stationary client: a visible touch removes health once. A visible gap does not remove health.
2. Moving/dashing client: direct hits and near misses behave similarly; no extra post-hit damage from the old host trigger.
3. Host player: direct hits still work. Single-player scene: normal and poison arrows retain existing behavior.
4. One arrow passes two players: only the first host-approved damage claim succeeds.
5. Client hit just before a wall: delayed approval still applies damage and feedback once, including when the wall impact was already displayed.
6. Walk into the location of an already-finished arrow: no new damage from retained evidence.
7. Fatal hit, round end, restart, disconnect/rejoin: no old pending report damages a new player or round.
8. Test real separate processes and added latency/packet loss. Check that rejected/timed-out reports do not leave a live arrow permanently hidden.
9. Poison arrow direct hit: shared splash callback runs once. Networked poison-over-time is a separate pre-existing status-effect task, not implemented here.

## Limits of this authority model

This is client-observed, host-validated contact, not server-determined contact or a cheat-proof system. A modified victim can withhold reports. Position validation is a bounded plausibility test around accepted movement, not a full replay of the owner's physics. The existing movement speed/buffer/tolerance settings also bound this test.

Arrow history currently stores the snapshot endpoints exposed each rendered host frame. A severe host hitch can leave an old endpoint unavailable; reports wait briefly, then fail closed if evidence cannot be retrieved. Very stale movement anchors and reports are also rejected. This favors no unjustified damage over inventing missing evidence.

The detector supports fixed-scale, non-turning arrows under an identity scene root, including Fusion's multi-peer root. It does not support a moving/scaled parent, turning projectiles, or changing collider geometry. Other viewers can still show different timing because their player and arrow playback differ; the victim's own contact drives the remote-player damage decision.
