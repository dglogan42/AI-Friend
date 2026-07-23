using System;

namespace VRCompanion.Singing
{
    /// <summary>
    /// Reduces a raw audio buffer to a fixed number of display points for oscilloscope-style
    /// rendering. Pure/stateless — no Unity dependency — so it's directly unit-testable.
    /// </summary>
    public static class WaveformVisualizer
    {
        /// <summary>
        /// Downsamples <paramref name="samples"/> to exactly <paramref name="pointCount"/> values,
        /// picking the peak (furthest from zero) sample in each bucket so the oscillation's
        /// amplitude survives the reduction instead of being averaged away.
        /// </summary>
        public static float[] Downsample(float[] samples, int pointCount)
        {
            var points = new float[Math.Max(0, pointCount)];
            if (samples == null || samples.Length == 0 || pointCount <= 0)
                return points;

            float samplesPerPoint = samples.Length / (float)pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                int start = (int)(i * samplesPerPoint);
                int end = (int)((i + 1) * samplesPerPoint);
                if (end <= start)
                    end = start + 1;
                end = Math.Min(end, samples.Length);
                start = Math.Min(start, samples.Length - 1);

                float peak = samples[start];
                for (int s = start; s < end; s++)
                {
                    if (Math.Abs(samples[s]) > Math.Abs(peak))
                        peak = samples[s];
                }
                points[i] = peak;
            }

            return points;
        }
    }
}
