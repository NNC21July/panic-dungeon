using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private CameraConfinerSetup camConfinerSetup;
    [SerializeField] private GameObject player;
    [SerializeField] private LayerMask blockedSpawnLayers;
    [SerializeField, Min(0f)] private float spawnPadding = 0.1f;
    private float playerSpawnCheckRadius;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        roomGen.Generate();
        Physics2D.SyncTransforms();
        camConfinerSetup.ConfigureBounds();

        CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
        if (playerCollider == null)
            throw new System.InvalidOperationException("Player prefab needs a CircleCollider2D for spawn clearance checks.");
        Vector3 scale = playerCollider.transform.localScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        playerSpawnCheckRadius = playerCollider.radius * maxScale + spawnPadding;

        playerSpawn.position = GetClosestClearSpawnPos(roomGen.FloorCentre);
        trapSetup.SpawnTraps();
    }

    private Vector2 GetClosestClearSpawnPos(Vector2 targetPos)
    {
        Vector2 closest = default;
        float closestSqrDist = float.MaxValue;
        bool foundValidPos = false;

        foreach (Vector2 openPos in roomGen.OpenFloorPos)
        {
            if (!SpawnSpaceChecker.IsCircleAreaClear(openPos, playerSpawnCheckRadius, blockedSpawnLayers))
                continue;

            float sqrDist = (openPos - targetPos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closest = openPos;
                closestSqrDist = sqrDist;
                foundValidPos = true;
            }
        }
        if (!foundValidPos)
            throw new System.InvalidOperationException("No valid player spawn position found.");

        return closest;
    }
}
