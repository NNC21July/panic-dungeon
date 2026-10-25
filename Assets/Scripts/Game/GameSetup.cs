using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;

public class GameSetup : MonoBehaviour
{
    [SerializeField, Required] private RoomGenerator roomGen;
    [SerializeField, Required] private TrapSetup trapSetup;
    [SerializeField, Required] private Transform playerSpawn;
    [SerializeField, Required] private CameraConfinerSetup camConfinerSetup;
    [SerializeField, Required] private GameObject player;
    [SerializeField] private LayerMask blockedSpawnLayers;
    [SerializeField, Min(0f)] private float spawnPadding = 0.1f;
    [SerializeField] private bool useFixedRoomSeed, spawnTraps = true;
    [SerializeField, ShowIf(nameof(useFixedRoomSeed))] private int roomSeed = 12345;
    [SerializeField] private Transform[] additionalPlayerSpawns = System.Array.Empty<Transform>();
    [SerializeField] private bool spreadPlayerSpawns;
    [SerializeField, ShowIf(nameof(spreadPlayerSpawns)), Min(0f)] private float spreadSpawnPadding = 0.75f;
    private readonly List<Vector2> spreadCandidates = new();
    private float playerBodyRadius;
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
        playerBodyRadius = (playerCollider.radius + playerCollider.offset.magnitude) * maxScale;
        playerSpawnCheckRadius = playerBodyRadius + spawnPadding;

        if (spreadPlayerSpawns)
        {
            float clearance = playerBodyRadius + Mathf.Max(spawnPadding, spreadSpawnPadding);
            foreach (Vector2 candidate in roomGen.OpenFloorPos)
            {
                if (candidate.x < roomGen.FloorBotLeft.x + clearance || candidate.x > roomGen.FloorTopRight.x - clearance ||
                    candidate.y < roomGen.FloorBotLeft.y + clearance || candidate.y > roomGen.FloorTopRight.y - clearance)
                    continue;
                if (SpawnSpaceChecker.IsCircleAreaClear(candidate, clearance, blockedSpawnLayers, physicsScene))
                    spreadCandidates.Add(candidate);
            }
        }

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

        Vector2 pos;
        if (spreadPlayerSpawns)
        {
            if (!PlayerSpawnRules.TryFindSpreadPosition(spreadCandidates, reservedSpawns, roomGen.FloorCentre,
                playerBodyRadius * 2f, out pos))
                throw new System.InvalidOperationException("Not enough safe spread-out player spawn tiles. Enlarge the room or reduce obstacles; wall padding was not removed.");
        }
        else
            pos = GetClosestClearSpawnPos(roomGen.FloorCentre, reservedSpawns);
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
