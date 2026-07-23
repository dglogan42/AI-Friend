using System;
using UnityEngine;

namespace VRCompanion.Diagnostics
{
    /// <summary>
    /// Rolling latency statistics for tracking / voice / vision streams.
    /// Pure enough for unit tests when fed synthetic samples.
    /// </summary>
    public sealed class LatencyMeter
    {
        readonly float[] _samples;
        int _count;
        int _next;
        float _sum;

        public string Label { get; }
        public int Count => _count;
        public float LastMs { get; private set; }
        public float AverageMs => _count == 0 ? 0f : _sum / _count;
        public float MaxMs { get; private set; }

        public LatencyMeter(string label, int capacity = 128)
        {
            Label = label ?? "latency";
            _samples = new float[Mathf.Max(8, capacity)];
        }

        public void RecordMs(float ms)
        {
            if (ms < 0f || float.IsNaN(ms) || float.IsInfinity(ms))
                return;

            LastMs = ms;
            if (ms > MaxMs)
                MaxMs = ms;

            if (_count < _samples.Length)
            {
                _samples[_next] = ms;
                _sum += ms;
                _count++;
                _next = (_next + 1) % _samples.Length;
                return;
            }

            _sum -= _samples[_next];
            _samples[_next] = ms;
            _sum += ms;
            _next = (_next + 1) % _samples.Length;
        }

        /// <summary>
        /// Inter-arrival gap between UDP packets (proxy for pipeline cadence).
        /// </summary>
        public void RecordInterval(float previousUnscaledTime, float nowUnscaledTime)
        {
            if (previousUnscaledTime < 0f)
                return;
            RecordMs((nowUnscaledTime - previousUnscaledTime) * 1000f);
        }

        public float PercentileMs(float p)
        {
            if (_count == 0)
                return 0f;
            p = Mathf.Clamp01(p);
            var tmp = new float[_count];
            // Samples may wrap; copy active window in insertion order is not required for percentile.
            if (_count < _samples.Length)
                Array.Copy(_samples, tmp, _count);
            else
            {
                // Ring full: next points at oldest.
                int oldest = _next;
                for (int i = 0; i < _count; i++)
                    tmp[i] = _samples[(oldest + i) % _samples.Length];
            }

            Array.Sort(tmp);
            int idx = Mathf.Clamp(Mathf.RoundToInt(p * (_count - 1)), 0, _count - 1);
            return tmp[idx];
        }

        public override string ToString()
            => $"[{Label}] n={_count} last={LastMs:0.0}ms avg={AverageMs:0.0}ms p95={PercentileMs(0.95f):0.0}ms max={MaxMs:0.0}ms";
    }
}
