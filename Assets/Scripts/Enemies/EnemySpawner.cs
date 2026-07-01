using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ZombieAI zombiePrefab;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private float spawnDelay = 10f;
    [SerializeField] private int maxEnemiesAlive = 4;
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float minSpawnDist = 3f;
    private bool spawning = false;
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

    private Vector2 GetSpawnPos()
    {
        Vector2 spawnPos;
        do
        {
            spawnPos = trapSetup.OpenFloorPos[Random.Range(0, trapSetup.OpenFloorPos.Count)];
        } while (((Vector2)player.position - spawnPos).sqrMagnitude < minSpawnDist * minSpawnDist); // too near player
        return spawnPos;
    }

    private IEnumerator Spawn()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (spawning)
        {
            activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeSelf);

            if (activeEnemies.Count < maxEnemiesAlive)
                activeEnemies.Add(Instantiate(zombiePrefab, GetSpawnPos(), Quaternion.identity));

            yield return new WaitForSeconds(difficultyScaler.CurEnemySpawnInterval);
        }
        spawnCoroutine = null;
    }
}