using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

// Standalone checks for the actual shared detector, using Unity's managed math types.
internal static class ArrowContactChecks
{
    private static int checks;

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            foreach (string folder in args)
            {
                string path = System.IO.Path.Combine(folder, new AssemblyName(request.Name).Name + ".dll");
                if (System.IO.File.Exists(path))
                    return Assembly.LoadFrom(path);
            }
            return null;
        };
        Run();
        Console.WriteLine($"Passed {checks} arrow contact checks.");
        SpikeContactChecks.Run();
        TrapContactPreviewChecks.Run();
        PoisonEffectChecks.Run();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        Vector2[] outline =
        {
            new Vector2(-0.5f, -0.1f), new Vector2(0.3f, -0.1f),
            new Vector2(0.5f, 0f), new Vector2(0.3f, 0.1f), new Vector2(-0.5f, 0.1f)
        };
        Matrix4x4 placement = Matrix4x4.identity;
        Check(PolygonContactDetector.TouchesCircle(placement, outline, Vector2.zero, Vector2.zero, 0.1f), "inside outline");
        Check(!PolygonContactDetector.TouchesCircle(placement, outline, Vector2.zero, new Vector2(0.8f, 0f), 0.2f), "visible gap is not contact");
        Check(PolygonContactDetector.TouchesCircle(placement, outline, Vector2.zero, new Vector2(0.7f, 0f), 0.2f), "touching arrow tip");

        bool hit = PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.25f, out float fraction);
        Check(hit && Math.Abs(fraction - 0.3125f) < 0.001f, "fast crossing finds first contact");
        Check(!PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(-2f, 1f), new Vector2(2f, 1f), 0.25f, out _), "near miss stays a miss");
        Check(PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            Vector2.zero, Vector2.zero, 0.25f, out fraction) && fraction == 0f, "starting overlap");
        Check(!PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(3f, 0f), new Vector2(3f, 0f), 0.25f, out _), "stationary separated shapes");

        placement.m03 = 6f;
        // Arrow 2 -> 6, stationary player at 5: equivalent circle path is 9 -> 5.
        Check(PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(9f, 0f), new Vector2(5f, 0f), 0.25f, out fraction) &&
            Math.Abs(fraction - 0.5625f) < 0.001f, "relative motion matches moving arrow");
        Check(!PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(9f, 1f), new Vector2(9f, 1f), 0.25f, out _), "same-speed parallel motion");

        // Arrow 2 -> 6, player 4 -> 7: both move right, but the arrow closes the gap without touching.
        Check(!PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(8f, 0f), new Vector2(7f, 0f), 0.25f, out _), "same-direction dash with gap does not preview contact");
        // Arrow 2 -> 6, player 4 -> 6.5: first contact must be detected before the final overlap.
        Check(PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(8f, 0f), new Vector2(6.5f, 0f), 0.25f, out fraction) &&
            fraction > 0f && fraction < 1f, "arrow overtaking same-direction dash detects first touch");

        placement = Matrix4x4.identity;
        placement.m00 = 0f;
        placement.m01 = -1f;
        placement.m10 = 1f;
        placement.m11 = 0f;
        Check(PolygonContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(0f, -2f), new Vector2(0f, 2f), 0.25f, out fraction) &&
            Math.Abs(fraction - 0.3125f) < 0.001f, "rotated placement");

        placement = Matrix4x4.identity;
        placement.m00 = placement.m11 = 2f;
        Check(PolygonContactDetector.TryGetContactFraction(placement, outline, new Vector2(1f, 0f),
            new Vector2(-2f, 0f), new Vector2(4f, 0f), 0.25f, out fraction) &&
            Math.Abs(fraction - (2.75f / 6f)) < 0.001f, "scale and collider offset");

        for (int i = 0; i < 100; i++)
        {
            float y = (i - 50) * 0.02f;
            Vector2 start = new Vector2(-2f, y), end = new Vector2(2f, y);
            bool swept = PolygonContactDetector.SweptTouchesCircle(Matrix4x4.identity, outline, Vector2.zero, start, end, 0.25f);
            bool timed = PolygonContactDetector.TryGetContactFraction(Matrix4x4.identity, outline, Vector2.zero, start, end, 0.25f, out fraction);
            Check(swept == timed && (!timed || (fraction >= 0f && fraction <= 1f)), "sweep/fraction agreement " + i);
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        checks++;
    }
}
