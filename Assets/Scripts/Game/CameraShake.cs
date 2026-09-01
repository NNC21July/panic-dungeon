using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    [SerializeField, Min(0f)] private float defaultStrength = 0.02f, maxStrength = 0.2f;
    private float pendingStrength;
    private CinemachineImpulseSource impulseSource;
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
        if (pendingStrength == 0f)
            return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero)
            dir = Vector2.up;

        Vector3 velocity = (Vector3)dir * pendingStrength;
        impulseSource.GenerateImpulse(velocity);
        pendingStrength = 0f;
    }

    private void OnDestroy()
    {
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