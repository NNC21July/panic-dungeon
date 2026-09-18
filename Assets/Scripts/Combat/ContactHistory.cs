using System.Collections.Generic;

// Keep copied states, never pointers into changing network buffers.
public class ContactHistory<T>
{
    private readonly SortedList<int, T> samples = new();
    public int Count => samples.Count;
    public T this[int tick] { set => samples[tick] = value; }
    public bool ContainsKey(int tick) => samples.ContainsKey(tick);
    public bool TryGetValue(int tick, out T state) => samples.TryGetValue(tick, out state);
    public void Clear() => samples.Clear();

    public void Prune(int ticksToKeep)
    {
        if (samples.Count == 0)
            return;
        int oldest = samples.Keys[samples.Count - 1] - ticksToKeep;
        while (samples.Count > 0 && samples.Keys[0] < oldest)
            samples.RemoveAt(0);
    }
}
