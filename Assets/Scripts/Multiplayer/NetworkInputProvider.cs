using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkRunner))]
public class NetworkInputProvider : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    private NetworkRunner networkRunner;

    private void Awake()
    {
        networkRunner = GetComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    public void CollectInput(NetworkRunner runner, NetworkInput input)
    {
        if (!moveAction.action.enabled)
            moveAction.action.Enable();

        NetworkInputData inputData = new NetworkInputData
        {
            MoveDir = moveAction.action.ReadValue<Vector2>()
        };
        input.Set(inputData);
    }
}