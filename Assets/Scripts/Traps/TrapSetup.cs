using System;
using System.Collections.Generic;
using UnityEngine;

public class TrapSetup : MonoBehaviour
{
    [SerializeField] private Spike spikePrefab;
    [SerializeField] private ArrowShooter arrowShooterPrefab;
    private List<Spike> topSpikes, bottomSpikes;
    private List<ArrowShooter> leftArrowShooters, rightArrowShooters;
    private float arrowShooterOffset = -1f;
    public IReadOnlyList<Spike> TopSpikes => topSpikes;
    public IReadOnlyList<Spike> BottomSpikes => bottomSpikes;
    public IReadOnlyList<ArrowShooter> LeftArrowShooters => leftArrowShooters;
    public IReadOnlyList<ArrowShooter> RightArrowShooters => rightArrowShooters;
    public int roomWidth = 14, roomHeight = 8;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        // placing spikes along top and bottom
        float topY = roomHeight / 2f + 2f / 3f, bottomY = -topY;
        topSpikes = SpawnSpikeRow("SpikeTop_", topY, 180f);
        bottomSpikes = SpawnSpikeRow("SpikeBottom_", bottomY, 0f);
        SpawnAlternatingArrowShooters();
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

            float xPos = (left ? -1 : 1) * (roomWidth / 2f + arrowShooterOffset);
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
}
