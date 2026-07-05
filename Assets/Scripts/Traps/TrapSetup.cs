using System.Collections.Generic;
using UnityEngine;

public class TrapSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private Spike spikePrefab;
    [SerializeField] private ArrowShooter arrowShooterPrefab;
    private List<Spike> topSpikes, bottomSpikes;
    private List<ArrowShooter> leftArrowShooters, rightArrowShooters;
    public int RoomWidth => roomGen.Config.RoomWidth;
    public int RoomHeight => roomGen.Config.RoomHeight;
    public IReadOnlyList<Vector2> FloorPos => roomGen.FloorPos;
    public IReadOnlyList<Vector2> OpenFloorPos => roomGen.OpenFloorPos;
    public IReadOnlyList<Spike> TopSpikes => topSpikes;
    public IReadOnlyList<Spike> BottomSpikes => bottomSpikes;
    public IReadOnlyList<ArrowShooter> LeftArrowShooters => leftArrowShooters;
    public IReadOnlyList<ArrowShooter> RightArrowShooters => rightArrowShooters;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    public void SpawnTraps()
    {
        SpawnSpikeRows();
        SpawnAlternatingArrowShooters();
    }

    private void SpawnSpikeRows()
    {
        // placing spikes along top and bottom
        topSpikes = SpawnSpikeRow("SpikeTop_", true);
        bottomSpikes = SpawnSpikeRow("SpikeBottom_", false);
    }

    private List<Spike> SpawnSpikeRow(string name, bool isTop)
    {
        RoomConfig cf = roomGen.Config;
        List<Spike> row = new List<Spike>();

        float roomWidth = roomGen.FloorTopRight.x - roomGen.FloorBotLeft.x, roomHeight = roomGen.FloorTopRight.y - roomGen.FloorBotLeft.y;
        int spikeCount = Mathf.CeilToInt(roomWidth / cf.SpikeWidth);
        float spikeWidth = roomWidth / spikeCount,
              rotAngle = isTop ? 180f : 0f;
        for (int i = 0; i < spikeCount; i++)
        {
            float xPos = roomGen.FloorBotLeft.x + spikeWidth * (i + 0.5f);

            Spike obj = Instantiate(spikePrefab, Vector2.zero, Quaternion.Euler(0f, 0f, rotAngle), transform);
            obj.name = name + i;
            obj.transform.localScale *= spikeWidth;
            obj.ConfigureBodyLength(roomHeight);

            Vector2 originTipPos = new(xPos, isTop ? roomGen.FloorTopRight.y : roomGen.FloorBotLeft.y);
            Vector2 targetTipPos = new(xPos, isTop ? roomGen.FloorBotLeft.y : roomGen.FloorTopRight.y);
            Vector2 tipOffset = obj.TipPos - (Vector2)obj.transform.position;
            Vector2 originPos = originTipPos - tipOffset;
            Vector2 targetPos = targetTipPos - tipOffset;

            obj.transform.position = originPos;
            obj.ConfigurePos(originPos, targetPos);

            row.Add(obj);
        }
        return row;
    }

    private void SpawnAlternatingArrowShooters()
    {
        RoomConfig cf = roomGen.Config;

        leftArrowShooters = new List<ArrowShooter>();
        rightArrowShooters = new List<ArrowShooter>();

        int shooterCount = Mathf.CeilToInt(cf.RoomWorldHeight / cf.ArrowShooterSpacing);
        float spacing = cf.RoomWorldHeight / shooterCount,
              bottomEdge = roomGen.FloorBotLeft.y,
              roomWidth = roomGen.FloorTopRight.x - roomGen.FloorBotLeft.x,
              shooterHalfWidth = arrowShooterPrefab.GetComponent<SpriteRenderer>().sprite.bounds.extents.x * Mathf.Abs(arrowShooterPrefab.transform.localScale.x);

        for (int i = 0; i < shooterCount; i++)
        {
            bool left = i % 2 == 0;

            float xPos = left ? roomGen.FloorBotLeft.x - shooterHalfWidth : roomGen.FloorTopRight.x + shooterHalfWidth;
            float yPos = bottomEdge + spacing * (i + 0.5f);
            Vector2 direction = left ? Vector2.right : Vector2.left;

            ArrowShooter shooter = Instantiate(arrowShooterPrefab, new Vector2(xPos, yPos), Quaternion.identity, transform);
            shooter.Configure(direction, roomWidth, shooterHalfWidth);

            if (left)
                leftArrowShooters.Add(shooter);
            else
                rightArrowShooters.Add(shooter);
        }
    }
}
