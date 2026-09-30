# Reported spike hits

## The short version

The host moves the real spikes. Clients replay copies of those positions, including the solid colliders.
Each remote player reports contact with their own visible player. The host checks recorded spike geometry,
accepted player movement, timing, and the attack number before applying damage and stopping the spike.
Single-player and the host's own player keep their normal physics contact path.

## Files and ownership

- `Spike`: shared motion, collision rules, damage amount, warning colors, and impact effects. No Fusion dependency.
  Its small playback API puts a client spike and its body at the same position. Reported damage still uses its
  normal damage amount and stop/retraction behavior.
- `NetworkTrapDirector`: host trap scheduling and the replicated array for both rows.
- `NetworkTrapDirector.Spikes`: another part of that SAME component, not an extra component to add.
  Captures positions/phases each network tick; copies available snapshot endpoints into bounded host history;
  plays client positions back from Fusion snapshot buffers; reconstructs contact geometry on the host.
- `NetworkSpikeState`: position, phase, attack number, impact count, solid-tip flag, and warning color blend.
  Phase changes, reset boundaries, and impact stops are not blended into fictional damaging paths.
- `NetworkPlayerSpikeHitReporter`: already attached to `NetworkPlayer.prefab` and registered in its
  NetworkObject behaviour list. Reports only its own player's contact through an InputAuthority RPC.
  Finds only a director belonging to the same runner, including in editor multi-peer mode.
- `PolygonContactDetector`: the renamed arrow geometry helper; the exact same polygon/circle sweep is reused.
- `ContactHistory<T>`: copied state values indexed by exact tick, with bounded retention.
- `ContactRenderSample`: snapshot endpoints/alpha and shared timestamp checks for both arrow and spike reports.
- `PlayerContactSample`: accepted movement-report reference, owner timestamp, visible center, correction generation.

## Runtime sequence

1. Host runs the existing shared spike pattern, including obstacle contacts and retraction.
2. The director captures each spike's actual body position and phase. Fusion transmits those states.
3. Clients do NOT run independent spike attack/retraction coroutines. They render between recorded host positions.
   The whole spike root moves, not a separate delayed artwork child. Rigidbody interpolation is disabled for playback
   so it does not add another smoothing pass. Warning icons/colors/beeps and impacts follow playback states.
4. After playback, the owner's reporter compares the visible player circle with the spike tip polygon.
   A relative-motion sweep checks between consecutive frames, with no sweep across corrections or attack changes.
5. On contact the client temporarily holds that spike's picture AND solid body at the contact position (up to 1.5s).
   This is provisional presentation, not authority to change health. No impact effect is played before approval.
6. The host verifies sender ownership, same runner/director, current attack number, timestamps, acknowledged movement,
   correction generation, movement allowance/static blocking geometry, and the recorded spike outline.
   Reports wait up to 0.25s if snapshot evidence has not been copied yet. Reports older than 1s fail closed.
7. Successful shared spike damage stops it at the reconstructed contact position. The host stop is replicated;
   the normal NetworkHealth events update health/feedback. A spike already stopped, retracting, or idle rejects the report.
8. Rejection/timeout releases the client's provisional hold. Confirmation hands it over to authoritative stop playback.
   The host's existing retract wave resumes from the blocked progress. Old stop messages cannot restart an idle spike.

## Test before committing

### Observer stop preview

`NetworkTrapDirector.SpikeObservers.cs` is another part of the existing director, not a component to attach.
It uses the existing polygon/circle contact detector against other players' displayed positions on this runner.
`SpikeObserverHold` keeps the spike-specific contact position, phase and impact-count checks. It delegates the target,
deadline, matching rejection and one-attempt-per-cycle rules to `TrapContactPreview<NetworkId>`, also used by arrows.
Spikes still stop their whole body; arrows only hide. Neither preview can approve damage.

1. Client A watches client B. A checks the moving spike tip against B's displayed circle, including the path
   between consecutive frames. Discontinuous frames, corrections and attack boundaries do not produce a sweep.
2. If A sees contact, A temporarily holds that spike's whole root AND solid body at the contact point. This
   does not change the replicated state, report a hit on B's behalf, play impact effects, or subtract health.
3. B's existing own-player reporter still sends the real contact claim. The server still performs the same checks
   and decides damage and the real stop. No new observer hit-report traffic is sent.
4. A hands playback back to the recorded stop when it arrives. Rejection, retraction, a new attack, departure,
   match end or timeout also releases the provisional hold. The default `Observer Hold Duration` on the existing
   director is 0.5 seconds. Each spike can start this observer hold only once per attack, so a rejected guess cannot
   repeatedly freeze it. Server replies now reach observers as well as the victim; rejection is target-specific.
5. A's own contact reporting takes priority. Observer holds are skipped/released if the spike's held-to-recorded
   path approaches A's player (the normal radius plus a 0.25-unit safety margin). This margin is NOT extra damage
   reach; it only prevents a guessed remote contact from obstructing the owner's contact checks.

The artwork is not separated from physics. Because the solid body is held too, this is a provisional local placement,
not a purely cosmetic sprite effect. It can still affect local collision; the owner safety check limits that interference.
Server confirmation can differ from the observer's guess, so a residual correction is possible. Damage feedback still
waits for server approval. This change targets the large observer-side overshoot, not zero-latency damage feedback.

Test this with a non-playing server and two clients, using fresh matching code on every peer:

- Keep A away from the tip, watch B stand in a top/bottom spike's path, then swap roles. The observer should see
  a provisional stop instead of the spike continuing deeply through the player while waiting for B's report.
- Repeat with moving and dashing B, and on a non-final killing blow. No premature hold release solely because B died.
- Move A near the held spike and try simultaneous contacts. A's own hit detection must still work; safety releases
  can expose a correction here and should not leave a permanent barrier.
- Check rejection/timeout under latency, B disconnecting, round end/restart, full retraction and the next attack.
  No permanently stuck tip, repeated hold in one attack, duplicate damage, or speculative impact effects.
- Repeat arrows and single-player as regression checks. They have no new observer-stop logic.

### Existing hit-report checks

Start with fresh matching code on both peers; RPC/NetworkBehaviour layouts changed. Stop Play mode before reimporting.
No Inspector wiring is needed for the new reporter. Do not add the `.Spikes` partial class as another component.

1. Host + client: test top and bottom spikes against the client-controlled player. A visible gap must not cause damage.
   Direct contact should damage once and stop the spike. Walk away afterward: no extra damage from the stopped spike.
2. Check warnings, beeps, impact particles and shake in BOTH views. No independent client motion should continue after a stop.
3. Block a spike with an obstacle, then with the host player. Host-local contacts must still work normally.
4. Check that a client can stand against the solid spike tip/body without passing through it. Watch the provisional stop
   under latency; rejected reports may release it. The hold does not guarantee zero correction or perfect physics under all latency.
5. Let stopped spikes retract completely. They must stay idle afterward, not return to their blocked position.
6. Repeat across multiple patterns, round end/restart, late join, and player disconnect/rejoin.
7. With two remote clients, try simultaneous contacts on one spike. Only the first accepted stop can damage;
   the other reports must not repeat the hit or move the stopped spike.
8. Test movement/dash near-misses and hits under added latency and packet loss, then in separate processes.
9. Re-test normal and poison arrows, including late wall hits; then run the single-player scene.

Run automated math/timing/history checks without starting Unity:

```powershell
.\Tests\Run-ArrowContactChecks.ps1
```

These tests compile the real shared helpers and state structs against installed Unity/Fusion assemblies.
They do NOT run Unity physics, Fusion weaving, RPC delivery, or live multiplayer. A full script C# compile is also
only a compile check, not a substitute for those runtime tests.

## Deliberate limits

- The victim can withhold reports; this is not cheat-proof server-determined hit detection.
- Movement validation is bounded plausibility, not a replay of all owner physics.
- The victim's own view decides when to report. Other viewers may still see timing differences; this is not a
  globally synchronized view of all player contacts. Spike feedback uses existing health events after host approval.
- A late report is rejected if the host spike has already stopped on something else or started retracting, even
  if contact could have occurred earlier. This avoids rewinding a finished trap or inventing damage.
- As with arrows, history copies the snapshot endpoints available each host render frame. Hitches can leave missing
  evidence. Short waits then rejection are intentional; no speculative fallback damage is applied.
- Contact sweeps cover up to 0.1s. Long frame stalls can miss contacts. One report per spike/player/attack prevents duplicates.
- Geometry assumes the generated spike's scale/rotation/outline and its parent transform remain fixed during the match.
  Only its translation is recorded. Uses the existing first polygon path (the current triangular tip).
- Maximum 64 spikes per row (128 slots total), matching the previous per-row limit. Oversized rooms emit a configuration
  error. Do not use them without raising capacity and checking bandwidth/memory. Unchanged states benefit from Fusion delta compression;
  moving positions add traffic. Measure real builds with the final room size rather than assuming a bandwidth target.
- No changes to the player ownership model, poison-over-time implementation, or unrelated room/build settings.

Reference for the buffer playback API: https://doc.photonengine.com/fusion/v2/manual/advanced/network-buffers
