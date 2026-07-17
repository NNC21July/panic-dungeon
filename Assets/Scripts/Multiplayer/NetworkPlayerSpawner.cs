using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private readonly Dictionary<PlayerRef, int> spawnAssignment = new();

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        int spawnIdx = FindFreeSpawnIdx();
        if (spawnIdx < 0)
        {
            Debug.Log($"No spawn point available for {player}.", this);
            return;
        }

        Transform spawnPoint = spawnPoints[spawnIdx];

        NetworkObject playerObj = Runner.Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, inputAuthority: player);
        spawnAssignment[player] = spawnIdx;

        Runner.SetPlayerObject(player, playerObj);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
            Runner.Despawn(playerObj);

        spawnAssignment.Remove(player);
    }

    private int FindFreeSpawnIdx()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
            if (!spawnAssignment.ContainsValue(i))
                return i;
        return -1;
    }
}