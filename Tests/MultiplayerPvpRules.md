# Multiplayer PvP rules

- NetworkPlayer sword damage mask includes Player (3) and Enemy (12). Health starts at 150, editable on NetworkHealth.
- Only NetworkPlayer/NetworkZombie root CircleCollider2D excludeLayers masks ignore Player and Enemy (4104).
  No global physics layer matrix changes or trigger conversions. Single-player prefabs are unchanged.
  Sword hits use explicit OverlapBox queries, not collision callbacks, so excluded physical contacts do not remove sword targets.
  Arrow layer 8 and spike layers 7/10 are not excluded. Zombie attacks remain server range checks.
- MultiplayerGame GameSetup enables spreadPlayerSpawns and spreadSpawnPadding = 0.75 world units beyond the player's body.
  Safe candidates respect floor boundaries and wall/obstacle overlap clearance. First point is farthest from room centre;
  subsequent points maximize distance to the nearest reserved point. Coordinate tie-breaking is deterministic.
  There is no fixed desired separation that forces unsafe placement: available separation naturally decreases with player count.
  Initial body overlap is still forbidden. Insufficient safe tiles produce an explicit error, never a wall-overlapping fallback.
  Single-player GameSetup defaults to its original centre-based placement. The existing server spawner uses the placed markers.
- NetworkMatchManager no longer has a match timer. Waiting/color selection and countdown are unchanged.
  More than one living connected player keeps Playing; one wins; zero draws. Dead/disconnected players are excluded.
  NetworkMatchUI keeps the old hidden timer text only as a font/style reference for generated UI.

## Live acceptance checks

1. Matching server/client builds, 2 then 3/10 players: unique spread-out starting positions, padded away from walls/obstacles.
2. Walk/dash through players and zombies, and observe zombies crossing each other without pushing or blocking.
3. Walk against walls, obstacles and solid spikes: these should still block. Verify spikes still damage/stop on zombies.
4. Sword hits still damage players and zombies. Normal/poison arrows and spike damage still work.
5. Players start at 150 HP; existing health UI shows the right fraction. Adjust prefab Max Health and retest if desired.
6. Keep two players alive beyond the old time limit: no timer UI or automatic draw. Eliminate down to one: winner appears.
7. Test disconnection, simultaneous deaths (draw), leader restart, color selection and initial countdown.
8. Single-player retains centre spawn, health, normal character collisions and round timer.

Standalone regression tests exercise spawn-selection math, not Unity contact filtering or network replication.
Rebuild all peers together because the match manager's replicated layout changed when the timer was removed.
