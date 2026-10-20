using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Gallery
{
// The list of target paintings shown in the gallery's "YENİ TABLO" tab.
// Artwork textures must have Read/Write enabled: ImageGridProcessor samples them for the match score.
[CreateAssetMenu(menuName = "Painting/Painting Catalog", fileName = "PaintingCatalog")]
public sealed class PaintingCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("Stable key stored with saved paintings and in PlayerPrefs. Do not rename after release.")]
        public string id;
        public string title;
        public Sprite artwork;
    }

    public List<Entry> paintings = new List<Entry>();
    [Tooltip("Adds a target-free blank canvas option. Free paintings are not scored.")]
    public bool allowFreePainting = true;
    public string freePaintingTitle = "SERBEST TUVAL";

    public Entry Default
    {
        get
        {
            foreach (Entry entry in paintings)
                if (entry != null && entry.artwork != null) return entry;
            return null;
        }
    }

    public Entry Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (Entry entry in paintings)
            if (entry != null && entry.artwork != null && entry.id == id) return entry;
        return null;
    }

    public string TitleOf(string id)
    {
        if (id == PaintingSession.FreePaintingId) return freePaintingTitle;
        Entry entry = Find(id);
        return entry != null ? entry.title : "";
    }

    public bool TryValidate(out string error)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Entry entry in paintings)
        {
            if (entry == null || entry.artwork == null) continue;
            if (string.IsNullOrWhiteSpace(entry.id))
            {
                error = "Every painting with artwork needs a stable id.";
                return false;
            }
            if (!ids.Add(entry.id))
            {
                error = "Duplicate painting id: " + entry.id;
                return false;
            }
        }
        error = null;
        return true;
    }
}
}
