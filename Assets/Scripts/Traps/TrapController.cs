using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float initSpikeInterval = 3f;
    [SerializeField, Min(0.01f)] private float minSpikeInterval = 0.5f;
    [SerializeField, Min(0.01f)] private float spikeIntervalReduction = 0.2f;
    [SerializeField, Min(0.01f)] private float initWarningDuration = 3f;
    [SerializeField, Min(0.01f)] private float minWarningDuration = 0.9f;
    [SerializeField, Min(0.01f)] private float warningDurationReduction = 0.1f;
    [SerializeField, Min(0.01f)] private float warningFlashDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float spikeRetractDelay = 2f;
    [SerializeField, Min(0.01f)] private float initEnemySpawnInterval = 5f;
    [SerializeField, Min(0.01f)] private float minEnemySpawnInterval = 1.5f;
    [SerializeField, Min(0.01f)] private float enemySpawnIntervalReduction = 0.1f;
    [SerializeField, Min(0.01f)] private float diffScaleInterval = 6f;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AudioClip warningSfx;
    [SerializeField] private TrapSetup trapSetup;
    private bool roundActive = false;
    private Coroutine trapCoroutine, diffScaleCoroutine;
    private DifficultyScaler difficultyScaler;
    private SpikePattern topSpikes, bottomSpikes;
    private ArrowPattern leftArrows, rightArrows;
    private ITrapPattern curPattern;
    private List<ITrapPattern> patterns;
    private int prevPatternIdx = -1;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        difficultyScaler = new DifficultyScaler(
                                initSpikeInterval,
                                initWarningDuration,
                                minSpikeInterval,
                                minWarningDuration,
                                spikeIntervalReduction,
                                warningDurationReduction, initEnemySpawnInterval, minEnemySpawnInterval, enemySpawnIntervalReduction);
    }

    private void Start()
    {
        topSpikes = new SpikePattern(trapSetup.TopSpikes, warningSfx, warningFlashDuration, spikeRetractDelay);
        bottomSpikes = new SpikePattern(trapSetup.BottomSpikes, warningSfx, warningFlashDuration, spikeRetractDelay);
        leftArrows = new ArrowPattern(trapSetup.LeftArrowShooters, warningSfx, warningFlashDuration);
        rightArrows = new ArrowPattern(trapSetup.RightArrowShooters, warningSfx, warningFlashDuration);
        patterns = new List<ITrapPattern> { topSpikes, bottomSpikes, leftArrows, rightArrows };
    }

    public void StartTraps()
    {
        if (roundActive)
            return;

        topSpikes.Reset();
        bottomSpikes.Reset();

        roundActive = true;
        difficultyScaler.Reset();

        if (trapCoroutine != null)
            StopCoroutine(trapCoroutine);
        trapCoroutine = StartCoroutine(Trap());
        if (diffScaleCoroutine != null)
            StopCoroutine(diffScaleCoroutine);
        diffScaleCoroutine = StartCoroutine(DiffScale());

        enemySpawner.StartSpawning(difficultyScaler);
    }

    public void StopTraps()
    {
        curPattern?.Cancel();
        curPattern = null;
        roundActive = false;
        if (trapCoroutine != null)
        {
            StopCoroutine(trapCoroutine);
            trapCoroutine = null;
        }
        if (diffScaleCoroutine != null)
        {
            StopCoroutine(diffScaleCoroutine);
            diffScaleCoroutine = null;
        }
        enemySpawner.StopSpawning();
    }

    public void ResetTraps()
    {
        topSpikes.Reset();
        bottomSpikes.Reset();
    }

    private ITrapPattern SelectRandomPattern()
    {
        if (patterns.Count == 0)
            return null;

        if (patterns.Count == 1)
            return patterns[0];

        int selectedIdx;
        do
            selectedIdx = Random.Range(0, patterns.Count);
        while (selectedIdx == prevPatternIdx);
        prevPatternIdx = selectedIdx;
        return patterns[selectedIdx];
    }

    private IEnumerator Trap()
    {
        while (roundActive)
        {
            curPattern = SelectRandomPattern();
            enemySpawner.SetSpawnPaused(curPattern.PreventsEnemySpawning);
            yield return curPattern.Run(difficultyScaler.CurWarningDuration);
            curPattern = null;
            enemySpawner.SetSpawnPaused(false);

            yield return new WaitForSeconds(difficultyScaler.CurSpikeInterval);
        }
        trapCoroutine = null;
    }

    private IEnumerator DiffScale()
    {
        while (roundActive)
        {
            yield return new WaitForSeconds(diffScaleInterval);
            difficultyScaler.IncreaseDiff();
        }
    }
}
