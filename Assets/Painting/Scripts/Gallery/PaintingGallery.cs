using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PaintingGame.Gallery
{
[Serializable]
public sealed class SavedPainting
{
    public string id;
    public string title;
    public string targetId;
    public bool hasScore;
    public float score;
    public long createdTicks;
    public int width, height;
    public DateTime Created => new DateTime(createdTicks, DateTimeKind.Local);
    public float Aspect => height > 0 ? width / (float)height : 1;
}

// Finished paintings live on the device: <persistentDataPath>/Gallery/<id>.png, <id>_thumb.png
// and an index.json with title, score and date. Nothing is written into the project.
public static class PaintingGallery
{
    [Serializable]
    private sealed class Index
    {
        public int version = 1;
        public List<SavedPainting> items = new List<SavedPainting>();
    }

    public const int ThumbnailWidth = 256;
    private static Index index;
    public static event Action Changed;

    public static string Folder => Path.Combine(Application.persistentDataPath, "Gallery");
    private static string IndexPath => Path.Combine(Folder, "index.json");
    private static string ImagePath(string id) => Path.Combine(Folder, id + ".png");
    private static string ThumbPath(string id) => Path.Combine(Folder, id + "_thumb.png");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { index = null; Changed = null; }

    // Newest first.
    public static IReadOnlyList<SavedPainting> Items
    {
        get { Load(); return index.items; }
    }

    public static SavedPainting Save(Texture2D image, string title, string targetId, bool hasScore, float score)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        Load();
        Directory.CreateDirectory(Folder);
        var item = new SavedPainting
        {
            id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            title = title ?? "", targetId = targetId ?? "",
            hasScore = hasScore, score = score, createdTicks = DateTime.Now.Ticks,
            width = image.width, height = image.height
        };
        try
        {
            WriteBytesAtomic(ImagePath(item.id), image.EncodeToPNG());
            int thumbHeight = Mathf.Max(1, Mathf.RoundToInt(ThumbnailWidth / item.Aspect));
            Texture2D thumb = PaintingCapture.Resize(image, ThumbnailWidth, thumbHeight);
            try { WriteBytesAtomic(ThumbPath(item.id), thumb.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(thumb); }
            index.items.Insert(0, item);
            WriteIndex();
        }
        catch
        {
            // A failed write must not leave an apparently saved card in memory or orphan images.
            index.items.Remove(item);
            TryDelete(ImagePath(item.id));
            TryDelete(ThumbPath(item.id));
            throw;
        }
        Changed?.Invoke();
        return item;
    }

    public static void Delete(SavedPainting item)
    {
        if (item == null) return;
        Load();
        index.items.RemoveAll(i => i.id == item.id);
        TryDelete(ImagePath(item.id));
        TryDelete(ThumbPath(item.id));
        WriteIndex();
        Changed?.Invoke();
    }

    // Caller owns the returned texture and must Destroy it.
    public static Texture2D LoadTexture(SavedPainting item, bool thumbnail)
    {
        if (item == null) return null;
        string path = thumbnail ? ThumbPath(item.id) : ImagePath(item.id);
        if (thumbnail && !File.Exists(path)) path = ImagePath(item.id);
        if (!File.Exists(path)) return null;
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = item.id, wrapMode = TextureWrapMode.Clamp };
        try
        {
            if (texture.LoadImage(File.ReadAllBytes(path), true)) return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Gallery image could not be read: " + exception.Message);
        }
        UnityEngine.Object.Destroy(texture);
        return null;
    }

    private static void Load()
    {
        if (index != null) return;
        index = new Index();
        try
        {
            if (File.Exists(IndexPath))
                index = JsonUtility.FromJson<Index>(File.ReadAllText(IndexPath)) ?? new Index();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Gallery index could not be read; starting with an empty list. " + exception.Message);
            index = new Index();
        }
        // Drop entries whose image was removed outside the game.
        if (index.items == null) index.items = new List<SavedPainting>();
        var unique = new HashSet<string>(StringComparer.Ordinal);
        index.items.RemoveAll(i => i == null || string.IsNullOrEmpty(i.id) || !unique.Add(i.id) || !File.Exists(ImagePath(i.id)));
        index.items.Sort((a, b) => b.createdTicks.CompareTo(a.createdTicks));
    }

    private static void WriteIndex()
    {
        Directory.CreateDirectory(Folder);
        WriteBytesAtomic(IndexPath, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(index, true)));
    }

    private static void WriteBytesAtomic(string path, byte[] bytes)
    {
        string temp = path + ".tmp";
        string backup = path + ".bak";
        File.WriteAllBytes(temp, bytes);
        try
        {
            TryDelete(backup);
            if (File.Exists(path)) File.Move(path, backup);
            File.Move(temp, path);
            TryDelete(backup);
        }
        catch
        {
            if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
            throw;
        }
        finally { TryDelete(temp); }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) { Debug.LogWarning("Could not delete " + path + ": " + exception.Message); }
    }
}
}
