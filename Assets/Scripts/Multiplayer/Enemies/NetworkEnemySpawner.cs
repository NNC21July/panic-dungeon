using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Called by the server's NetworkTrapDirector tick; clients never run an independent spawn loop.
public class NetworkEnemySpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject zombiePrefab;
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField, Min(0f)] private float spawnDelay = 10f;
    [SerializeField, Min(1)] private int maxEnemiesAlive = 10;
    [SerializeField, Min(0f)] private float minSpawnDist = 3f, spawnPadding = 0.1f;
    [SerializeField] private LayerMask blockedSpawnLayers;
    private readonly List<NetworkObject> activeEnemies = new();
    private readonly List<Vector2> playerPositions = new();
    private TickTimer nextSpawn;
    private bool spawning;
    private float spawnRadius;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        CircleCollider2D circle = zombiePrefab.GetComponent<CircleCollider2D>();
        if (circle == null)
            throw new System.InvalidOperationException("Network zombie prefab needs a CircleCollider2D for spawn clearance.");
        Vector3 scale = circle.transform.lossyScale;
        spawnRadius = (circle.radius + circle.offset.magnitude) * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) + spawnPadding;
    }

    public void Tick(NetworkRunner runner, bool playing, bool paused, float spawnInterval)
    {
        if (runner == null || !runner.IsServer || !runner.IsForward)
            return;
        if (!playing)
        {
            StopSpawning(runner);
            return;
        }
        if (!spawning)
        {
            spawning = true;
            nextSpawn = TickTimer.CreateFromSeconds(runner, spawnDelay);
        }
        if (!nextSpawn.Expired(runner))
            return;
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.IsValid);
        if (!paused && activeEnemies.Count < maxEnemiesAlive && TryGetSpawnPos(runner, out Vector2 position))
        {
            NetworkObject enemy = runner.Spawn(zombiePrefab, position, Quaternion.identity);
            if (enemy != null)
                activeEnemies.Add(enemy);
        }
        // Preserve the single-player cadence: blocked/paused attempts wait for the next interval too.
        nextSpawn = TickTimer.CreateFromSeconds(runner, Mathf.Max(runner.DeltaTime, spawnInterval));
    }

    public void StopSpawning(NetworkRunner runner)
    {
        if (runner != null && runner.IsRunning && runner.IsServer)
            foreach (NetworkObject enemy in activeEnemies)
                if (enemy != null && enemy.IsValid && enemy.HasStateAuthority)
                    runner.Despawn(enemy);
        activeEnemies.Clear();
        playerPositions.Clear();
        spawning = false;
        nextSpawn = TickTimer.None;
    }

    private bool TryGetSpawnPos(NetworkRunner runner, out Vector2 position)
    {
        position = default;
        if (roomGen.OpenFloorPos.Count == 0)
            return false;
        playerPositions.Clear();
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid)
                continue;
            NetworkHealth health = obj.GetComponent<NetworkHealth>();
            if (health != null && !health.IsDead)
                playerPositions.Add(obj.transform.position);
        }
        PhysicsScene2D physicsScene = runner.GetPhysicsScene2D();
        int startIndex = Random.Range(0, roomGen.OpenFloorPos.Count);
        return EnemySpawnRules.TryFindPosition(roomGen.OpenFloorPos, playerPositions, minSpawnDist, startIndex,
            candidate => IsClear(candidate, physicsScene), out position);
    }

    private bool IsClear(Vector2 candidate, PhysicsScene2D physicsScene)
    {
        // Also check tracked positions so a just-spawned collider need not wait for the next physics step.
        foreach (NetworkObject enemy in activeEnemies)
            if (enemy != null && enemy.IsValid && ((Vector2)enemy.transform.position - candidate).sqrMagnitude < 4f * spawnRadius * spawnRadius)
                return false;
        return SpawnSpaceChecker.IsCircleAreaClear(candidate, spawnRadius, blockedSpawnLayers | (1 << GameLayers.Enemy), physicsScene);
    }
}
