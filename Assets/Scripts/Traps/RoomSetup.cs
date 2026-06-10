using System;
using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    [SerializeField] private Spike spikePrefab;
    public int roomWidth = 14, roomHeight = 8;

    private void Start()
    {
        // placing spikes along top and bottom
        float topY = roomHeight / 2f + 2f / 3f, bottomY = -topY;
        SpawnSpikeRow("SpikeTop_", topY, 180f);
        SpawnSpikeRow("SpikeBottom_", bottomY, 0f);
    }

    private void SpawnSpikeRow(string name, float yPos, float rotAngle)
    {
        if (spikePrefab == null)
            throw new ArgumentNullException("Spike prefab must be assigned in the inspector");

        for (int i = 0; i < roomWidth; i++)
        {
            Vector2 pos = new Vector2(i - roomWidth / 2f + 0.5f, yPos);
            Spike obj = Instantiate(spikePrefab, pos, Quaternion.Euler(0f, 0f, rotAngle), gameObject.transform);
            obj.name = name + i;
            obj.ConfigurePos(pos, pos + (Vector2)(Quaternion.Euler(0f, 0f, rotAngle) * Vector3.up) * roomHeight);
        }
    }
}
