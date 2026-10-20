using System;
using System.Linq;
using PaintingGame.Gallery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AssetLayoutChecks
{
    private static int checks;

    public static void RunBatch()
    {
        try
        {
            checks = 0;
            string[] rootFolders = AssetDatabase.GetSubFolders("Assets")
                .Select(path => path.Substring("Assets/".Length)).OrderBy(name => name).ToArray();
            string[] expected = { "Archive", "Painting", "Plugins", "ThirdParty", "_Recovery" };
            Check(rootFolders.All(expected.Contains), "Assets root only contains documented ownership groups");
            Check(AssetDatabase.IsValidFolder("Assets/Painting"), "Production game folder exists");
            Check(AssetDatabase.IsValidFolder("Assets/Archive/Prototypes"), "Legacy prototypes are isolated under Archive");
            Check(AssetDatabase.IsValidFolder("Assets/ThirdParty/GUIProKit"), "GUI asset package is isolated under ThirdParty");
            Check(AssetDatabase.IsValidFolder("Assets/ThirdParty/TextMeshPro"), "Imported TMP essentials are isolated under ThirdParty");
            Check(AssetDatabase.IsValidFolder("Assets/Plugins/CW/PaintIn3D"), "Paint in 3D remains in Unity's Plugins folder");
            Check(!AssetDatabase.IsValidFolder("Assets/Prototypes") &&
                  !AssetDatabase.IsValidFolder("Assets/GUI PRO Kit - Casual Game") &&
                  !AssetDatabase.IsValidFolder("Assets/TextMesh Pro"), "Old root folders are gone");

            int assetCount = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" })
                .Count(guid => !AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)));
            Check(assetCount >= 2300, "Organized asset inventory resolves (count=" + assetCount + ")");
            ValidateScene("Assets/Painting/Scenes/Painting.unity", "Main painting scene");
            ValidateScene("Assets/Painting/Scenes/AtelierShop.unity", "Atelier shop scene");

            PaintingCatalog catalog = AssetDatabase.LoadAssetAtPath<PaintingCatalog>(
                "Assets/Painting/Settings/PaintingCatalog.asset");
            Check(catalog == null || catalog.TryValidate(out _), "Gallery catalog ids are valid and unique");
            Debug.Log("ASSET LAYOUT CHECKS PASSED: " + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateScene(string path, string label)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int missing = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
        Check(scene.IsValid() && scene.isLoaded && missing == 0, label + " opens with no missing scripts");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSET LAYOUT CHECK FAILED: " + message);
        checks++;
        Debug.Log("PASS: " + message);
    }
}
