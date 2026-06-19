using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TrapPattern : ITrapPattern
{
    private readonly IReadOnlyList<ITrap> traps;
    private bool isActive;
    public bool IsActive => isActive;

    public TrapPattern(IReadOnlyList<ITrap> traps)
    {
        this.traps = traps;
    }

    public bool EveryTrapInactive()
    {
        foreach (ITrap trap in traps)
            if (trap.IsActive)
                return false;
        return true;
    }

    public IEnumerator Run(float warningDuration)
    {
        isActive = true;
        foreach (ITrap trap in traps)
            trap.Activate(warningDuration);
        yield return new WaitUntil(() => EveryTrapInactive());
        isActive = false;
    }

    public void Cancel()
    {
        isActive = false;
    }
}
