using NUnit.Framework;
using VRCompanion.Singing;

namespace VRCompanion.Tests
{
    /// <summary>
    /// PitchDetector and SingingScorer are pure C# (no Unity/mic dependency), so these
    /// run against synthetic sine waves — deterministic, no live microphone needed.
    /// </summary>
    public class SingingRaterTests
    {
        const int SampleRate = 24000;

        static float[] GenerateSineWave(float frequencyHz, float durationSeconds, int sampleRate, float amplitude = 0.5f)
        {
            int count = (int)(durationSeconds * sampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                samples[i] = amplitude * (float)System.Math.Sin(2 * System.Math.PI * frequencyHz * t);
            }
            return samples;
        }

        [Test]
        public void PitchDetector_DetectsKnownFrequency_FromSyntheticSineWave()
        {
            var samples = GenerateSineWave(440f, 0.1f, SampleRate);

            bool ok = PitchDetector.TryDetectPitch(samples, SampleRate, out float freq, out float confidence);

            Assert.IsTrue(ok);
            Assert.AreEqual(440f, freq, 5f, "Detected frequency should be within 5Hz of the synthesized 440Hz tone.");
            Assert.Greater(confidence, 0.5f);
        }

        [Test]
        public void PitchDetector_DetectsLowerFrequency_TwinkleC4()
        {
            var samples = GenerateSineWave(261.63f, 0.1f, SampleRate);

            bool ok = PitchDetector.TryDetectPitch(samples, SampleRate, out float freq, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(261.63f, freq, 5f);
        }

        [Test]
        public void PitchDetector_Silence_ReturnsFalse()
        {
            var samples = new float[(int)(0.1f * SampleRate)]; // all zero

            bool ok = PitchDetector.TryDetectPitch(samples, SampleRate, out _, out _);

            Assert.IsFalse(ok);
        }

        [Test]
        public void CentsError_OctaveApart_Is1200()
        {
            float cents = PitchDetector.CentsError(880f, 440f);
            Assert.AreEqual(1200f, cents, 1f);
        }

        [Test]
        public void SingingScorer_PerfectPitchMatch_ScoresHigh()
        {
            // Build a recording that sings exactly the reference melody's notes/timing.
            var melody = ReferenceMelody.TwinkleOpening;
            var recorded = new System.Collections.Generic.List<float>();
            foreach (var note in melody)
                recorded.AddRange(GenerateSineWave(note.FrequencyHz, note.DurationSeconds, SampleRate));

            var result = SingingScorer.Score(recorded.ToArray(), SampleRate, melody);

            Assert.Greater(result.Score, 90f, $"Expected a near-perfect score, got {result.Score}");
            Assert.Greater(result.VoicedMatchedWindows, 0);
        }

        [Test]
        public void SingingScorer_OctaveOff_ScoresLow()
        {
            var melody = ReferenceMelody.TwinkleOpening;
            var recorded = new System.Collections.Generic.List<float>();
            foreach (var note in melody)
                recorded.AddRange(GenerateSineWave(note.FrequencyHz * 2f, note.DurationSeconds, SampleRate)); // an octave sharp throughout

            var result = SingingScorer.Score(recorded.ToArray(), SampleRate, melody);

            Assert.Less(result.Score, 20f, $"Expected a low score for singing a full octave off, got {result.Score}");
        }

        [Test]
        public void SingingScorer_Silence_ReturnsZeroScoreAndNoSignal()
        {
            var melody = ReferenceMelody.TwinkleOpening;
            var silence = new float[(int)(ReferenceMelody.TotalDuration(melody) * SampleRate)];

            var result = SingingScorer.Score(silence, SampleRate, melody);

            Assert.AreEqual(0f, result.Score);
            Assert.AreEqual(0, result.VoicedMatchedWindows);
        }

        [Test]
        public void FeedbackAndExpression_MatchScoreBands()
        {
            var high = new SingingResult(90f, 5, 5, 10f);
            var mid = new SingingResult(60f, 5, 5, 150f);
            var low = new SingingResult(10f, 5, 5, 400f);
            var noSignal = SingingResult.NoSignal;

            StringAssert.Contains("nailed", SingingRaterService.FeedbackFor(high));
            Assert.AreEqual(ExpressionId.Happy, SingingRaterService.ExpressionFor(high));

            Assert.AreEqual(ExpressionId.Neutral, SingingRaterService.ExpressionFor(mid));
            Assert.AreEqual(ExpressionId.Sad, SingingRaterService.ExpressionFor(low));
            Assert.AreEqual(ExpressionId.Curious, SingingRaterService.ExpressionFor(noSignal));
        }

        [Test]
        public void WaveformVisualizer_Downsample_ReturnsRequestedPointCount()
        {
            var samples = GenerateSineWave(440f, 0.1f, SampleRate);

            var points = WaveformVisualizer.Downsample(samples, 64);

            Assert.AreEqual(64, points.Length);
        }

        [Test]
        public void WaveformVisualizer_Downsample_PreservesPeakAmplitude()
        {
            var samples = GenerateSineWave(440f, 0.1f, SampleRate, amplitude: 0.8f);

            var points = WaveformVisualizer.Downsample(samples, 32);

            float maxAbs = 0f;
            foreach (var p in points)
                maxAbs = System.Math.Max(maxAbs, System.Math.Abs(p));
            Assert.Greater(maxAbs, 0.7f, "Peak-picking downsample should preserve near-full amplitude, not average it away.");
        }

        [Test]
        public void WaveformVisualizer_Downsample_EmptyInput_ReturnsFlatZeroedArray()
        {
            var points = WaveformVisualizer.Downsample(System.Array.Empty<float>(), 16);

            Assert.AreEqual(16, points.Length);
            foreach (var p in points)
                Assert.AreEqual(0f, p);
        }
    }
}
