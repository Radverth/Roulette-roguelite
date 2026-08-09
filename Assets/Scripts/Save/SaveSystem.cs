using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SinWheel
{
    [Serializable]
    public class UpgradeTierEntry
    {
        public string id;
        public int tier;
    }

    /// <summary>Everything that persists across runs. Serialized with JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public int schemaVersion = 1;
        public int metaCoins;
        public int gems;
        public int level = 1;
        public int xp;
        public int totalBanked;
        public int runsCompleted;
        public int bestSingleBank;
        public List<UpgradeTierEntry> upgradeTiers = new List<UpgradeTierEntry>();
        public long lastSaveUnix;

        public int GetUpgradeTier(string id)
        {
            foreach (var e in upgradeTiers)
                if (e.id == id) return e.tier;
            return 0;
        }

        public void SetUpgradeTier(string id, int tier)
        {
            foreach (var e in upgradeTiers)
            {
                if (e.id == id)
                {
                    e.tier = tier;
                    return;
                }
            }
            upgradeTiers.Add(new UpgradeTierEntry { id = id, tier = tier });
        }
    }

    /// <summary>
    /// Cloud sync seam. The Google Play Games Services implementation plugs in
    /// here without touching gameplay code; conflict policy is last-write-wins
    /// on lastSaveUnix.
    /// </summary>
    public interface ICloudSaveProvider
    {
        bool IsAvailable { get; }
        void Push(string saveJson);
        /// <returns>Cloud snapshot JSON, or null if none/unavailable.</returns>
        string Pull();
    }

    public sealed class NullCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false;
        public void Push(string saveJson) { }
        public string Pull() => null;
    }

    /// <summary>
    /// Stub for Google Play Games Services saved games. Wire up the GPGS Unity
    /// plugin (com.google.play.games) here: authenticate in Init, then map
    /// Push/Pull to OpenWithAutomaticConflictResolution + CommitUpdate/ReadBinaryData.
    /// </summary>
    public sealed class GooglePlayCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false; // flips true once GPGS auth succeeds
        public void Push(string saveJson) { /* TODO: GPGS SavedGame CommitUpdate */ }
        public string Pull() => null;     /* TODO: GPGS SavedGame ReadBinaryData */
    }

    /// <summary>
    /// Local JSON save with a cloud sync seam. Persist() is cheap and called on
    /// every meaningful commit point (bank, run end, purchase, level up, pause).
    /// </summary>
    public sealed class SaveSystem
    {
        private readonly ICloudSaveProvider _cloud;

        public SaveData Data { get; private set; } = new SaveData();

        private static string FilePath => Path.Combine(Application.persistentDataPath, "sinwheel_save.json");

        public SaveSystem(ICloudSaveProvider cloud)
        {
            _cloud = cloud ?? new NullCloudSaveProvider();
        }

        public void Load()
        {
            SaveData local = null;
            try
            {
                if (File.Exists(FilePath))
                    local = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to read local save: {e.Message}");
            }

            SaveData cloud = null;
            try
            {
                string cloudJson = _cloud.IsAvailable ? _cloud.Pull() : null;
                if (!string.IsNullOrEmpty(cloudJson))
                    cloud = JsonUtility.FromJson<SaveData>(cloudJson);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Cloud pull failed: {e.Message}");
            }

            // Last-write-wins between local and cloud.
            if (local != null && cloud != null)
                Data = cloud.lastSaveUnix > local.lastSaveUnix ? cloud : local;
            else
                Data = cloud ?? local ?? new SaveData();
        }

        public void Persist()
        {
            Data.lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(Data, true);

            try
            {
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to write local save: {e.Message}");
            }

            try
            {
                if (_cloud.IsAvailable) _cloud.Push(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Cloud push failed: {e.Message}");
            }
        }
    }
}
