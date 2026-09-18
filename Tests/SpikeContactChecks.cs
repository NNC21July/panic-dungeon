using System;
using UnityEngine;

internal static class SpikeContactChecks
{
    private static int checks;

    public static void Run()
    {
        Vector2[] tip = { new Vector2(0f, 0.5773587f), new Vector2(-0.5f, -0.28866667f), new Vector2(0.5f, -0.28866667f) };
        Matrix4x4 matrix = Matrix4x4.identity;
        Check(!PolygonContactDetector.TouchesCircle(matrix, tip, Vector2.zero, new Vector2(0f, 0.9f), 0.25f), "visible tip gap");
        Check(PolygonContactDetector.TouchesCircle(matrix, tip, Vector2.zero, new Vector2(0f, 0.8f), 0.25f), "tip contact");
        Check(PolygonContactDetector.TryGetContactFraction(matrix, tip, Vector2.zero,
            new Vector2(0f, 3f), Vector2.zero, 0.25f, out float fraction) && fraction > 0f && fraction < 1f, "fast spike sweep");
        matrix.m00 = matrix.m11 = -2f;
        Check(PolygonContactDetector.TouchesCircle(matrix, tip, Vector2.zero, new Vector2(0f, -1.3f), 0.25f), "scaled top spike");
        Check(!PolygonContactDetector.TouchesCircle(matrix, tip, Vector2.zero, new Vector2(0f, -1.5f), 0.25f), "scaled top spike gap");

        NetworkSpikeState first = new NetworkSpikeState
        {
            Position = Vector2.zero, PatternSequence = 7, ImpactCount = 3,
            Phase = SpikePhase.Advancing, IsSolid = true
        };
        NetworkSpikeState last = first;
        last.Position = new Vector2(0f, 4f);
        Check(NetworkSpikeState.CanCheckContact(first, last, 7), "same attacking cycle can report");
        Check(!NetworkSpikeState.CanCheckContact(first, last, 8), "old attack cannot report in new cycle");
        Check(NetworkSpikeState.Interpolate(first, last, 0.5f).Position.y == 2f, "interpolate host positions");
        last.Phase = SpikePhase.Stopped;
        last.ImpactCount++;
        Check(!NetworkSpikeState.CanCheckContact(first, last, 7), "stopped spike is not a new damage sweep");
        Check(NetworkSpikeState.Interpolate(first, last, 0.5f).Position.y == 0f, "do not slide through contact stop");
        Check(NetworkSpikeState.Interpolate(first, last, 1f).Phase == SpikePhase.Stopped, "apply stop at boundary");
        last.Phase = SpikePhase.Retracting;
        Check(!NetworkSpikeState.CanCheckContact(first, last, 7), "retraction cannot deal reported damage");
        Check(NetworkSpikeState.Interpolate(first, last, 0.5f).Phase == SpikePhase.Advancing, "phase is discrete");
        first = last;
        last.Position = new Vector2(0f, 2f);
        Check(NetworkSpikeState.Interpolate(first, last, 0.5f).Position.y == 3f, "interpolate retraction");
        last.PatternSequence++;
        last.Position = Vector2.zero;
        Check(NetworkSpikeState.Interpolate(first, last, 0.5f).Position.y == 4f, "no sweep into new cycle");
        last.Phase = SpikePhase.Idle;
        last.IsSolid = false;
        Check(!NetworkSpikeState.Interpolate(first, last, 1f).IsSolid, "idle disables solid tip");

        var history = new ContactHistory<int>();
        history[10] = 100;
        history[12] = 120;
        history[11] = 110;
        history.Prune(1);
        Check(!history.ContainsKey(10) && history.Count == 2, "bounded history with out-of-order insertion");
        Check(history.TryGetValue(11, out int value) && value == 110, "exact tick lookup");
        Check(!history.TryGetValue(13, out _), "missing evidence is not invented");
        history.Clear();
        Check(history.Count == 0, "restart clears history");

        const float dt = 0.02f;
        var start = new ContactRenderSample { FromTick = 40, ToTick = 42, Alpha = 0.5f };
        var end = new ContactRenderSample { FromTick = 41, ToTick = 43, Alpha = 0.5f };
        var playerStart = new PlayerContactSample { Time = 0.9d };
        var playerEnd = new PlayerContactSample { Time = 0.92d };
        Check(Math.Abs(start.GetTime(dt) - 0.82d) < 0.0001d, "render sample time");
        Check(ContactRenderSample.CanCheckPath(start, end, playerStart, playerEnd, 1d, dt), "separate owner and render clocks");
        Check(!start.IsValid(3d, dt, 1d), "stale sample");
        var invalid = start;
        invalid.Alpha = float.NaN;
        Check(!invalid.IsValid(1d, dt, 1d), "NaN alpha");
        invalid = start;
        invalid.ToTick = 39;
        Check(!invalid.IsValid(1d, dt, 1d), "reversed ticks");
        invalid = end;
        invalid.ToTick = 100;
        Check(!invalid.IsValid(1d, dt, 1d), "future ticks");
        Check(!ContactRenderSample.CanCheckPath(end, start, playerStart, playerEnd, 1d, dt), "backwards render time");
        playerEnd.Time = double.PositiveInfinity;
        Check(!ContactRenderSample.CanCheckPath(start, end, playerStart, playerEnd, 1d, dt), "infinite owner timestamp");
        playerEnd.Time = 1.3d;
        Check(!ContactRenderSample.CanCheckPath(start, end, playerStart, playerEnd, 1d, dt), "oversized interval");
        Console.WriteLine($"Passed {checks} spike/shared history and timing checks.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        checks++;
    }
}
