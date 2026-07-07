using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class CameraConfinerSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private CinemachineConfiner2D cameraConfiner;
    private PolygonCollider2D cameraBounds;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        cameraBounds = GetComponent<PolygonCollider2D>();
    }

    public void ConfigureBounds()
    {
        Vector2 botLeft = roomGen.OuterBotLeft,
                topRight = roomGen.OuterTopRight,
                botRight = new Vector2(topRight.x, botLeft.y),
                topLeft = new Vector2(botLeft.x, topRight.y);

        Vector2[] localPoints =
        {
            cameraBounds.transform.InverseTransformPoint(botLeft),
            cameraBounds.transform.InverseTransformPoint(botRight),
            cameraBounds.transform.InverseTransformPoint(topRight),
            cameraBounds.transform.InverseTransformPoint(topLeft)
        };

        cameraBounds.pathCount = 1;
        cameraBounds.SetPath(0, localPoints);
        cameraConfiner.InvalidateBoundingShapeCache();
    }
}