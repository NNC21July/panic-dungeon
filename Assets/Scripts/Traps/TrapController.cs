using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    [SerializeField] private RoomSetup roomSetup;
    [SerializeField, Min(0.01f)] private float initSpikeInterval, initWarningDuration, minSpikeInterval, minWarningDuration, spikeIntervalReduction, warningDurationReduction, diffScaleInterval;
    private bool roundActive = false;
    private Coroutine trapCoroutine, diffScaleCoroutine;
    private DifficultyScaler difficultyScaler;
    private TrapPattern topSpikes, bottomSpikes;
    private List<TrapPattern> patterns;
    private int prevPatternIdx = -1;

    private void Awake()
    {
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
        topSpikes = new TrapPattern(roomSetup.TopSpikes);
        bottomSpikes = new TrapPattern(roomSetup.BottomSpikes);
        patterns = new List<TrapPattern> { topSpikes, bottomSpikes };
    }

    public void StartTraps()
    {
        if (roundActive)
            return;

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

    private TrapPattern SelectRandomPattern()
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
            TrapPattern selectedPattern = SelectRandomPattern();
            if (selectedPattern == null)
                yield break;

            selectedPattern.Activate(difficultyScaler.CurWarningDuration);

            yield return new WaitUntil(() => !selectedPattern.IsActive);
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
