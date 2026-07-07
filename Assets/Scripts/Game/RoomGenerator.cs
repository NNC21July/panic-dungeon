using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private RoomConfig roomConfig;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private Grid roomGrid;

    [Header("Obstacles")]
    [SerializeField] private GameObject obstaclePrefab;

    private int startX, startY, endX, endY;
    private readonly List<Vector2> floorPos = new();
    private readonly List<Vector2> openFloorPos = new();
    private readonly List<GameObject> spawnedObstacles = new();
    private readonly HashSet<Vector2> obstaclePos = new();
    public RoomConfig Config => roomConfig;
    public IReadOnlyList<Vector2> FloorPos => floorPos;
    public IReadOnlyList<Vector2> OpenFloorPos => openFloorPos;
    public IReadOnlyCollection<Vector2> ObstaclePos => obstaclePos;
    public Vector2 FloorBotLeft { get; private set; }
    public Vector2 FloorTopRight { get; private set; }
    public Vector2 FloorCentre { get; private set; }
    public Vector2 OuterBotLeft { get; private set; }
    public Vector2 OuterTopRight { get; private set; }

    public void Generate()
    {
        roomGrid.cellSize = Vector3.one;
        roomGrid.transform.localScale = new Vector3(roomConfig.TileSize, roomConfig.TileSize, 1f);

        CalculateBounds();
        ClearPrevRoom();
        BuildFloorAndWalls();
        CalculateWorldBounds();
        BuildFloorPos();
        SpawnObstacles();
        BuildOpenFloorPos();
    }

    private void CalculateBounds()
    {
        startX = -roomConfig.RoomWidth / 2;
        startY = -roomConfig.RoomHeight / 2;
        endX = startX + roomConfig.RoomWidth - 1;
        endY = startY + roomConfig.RoomHeight - 1;
    }

    private void ClearPrevRoom()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        floorPos.Clear();
        openFloorPos.Clear();
        obstaclePos.Clear();

        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }
        spawnedObstacles.Clear();
    }

    private void BuildFloorAndWalls()
    {
        for (int x = 0; x < roomConfig.RoomWidth; x++)
        {
            for (int y = 0; y < roomConfig.RoomHeight; y++)
            {
                Vector3Int cell = new(startX + x, startY + y, 0);
                floorTilemap.SetTile(cell, floorTile);
            }
        }
        for (int x = startX - 1; x <= endX + 1; x++) // build top and bottom walls
        {
            wallTilemap.SetTile(new Vector3Int(x, startY - 1, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(x, endY + 1, 0), wallTile);
        }
        for (int y = startY - 1; y <= endY + 1; y++) // build left and right walls
        {
            wallTilemap.SetTile(new Vector3Int(startX - 1, y, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(endX + 1, y, 0), wallTile);
        }
    }

    private void CalculateWorldBounds()
    {
        FloorBotLeft = floorTilemap.CellToWorld(new Vector3Int(startX, startY, 0));
        FloorTopRight = floorTilemap.CellToWorld(new Vector3Int(endX + 1, endY + 1, 0));
        FloorCentre = (FloorBotLeft + FloorTopRight) / 2f;
        OuterBotLeft = floorTilemap.CellToWorld(new Vector3Int(startX - 1, startY - 1, 0));
        OuterTopRight = floorTilemap.CellToWorld(new Vector3Int(endX + 2, endY + 2, 0));
    }

    private void BuildFloorPos()
    {
        foreach (Vector3Int cell in floorTilemap.cellBounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(cell))
                continue;
            floorPos.Add(floorTilemap.GetCellCenterWorld(cell));
        }
    }

    private void SpawnObstacles()
    {
        List<Vector2> spawnSpots = new();

        foreach (Vector2 pos in floorPos)
            if (CanPlaceObstacle(pos))
                spawnSpots.Add(pos);

        int obstacleCount = Mathf.Min(Random.Range(roomConfig.MinObstacleCount, roomConfig.MaxObstacleCount + 1), spawnSpots.Count);
        spawnSpots = spawnSpots.OrderBy(_ => Random.value).ToList();
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector2 spawnPos = spawnSpots[i];
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, transform);
            Vector2 size = roomConfig.ObstacleSize;
            obstacle.transform.localScale = new Vector3(size.x, size.y, 1f);
            spawnedObstacles.Add(obstacle);
            obstaclePos.Add(spawnPos);
        }
    }

    private bool CanPlaceObstacle(Vector2 pos)
    {
        Vector3Int cell = floorTilemap.WorldToCell(pos);
        int padding = roomConfig.EdgePadding;

        return cell.x >= startX + padding &&
               cell.x <= endX - padding &&
               cell.y >= startY + padding &&
               cell.y <= endY - padding;
    }

    private void BuildOpenFloorPos()
    {
        openFloorPos.Clear();

        foreach (Vector2 pos in floorPos)
        {
            if (!obstaclePos.Contains(pos))
                openFloorPos.Add(pos);
        }
    }

    public Vector2 GetClosestOpenPos(Vector2 pos)
    {
        if (OpenFloorPos.Count == 0)
            throw new System.InvalidOperationException("Open floor position empty!");

        Vector2 closest = new();
        float closestSqrDist = float.MaxValue;
        foreach (Vector2 openPos in OpenFloorPos)
        {
            float sqrDist = (openPos - pos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closest = openPos;
                closestSqrDist = sqrDist;
            }
        }
        return closest;
    }
}