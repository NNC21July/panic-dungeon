using System.Collections;

public interface ITrapPattern
{
    bool IsActive { get; }
    IEnumerator Run(float warningDuration);
    void Cancel();
}