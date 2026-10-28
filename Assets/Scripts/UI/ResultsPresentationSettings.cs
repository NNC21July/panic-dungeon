using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(menuName = "Panic Dungeon/Results Presentation")]
public class ResultsPresentationSettings : ScriptableObject
{
    [Header("Panel and Scene Motion")]
    [Min(0.1f)] public float panelSlideDuration = 0.6f;
    public Ease panelSlideEase = Ease.OutCubic;
    [Min(0.1f)] public float wipeDuration = 0.45f;
    public Ease wipeEase = Ease.InOutCubic;
    public Color wipeColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [Header("Results Audio")]
    public AudioClip tickSound, dingSound, whooshSound, medalSound, victorySound;
    [Range(0f, 1f)] public float tickVolume = 0.9f;
    [Range(0f, 1f)] public float dingVolume = 0.4f;
    [Range(0f, 1f)] public float whooshVolume = 0.65f;
    [Range(0f, 1f)] public float medalVolume = 0.5f;
    [Range(0f, 1f)] public float victoryVolume = 0.55f;
    [Min(0.02f)] public float tickInterval = 0.08f;
    [Header("Medals (Gold, Silver, Bronze)")]
    public Sprite[] medals = new Sprite[3];
    public Vector2 medalSize = new Vector2(34f, 36f);
    [Header("Points")]
    [Min(0f)] public float pointsDelay = 1.5f;
    [Min(0.1f)] public float pointsDuration = 1.4f;
    public float pointsScale = 1.7f;
    public float pointsRise = 5f;
    [Header("Final Podium")]
    [Min(0f)] public float medalDelayAfterPoints = 1f;
    [Min(0f)] public float rankStagger = 0.4f;
    [Min(0.1f)] public float medalDuration = 0.65f;
    public Ease medalEase = Ease.OutBack;
    [Min(0.1f)] public float gradientDuration = 0.65f;
    public Color goldLight = new Color(1f, 0.88f, 0.28f);
    public Color goldDark = new Color(0.66f, 0.46f, 0.02f);
    public Color silverLight = new Color(0.95f, 0.98f, 1f);
    public Color silverDark = new Color(0.48f, 0.56f, 0.66f);
    public Color bronzeLight = new Color(1f, 0.69f, 0.42f);
    public Color bronzeDark = new Color(0.52f, 0.25f, 0.10f);
    [Header("Repeating Username Shine")]
    [Min(0.1f)] public float shineDuration = 1.1f;
    [Min(0f)] public float shinePause = 1.8f;
    [Range(0.01f, 0.5f)] public float shineWidth = 0.12f;
    [Range(0f, 1f)] public float shineStrength = 0.65f;
}
