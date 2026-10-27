using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResultsPreviewMenu
{
    [MenuItem("Tools/Panic Dungeon/Open Scoreboard Preview")]
    private static void OpenPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Scoreboard Preview", "Exit Play Mode before opening the preview scene.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject preview = new GameObject("Scoreboard Preview", typeof(ResultsPreview));
        Selection.activeGameObject = preview;
        Debug.Log("Scoreboard preview ready. Press Play, adjust Assets/Resources/ResultsPresentationSettings, then click Replay Animation. Enable Preview Tied Ranks and click Rebuild Preview to test 1, 2, 2, 4 ranking.", preview);
    }
}
