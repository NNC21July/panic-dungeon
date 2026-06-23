using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ZombieAI zombiePrefab;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private float spawnDelay = 10f, spawnInterval = 8f;
    [SerializeField] private int maxEnemiesAlive = 4;
    private bool spawning = false;
    private Coroutine spawnCoroutine;
    private List<ZombieAI> activeEnemies;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        activeEnemies = new List<ZombieAI>();
    }

    public void StartSpawning()
    {
        if (spawning)
            return;

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
        float roomWidth = trapSetup.RoomWidth, roomHeight = trapSetup.RoomHeight;
        Vector2 spawnPos;
        do
        {
            float xPos = -roomWidth / 2f + Random.Range(0, roomWidth) + 0.5f;
            float yPos = -roomHeight / 2f + Random.Range(0, roomHeight) + 0.5f;
            spawnPos = new Vector2(xPos, yPos);
        } while (trapSetup.ObstaclePosOccupied.Contains(spawnPos));
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

            yield return new WaitForSeconds(spawnInterval);
        }
        spawnCoroutine = null;
    }
}