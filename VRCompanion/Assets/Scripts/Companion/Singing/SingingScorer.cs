using System.Collections.Generic;

namespace VRCompanion.Singing
{
    public readonly struct SingingResult
    {
        public readonly float Score; // 0..100
        public readonly int VoicedMatchedWindows;
        public readonly int TotalWindows;
        public readonly float AverageAbsCentsError;

        public SingingResult(float score, int voicedMatchedWindows, int totalWindows, float averageAbsCentsError)
        {
            Score = score;
            VoicedMatchedWindows = voicedMatchedWindows;
            TotalWindows = totalWindows;
            AverageAbsCentsError = averageAbsCentsError;
        }

        public static readonly SingingResult NoSignal = new SingingResult(0f, 0, 0, float.PositiveInfinity);
    }

    /// <summary>
    /// Scores a recorded take against a reference melody by running windowed pitch
    /// detection and comparing each window's detected pitch to the note expected at
    /// that point in time. Pure C# — no Unity dependency — for direct unit testing.
    /// </summary>
    public static class SingingScorer
    {
        public const float CentsErrorForZeroScore = 500f; // fully off-key beyond this -> score 0

        public static SingingResult Score(
            float[] samples,
            int sampleRate,
            IReadOnlyList<MelodyNote> melody,
            float windowSeconds = 0.1f)
        {
            if (samples == null || samples.Length == 0 || sampleRate <= 0 || melody == null || melody.Count == 0)
                return SingingResult.NoSignal;

            int windowSize = (int)(sampleRate * windowSeconds);
            if (windowSize <= 0)
                return SingingResult.NoSignal;

            int totalWindows = samples.Length / windowSize;
            int voicedMatched = 0;
            double sumAbsCents = 0;

            var window = new float[windowSize];

            for (int w = 0; w < totalWindows; w++)
            {
                int offset = w * windowSize;
                System.Array.Copy(samples, offset, window, 0, windowSize);

                float windowCenterTime = (offset + windowSize / 2f) / sampleRate;
                if (!ReferenceMelody.TryGetNoteAtTime(melody, windowCenterTime, out var expected))
                    continue;

                if (!PitchDetector.TryDetectPitch(window, sampleRate, out float detectedHz, out _))
                    continue;

                float centsError = PitchDetector.CentsError(detectedHz, expected.FrequencyHz);
                if (float.IsInfinity(centsError))
                    continue;

                sumAbsCents += System.Math.Abs(centsError);
                voicedMatched++;
            }

            if (voicedMatched == 0)
                return new SingingResult(0f, 0, totalWindows, float.PositiveInfinity);

            float avgAbsCents = (float)(sumAbsCents / voicedMatched);
            float score = 100f - (avgAbsCents / CentsErrorForZeroScore) * 100f;
            score = score < 0f ? 0f : (score > 100f ? 100f : score);

            return new SingingResult(score, voicedMatched, totalWindows, avgAbsCents);
        }
    }
}
