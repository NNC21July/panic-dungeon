using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField, Min(0f)] private float defaultDuration = 0.12f, defaultStrength = 0.02f, maxStrength = 0.2f;
    private Vector3 camOriginalPos;
    private float remShakeTime, curStrength, shakeFadeDuration;
    private Coroutine shakeCoroutine;
    public static CameraShake Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        camOriginalPos = transform.localPosition;
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        Restore();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddShake()
    {
        AddShake(defaultDuration, defaultStrength);
    }

    public void AddShake(float duration, float strength)
    {
        if (!isActiveAndEnabled || duration <= 0f || strength <= 0f)
            return;

        if (shakeFadeDuration > 0f)
            curStrength *= Mathf.Clamp01(remShakeTime / shakeFadeDuration);

        remShakeTime = Mathf.Max(remShakeTime, duration);
        shakeFadeDuration = remShakeTime;
        curStrength = Mathf.Min(curStrength + strength, maxStrength);

        if (shakeCoroutine == null)
            shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private void Restore()
    {
        transform.localPosition = camOriginalPos;
        remShakeTime = 0f;
        shakeFadeDuration = 0f;
        curStrength = 0f;
        shakeCoroutine = null;
    }

    private IEnumerator ShakeRoutine()
    {
        while (remShakeTime > 0f)
        {
            float fade = Mathf.Clamp01(remShakeTime / shakeFadeDuration), fadedStrength = curStrength * fade;
            Vector2 offset = Random.insideUnitCircle * fadedStrength;
            transform.localPosition = camOriginalPos + new Vector3(offset.x, offset.y, 0f);

            remShakeTime -= Time.unscaledDeltaTime;
            yield return null;
        }
        Restore();
    }
}