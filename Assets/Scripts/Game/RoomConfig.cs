using UnityEngine;

[CreateAssetMenu(fileName = "RoomConfig", menuName = "Scriptable Objects/RoomConfig")]
public class RoomConfig : ScriptableObject
{
    [Header("Room")]
    [SerializeField, Min(4)] private int roomWidth = 14;
    [SerializeField, Min(4)] private int roomHeight = 8;
    [SerializeField, Min(0.1f)] private float tileSize = 1f;
    [SerializeField, Min(0)] private int edgePadding = 2;

    [Header("Spikes")]
    [SerializeField, Min(0.1f)] private float spikeWidth = 1f;

    [Header("Arrow Shooters")]
    [SerializeField, Min(0.1f)] private float arrowShooterSpacing = 1f;

    [Header("Obstacles")]
    [SerializeField]
    private Vector2 obstacleSize = Vector2.one;

    [SerializeField, Min(0)]
    private int minObstacleCount = 6;

    [SerializeField, Min(0)]
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
