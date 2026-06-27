using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ZombieAI zombiePrefab;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private float spawnDelay = 10f;
    [SerializeField] private int maxEnemiesAlive = 4;
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
            int xTile = Random.Range(0, trapSetup.RoomWidth),
                yTile = Random.Range(0, trapSetup.RoomHeight);
            float xPos = -trapSetup.RoomWidth / 2f + xTile + 0.5f,
                  yPos = -trapSetup.RoomHeight / 2f + yTile + 0.5f;
            spawnPos = new Vector2(xPos, yPos);
        } while (trapSetup.IsObstacleAt(spawnPos));
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