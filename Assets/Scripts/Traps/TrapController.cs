using System.Collections;
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
    [SerializeField] private AudioClip warningSfx;
    [SerializeField] private TrapSetup trapSetup;
    [SerializeField] private HazardWarningUI hazardWarningUI;
    private Coroutine commandedPatternCoroutine;
    private DifficultyScaler difficultyScaler;
    private SpikePattern topSpikes, bottomSpikes;
    private ArrowPattern leftArrows, rightArrows;
    private ITrapPattern curPattern;
    private static readonly TrapPatternId[] PatternIds = {
        TrapPatternId.TopSpikes,
        TrapPatternId.BottomSpikes,
        TrapPatternId.LeftArrows,
        TrapPatternId.RightArrows
        };
    private TrapPatternId previousPatternId = TrapPatternId.None;
    public bool HasActivePattern => curPattern != null || commandedPatternCoroutine != null;
    public float CurrentWarningDuration => difficultyScaler.CurWarningDuration;
    public float CurrentPatternInterval => difficultyScaler.CurSpikeInterval;
    public float DifficultyScaleInterval => diffScaleInterval;
    public DifficultyScaler DifficultyScaler => difficultyScaler;

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
        topSpikes = new SpikePattern(trapSetup.TopSpikes, warningSfx, warningFlashDuration, spikeRetractDelay, hazardWarningUI.ShowSpikeWarning, hazardWarningUI.HideSpikeWarning, true);
        bottomSpikes = new SpikePattern(trapSetup.BottomSpikes, warningSfx, warningFlashDuration, spikeRetractDelay, hazardWarningUI.ShowSpikeWarning, hazardWarningUI.HideSpikeWarning, false);

        leftArrows = new ArrowPattern(trapSetup.LeftArrowShooters, warningSfx, warningFlashDuration);
        rightArrows = new ArrowPattern(trapSetup.RightArrowShooters, warningSfx, warningFlashDuration);
    }

    public void ResetTraps()
    {
        topSpikes.Reset();
        bottomSpikes.Reset();
    }

    public void ResetForNewRound()
    {
        CancelCurrentPattern();
        ResetTraps();
        previousPatternId = TrapPatternId.None;
        difficultyScaler.Reset();
    }

    public bool StartPattern(TrapPatternId patternId, float warningDuration)
    {
        if (HasActivePattern)
            return false;
        if (GetPattern(patternId) == null)
            return false;
        commandedPatternCoroutine = StartCoroutine(RunCommandedPattern(patternId, warningDuration));
        return true;
    }

    public void CancelCurrentPattern()
    {
        if (commandedPatternCoroutine != null)
        {
            StopCoroutine(commandedPatternCoroutine);
            commandedPatternCoroutine = null;
        }
        curPattern?.Cancel();
        curPattern = null;
    }

    public void IncreaseDifficulty()
    {
        difficultyScaler.IncreaseDiff();
    }

    public bool PatternPreventsEnemySpawning(TrapPatternId patternId)
    {
        ITrapPattern pattern = GetPattern(patternId);
        return pattern?.PreventsEnemySpawning ?? false;
    }

    public float GetPatternDuration(TrapPatternId patternId, float warningDuration)
    {
        ITrapPattern pattern = GetPattern(patternId);
        return pattern?.GetDuration(warningDuration) ?? 0f;
    }

    public TrapPatternId SelectRandomPatternId()
    {
        TrapPatternId selectedId;
        do
        {
            int index = Random.Range(0, PatternIds.Length);
            selectedId = PatternIds[index];
        }
        while (selectedId == previousPatternId);
        previousPatternId = selectedId;
        return selectedId;
    }

    private ITrapPattern GetPattern(TrapPatternId patternId)
    {
        return patternId switch
        {
            TrapPatternId.TopSpikes => topSpikes,
            TrapPatternId.BottomSpikes => bottomSpikes,
            TrapPatternId.LeftArrows => leftArrows,
            TrapPatternId.RightArrows => rightArrows,
            _ => null
        };
    }

    public IEnumerator RunPattern(TrapPatternId patternId, float warningDuration)
    {
        ITrapPattern selectedPattern = GetPattern(patternId);

        if (selectedPattern == null || selectedPattern.IsActive)
            yield break;

        curPattern = selectedPattern;
        yield return selectedPattern.Run(warningDuration);

        if (curPattern == selectedPattern)
            curPattern = null;
    }

    private IEnumerator RunCommandedPattern(TrapPatternId patternId, float warningDuration)
    {
        yield return RunPattern(patternId, warningDuration);
        commandedPatternCoroutine = null;
    }
}
