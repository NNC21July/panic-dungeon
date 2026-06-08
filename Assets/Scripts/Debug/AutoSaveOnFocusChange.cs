#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoSaveOnFocusChange
{
    private const string MenuPath = "Tools/Auto Save/Save On Focus Change";
    private const string EnabledKey = "AutoSaveOnFocusChange.Enabled";

    private const double MinSecondsBetweenSaves = 1.0;
    private static double lastSaveTime;

    static AutoSaveOnFocusChange()
    {
        EditorApplication.focusChanged -= OnFocusChanged;
        EditorApplication.focusChanged += OnFocusChanged;
    }

    private static void OnFocusChanged(bool hasFocus)
    {
        // Only save when Unity loses focus, not when it gains focus
        if (hasFocus) return;

        if (!IsEnabled()) return;

        // Avoid saving during play mode, compiling, or asset importing
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        // Prevent spam-saving
        if (EditorApplication.timeSinceStartup - lastSaveTime < MinSecondsBetweenSaves) return;

        lastSaveTime = EditorApplication.timeSinceStartup;

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("Auto-saved because Unity lost focus.");
    }

    [MenuItem(MenuPath)]
    private static void ToggleAutoSave()
    {
        bool newValue = !IsEnabled();
        EditorPrefs.SetBool(EnabledKey, newValue);

        Debug.Log("Save On Focus Change: " + (newValue ? "ON" : "OFF"));
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleAutoSaveValidate()
    {
        Menu.SetChecked(MenuPath, IsEnabled());
        return true;
    }

    private static bool IsEnabled()
    {
        return EditorPrefs.GetBool(EnabledKey, true);
    }
}
#endif