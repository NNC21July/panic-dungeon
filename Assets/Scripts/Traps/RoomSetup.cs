using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    [SerializeField] private Spike spikePrefab;
    private List<Spike> topSpikes, bottomSpikes;
    public IReadOnlyList<Spike> TopSpikes => topSpikes;
    public IReadOnlyList<Spike> BottomSpikes => bottomSpikes;
    public int roomWidth = 14, roomHeight = 8;

    private void Awake()
    {
        // placing spikes along top and bottom
        float topY = roomHeight / 2f + 2f / 3f, bottomY = -topY;
        topSpikes = SpawnSpikeRow("SpikeTop_", topY, 180f);
        bottomSpikes = SpawnSpikeRow("SpikeBottom_", bottomY, 0f);
    }

    private List<Spike> SpawnSpikeRow(string name, float yPos, float rotAngle)
    {
        if (spikePrefab == null)
            throw new ArgumentNullException("Spike prefab must be assigned in the inspector");

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
}
