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
}