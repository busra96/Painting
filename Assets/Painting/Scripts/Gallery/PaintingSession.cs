using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintingGame.Gallery
{
// Which painting the player chose. The choice survives the scene reload used by RETRY and by
// starting a new painting, and is remembered between app launches.
public static class PaintingSession
{
    public const string FreePaintingId = "__free__";
    private const string SelectionKey = "Painting.SelectedTarget";

    // Resolved by PaintingTargetApplier for the scene that is currently loaded.
    public static string ActiveId { get; private set; } = "";
    public static string ActiveTitle { get; private set; } = "";
    public static bool ActiveIsFreePainting { get; private set; }

    public static string SelectedId => PlayerPrefs.GetString(SelectionKey, "");

    // Clears stale state when domain reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveId = ActiveTitle = "";
        ActiveIsFreePainting = false;
    }

    public static void Select(string id)
    {
        PlayerPrefs.SetString(SelectionKey, id ?? "");
        PlayerPrefs.Save();
    }

    // Starts over with a clean surface, paint budget and history, exactly like RETRY.
    public static void StartNewPainting(string id)
    {
        Select(id);
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    internal static void SetActive(string id, string title, bool free)
    {
        ActiveId = id ?? "";
        ActiveTitle = title ?? "";
        ActiveIsFreePainting = free;
    }
}
}
