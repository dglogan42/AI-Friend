using System;
using UnityEngine;

namespace VRCompanion.Audio
{
    /// <summary>
    /// Voice/sound level helpers — RMS, peak, simple dBFS.
    /// </summary>
    public static class AudioMeter
    {
        public static float Rms(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;
            double sum = 0;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * (double)samples[i];
            return (float)Math.Sqrt(sum / samples.Length);
        }

        public static float Peak(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float a = Math.Abs(samples[i]);
                if (a > peak)
                    peak = a;
            }
            return peak;
        }

        /// <summary>dBFS relative to full-scale 1.0 (silence → large negative).</summary>
        public static float DbFs(float linear)
        {
            if (linear <= 1e-8f)
                return -80f;
            return 20f * Mathf.Log10(linear);
        }

        public static bool IsVoiced(float[] samples, float rmsThreshold = 0.01f)
            => Rms(samples) >= rmsThreshold;
    }
}
