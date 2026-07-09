using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikePattern : ITrapPattern
{
    private static readonly WaitForFixedUpdate FixedUpdateWait = new WaitForFixedUpdate();
    private readonly IReadOnlyList<Spike> spikes;
    private readonly AudioClip warningSfx;
    private readonly float warningFlashDuration, retractDelay;
    private readonly Action<bool> onWarningStart;
    private readonly Action onWarningEnd;
    private readonly bool isTopSpike;
    private bool isActive;
    public bool IsActive => isActive;
    public bool PreventsEnemySpawning => true;

    public SpikePattern(IReadOnlyList<Spike> spikes, AudioClip warningSfx, float warningFlashDuration, float retractDelay, Action<bool> onWarningStart, Action onWarningEnd, bool isTopSpike)
    {
        this.spikes = spikes;
        this.warningSfx = warningSfx;
        this.warningFlashDuration = warningFlashDuration;
        this.retractDelay = retractDelay;
        this.onWarningStart = onWarningStart;
        this.onWarningEnd = onWarningEnd;
        this.isTopSpike = isTopSpike;
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

        onWarningStart?.Invoke(isTopSpike);
        foreach (Spike spike in spikes)
            spike.BeginWarning(warningDuration, warningFlashDuration);
        yield return WarningBeepRoutine.Play(warningDuration, warningFlashDuration, warningSfx);
        onWarningEnd?.Invoke();

        foreach (Spike spike in spikes)
            spike.BeginAttack();
        yield return new WaitForSeconds(spikes[0].MoveDuration + retractDelay);

        foreach (Spike spike in spikes)
            spike.PrepareRetract();
        float retractDuration = spikes[0].MoveDuration;
        float retractTimer = 0f;
        while (retractTimer < retractDuration)
        {
            yield return FixedUpdateWait;
            retractTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(retractTimer / retractDuration);

            foreach (Spike spike in spikes)
                spike.ApplyRetractProgress(t);
        }
        foreach (Spike spike in spikes)
            spike.ApplyRetractProgress(1f);

        foreach (Spike spike in spikes)
            spike.ForceIdle();
        isActive = false;
    }

    public void Cancel()
    {
        foreach (Spike spike in spikes)
            spike.StopInPlace(false);
        isActive = false;
        onWarningEnd?.Invoke();
    }

    public void Reset()
    {
        foreach (Spike spike in spikes)
            spike.ForceIdle();
        isActive = false;
        onWarningEnd?.Invoke();
    }
}