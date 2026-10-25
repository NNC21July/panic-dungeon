using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomConfig", menuName = "Scriptable Objects/RoomConfig")]
public class RoomConfig : ScriptableObject
{
    [SerializeField, BoxGroup("Room"), Min(4)] private int roomWidth = 14;
    [SerializeField, BoxGroup("Room"), Min(4)] private int roomHeight = 8;
    [SerializeField, BoxGroup("Room"), Min(0.1f)] private float tileSize = 1f;
    [SerializeField, BoxGroup("Room"), Min(0)] private int edgePadding = 2;

    [SerializeField, BoxGroup("Spikes"), Min(0.1f)] private float spikeWidth = 1f;

    [SerializeField, BoxGroup("Arrow Shooters"), Min(0.1f)] private float arrowShooterSpacing = 1f;

    [SerializeField, BoxGroup("Obstacles")]
    private Vector2 obstacleSize = Vector2.one;

    [SerializeField, BoxGroup("Obstacles"), Min(0)]
    private int minObstacleCount = 6;

    [SerializeField, BoxGroup("Obstacles"), Min(0)]
    private int maxObstacleCount = 9;

    public int RoomWidth => roomWidth;
    public int RoomHeight => roomHeight;
    public float TileSize => tileSize;
    public int EdgePadding => edgePadding;

    public float SpikeWidth => spikeWidth;
    public float ArrowShooterSpacing => arrowShooterSpacing;

    public Vector2 ObstacleSize => obstacleSize;
    public int MinObstacleCount => minObstacleCount;
    public int MaxObstacleCount => maxObstacleCount;

    public float RoomWorldWidth => roomWidth * tileSize;
    public float RoomWorldHeight => roomHeight * tileSize;

    private void OnValidate()
    {
        maxObstacleCount =
            Mathf.Max(maxObstacleCount, minObstacleCount);

        obstacleSize.x = Mathf.Max(0.1f, obstacleSize.x);
        obstacleSize.y = Mathf.Max(0.1f, obstacleSize.y);
    }
}
