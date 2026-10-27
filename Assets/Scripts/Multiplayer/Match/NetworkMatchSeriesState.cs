using Fusion;
using UnityEngine;

// Server-only carry-over on the runner, not a static shared by multi-peer instances.
// Scene objects are replaced each round; the runner survives those scene loads.
public class NetworkMatchSeriesState : MonoBehaviour
{
    public int NextRound { get; private set; }
    public MatchPlayerResult[] Results { get; private set; }
    public PlayerRef[] Colors { get; private set; }

    public void Save(int nextRound, MatchPlayerResult[] results, PlayerRef[] colors)
    {
        NextRound = nextRound;
        Results = results;
        Colors = colors;
    }

    public void Clear()
    {
        NextRound = 0;
        Results = null;
        Colors = null;
    }
}
