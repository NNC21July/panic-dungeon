using UnityEngine;

public static class CharacterColors
{
    private static readonly Color32[] colors =
    {
        new(255, 255, 255, 255), new(236, 255, 0, 255), new(136, 255, 0, 255),
        new(0, 255, 171, 255), new(0, 171, 255, 255), new(175, 183, 255, 255),
        new(242, 129, 255, 255), new(255, 151, 40, 255), new(140, 140, 140, 255),
        new(255, 92, 92, 255)
    };
    public static int Count => colors.Length;
    public static Color Get(int index) => colors[index];
}
