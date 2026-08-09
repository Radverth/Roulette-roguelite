using UnityEngine;

namespace SinWheel
{
    /// <summary>Looping background score with a persisted volume.</summary>
    public static class Music
    {
        public const float DefaultVolume = 0.35f;

        private static AudioSource _source;

        public static void Init(float volume)
        {
            if (_source != null) return;

            var clip = Resources.Load<AudioClip>("Audio/cathedral_rift");
            if (clip == null)
            {
                Debug.LogWarning("[Music] Missing Resources/Audio/cathedral_rift");
                return;
            }

            var go = new GameObject("Music");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.clip = clip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = Mathf.Clamp01(volume);
            _source.Play();
        }

        public static void SetVolume(float volume)
        {
            if (_source != null)
                _source.volume = Mathf.Clamp01(volume);
        }
    }
}
