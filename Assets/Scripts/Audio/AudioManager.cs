using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        } // only one AudioManager should exist

        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}