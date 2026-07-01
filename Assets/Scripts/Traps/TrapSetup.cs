using System.Collections.Generic;
using UnityEngine;

public class TrapSetup : MonoBehaviour
{
    [SerializeField] private Spike spikePrefab;
    [SerializeField] private ArrowShooter arrowShooterPrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private int roomWidth = 14, roomHeight = 8, minObstacleCount = 4, maxObstacleCount = 7;
    public int RoomWidth => roomWidth;
    public int RoomHeight => roomHeight;
    private List<Spike> topSpikes, bottomSpikes;
    private List<ArrowShooter> leftArrowShooters, rightArrowShooters;
    private int obstacleCount;
    private List<Vector2> floorPos, openFloorPos;
    private HashSet<Vector2> obstaclePosOccupied;
    public IReadOnlyList<Spike> TopSpikes => topSpikes;
    public IReadOnlyList<Spike> BottomSpikes => bottomSpikes;
    public IReadOnlyList<ArrowShooter> LeftArrowShooters => leftArrowShooters;
    public IReadOnlyList<ArrowShooter> RightArrowShooters => rightArrowShooters;
    public IReadOnlyList<Vector2> FloorPos => floorPos;
    public IReadOnlyList<Vector2> OpenFloorPos => openFloorPos;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        BuildFloorPos();
        SpawnSpikeRows();
        SpawnAlternatingArrowShooters();
        SpawnObstacles();
        BuildOpenFloorPos();
    }

    private void BuildFloorPos()
    {
        floorPos = new List<Vector2>();
        for (int xTile = 0; xTile < roomWidth; xTile++)
        {
            for (int yTile = 0; yTile < roomHeight; yTile++)
            {
                float x = -roomWidth / 2f + xTile + 0.5f,
                      y = -roomHeight / 2f + yTile + 0.5f;
                floorPos.Add(new Vector2(x, y));
            }
        }
    }

    private void SpawnSpikeRows()
    {
        // placing spikes along top and bottom
        float topY = roomHeight / 2f + 2f / 3f, bottomY = -topY;
        topSpikes = SpawnSpikeRow("SpikeTop_", topY, 180f);
        bottomSpikes = SpawnSpikeRow("SpikeBottom_", bottomY, 0f);
    }

    private List<Spike> SpawnSpikeRow(string name, float yPos, float rotAngle)
    {
        List<Spike> row = new List<Spike>();
        for (int i = 0; i < roomWidth; i++)
        {
            Vector2 pos = new Vector2(i - roomWidth / 2f + 0.5f, yPos);
            Spike obj = Instantiate(spikePrefab, pos, Quaternion.Euler(0f, 0f, rotAngle), gameObject.transform);
            obj.name = name + i;
            obj.ConfigurePos(pos, pos + (Vector2)(Quaternion.Euler(0f, 0f, rotAngle) * Vector3.up) * roomHeight);
            row.Add(obj);
        }
        return row;
    }

    private void SpawnAlternatingArrowShooters()
    {
        leftArrowShooters = new List<ArrowShooter>();
        rightArrowShooters = new List<ArrowShooter>();

        for (int i = 0; i < roomHeight; i++)
        {
            bool left = i % 2 == 0;

            float xPos = (left ? -1 : 1) * (roomWidth / 2f + 0.5f);
            float yPos = i - roomHeight / 2f + 0.5f;
            Vector2 direction = left ? Vector2.right : Vector2.left;

            ArrowShooter shooter = Instantiate(arrowShooterPrefab, new Vector2(xPos, yPos), Quaternion.identity);
            shooter.Configure(direction);

            if (left)
                leftArrowShooters.Add(shooter);
            else
                rightArrowShooters.Add(shooter);
        }
    }

    private void SpawnObstacles()
    {
        List<Vector2> validObstaclePos = new List<Vector2>();
        obstaclePosOccupied = new HashSet<Vector2>();

        float maxObstacleX = roomWidth / 2f - 2f;
        float maxObstacleY = roomHeight / 2f - 2f;

        foreach (Vector2 pos in floorPos)
        {
            if (Mathf.Abs(pos.x) < maxObstacleX && Mathf.Abs(pos.y) < maxObstacleY) // away from edges
                validObstaclePos.Add(pos);
        }

        obstacleCount = Mathf.Min(Random.Range(minObstacleCount, maxObstacleCount + 1), validObstaclePos.Count);

        for (int i = 0; i < validObstaclePos.Count; i++)
        {
            int idx = Random.Range(i, validObstaclePos.Count);
            (validObstaclePos[i], validObstaclePos[idx]) = (validObstaclePos[idx], validObstaclePos[i]);
        } // shuffle valid positions
        for (int i = 0; i < obstacleCount; i++)
        {
            Instantiate(obstaclePrefab, validObstaclePos[i], Quaternion.identity);
            obstaclePosOccupied.Add(validObstaclePos[i]);
        }
    }

    private void BuildOpenFloorPos()
    {
        openFloorPos = new List<Vector2>();
        foreach (Vector2 pos in floorPos)
        {
            if (!obstaclePosOccupied.Contains(pos))
                openFloorPos.Add(pos);
        }
    }

    public bool IsObstacleAt(Vector2 position)
    {
        return obstaclePosOccupied.Contains(position);
    }
}
