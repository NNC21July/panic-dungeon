using UnityEngine;
using System.Collections.Generic;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private CameraConfinerSetup camConfinerSetup;
    [SerializeField] private GameObject player;
    [SerializeField] private LayerMask blockedSpawnLayers;
    [SerializeField, Min(0f)] private float spawnPadding = 0.1f;
    [SerializeField] private bool useFixedRoomSeed, spawnTraps = true;
    [SerializeField] private int roomSeed = 12345;
    [SerializeField] private Transform[] additionalPlayerSpawns = System.Array.Empty<Transform>();
    private float playerSpawnCheckRadius;
    private PhysicsScene2D physicsScene;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        physicsScene = gameObject.scene.GetPhysicsScene2D();

        UnityEngine.Random.State prevRandomState = UnityEngine.Random.state;
        if (useFixedRoomSeed)
            UnityEngine.Random.InitState(roomSeed);
        roomGen.Generate();
        if (useFixedRoomSeed)
            UnityEngine.Random.state = prevRandomState;

        Physics2D.SyncTransforms();
        camConfinerSetup.ConfigureBounds();

        CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
        if (playerCollider == null)
            throw new System.InvalidOperationException("Player prefab needs a CircleCollider2D for spawn clearance checks.");
        Vector3 scale = playerCollider.transform.localScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        playerSpawnCheckRadius = playerCollider.radius * maxScale + spawnPadding;

        List<Vector2> reservedSpawns = new();
        PositionSpawn(playerSpawn, reservedSpawns);
        foreach (Transform extraSpawn in additionalPlayerSpawns)
            PositionSpawn(extraSpawn, reservedSpawns);

        if (spawnTraps)
            trapSetup.SpawnTraps();
    }

    private void PositionSpawn(Transform spawn, List<Vector2> reservedSpawns)
    {
        if (spawn == null)
            throw new System.InvalidOperationException("An additional player spawn is missing.");

        Vector2 pos = GetClosestClearSpawnPos(roomGen.FloorCentre, reservedSpawns);
        spawn.position = pos;
        reservedSpawns.Add(pos);
    }

    private Vector2 GetClosestClearSpawnPos(Vector2 targetPos, List<Vector2> reservedSpawns)
    {
        Vector2 closest = default;
        float closestSqrDist = float.MaxValue;
        bool foundValidPos = false;

        foreach (Vector2 openPos in roomGen.OpenFloorPos)
        {
            if (reservedSpawns.Contains(openPos))
                continue;

            if (!SpawnSpaceChecker.IsCircleAreaClear(openPos, playerSpawnCheckRadius, blockedSpawnLayers, physicsScene))
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
