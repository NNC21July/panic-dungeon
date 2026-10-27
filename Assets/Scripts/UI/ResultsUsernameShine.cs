using TMPro;
using UnityEngine;

// Colors the actual text vertices, so the light never paints the empty space around a name.
[RequireComponent(typeof(TMP_Text))]
public class ResultsUsernameShine : MonoBehaviour
{
    private TMP_Text label;
    private ResultsPresentationSettings settings;
    private Color lightColor, darkColor;
    private float elapsed, blend;

    public void Initialize(ResultsPresentationSettings settings, int rank)
    {
        this.settings = settings;
        lightColor = rank == 1 ? settings.goldLight : rank == 2 ? settings.silverLight : settings.bronzeLight;
        darkColor = rank == 1 ? settings.goldDark : rank == 2 ? settings.silverDark : settings.bronzeDark;
    }

    public void SetPresentation(float elapsed, float blend)
    {
        this.elapsed = elapsed;
        this.blend = blend;
        RefreshText();
    }

    private void Awake() => label = GetComponent<TMP_Text>();
    private void OnEnable() => GetComponent<TMP_Text>().OnPreRenderText += PaintText;
    private void OnDisable()
    {
        if (label == null)
            return;
        label.OnPreRenderText -= PaintText;
        RefreshText();
    }

    private void RefreshText()
    {
        if (label == null)
            return;
        // Scheduling a canvas redraw alone does not regenerate TMP's mesh or invoke OnPreRenderText.
        label.havePropertiesChanged = true;
        label.SetVerticesDirty();
    }

    private void PaintText(TMP_TextInfo info)
    {
        if (settings == null || blend <= 0f)
            return;
        float left = float.MaxValue, right = float.MinValue;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;
            left = Mathf.Min(left, character.bottomLeft.x);
            right = Mathf.Max(right, character.topRight.x);
        }
        float width = Mathf.Max(1f, right - left);
        float cycle = Mathf.Max(0.1f, settings.shineDuration + settings.shinePause);
        float phase = Mathf.Max(0f, elapsed) % cycle / Mathf.Max(0.1f, settings.shineDuration);
        float sweep = Mathf.Lerp(-0.3f, 1.3f, Mathf.Clamp01(phase));
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;
            TMP_MeshInfo mesh = info.meshInfo[character.materialReferenceIndex];
            for (int j = 0; j < 4; j++)
            {
                int index = character.vertexIndex + j;
                Vector3 vertex = mesh.vertices[index];
                float x = (vertex.x - left) / width;
                Color color = Color.Lerp(lightColor, darkColor, x);
                float diagonal = x + (vertex.y - character.baseLine) / width * 0.4f;
                float shine = phase <= 1f ? Mathf.Clamp01(1f - Mathf.Abs(diagonal - sweep) / settings.shineWidth) : 0f;
                color = Color.Lerp(color, Color.white, shine * settings.shineStrength);
                Color original = mesh.colors32[index];
                color = Color.Lerp(original, color, blend);
                color.a = original.a;
                mesh.colors32[index] = color;
            }
        }
    }
}
