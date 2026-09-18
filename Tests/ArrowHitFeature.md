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

## Automated geometry checks

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
