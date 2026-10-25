using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class NetworkLocalPlayerCamera : NetworkBehaviour
{
    private CinemachineCamera playerCamera;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        playerCamera = FindCameraInPlayerScene();
        CinemachineCamera playerCam = playerCamera;
        if (playerCam == null)
            return;

        ConfigureImpulseChannel(playerCam);
        playerCam.Follow = transform;
        playerCam.gameObject.SetActive(true);
    }

    // Clear this viewer's effects without touching other runners' impulse channels.
    public void ResetFeedback()
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority)
            return;
        CameraShake.FindInScene(gameObject.scene)?.ClearShake();
        if (playerCamera == null)
            playerCamera = FindCameraInPlayerScene();
        CinemachineImpulseListener listener = playerCamera != null ? playerCamera.GetComponent<CinemachineImpulseListener>() : null;
        if (listener != null)
        {
            var settings = listener.ReactionSettings;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                m_SecondaryNoise = settings.m_SecondaryNoise,
                AmplitudeGain = settings.AmplitudeGain,
                FrequencyGain = settings.FrequencyGain,
                Duration = settings.Duration
            };
        }
    }

    // Changes only this runner's camera, never the target player's input authority.
    public void FollowTarget(Transform target)
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority)
            return;
        if (playerCamera == null)
            playerCamera = FindCameraInPlayerScene();
        if (playerCamera != null)
            playerCamera.Follow = target;
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
