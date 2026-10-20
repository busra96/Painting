using System;
using System.IO;
using PaintingGame.Atelier;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local editor validation entry point when this project is already open in Unity.
// It accepts only the named checks below and never exits the user's editor or edits their save.
[InitializeOnLoad]
public static class GalleryEditorValidation
{
    private const string Request = "Logs/GalleryEditor.request";
    private static double nextPoll;
    [Serializable] private sealed class State
    {
        public bool playing, compiling, dirty;
        public string scene, result;
    }
    static GalleryEditorValidation() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var scene = SceneManager.GetActiveScene();
        string command = File.ReadAllText(Request).Trim();
        var state = new State { playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling,
            dirty = scene.isDirty, scene = scene.path, result = "Ready" };
        try
        {
            if (command == "configure" && state.playing)
            {
                var game = UnityEngine.Object.FindFirstObjectByType<GalleryGameController>();
                if (game != null && game.Ready) { game.tools.EndStroke(); game.SaveDraft(); }
                EditorApplication.isPlaying = false;
                return;
            }
            File.Delete(Request);
            if (command != "status")
            {
                if (state.playing || state.dirty) throw new InvalidOperationException("Validation needs a stopped editor with saved scenes.");
                if (command == "configure") GalleryArtDirectionAuthoring.ApplyContactCursorsInOpenScene();
                else if (command == "checks") GalleryGameChecks.RunBatch();
                else if (command == "tool-checks") AtelierToolChecks.RunBatch();
                else if (command == "reference-tools") GalleryReferenceToolsAuthoring.Apply();
                else if (command == "refresh") AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else throw new InvalidOperationException("Unknown validation request.");
                state.result = command + " started";
            }
        }
        catch (Exception e) { state.result = e.ToString(); Debug.LogException(e); }
        File.WriteAllText("Logs/GalleryEditorState.json", JsonUtility.ToJson(state, true));
    }
}
