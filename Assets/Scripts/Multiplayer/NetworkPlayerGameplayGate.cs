using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkHealth))]
public class NetworkPlayerGameplayGate : NetworkBehaviour
{
    private NetworkHealth networkHealth;
    private NetworkMatchManager matchManager;

    public bool CanAct =>
        networkHealth != null && !networkHealth.IsDead &&
        (matchManager == null || matchManager.State == NetworkMatchState.Playing);

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
    }

    public override void Spawned()
    {
        matchManager = FindMatchManagerInScene();
    }

    private NetworkMatchManager FindMatchManagerInScene()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            NetworkMatchManager manager = root.GetComponentInChildren<NetworkMatchManager>(true);
            if (manager != null)
                return manager;
        }
        return null;
    }
}