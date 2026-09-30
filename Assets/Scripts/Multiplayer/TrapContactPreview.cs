using System.Collections.Generic;

// Local presentation bookkeeping only; this never approves a hit or changes replicated state.
public class TrapContactPreview<TTarget>
{
    public bool IsActive { get; private set; }
    public int Cycle { get; private set; }
    public TTarget Target { get; private set; }
    private bool hasAttempted;
    private int attemptedCycle;
    private double deadline;

    public bool TryBegin(int cycle, TTarget target, double now, double duration)
    {
        if (hasAttempted && attemptedCycle == cycle)
            return false;
        hasAttempted = true;
        attemptedCycle = Cycle = cycle;
        Target = target;
        deadline = now + duration;
        IsActive = true;
        return true;
    }

    public bool Evaluate(int cycle, double now, bool canContinue)
    {
        if (IsActive && (!canContinue || cycle != Cycle || now >= deadline))
            Release();
        return IsActive;
    }

    public void Reject(int cycle, TTarget target)
    {
        if (Cycle == cycle && EqualityComparer<TTarget>.Default.Equals(Target, target))
            Release();
    }

    public void Release()
    {
        IsActive = false;
    }

    public void Reset()
    {
        Release();
        hasAttempted = false;
        attemptedCycle = Cycle = 0;
        Target = default;
        deadline = 0d;
    }
}
