using System.Text;
using UnityEngine;

// Shared validation for the menu and server. Display names are not account identities.
public static class PlayerDisplayName
{
    public const int MaxLength = 16;
    private const string PreferenceKey = "PlayerDisplayName";
    public static string Saved => Clean(PlayerPrefs.GetString(PreferenceKey, ""));

    public static string Clean(string value)
    {
        StringBuilder result = new StringBuilder();
        foreach (char character in value ?? "")
        {
            if (result.Length >= MaxLength)
                break;
            if (char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-')
                result.Append(character);
        }
        return result.ToString().Trim();
    }

    public static void Save(string value)
    {
        PlayerPrefs.SetString(PreferenceKey, Clean(value));
        PlayerPrefs.Save();
    }
}
