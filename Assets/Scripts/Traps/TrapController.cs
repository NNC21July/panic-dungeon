using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField, Min(0.01f)] private float initSpikeInterval, initWarningDuration, minSpikeInterval, minWarningDuration, spikeIntervalReduction, warningDurationReduction, diffScaleInterval;
    private bool roundActive = false;
    private Coroutine trapCoroutine, diffScaleCoroutine;
    private DifficultyScaler difficultyScaler;
    private SpikePattern topSpikes, bottomSpikes;
    private TrapPattern leftArrows, rightArrows;
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
                                warningDurationReduction);
    }

    private void Start()
    {
        topSpikes = new SpikePattern(trapSetup.TopSpikes);
        bottomSpikes = new SpikePattern(trapSetup.BottomSpikes);
        leftArrows = new TrapPattern(trapSetup.LeftArrowShooters);
        rightArrows = new TrapPattern(trapSetup.RightArrowShooters);
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
            yield return curPattern.Run(difficultyScaler.CurWarningDuration);
            curPattern = null;

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
