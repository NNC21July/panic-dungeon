using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkRunner))]
public class NetworkInputProvider : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction, dashAction, attackAction;
    private NetworkRunner networkRunner;
    private NetworkButtons accumulatedButtons;
    private bool resetAccumulatedButtons;
    private Vector2 aimDirection = Vector2.right;
    private Camera inputCamera;
    private int suppressInputThroughFrame = -1;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        networkRunner = GetComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;
        accumulatedButtons = new NetworkButtons();
    }

    private void Update()
    {
        if (!networkRunner.ProvideInput || Time.frameCount <= suppressInputThroughFrame)
        {
            accumulatedButtons = default;
            return;
        }
        if (resetAccumulatedButtons)
        {
            accumulatedButtons.Set(NetworkInputButton.Dash, false);
            accumulatedButtons.Set(NetworkInputButton.Attack, false);
            resetAccumulatedButtons = false;
        }

        if (dashAction.action?.WasPerformedThisFrame() == true)
            accumulatedButtons.Set(NetworkInputButton.Dash, true);
        if (attackAction.action?.WasPerformedThisFrame() == true)
            accumulatedButtons.Set(NetworkInputButton.Attack, true);

        UpdateAimDirection();
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
        dashAction.action?.Enable();
        attackAction.action?.Enable();
    }

    public void CollectInput(NetworkRunner runner, NetworkInput input)
    {
        if (!runner.ProvideInput || Time.frameCount <= suppressInputThroughFrame)
        {
            input.Set(default(NetworkInputData));
            return;
        }
        if (!moveAction.action.enabled)
            moveAction.action.Enable();

        NetworkInputData inputData = new NetworkInputData
        {
            MoveDir = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero,
            AimDirection = aimDirection,
            Buttons = accumulatedButtons
        };
        input.Set(inputData);
        resetAccumulatedButtons = true;
    }

    public void ClearMenuInput()
    {
        accumulatedButtons = default;
        resetAccumulatedButtons = false;
        // Do not turn the Cancel/Leave UI click into a sword attack on the next tick.
        suppressInputThroughFrame = Time.frameCount + 1;
    }

    private void UpdateAimDirection()
    {
        if (Mouse.current == null || networkRunner == null || !networkRunner.IsRunning)
            return;
        if (!networkRunner.TryGetPlayerObject(networkRunner.LocalPlayer, out NetworkObject playerObj) || playerObj == null)
            return;

        if (inputCamera == null ||
    inputCamera.gameObject.scene != playerObj.gameObject.scene)
            inputCamera = FindCameraInScene(playerObj.gameObject.scene);
        if (inputCamera == null)
            return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(inputCamera.transform.position.z - playerObj.transform.position.z);
        Vector3 mouseWorldPos = inputCamera.ScreenToWorldPoint(mouseScreenPos);

        Vector2 direction = mouseWorldPos - playerObj.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            aimDirection = direction.normalized;
    }

    private static Camera FindCameraInScene(Scene scene)
    {
        Camera fallbackCamera = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(includeInactive: true);
            foreach (Camera camera in cameras)
            {
                if (camera.CompareTag("MainCamera"))
                    return camera;
                if (fallbackCamera == null)
                    fallbackCamera = camera;
            }
        }
        return fallbackCamera;
    }
}
