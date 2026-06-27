using UnityEngine;
using System.Collections;

public static class WarningBeepRoutine
{
    public static IEnumerator Play(float duration, float flashDuration, AudioClip clip)
    {
        float timer = 0f;
        bool beeped = false;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.PingPong(timer / (flashDuration / 2f), 1f);

            if (t >= 0.95f && !beeped)
            {
                AudioManager.Instance?.PlaySfx(clip, 0.15f);
                beeped = true;
            }

            if (t < 0.5f)
                beeped = false;

            yield return null;
        }
    }
}