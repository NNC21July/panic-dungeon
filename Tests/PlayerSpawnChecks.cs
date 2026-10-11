using System;
using System.Collections.Generic;
using UnityEngine;

internal static class PlayerSpawnChecks
{
    private static int checks;

    public static void Run()
    {
        Vector2[] tiles = { Vector2.zero, new Vector2(5f, 5f), new Vector2(-5f, -5f), new Vector2(5f, -5f), new Vector2(-5f, 5f) };
        var used = new List<Vector2>();
        Check(PlayerSpawnRules.TryFindSpreadPosition(tiles, used, Vector2.zero, 1f, out Vector2 first) &&
            first == new Vector2(-5f, -5f), "first spawn uses a distant safe tile with stable tie-breaking");
        used.Add(first);
        Check(PlayerSpawnRules.TryFindSpreadPosition(tiles, used, Vector2.zero, 1f, out Vector2 second) &&
            second == new Vector2(5f, 5f), "second player gets opposite corner");
        used.Add(second);
        Check(PlayerSpawnRules.TryFindSpreadPosition(tiles, used, Vector2.zero, 1f, out Vector2 third) &&
            third == new Vector2(-5f, 5f), "third maximizes distance from nearest existing player");
        Array.Reverse(tiles);
        Check(PlayerSpawnRules.TryFindSpreadPosition(tiles, used, Vector2.zero, 1f, out Vector2 reversed) && reversed == third,
            "floor enumeration order does not change result");
        Check(!PlayerSpawnRules.TryFindSpreadPosition(Array.Empty<Vector2>(), used, Vector2.zero, 1f, out _), "no safe floor fails safely");
        Check(!PlayerSpawnRules.TryFindSpreadPosition(new[] { first }, used, Vector2.zero, 0f, out _), "never reuse a reserved position");
        Check(!PlayerSpawnRules.TryFindSpreadPosition(new[] { first + new Vector2(0.2f, 0f) }, used, Vector2.zero, 0.75f, out _),
            "initial body overlap is rejected even though characters can later pass through");
        var grid = new List<Vector2>();
        for (int x = -10; x <= 10; x++)
            for (int y = -5; y <= 5; y++)
                grid.Add(new Vector2(x, y));
        used.Clear();
        for (int i = 0; i < 10; i++)
        {
            bool found = PlayerSpawnRules.TryFindSpreadPosition(grid, used, Vector2.zero, 0.75f, out Vector2 pos);
            Check(found && grid.Contains(pos) && !used.Contains(pos), "ten-player placement " + i);
            used.Add(pos);
        }
        Console.WriteLine($"Passed {checks} spread player spawn checks.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        checks++;
    }
}
