using System;

public class DifficultyScaler
{
    private readonly float initSpikeInterval, initWarningDuration, minSpikeInterval, minWarningDuration, spikeIntervalReduction, warningDurationReduction;
    public float CurSpikeInterval { get; private set; }
    public float CurWarningDuration { get; private set; }

    public DifficultyScaler(float initSpikeInterval, float initWarningDuration, float minSpikeInterval, float minWarningDuration, float spikeIntervalReduction, float warningDurationReduction)
    {
        this.initSpikeInterval = initSpikeInterval;
        this.initWarningDuration = initWarningDuration;
        this.minSpikeInterval = minSpikeInterval;
        this.minWarningDuration = minWarningDuration;
        this.spikeIntervalReduction = spikeIntervalReduction;
        this.warningDurationReduction = warningDurationReduction;

        CurSpikeInterval = initSpikeInterval;
        CurWarningDuration = initWarningDuration;
    }

    public void Reset()
    {
        CurSpikeInterval = initSpikeInterval;
        CurWarningDuration = initWarningDuration;
    }

    public void IncreaseDiff()
    {
        CurSpikeInterval = MathF.Max(minSpikeInterval, CurSpikeInterval - spikeIntervalReduction);
        CurWarningDuration = MathF.Max(minWarningDuration, CurWarningDuration - warningDurationReduction);
    }
}
