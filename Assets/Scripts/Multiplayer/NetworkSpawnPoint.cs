using UnityEngine;

public class NetworkSpawnPoint : MonoBehaviour
{
    // order 0 -> first player, order 1 -> second player, etc.
    [SerializeField, Min(0)] private int order;
    public int Order => order;
}