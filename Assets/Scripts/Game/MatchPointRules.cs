// Scoring policy only. Networking and UI animations do not calculate their own points.
public static class MatchPointRules
{
    public static int PlacementPoints(int playerCount, int placement)
    {
        return placement < 1 || placement > playerCount ? 0 : playerCount - placement + 1;
    }

    public static int CompareRound(float duration, bool winner, float otherDuration, bool otherWinner)
    {
        // The survivor wins even when the final elimination happens on the same tick.
        if (winner != otherWinner)
            return winner ? -1 : 1;
        return otherDuration.CompareTo(duration);
    }

    public static int CompareSeries(int points, float duration, int otherPoints, float otherDuration)
    {
        int score = otherPoints.CompareTo(points);
        return score != 0 ? score : otherDuration.CompareTo(duration);
    }
}
