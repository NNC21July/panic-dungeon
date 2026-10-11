using System;
using System.Collections.Generic;
using UnityEngine;

internal static class CharacterColorChecks
{
    public static void Run()
    {
        string[] expected = { "FFFFFF", "ECFF00", "88FF00", "00FFAB", "00ABFF", "AFB7FF", "F281FF", "FF9728", "8C8C8C", "FF5C5C" };
        if (CharacterColors.Count != 10)
            throw new Exception("Selection grid needs exactly ten colors.");
        var unique = new HashSet<string>();
        for (int i = 0; i < expected.Length; i++)
        {
            Color32 color = CharacterColors.Get(i);
            string hex = $"{color.r:X2}{color.g:X2}{color.b:X2}";
            if (hex != expected[i] || color.a != 255 || !unique.Add(hex))
                throw new Exception("Invalid or duplicate character color at slot " + i);
        }
        Console.WriteLine("Passed 11 character palette checks.");
    }
}
