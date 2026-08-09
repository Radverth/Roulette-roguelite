using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Procedural placeholder audio so the slice has tactile feedback with zero
    /// binary assets. Replace clips with authored audio later; call sites stay.
    /// </summary>
    public static class Sfx
    {
        private const int SampleRate = 44100;

        private static AudioSource _source;
        private static AudioClip _tick, _land, _coin, _damage, _boss, _levelUp;

        public static void Init()
        {
            if (_source != null) return;

            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _tick = Tone("tick", 1400f, 0.03f);
            _land = Tone("land", 220f, 0.30f);
            _coin = Tone("coin", 950f, 0.12f);
            _damage = Tone("damage", 130f, 0.35f);
            _boss = Tone("boss", 75f, 0.8f);
            _levelUp = Tone("levelup", 620f, 0.45f);
        }

        private static AudioClip Tone(string name, float freq, float duration)
        {
            int count = Mathf.Max(1, (int)(SampleRate * duration));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-6f * t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void Play(AudioClip clip, float volume)
        {
            if (_source != null && clip != null)
                _source.PlayOneShot(clip, volume);
        }

        public static void Tick() => Play(_tick, 0.25f);
        public static void Land() => Play(_land, 0.8f);
        public static void Reward() => Play(_coin, 0.6f);
        public static void Damage() => Play(_damage, 0.9f);
        public static void Boss() => Play(_boss, 1f);
        public static void LevelUp() => Play(_levelUp, 0.8f);
    }
}
