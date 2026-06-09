using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    public GameObject spike;
    public int roomLength = 14, roomWidth = 8;

    void Start()
    {
        // placing spikes along top and bottom
        float topY = roomWidth / 2f + 2f * 0.3333f, bottomY = -topY;
        SpawnSpike("SpikeTop_", topY, 180f);
        SpawnSpike("SpikeBottom_", bottomY, 0f);
    }

    private void SpawnSpike(string name, float yPos, float rotAngle)
    {
        for (int i = 0; i < roomLength; i++)
        {
            Vector2 pos = new Vector2(i - roomLength / 2f + 0.5f, yPos);
            GameObject obj = Instantiate(spike, pos, Quaternion.Euler(0f, 0f, rotAngle), gameObject.transform);
            obj.name = name + i;

            Spike spikeScript = obj.GetComponent<Spike>();
            spikeScript.originPos = pos;
            spikeScript.targetPos = pos + (Vector2)(Quaternion.Euler(0f, 0f, rotAngle) * Vector3.up) * roomWidth;
        }
    }
}
