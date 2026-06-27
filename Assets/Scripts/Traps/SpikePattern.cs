using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikePattern : ITrapPattern
{
    private readonly IReadOnlyList<Spike> spikes;
    private readonly AudioClip warningSfx;
    private readonly float warningFlashDuration, retractDelay;
    private bool isActive;
    public bool IsActive => isActive;

    public SpikePattern(IReadOnlyList<Spike> spikes, AudioClip warningSfx, float warningFlashDuration, float retractDelay)
    {
        this.spikes = spikes;
        this.warningSfx = warningSfx;
        this.warningFlashDuration = warningFlashDuration;
        this.retractDelay = retractDelay;
    }

    public IEnumerator Run(float warningDuration)
    {
        if (isActive)
            yield break;

        if (spikes.Count == 0)
        {
            isActive = false;
            yield break;
        }
        isActive = true;
        foreach (Spike spike in spikes)
            spike.ForceIdle();
        foreach (Spike spike in spikes)
            spike.BeginWarning(warningDuration, warningFlashDuration);
        yield return WarningBeepRoutine.Play(warningDuration, warningFlashDuration, warningSfx);
        foreach (Spike spike in spikes)
            spike.BeginAttack();
        yield return new WaitForSeconds(spikes[0].MoveDuration + retractDelay);
        foreach (Spike spike in spikes)
            spike.BeginRetractWithWave();
        yield return new WaitForSeconds(spikes[0].MoveDuration);
        foreach (Spike spike in spikes)
            spike.ForceIdle();
        isActive = false;
    }

    public void Cancel()
    {
        foreach (Spike spike in spikes)
            spike.StopInPlace(false);
        isActive = false;
    }

    public void Reset()
    {
        foreach (Spike spike in spikes)
            spike.ForceIdle();
        isActive = false;
    }
}