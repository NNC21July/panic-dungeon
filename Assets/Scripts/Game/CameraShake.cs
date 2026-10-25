using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    [SerializeField, Min(0f)] private float defaultStrength = 0.02f, maxStrength = 0.2f;
    private float pendingStrength;
    private CinemachineImpulseSource impulseSource;
    private readonly List<(CinemachineImpulseManager.ImpulseEvent impulse, object signal)> ownImpulses = new();
    public static CameraShake Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public static CameraShake FindInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CameraShake cameraShake =
                root.GetComponentInChildren<CameraShake>(
                    includeInactive: true
                );
            if (cameraShake != null)
                return cameraShake;
        }
        return null;
    }

    public void SetImpulseChannel(int channel)
    {
        if (channel == 0)
            return;
        impulseSource.ImpulseDefinition.ImpulseChannel = channel;
    }

    private void LateUpdate()
    {
        ownImpulses.RemoveAll(entry => entry.impulse.Expired || !ReferenceEquals(entry.impulse.SignalSource, entry.signal));
        if (pendingStrength == 0f)
            return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero)
            dir = Vector2.up;

        Vector3 velocity = (Vector3)dir * pendingStrength;
        var impulse = impulseSource.ImpulseDefinition.CreateAndReturnEvent(transform.position, velocity);
        if (impulse != null)
            ownImpulses.Add((impulse, impulse.SignalSource));
        pendingStrength = 0f;
    }

    public void ClearShake()
    {
        pendingStrength = 0f;
        // Never clear the global impulse manager: other multi-peer cameras use it too.
        foreach (var entry in ownImpulses)
            if (ReferenceEquals(entry.impulse.SignalSource, entry.signal) && !entry.impulse.Expired)
            {
                entry.impulse.Cancel(CinemachineImpulseManager.Instance.CurrentTime, true);
                entry.impulse.Channel = 0;
            }
        ownImpulses.Clear();
    }

    private void OnDestroy()
    {
        ClearShake();
        if (Instance == this)
            Instance = null;
    }

    public void AddShake()
    {
        AddShake(defaultStrength);
    }

    public void AddShake(float strength)
    {
        if (!isActiveAndEnabled || strength <= 0f)
            return;
        pendingStrength = Mathf.Min(pendingStrength + strength, maxStrength);
    }
}
