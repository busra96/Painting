using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Atelier
{
    // Fill level of every paint tube the player owns. Shared by the atelier now and by the market later.
    // Change notifications are coalesced: tools spend paint many times per frame, the UI hears about it once.
    public sealed class TubeInventory
    {
        [Serializable]
        private sealed class SaveData
        {
            public List<string> ids = new List<string>();
            public List<float> levels = new List<float>();
        }

        private readonly string saveKey;
        private readonly List<PaintColorDefinition> colors;
        private readonly float[] levels;
        private readonly bool[] changed;
        private bool anyChanged, needsSave;

        public event Action<PaintColorDefinition> Changed;
        public event Action<PaintColorDefinition> RanOut;
        public IReadOnlyList<PaintColorDefinition> Colors => colors;

        public TubeInventory(IReadOnlyList<PaintColorDefinition> definitions, string persistenceKey = "Atelier.Tubes")
        {
            saveKey = persistenceKey;
            colors = new List<PaintColorDefinition>(definitions.Count);
            foreach (PaintColorDefinition definition in definitions)
                if (definition != null) colors.Add(definition);
            levels = new float[colors.Count];
            changed = new bool[colors.Count];
            for (int i = 0; i < colors.Count; i++) levels[i] = colors[i].tubeCapacity * colors[i].startingFill;
            Load();
        }

        public float Level(PaintColorDefinition color) { int i = IndexOf(color); return i < 0 ? 0 : levels[i]; }
        public float Fraction(PaintColorDefinition color)
        {
            int i = IndexOf(color);
            return i < 0 ? 0 : Mathf.Clamp01(levels[i] / Mathf.Max(0.0001f, colors[i].tubeCapacity));
        }
        public bool IsEmpty(PaintColorDefinition color) => Level(color) <= 0.0001f;

        // Returns how much was actually available.
        public float Spend(PaintColorDefinition color, float units)
        {
            int i = IndexOf(color);
            if (i < 0 || units <= 0 || float.IsNaN(units) || float.IsInfinity(units)) return 0;
            float granted = Mathf.Min(units, levels[i]);
            if (granted <= 0) return 0;
            levels[i] -= granted;
            if (levels[i] < 0.0001f)
            {
                levels[i] = 0;
                RanOut?.Invoke(color);
            }
            MarkChanged(i);
            return granted;
        }

        public void Refill(PaintColorDefinition color, float units = float.PositiveInfinity)
        {
            int i = IndexOf(color);
            if (i < 0 || float.IsNaN(units) || units <= 0) return;
            levels[i] = Mathf.Min(colors[i].tubeCapacity, levels[i] + units);
            MarkChanged(i);
        }

        public float[] Snapshot(float[] into = null)
        {
            if (into == null || into.Length != levels.Length) into = new float[levels.Length];
            Array.Copy(levels, into, levels.Length);
            return into;
        }

        public void Restore(float[] snapshot)
        {
            if (snapshot == null || snapshot.Length != levels.Length) return;
            for (int i = 0; i < levels.Length; i++)
                if (!float.IsNaN(snapshot[i]) && !float.IsInfinity(snapshot[i]) && !Mathf.Approximately(levels[i], snapshot[i]))
                { levels[i] = Mathf.Clamp(snapshot[i], 0, colors[i].tubeCapacity); MarkChanged(i); }
        }

        // Call once per frame (or after a batch of spends) to notify listeners.
        public void FlushNotifications()
        {
            if (!anyChanged) return;
            anyChanged = false;
            for (int i = 0; i < changed.Length; i++)
            {
                if (!changed[i]) continue;
                changed[i] = false;
                Changed?.Invoke(colors[i]);
            }
        }

        public void SaveIfNeeded()
        {
            if (!needsSave || string.IsNullOrEmpty(saveKey)) return;
            needsSave = false;
            var data = new SaveData();
            for (int i = 0; i < colors.Count; i++) { data.ids.Add(colors[i].id); data.levels.Add(levels[i]); }
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (string.IsNullOrEmpty(saveKey)) return;
            string json = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.ids == null || data.levels == null) return;
                for (int n = 0; n < data.ids.Count && n < data.levels.Count; n++)
                {
                    int i = colors.FindIndex(c => c.id == data.ids[n]);
                    if (i >= 0) levels[i] = Mathf.Clamp(data.levels[n], 0, colors[i].tubeCapacity);
                }
            }
            catch (Exception exception) { Debug.LogWarning("Tube save could not be read: " + exception.Message); }
        }

        private void MarkChanged(int i) { changed[i] = true; anyChanged = true; needsSave = true; }

        private int IndexOf(PaintColorDefinition color)
        {
            for (int i = 0; i < colors.Count; i++) if (colors[i] == color) return i;
            return -1;
        }
    }
}
