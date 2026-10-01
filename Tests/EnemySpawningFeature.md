# Enemy networking: points 1 through 4

## Scope

Implemented shared enemy calculations/tuning, server-controlled spawning and dynamic nearest-player chasing.
`NetworkZombie` now uses NetworkHealth (70 HP) and the existing DamageFlash. Only the server applies combat damage.
It has a solid circle body, but no local ZombieAI or single-player Health running on clients.

## Files and workflow

- `DefaultZombieTuning.asset`: shared speed 2, attack range 0.8, damage 15, cooldown 1 (migrated from Zombie.prefab unchanged).
  Also stores the agreed 0.3 distance margin for target switching.
- `ZombieRules`: Fusion-free movement, range, cooldown, attack damage and target-switch comparison. `ZombieAI` now calls these
  for its existing single-player behavior. Its target-selection policy is intentionally unchanged for this checkpoint.
- `EnemySpawnRules`: walks the existing floor candidates from a random starting index, rejects positions too close to any
  supplied player, then asks the caller to check occupancy. Both single-player and multiplayer spawners use this helper.
- `NetworkEnemySpawner`: a MonoBehaviour driven explicitly by the authority director's Fusion tick, not its own Update or coroutine.
  It uses runner-local players/physics, skips dead players, tracks network enemies, and uses Runner.Spawn/Despawn without player ownership.
- `NetworkTrapDirector`: supplies match state, shared difficulty spawn interval and active trap spawning restrictions to the spawner.
  `TrapController.EnemySpawningPaused` reads the running pattern's existing flag. Spikes pause spawning, arrows do not;
  the gap between patterns is not incorrectly treated as an active spike pattern.
- `NetworkEnemy`: the server checks its runner's living registered players every forward network tick. It finds the closest
  first, then switches only if that player is more than 0.3 units closer than the current target. A dead/disconnected target
  is replaced without applying the margin. With no living target it stops. Only the server runs movement, using shared
  ZombieRules and a dynamic Rigidbody2D in Unity FixedUpdate, just like single-player. This avoids submitting multiple
  MovePosition requests in one physics step when Fusion catches up. Clients have kinematic bodies and NetworkTransform
  plays back the replicated movement, with no second smoothing layer or independent client AI.
- `NetworkZombie.prefab`: existing zombie artwork/circle with NetworkObject, NetworkTransform and NetworkEnemy. FusionPrefab label
  is included for prefab discovery. NetworkHealth is registered in the NetworkObject behaviour list before NetworkEnemy.
- Combat reuses ZombieRules.ApplyAttack and the shared tuning: 15 damage, 0.8 centre-distance range, 1-second cooldown.
  The cooldown belongs to the zombie, not its target, so switching targets cannot reset it. Wall/obstacle linecasts in the
  runner's physics scene block attacks through barriers. This is a range attack like single-player, not an animated bite.
- NetworkPlayer's sword mask includes Player and Enemy. Existing server sword checks and per-swing deduplication damage
  NetworkHealth without introducing another combat implementation. Existing damage RPCs trigger red flashes on all clients.
- Dead zombies stop moving/attacking and disable their collider. The server retains the body for 0.5 seconds for the final
  flash, then despawns it for everyone. The spawner removes invalid entries on its next scheduled attempt, freeing capacity.

The new spawner is wired onto the multiplayer match object. The old inactive single-player EnemySpawner in that scene remains
inactive. Multiplayer uses its previous scene settings: 10-second initial delay, maximum 10 enemies, minimum distance 3 and
padding 0.1. The default interval comes from the existing trap difficulty scaler. Paused/full/blocked attempts wait for the next
interval, like single-player. Spawn positions also avoid tracked enemies immediately and use the enemy collision layer for clearance.

Match end clears the tracked enemies; scene restart/despawn clears the timer/list. No clients independently create enemy copies.
If there is no valid position or no living players, no enemy is spawned. Existing room layout, match duration, poison-arrow chance,
build profile and Unity services edits are preserved.

## Test this checkpoint

1. Stop Play mode and let Unity import/compile. Confirm NetworkZombie appears in Fusion's prefab table (rebuild the table from the
   Fusion configuration Inspector if automatic discovery has not refreshed). Start fresh matching server + two clients.
2. Countdown: no enemies. While Playing: first attempt after 10 seconds; if a spike pattern is active, the next eligible attempt
   is later. Expect up to 10 green zombies chasing players, stopping at attack range and attacking on cooldown.
3. Switch peer views: each shows the same network enemies at the same positions. No overlapping copies from another runner.
4. Put the players far apart: enemies must respect the 3-unit minimum from both living players and not overlap walls/obstacles/enemies.
5. Confirm spawning pauses during spike patterns and resumes on a later eligible attempt. Difficulty increases shorten later intervals.
6. Let the match end and restart: zombies disappear and the initial delay/count reset. No extra old enemies after scene reload.
7. Run single-player: the existing zombie still moves, attacks with the same damage/range/cooldown and dies normally.
8. With two living players, approach a zombie from different directions. It should chase the nearest, keep its target for
   tiny distance differences, and switch when the other player becomes more than 0.3 units closer (including after a dash).
9. Eliminate/disconnect its target while the match is still Playing: it must choose another living player. With none, it stops.
10. Watch from both clients while moving and stopping: movement should interpolate, without either client steering the zombie.
    Lead it into walls/obstacles to check solid collision. This is direct pursuit, not pathfinding around obstacles.
11. Stand within 0.8 units: expect 15 damage per successful attack, with at least 1 second between attacks by that zombie.
    Swap its target during cooldown: this must not grant an immediate extra attack. A wall/obstacle between centres blocks damage.
12. Hit a zombie with each client's sword: each swing damages it at most once; both clients see red flashes. Starting HP is 70.
    On lethal damage it stops/cannot attack, loses its solid collision, then disappears on both clients after 0.5 seconds.
13. Confirm player damage still updates health UI, victim-only camera shake, and visible red flashes. Confirm match end/restart
    still removes enemies, and killing a zombie does not count as eliminating a player.

Target selection uses positions known by the server, so a client's dash affects selection after its movement report arrives.
Spikes pause new spawning, not the chasing of existing enemies. Match-end cleanup remains unchanged.

Run `Tests/Run-ArrowContactChecks.ps1` for shared enemy math/spawn-selection and existing regression tests. This is not a live
Fusion spawn, physics, serialization, or Unity play-mode test. Enemy bites currently use server-known positions; latency/contact
presentation tuning is not implemented by this step and must be evaluated with moving/dashing clients under realistic latency.
