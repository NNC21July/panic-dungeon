using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [SerializeField] private RoomGenerator roomGen;
    [SerializeField] private TrapSetup trapSetup;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        roomGen.Generate();
        trapSetup.SpawnTraps();
    }
}