using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Sink seam for real analytics backends (Firebase, Unity Analytics, ...).
    /// Balancing depends on spin frequency, session length and boss-encounter
    /// drop-off, so those events are first-class here.
    /// </summary>
    public interface IAnalyticsSink
    {
        void Track(string eventName, IReadOnlyDictionary<string, object> properties);
    }

    public sealed class DebugLogAnalyticsSink : IAnalyticsSink
    {
        public void Track(string eventName, IReadOnlyDictionary<string, object> properties)
        {
            var sb = new StringBuilder("[Analytics] ").Append(eventName);
            foreach (var kv in properties)
                sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
            Debug.Log(sb.ToString());
        }
    }

    public sealed class AnalyticsSystem
    {
        private readonly List<IAnalyticsSink> _sinks = new List<IAnalyticsSink>();
        private readonly string _sessionId = Guid.NewGuid().ToString("N");
        private float _sessionStartTime;
        private bool _sessionOpen;

        public int SpinsThisSession { get; private set; }

        public AnalyticsSystem(params IAnalyticsSink[] sinks)
        {
            _sinks.AddRange(sinks);
        }

        public void AddSink(IAnalyticsSink sink) => _sinks.Add(sink);

        /// <summary>Track with alternating key/value pairs: Track("spin", "index", 3, "boss", "sloth").</summary>
        public void Track(string eventName, params object[] kvPairs)
        {
            var props = new Dictionary<string, object>
            {
                ["session_id"] = _sessionId,
                ["t"] = Mathf.RoundToInt(Time.realtimeSinceStartup)
            };
            for (int i = 0; i + 1 < kvPairs.Length; i += 2)
                props[kvPairs[i].ToString()] = kvPairs[i + 1];

            foreach (var sink in _sinks)
            {
                try { sink.Track(eventName, props); }
                catch (Exception e) { Debug.LogWarning($"[Analytics] Sink failed: {e.Message}"); }
            }
        }

        public void TrackSpin(int segmentIndex, string segmentType, bool bossActive)
        {
            SpinsThisSession++;
            Track("spin", "index", segmentIndex, "segment", segmentType, "boss_active", bossActive);
        }

        public void TrackSessionStart()
        {
            _sessionStartTime = Time.realtimeSinceStartup;
            _sessionOpen = true;
            Track("session_start");
        }

        public void TrackSessionEnd()
        {
            if (!_sessionOpen) return;
            _sessionOpen = false;
            float length = Time.realtimeSinceStartup - _sessionStartTime;
            Track("session_end", "length_sec", Mathf.RoundToInt(length), "spins", SpinsThisSession);
        }
    }
}
