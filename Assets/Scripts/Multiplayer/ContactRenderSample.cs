using Fusion;

public struct ContactRenderSample : INetworkStruct
{
    public int FromTick, ToTick;
    public float Alpha;

    public double GetTime(float deltaTime)
    {
        return (FromTick + ((double)ToTick - FromTick) * Alpha) * deltaTime;
    }

    public bool IsValid(double now, float deltaTime, double maxAge)
    {
        return FromTick >= 0 && ToTick >= FromTick &&
            !float.IsNaN(Alpha) && !float.IsInfinity(Alpha) && Alpha >= 0f && Alpha <= 1f &&
            FromTick * (double)deltaTime >= now - maxAge &&
            ToTick * (double)deltaTime <= now + deltaTime * 2d;
    }

    public static bool CanCheckPath(ContactRenderSample start, ContactRenderSample end,
        PlayerContactSample playerStart, PlayerContactSample playerEnd, double now, float deltaTime,
        double maxAge = 1d, double maxInterval = 0.1d)
    {
        if (!start.IsValid(now, deltaTime, maxAge) || !end.IsValid(now, deltaTime, maxAge) ||
            double.IsNaN(playerStart.Time) || double.IsInfinity(playerStart.Time) ||
            double.IsNaN(playerEnd.Time) || double.IsInfinity(playerEnd.Time))
            return false;
        double startTime = start.GetTime(deltaTime), endTime = end.GetTime(deltaTime);
        return endTime >= startTime && endTime - startTime <= maxInterval &&
            playerEnd.Time >= playerStart.Time && playerEnd.Time - playerStart.Time <= maxInterval &&
            playerStart.Time >= startTime - deltaTime * 2d && playerEnd.Time >= endTime - deltaTime * 2d &&
            playerStart.Time - startTime <= maxAge && playerEnd.Time - endTime <= maxAge;
    }
}
