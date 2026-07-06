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
    [SerializeField, Min(0f)] private float minSpawnDist = 3f;
    private bool spawning = false, spawnPaused;
    private Coroutine spawnCoroutine;
    private List<ZombieAI> activeEnemies;
    private DifficultyScaler difficultyScaler;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        activeEnemies = new List<ZombieAI>();
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
            if (distSqr >= minSpawnDistSqr)
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