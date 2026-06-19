using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikePattern : ITrapPattern
{
    private IReadOnlyList<Spike> spikes;
    private bool isActive;
    public bool IsActive => isActive;

    public SpikePattern(IReadOnlyList<Spike> spikes)
    {
        this.spikes = spikes;
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
            spike.BeginWarning(warningDuration);
        yield return new WaitForSeconds(warningDuration);
        foreach (Spike spike in spikes)
            spike.BeginAttack();
        yield return new WaitForSeconds(spikes[0].MoveDuration + spikes[0].RetractDelay);
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
            spike.StopInPlace();
        isActive = false;
    }

    public void Reset()
    {
        foreach (Spike spike in spikes)
            spike.ForceIdle();
        isActive = false;
    }
}