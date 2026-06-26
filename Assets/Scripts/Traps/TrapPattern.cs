using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TrapPattern : ITrapPattern
{
    private readonly IReadOnlyList<ITrap> traps;
    private readonly AudioClip warningSfx;
    private readonly float warningFlashDuration;
    private bool isActive;
    public bool IsActive => isActive;

    public TrapPattern(IReadOnlyList<ITrap> traps, AudioClip warningSfx, float warningFlashDuration)
    {
        this.traps = traps;
        this.warningSfx = warningSfx;
        this.warningFlashDuration = warningFlashDuration;
    }

    public bool EveryTrapInactive()
    {
        foreach (ITrap trap in traps)
            if (trap.IsActive)
                return false;
        return true;
    }

    public IEnumerator Run(float warningDuration)
    {
        isActive = true;
        foreach (ITrap trap in traps)
            trap.Activate(warningDuration);
        yield return WarningBeepCycle(warningDuration);
        yield return new WaitUntil(() => EveryTrapInactive());
        isActive = false;
    }

    public void Cancel()
    {
        isActive = false;
    }

    private IEnumerator WarningBeepCycle(float warningDuration)
    {
        float timer = 0f;
        bool hasBeepedThisFlash = false;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.PingPong(timer / (warningFlashDuration / 2f), 1f);

            if (t >= 0.95f && !hasBeepedThisFlash)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfx(warningSfx, 0.15f);
                hasBeepedThisFlash = true;
            }
            if (t < 0.5f)
                hasBeepedThisFlash = false;

            yield return null;
        }
    }
}
