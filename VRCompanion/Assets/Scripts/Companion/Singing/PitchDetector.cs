using System;

namespace VRCompanion.Singing
{
    /// <summary>
    /// Time-domain autocorrelation pitch detector. Pure/stateless — no Unity
    /// dependency — so it's directly unit-testable against synthetic sine waves.
    /// </summary>
    public static class PitchDetector
    {
        public const float DefaultMinHz = 80f;
        public const float DefaultMaxHz = 1000f;
        public const float DefaultConfidenceThreshold = 0.5f;
        public const float DefaultSilenceRms = 0.01f;

        public static bool TryDetectPitch(
            float[] samples,
            int sampleRate,
            out float frequencyHz,
            out float confidence,
            float minHz = DefaultMinHz,
            float maxHz = DefaultMaxHz,
            float silenceRms = DefaultSilenceRms)
        {
            frequencyHz = 0f;
            confidence = 0f;

            if (samples == null || samples.Length < 2 || sampleRate <= 0)
                return false;

            double energy = 0;
            for (int i = 0; i < samples.Length; i++)
                energy += samples[i] * (double)samples[i];
            double rms = Math.Sqrt(energy / samples.Length);
            if (rms < silenceRms)
                return false; // treated as silence/unvoiced

            int minLag = Math.Max(1, (int)(sampleRate / maxHz));
            int maxLag = Math.Min(samples.Length - 1, (int)(sampleRate / minHz));
            if (maxLag <= minLag)
                return false;

            double energy0 = 0;
            for (int i = 0; i < samples.Length; i++)
                energy0 += samples[i] * (double)samples[i];

            int bestLag = -1;
            double bestCorrelation = 0;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                double cross = 0;
                double energyLag = 0;
                int n = samples.Length - lag;
                for (int i = 0; i < n; i++)
                {
                    cross += samples[i] * (double)samples[i + lag];
                    energyLag += samples[i + lag] * (double)samples[i + lag];
                }

                double denom = Math.Sqrt(energy0 * energyLag);
                if (denom < 1e-9)
                    continue;

                double correlation = cross / denom;
                if (correlation > bestCorrelation)
                {
                    bestCorrelation = correlation;
                    bestLag = lag;
                }
            }

            if (bestLag < 0 || bestCorrelation < DefaultConfidenceThreshold)
                return false;

            frequencyHz = sampleRate / (float)bestLag;
            confidence = (float)bestCorrelation;
            return true;
        }

        public static float CentsError(float detectedHz, float expectedHz)
        {
            if (detectedHz <= 0f || expectedHz <= 0f)
                return float.PositiveInfinity;
            return 1200f * (float)Math.Log(detectedHz / expectedHz, 2);
        }
    }
}
