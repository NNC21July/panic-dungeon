using System.Collections;
using NaughtyAttributes;
using UnityEngine;

public class SinglePlayerTrapDirector : MonoBehaviour
{
    [SerializeField, Required] private TrapController trapController;
    [SerializeField, Required] private EnemySpawner enemySpawner;
    [ShowNonSerializedField]
    private bool roundActive = false;
    private Coroutine trapCoroutine, diffScaleCoroutine;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    public void StartTraps()
    {
        if (roundActive)
            return;

        trapController.ResetForNewRound();
        roundActive = true;

        if (trapCoroutine != null)
            StopCoroutine(trapCoroutine);
        trapCoroutine = StartCoroutine(Trap());
        if (diffScaleCoroutine != null)
            StopCoroutine(diffScaleCoroutine);
        diffScaleCoroutine = StartCoroutine(DiffScale());

        enemySpawner.StartSpawning(trapController.DifficultyScaler);
    }

    public void StopTraps()
    {
        trapController.CancelCurrentPattern();
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
        trapController.ResetTraps();
    }

    private IEnumerator Trap()
    {
        while (roundActive)
        {
            TrapPatternId selectedId = trapController.SelectRandomPatternId();

            enemySpawner.SetSpawnPaused(trapController.PatternPreventsEnemySpawning(selectedId));

            yield return trapController.RunPattern(selectedId, trapController.CurrentWarningDuration);

            enemySpawner.SetSpawnPaused(false);

            yield return new WaitForSeconds(trapController.CurrentPatternInterval);
        }
        trapCoroutine = null;
    }

    private IEnumerator DiffScale()
    {
        while (roundActive)
        {
            yield return new WaitForSeconds(trapController.DifficultyScaleInterval);
            trapController.IncreaseDifficulty();
        }
    }
}
