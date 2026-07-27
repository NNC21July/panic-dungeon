using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft, ISceneLoadStart, ISceneLoadDone
{
    [SerializeField] private NetworkObject playerPrefab;
    private Transform[] spawnPoints = System.Array.Empty<Transform>();
    private readonly Dictionary<PlayerRef, int> spawnAssignment = new();

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        SpawnPlayer(player);
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

    public void SceneLoadStart(SceneRef sceneRef)
    {
        if (Runner.IsServer)
        {
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj) && playerObj != null)
                    Runner.Despawn(playerObj);

                Runner.SetPlayerObject(player, null);
            }
        }
        spawnAssignment.Clear();
        spawnPoints = System.Array.Empty<Transform>();
    }

    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        List<NetworkSpawnPoint> discoveredPoints = new();

        // extract all spawn point objects from scene
        foreach (GameObject root in sceneInfo.RootGameObjects)
        {
            discoveredPoints.AddRange(root.GetComponentsInChildren<NetworkSpawnPoint>(true));
        }
        discoveredPoints.Sort((a, b) => a.Order.CompareTo(b.Order));
        spawnPoints = new Transform[discoveredPoints.Count];

        for (int i = 0; i < discoveredPoints.Count; i++)
            spawnPoints[i] = discoveredPoints[i].transform;

        if (spawnPoints.Length == 0)
        {
            Debug.LogError($"No network spawn points found in {sceneInfo.Scene.name}.", this);
            return;
        }

        if (!Runner.IsServer)
            return;
        foreach (PlayerRef player in Runner.ActivePlayers)
            SpawnPlayer(player);
    }

    private void SpawnPlayer(PlayerRef player)
    {
        if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj) && playerObj != null)
            return;

        int spawnIdx = FindFreeSpawnIdx();
        if (spawnIdx < 0)
        {
            Debug.Log($"No spawn point available for {player}.", this);
            return;
        }

        Transform spawnPoint = spawnPoints[spawnIdx];

        playerObj = Runner.Spawn(playerPrefab, spawnPoint.position, spawnPoint.rotation, inputAuthority: player);
        spawnAssignment[player] = spawnIdx;

        Runner.SetPlayerObject(player, playerObj);
    }
}