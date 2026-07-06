using System.Collections;

public interface ITrapPattern
{
    bool IsActive { get; }
    bool PreventsEnemySpawning { get; }
    IEnumerator Run(float warningDuration);
    void Cancel();
}