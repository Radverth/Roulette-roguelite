using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Loads balance JSON from Resources/Config. Fails loudly: a missing or
    /// malformed config file is a programmer/designer error we want to see
    /// immediately, not a silent default.
    /// </summary>
    public static class ConfigLoader
    {
        public static GameConfigRoot LoadAll()
        {
            return new GameConfigRoot
            {
                Tuning = Load<GameTuningConfig>("Config/tuning"),
                Wheel = Load<WheelConfig>("Config/wheel"),
                Sins = Load<SinsConfig>("Config/sins"),
                Upgrades = Load<UpgradesConfig>("Config/upgrades"),
                Tables = Load<TablesConfig>("Config/tables"),
                Marks = Load<MarksConfig>("Config/marks"),
                Interludes = Load<InterludesConfig>("Config/interludes")
            };
        }

        private static T Load<T>(string resourcePath) where T : class
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[Config] Missing config file: Resources/{resourcePath}.json");
                throw new System.IO.FileNotFoundException(resourcePath);
            }

            T parsed = JsonUtility.FromJson<T>(asset.text);
            if (parsed == null)
            {
                Debug.LogError($"[Config] Failed to parse config file: Resources/{resourcePath}.json");
                throw new System.FormatException(resourcePath);
            }

            return parsed;
        }
    }
}
