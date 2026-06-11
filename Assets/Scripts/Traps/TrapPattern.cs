using System.Collections.Generic;

public class TrapPattern
{
    private readonly IReadOnlyList<Spike> spikes;

    public TrapPattern(IReadOnlyList<Spike> spikes)
    {
        this.spikes = spikes;
    }

    public bool IsActive
    {
        get
        {
            foreach (Spike spike in spikes)
                if (spike.IsActivated)
                    return true;
            return false;
        }
    }

    public void TryActivate(float warningDuration)
    {
        foreach (Spike spike in spikes)
            spike.TryActivate(warningDuration);
    }
}
