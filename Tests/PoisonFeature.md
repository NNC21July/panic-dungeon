# Multiplayer poison

## Workflow and ownership

1. A poison arrow applies its direct hit through the existing server-approved arrow path.
2. Shared `PoisonArrow` finds `IPoisonReceiver` on the target. Single-player forwards to `StatusEffectController`;
   multiplayer forwards to `NetworkHealth.ApplyPoison`. The shared trap/effect files have no Fusion dependency.
3. Both controllers reuse `PoisonEffect.ApplyTick` for damage and `StatusEffectTimer` for tick timing.
   Single-player advances using Unity frame time; only the multiplayer state authority advances using Fusion tick time.
4. A second hit replaces the effect and restarts its timer. It does not add a second ticking effect. A fatal direct hit
   cannot poison a dead player. Each poison tick uses `DamageType.Poison`, separate from the original direct arrow hit.
5. `NetworkHealth.TakeDamage` replicates health and sends its existing reliable damage-feedback RPC to all peers.
   `DamageFlash` subscribes on every player copy, so observers also see the affected sprite flash red. Its existing
   NetworkPlayer prefab color is red; alpha follows the original sprite. No owner-only filter was added to the flash.
   The existing owner-only audio feedback remains; poison deliberately does not shake the camera.
6. `IsPoisoned` replicates active status. `NetworkLocalPlayerUIBinder` binds/enables only the local player's existing
   `PoisonStatusUI` in the matching scene. It clears that binding on despawn. No new component or prefab wiring is required.
7. Expiry, death, match end, and despawn stop ticking. Spawn initializes an empty effect; restart cannot carry a previous
   timer forward. No client request can directly start poison or subtract poison health.

## Current settings and single-player timing

Both poison arrow prefabs currently specify duration 5 seconds, interval 1.5 seconds, damage 4.
That means three poison ticks (at approximately 1.5, 3 and 4.5 seconds), for 12 poison damage in addition to direct damage.
Server tick resolution can shift delivery by a simulation tick. Refreshing the effect delays the next tick from the new hit.

The shared timer keeps the status active for the full duration, including the final half-second with no tick. Previously the
single-player coroutine ended at its last full tick (4.5 seconds for these settings). It now also catches up due ticks after
a long frame, rather than losing elapsed time to repeated coroutine waits. A tick exactly at expiry is included; no partial
tick is applied if duration is shorter than interval. Invalid non-finite effect settings are rejected.

## Test in Unity before committing

Stop Play mode, let imports/compilation finish, then start fresh matching server + two clients. NetworkHealth's networked
layout changed. Do not mix old builds with the updated editor. Inspect the Console for import/weaving errors first.

- Let one poison arrow hit a client, then avoid other damage. Confirm direct hit damage, followed by three 4-point ticks.
- Watch that victim from the other client: every tick must flash the victim red, not the observer. Swap views and roles.
- Confirm only the victim's local poison indicator is shown. It disappears at five seconds, on death, and on match end.
- Hit again before expiry: refresh, no stacked tick streams. Test ordinary arrows while poisoned too.
- Test fatal direct hits, fatal poison ticks, countdown, match timeout, restart, and disconnect/rejoin: no stale damage/UI.
- Test normal/poison arrows' observer hiding and spike previews as regressions. Do not trigger splash or arrow-impact
  feedback again on a poison tick.
- Run single-player: poison damage and UI work using the same helpers; expiry now lasts the full configured duration.
- Try added latency/separate processes. Damage flashes follow confirmed server ticks; they are not locally predicted.
  Several notifications arriving together may restart/overlap one flash, rather than show several separately spaced flashes.

`Tests/Run-ArrowContactChecks.ps1` also runs the shared poison timer/damage tests. These do not exercise live Fusion RPCs,
Unity coroutines, sprite rendering, or UI binding. Static C# compilation is not a Unity/Fusion-weaving or play-test result.
