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

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();
    }

    public void CollectInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData inputData = new NetworkInputData
        {
            MoveDir = moveAction.action.ReadValue<Vector2>()
        };
        input.Set(inputData);
    }
}