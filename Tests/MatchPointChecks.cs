using System;

internal static class MatchPointChecks
{
    public static void Run()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            checks++;
            if (!condition)
                throw new Exception(message);
        }

        for (int players = 2; players <= 10; players++)
        {
            int sum = 0;
            for (int rank = 1; rank <= players; rank++)
            {
                int points = MatchPointRules.PlacementPoints(players, rank);
                Check(points == players - rank + 1, "Placement points must descend from player count to one.");
                sum += points;
            }
            Check(sum == players * (players + 1) / 2, "A round without ties must award the expected total.");
        }
        Check(MatchPointRules.PlacementPoints(0, 1) == 0, "An empty round awards no points.");
        Check(MatchPointRules.PlacementPoints(3, 0) == 0, "A nonparticipant awards no points.");
        Check(MatchPointRules.PlacementPoints(3, 4) == 0, "Invalid placement awards no points.");
        Check(MatchPointRules.CompareRound(10f, true, 10f, false) < 0, "The survivor outranks the final victim on the same tick.");
        Check(MatchPointRules.CompareRound(10f, false, 10f, true) > 0, "The final victim does not share the survivor's first place.");
        Check(MatchPointRules.CompareRound(9f, false, 8f, false) < 0, "Longer survival wins round placement.");
        Check(MatchPointRules.CompareRound(8f, false, 8f, false) == 0, "Simultaneous eliminations share placement.");
        Check(MatchPointRules.CompareSeries(9, 10f, 8, 100f) < 0, "Points take priority over duration.");
        Check(MatchPointRules.CompareSeries(8, 100f, 9, 10f) > 0, "Longer survival cannot beat more points.");
        Check(MatchPointRules.CompareSeries(8, 100f, 8, 90f) < 0, "Total survival breaks a points tie.");
        Check(MatchPointRules.CompareSeries(8, 100f, 8, 100f) == 0, "Exact ties remain joint places.");

        int playerA = 0, playerB = 0, playerC = 0;
        int[,] placements = { { 1, 2, 3 }, { 2, 1, 3 }, { 1, 3, 2 } };
        for (int round = 0; round < 3; round++)
        {
            playerA += MatchPointRules.PlacementPoints(3, placements[round, 0]);
            playerB += MatchPointRules.PlacementPoints(3, placements[round, 1]);
            playerC += MatchPointRules.PlacementPoints(3, placements[round, 2]);
        }
        Check(playerA == 8 && playerB == 6 && playerC == 4, "Points must accumulate across three rounds.");
        Check(MatchPointRules.PlacementPoints(2, 1) == 2, "After a disconnect, the next round uses its own participant count.");
        Console.WriteLine($"Passed {checks} match placement and series scoring checks.");
    }
}
