#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorAutoSaveAndScriptRefresh
{
    private const string AutoSaveMenuPath = "Tools/Auto Save/Save On Focus Change";
    private const string ScriptRefreshMenuPath = "Tools/Auto Save/Refresh Scripts On Save";

    private const string AutoSaveEnabledKey = "EditorAutoSaveAndScriptRefresh.AutoSaveEnabled";
    private const string ScriptRefreshEnabledKey = "EditorAutoSaveAndScriptRefresh.ScriptRefreshEnabled";

    private const double MinSecondsBetweenSaves = 1.0;
    private const double RefreshDelaySeconds = 0.5;

    private static double lastSaveTime;

    private static FileSystemWatcher scriptWatcher;
    private static bool refreshQueued;
    private static DateTime lastScriptChangeTime;

    static EditorAutoSaveAndScriptRefresh()
    {
        // Focus save
        EditorApplication.focusChanged -= OnFocusChanged;
        EditorApplication.focusChanged += OnFocusChanged;

        // Script refresh checker
        EditorApplication.update -= Update;
        EditorApplication.update += Update;

        StartWatchingScripts();
    }

    // =========================================================
    // AUTO SAVE WHEN UNITY LOSES FOCUS
    // =========================================================

    private static void OnFocusChanged(bool hasFocus)
    {
        // Only save when Unity loses focus
        if (hasFocus) return;

        if (!IsAutoSaveEnabled()) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (EditorApplication.timeSinceStartup - lastSaveTime < MinSecondsBetweenSaves) return;

        lastSaveTime = EditorApplication.timeSinceStartup;

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("Auto-saved because Unity lost focus.");
    }

    // =========================================================
    // AUTO REFRESH / COMPILE WHEN SCRIPT IS SAVED OUTSIDE UNITY
    // =========================================================

    private static void StartWatchingScripts()
    {
        if (scriptWatcher != null)
        {
            scriptWatcher.EnableRaisingEvents = false;
            scriptWatcher.Dispose();
            scriptWatcher = null;
        }

        string assetsPath = Application.dataPath;

        scriptWatcher = new FileSystemWatcher(assetsPath)
        {
            IncludeSubdirectories = true,
            Filter = "*.cs",
            NotifyFilter = NotifyFilters.LastWrite |
                           NotifyFilters.FileName |
                           NotifyFilters.CreationTime
        };

        scriptWatcher.Changed += OnScriptChanged;
        scriptWatcher.Created += OnScriptChanged;
        scriptWatcher.Deleted += OnScriptChanged;
        scriptWatcher.Renamed += OnScriptRenamed;

        scriptWatcher.EnableRaisingEvents = true;
    }

    private static void OnScriptChanged(object sender, FileSystemEventArgs e)
    {
        QueueScriptRefresh();
    }

    private static void OnScriptRenamed(object sender, RenamedEventArgs e)
    {
        QueueScriptRefresh();
    }

    private static void QueueScriptRefresh()
    {
        refreshQueued = true;
        lastScriptChangeTime = DateTime.UtcNow;
    }

    private static void Update()
    {
        if (!refreshQueued) return;
        if (!IsScriptRefreshEnabled()) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        double secondsSinceLastChange = (DateTime.UtcNow - lastScriptChangeTime).TotalSeconds;

        // Wait a tiny bit so VS Code fully finishes writing the file
        if (secondsSinceLastChange < RefreshDelaySeconds) return;

        refreshQueued = false;

        Debug.Log("Script file changed. Refreshing Unity AssetDatabase...");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    // =========================================================
    // MENU TOGGLES
    // =========================================================

    [MenuItem(AutoSaveMenuPath)]
    private static void ToggleAutoSave()
    {
        bool newValue = !IsAutoSaveEnabled();
        EditorPrefs.SetBool(AutoSaveEnabledKey, newValue);

        Debug.Log("Save On Focus Change: " + (newValue ? "ON" : "OFF"));
    }

    [MenuItem(AutoSaveMenuPath, true)]
    private static bool ToggleAutoSaveValidate()
    {
        Menu.SetChecked(AutoSaveMenuPath, IsAutoSaveEnabled());
        return true;
    }

    [MenuItem(ScriptRefreshMenuPath)]
    private static void ToggleScriptRefresh()
    {
        bool newValue = !IsScriptRefreshEnabled();
        EditorPrefs.SetBool(ScriptRefreshEnabledKey, newValue);

        Debug.Log("Refresh Scripts On Save: " + (newValue ? "ON" : "OFF"));
    }

    [MenuItem(ScriptRefreshMenuPath, true)]
    private static bool ToggleScriptRefreshValidate()
    {
        Menu.SetChecked(ScriptRefreshMenuPath, IsScriptRefreshEnabled());
        return true;
    }

    private static bool IsAutoSaveEnabled()
    {
        return EditorPrefs.GetBool(AutoSaveEnabledKey, true);
    }

    private static bool IsScriptRefreshEnabled()
    {
        return EditorPrefs.GetBool(ScriptRefreshEnabledKey, true);
    }
}
#endif