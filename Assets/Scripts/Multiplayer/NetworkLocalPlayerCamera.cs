using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class NetworkLocalPlayerCamera : NetworkBehaviour
{
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        CinemachineCamera playerCam = FindCameraInPlayerScene();
        if (playerCam == null)
            return;

        ConfigureImpulseChannel(playerCam);
        playerCam.Follow = transform;
        playerCam.gameObject.SetActive(true);
    }

    private CinemachineCamera FindCameraInPlayerScene()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            CinemachineCamera playerCam = root.GetComponentInChildren<CinemachineCamera>(includeInactive: true);

            if (playerCam != null)
                return playerCam;
        }
        return null;
    }

    private void ConfigureImpulseChannel(
    CinemachineCamera playerCam)
    {
        int playerIndex = Mathf.Max(0, Object.InputAuthority.AsIndex),
            impulseChannel = 1 << (playerIndex % 30);

        CinemachineImpulseListener impulseListener = playerCam.GetComponent<CinemachineImpulseListener>();
        if (impulseListener != null)
            impulseListener.ChannelMask = impulseChannel;

        CameraShake cameraShake = CameraShake.FindInScene(gameObject.scene);
        if (cameraShake != null)
            cameraShake.SetImpulseChannel(impulseChannel);
    }
}