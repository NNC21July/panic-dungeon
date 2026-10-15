using Fusion;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NetworkSessionBootstrap))]
public class NetworkSessionBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying)
            return;
        NetworkSessionBootstrap bootstrap = (NetworkSessionBootstrap)target;
        using (new EditorGUI.DisabledScope(bootstrap.IsReturning))
        {
            if (bootstrap.CurrentStage == FusionBootstrap.Stage.Disconnected)
            {
                if (GUILayout.Button("Start Server Plus Clients"))
                    bootstrap.StartServerPlusClients();
                if (GUILayout.Button("Start Client"))
                    bootstrap.StartClient();
            }
            else if (bootstrap.CurrentStage == FusionBootstrap.Stage.AllConnected)
            {
                if (GUILayout.Button("Add Client"))
                    bootstrap.AddClient();
            }
            if (GUILayout.Button("Close Local Session"))
                bootstrap.LeaveSession();
        }
    }
}
