using System;

internal static class TrapContactPreviewChecks
{
    private static int checks;

    public static void Run()
    {
        var preview = new TrapContactPreview<int>();
        Check(!preview.IsActive && !preview.Evaluate(0, 0d, true), "new preview cannot hide or hold anything");
        Check(preview.TryBegin(0, 12, 1d, 0.5d), "arrow starts one preview for its lifetime");
        Check(preview.Target == 12 && preview.Cycle == 0, "preview records exact contact identity");
        Check(preview.Evaluate(0, 1.2d, true), "preview waits within deadline");
        preview.Reject(0, 13);
        Check(preview.IsActive, "another player's rejected claim cannot cancel this preview");
        preview.Reject(1, 12);
        Check(preview.IsActive, "another attack's rejection cannot cancel this preview");
        Check(!preview.TryBegin(0, 13, 1.4d, 10d) && preview.Target == 12, "repeated contact cannot change target or extend deadline");
        Check(!preview.Evaluate(0, 1.5d, true), "preview expires at original deadline");
        Check(!preview.TryBegin(0, 12, 1.6d, 0.5d), "timeout cannot produce a hide/show loop");

        preview.Reset();
        Check(!preview.IsActive && preview.Target == 0, "pooled respawn clears active preview and old target");
        Check(preview.TryBegin(0, 13, 2d, 0.5d), "pooled arrow can preview its new lifetime");
        preview.Reject(0, 12);
        Check(preview.IsActive, "old target's rejection cannot cancel new target");
        preview.Reject(0, 13);
        Check(!preview.IsActive, "matching rejection releases arrow preview");
        Check(!preview.TryBegin(0, 13, 2.1d, 0.5d), "rejection cannot immediately hide same arrow again");

        Check(preview.TryBegin(7, 12, 3d, 0.5d), "new spike attack can preview again");
        preview.Release();
        Check(!preview.IsActive, "authoritative stop or finish can end provisional action");
        Check(!preview.TryBegin(7, 12, 3.1d, 0.5d), "completed attack stays completed");
        Check(preview.TryBegin(8, 12, 4d, 0.5d), "next spike cycle is independent");
        Check(!preview.Evaluate(9, 4.1d, true), "attack change releases old preview");
        Check(preview.TryBegin(9, 12, 5d, 0.5d), "attack after reset boundary can start");
        Check(!preview.Evaluate(9, 5.1d, false), "owner proximity or target departure releases preview");
        Check(!preview.TryBegin(9, 12, 5.2d, 0.5d), "safety release cannot repeatedly obstruct owner");

        var otherPeer = new TrapContactPreview<int>();
        otherPeer.TryBegin(0, 12, 1d, 0.5d);
        preview.Reset();
        Check(otherPeer.IsActive, "one client's reset cannot change another client's preview");
        Check(!otherPeer.Evaluate(0, 100d, true), "long frame stall still expires preview");
        Console.WriteLine($"Passed {checks} shared trap contact preview checks.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        checks++;
    }
}
