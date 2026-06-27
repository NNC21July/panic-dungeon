using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ArrowPattern : ITrapPattern
{
    private readonly IReadOnlyList<ArrowShooter> shooters;
    private readonly AudioClip warningSfx;
    private readonly float warningFlashDuration;
    private bool isActive;
    public bool IsActive => isActive;

    public ArrowPattern(IReadOnlyList<ArrowShooter> shooters, AudioClip warningSfx, float warningFlashDuration)
    {
        this.shooters = shooters;
        this.warningSfx = warningSfx;
        this.warningFlashDuration = warningFlashDuration;
    }

    private bool EveryTrapInactive()
    {
        foreach (ArrowShooter shooter in shooters)
            if (shooter.IsActive)
                return false;
        return true;
    }

    public IEnumerator Run(float warningDuration)
    {
        isActive = true;
        foreach (ArrowShooter shooter in shooters)
            shooter.Activate(warningDuration, warningFlashDuration);
        yield return WarningBeepRoutine.Play(warningDuration, warningFlashDuration, warningSfx);
        yield return new WaitUntil(() => EveryTrapInactive());
        isActive = false;
    }

    public void Cancel()
    {
        isActive = false;
    }
}
