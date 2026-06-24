using System;

public class DifficultyScaler
{
    private readonly float initSpikeInterval, initWarningDuration, minSpikeInterval, minWarningDuration, spikeIntervalReduction, warningDurationReduction, initEnemySpawnInterval, minEnemySpawnInterval, enemySpawnIntervalReduction;
    public float CurSpikeInterval { get; private set; }
    public float CurWarningDuration { get; private set; }
    public float CurEnemySpawnInterval { get; private set; }

    public DifficultyScaler(float initSpikeInterval, float initWarningDuration, float minSpikeInterval, float minWarningDuration, float spikeIntervalReduction, float warningDurationReduction, float initEnemySpawnInterval, float minEnemySpawnInterval, float enemySpawnIntervalReduction)
    {
        this.initSpikeInterval = initSpikeInterval;
        this.initWarningDuration = initWarningDuration;
        this.minSpikeInterval = minSpikeInterval;
        this.minWarningDuration = minWarningDuration;
        this.spikeIntervalReduction = spikeIntervalReduction;
        this.warningDurationReduction = warningDurationReduction;
        this.initEnemySpawnInterval = initEnemySpawnInterval;
        this.minEnemySpawnInterval = minEnemySpawnInterval;
        this.enemySpawnIntervalReduction = enemySpawnIntervalReduction;

        CurSpikeInterval = initSpikeInterval;
        CurWarningDuration = initWarningDuration;
        CurEnemySpawnInterval = initEnemySpawnInterval;
    }

    public void Reset()
    {
        CurSpikeInterval = initSpikeInterval;
        CurWarningDuration = initWarningDuration;
        CurEnemySpawnInterval = initEnemySpawnInterval;
    }

    public void IncreaseDiff()
    {
        CurSpikeInterval = MathF.Max(minSpikeInterval, CurSpikeInterval - spikeIntervalReduction);
        CurWarningDuration = MathF.Max(minWarningDuration, CurWarningDuration - warningDurationReduction);
        CurEnemySpawnInterval = MathF.Max(minEnemySpawnInterval, CurEnemySpawnInterval - enemySpawnIntervalReduction);
    }
}
