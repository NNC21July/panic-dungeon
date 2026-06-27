using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource generalSfxSource, trapSfxSource;
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        } // only one AudioManager should exist

        Instance = this;

        SerializedFieldValidator.Validate(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        Play(generalSfxSource, clip, volume);
    }

    public void PlayTrapSfx(AudioClip clip, float volume = 1f)
    {
        Play(trapSfxSource, clip, volume);
    }

    private void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}