using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ZombieAI zombiePrefab;
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private float spawnDelay = 10f;
    [SerializeField] private int maxEnemiesAlive = 4;
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float minSpawnDist = 3f, spawnPadding = 0.1f;
    [SerializeField] private LayerMask blockedSpawnLayers;
    private bool spawning = false, spawnPaused;
    private float zombieSpawnCheckRadius;
    private Coroutine spawnCoroutine;
    private List<ZombieAI> activeEnemies;
    private DifficultyScaler difficultyScaler;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        activeEnemies = new List<ZombieAI>();

        CircleCollider2D zombieCollider = zombiePrefab.GetComponent<CircleCollider2D>();
        if (zombieCollider == null)
            throw new System.InvalidOperationException("Zombie prefab needs a CircleCollider2D for spawn clearance checks.");
        Vector3 scale = zombieCollider.transform.localScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        zombieSpawnCheckRadius = zombieCollider.radius * maxScale + spawnPadding;
    }

    public void StartSpawning(DifficultyScaler difficultyScaler)
    {
        if (spawning)
            return;

        this.difficultyScaler = difficultyScaler;

        spawning = true;
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeSelf);
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(Spawn());
    }

    public void StopSpawning()
    {
        SetSpawnPaused(false);
        spawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        foreach (ZombieAI enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    public void SetSpawnPaused(bool paused)
    {
        spawnPaused = paused;
    }

    private bool TryGetSpawnPos(out Vector2 spawnPos)
    {
        spawnPos = default;
        if (roomGen.OpenFloorPos.Count == 0)
            return false;
        float minSpawnDistSqr = minSpawnDist * minSpawnDist;
        int startIdx = Random.Range(0, roomGen.OpenFloorPos.Count);
        for (int i = 0; i < roomGen.OpenFloorPos.Count; i++)
        {
            int idx = (startIdx + i) % roomGen.OpenFloorPos.Count;
            Vector2 candidate = roomGen.OpenFloorPos[idx];
            float distSqr = ((Vector2)player.position - candidate).sqrMagnitude;
            if (distSqr >= minSpawnDistSqr && SpawnSpaceChecker.IsCircleAreaClear(candidate, zombieSpawnCheckRadius, blockedSpawnLayers))
            {
                spawnPos = candidate;
                return true;
            }
        }
        return false;
    }

    private IEnumerator Spawn()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (spawning)
        {
            activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeSelf);

            if (activeEnemies.Count < maxEnemiesAlive && !spawnPaused && TryGetSpawnPos(out Vector2 spawnPos))
                activeEnemies.Add(Instantiate(zombiePrefab, spawnPos, Quaternion.identity));

            yield return new WaitForSeconds(difficultyScaler.CurEnemySpawnInterval);
        }
        spawnCoroutine = null;
    }
}