using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private Transform playerSpawn;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        roomGen.Generate();
        playerSpawn.position = roomGen.GetClosestOpenPos(roomGen.FloorCentre);
        trapSetup.SpawnTraps();
    }
}
