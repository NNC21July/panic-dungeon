using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private CameraConfinerSetup camConfinerSetup;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        roomGen.Generate();
        camConfinerSetup.ConfigureBounds();
        playerSpawn.position = roomGen.GetClosestOpenPos(roomGen.FloorCentre);
        trapSetup.SpawnTraps();
    }
}
