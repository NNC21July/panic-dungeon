using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    public GameObject spike;
    public float roomLength = 14f, roomWidth = 8f;

    void Start()
    {
        // placing spikes along top and bottom
        float topY = roomWidth / 2f + 2f * 0.3333f, bottomY = -topY;
        SpawnSpike("SpikeTop_", topY, 180f);
        SpawnSpike("SpikeBottom_", bottomY, 0f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void SpawnSpike(string name, float yPos, float rotAngle)
    {
        for (int i = 0; i < roomLength; i++)
        {
            Vector3 pos = new Vector3(i - roomLength / 2f + 0.5f, yPos, 0f);
            GameObject obj = Instantiate(spike, pos, Quaternion.Euler(0f, 0f, rotAngle), gameObject.transform);
            obj.name = name + i;
        }
    }
}
