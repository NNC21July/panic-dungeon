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
            string path = System.IO.Path.Combine(args[0], new AssemblyName(request.Name).Name + ".dll");
            return System.IO.File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        Run();
        Console.WriteLine($"Passed {checks} arrow contact checks.");
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
        Check(ArrowContactDetector.TouchesCircle(placement, outline, Vector2.zero, Vector2.zero, 0.1f), "inside outline");
        Check(!ArrowContactDetector.TouchesCircle(placement, outline, Vector2.zero, new Vector2(0.8f, 0f), 0.2f), "visible gap is not contact");
        Check(ArrowContactDetector.TouchesCircle(placement, outline, Vector2.zero, new Vector2(0.7f, 0f), 0.2f), "touching arrow tip");

        bool hit = ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.25f, out float fraction);
        Check(hit && Math.Abs(fraction - 0.3125f) < 0.001f, "fast crossing finds first contact");
        Check(!ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(-2f, 1f), new Vector2(2f, 1f), 0.25f, out _), "near miss stays a miss");
        Check(ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            Vector2.zero, Vector2.zero, 0.25f, out fraction) && fraction == 0f, "starting overlap");
        Check(!ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(3f, 0f), new Vector2(3f, 0f), 0.25f, out _), "stationary separated shapes");

        placement.m03 = 6f;
        // Arrow 2 -> 6, stationary player at 5: equivalent circle path is 9 -> 5.
        Check(ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(9f, 0f), new Vector2(5f, 0f), 0.25f, out fraction) &&
            Math.Abs(fraction - 0.5625f) < 0.001f, "relative motion matches moving arrow");
        Check(!ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(9f, 1f), new Vector2(9f, 1f), 0.25f, out _), "same-speed parallel motion");

        placement = Matrix4x4.identity;
        placement.m00 = 0f;
        placement.m01 = -1f;
        placement.m10 = 1f;
        placement.m11 = 0f;
        Check(ArrowContactDetector.TryGetContactFraction(placement, outline, Vector2.zero,
            new Vector2(0f, -2f), new Vector2(0f, 2f), 0.25f, out fraction) &&
            Math.Abs(fraction - 0.3125f) < 0.001f, "rotated placement");

        placement = Matrix4x4.identity;
        placement.m00 = placement.m11 = 2f;
        Check(ArrowContactDetector.TryGetContactFraction(placement, outline, new Vector2(1f, 0f),
            new Vector2(-2f, 0f), new Vector2(4f, 0f), 0.25f, out fraction) &&
            Math.Abs(fraction - (2.75f / 6f)) < 0.001f, "scale and collider offset");

        for (int i = 0; i < 100; i++)
        {
            float y = (i - 50) * 0.02f;
            Vector2 start = new Vector2(-2f, y), end = new Vector2(2f, y);
            bool swept = ArrowContactDetector.SweptTouchesCircle(Matrix4x4.identity, outline, Vector2.zero, start, end, 0.25f);
            bool timed = ArrowContactDetector.TryGetContactFraction(Matrix4x4.identity, outline, Vector2.zero, start, end, 0.25f, out fraction);
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
