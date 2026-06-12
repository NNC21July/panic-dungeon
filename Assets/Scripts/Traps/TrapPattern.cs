using System.Collections.Generic;

public class TrapPattern
{
    private readonly IReadOnlyList<ITrap> traps;

    public TrapPattern(IReadOnlyList<ITrap> traps)
    {
        this.traps = traps;
    }

    public bool IsActive
    {
        get
        {
            foreach (ITrap trap in traps)
                if (trap.IsActive)
                    return true;
            return false;
        }
    }

    public void Activate(float warningDuration)
    {
        foreach (ITrap trap in traps)
            trap.Activate(warningDuration);
    }
}
