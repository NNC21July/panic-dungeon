using System.Collections.Generic;
using UnityEngine;

// Local presentation only: never send sound events over the network.
public class ResultsAudio : MonoBehaviour
{
    private AudioSource source;
    private ResultsPresentationSettings settings;
    private readonly HashSet<float> medalTimes = new();
    private float previousTime, nextTickTime;
    private bool hasPoints;
    private int previousTotal;

    public void Begin(ResultsPresentationSettings settings, float elapsed, bool hasPoints,
        IEnumerable<float> medalStarts, bool finalResults, int displayedTotal)
    {
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }
        source.Stop();
        this.settings = settings;
        this.hasPoints = hasPoints;
        previousTime = elapsed <= 0.25f ? -0.001f : elapsed;
        previousTotal = displayedTotal;
        nextTickTime = 0f;
        medalTimes.Clear();
        foreach (float start in medalStarts)
            medalTimes.Add(start);
        // Do not replay opening sounds when catching up to an old results screen.
        if (elapsed > 0.25f)
            return;
        Play(settings.whooshSound, settings.whooshVolume);
        if (finalResults)
            Play(settings.victorySound, settings.victoryVolume);
    }

    public void Advance(float elapsed, bool visible, int displayedTotal)
    {
        if (settings == null)
            return;
        if (!visible)
        {
            source.Stop();
            previousTime = elapsed;
            previousTotal = displayedTotal;
            return;
        }
        // Seeking or a long frame should not produce a burst of missed sounds.
        bool continuous = elapsed >= previousTime && elapsed - previousTime <= 0.3f;
        if (continuous)
        {
            float pointsStart = Mathf.Max(0f, settings.pointsDelay);
            if (hasPoints && previousTime < pointsStart && elapsed >= pointsStart)
                Play(settings.dingSound, settings.dingVolume);
            foreach (float start in medalTimes)
                if (previousTime < start && elapsed >= start)
                    Play(settings.medalSound, settings.medalVolume);
            if (hasPoints && displayedTotal != previousTotal && elapsed >= nextTickTime &&
                elapsed >= pointsStart && elapsed <= pointsStart + Mathf.Max(0.1f, settings.pointsDuration))
            {
                Play(settings.tickSound, settings.tickVolume);
                nextTickTime = elapsed + Mathf.Max(0.02f, settings.tickInterval);
            }
        }
        previousTime = elapsed;
        previousTotal = displayedTotal;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && source != null && source.enabled && gameObject.activeInHierarchy)
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void OnDisable()
    {
        if (source != null)
            source.Stop();
    }
}
