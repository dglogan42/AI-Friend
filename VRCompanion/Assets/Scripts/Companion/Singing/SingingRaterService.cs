using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRCompanion;

namespace VRCompanion.Singing
{
    /// <summary>
    /// Call-and-response singing challenge: plays a short reference melody, records
    /// the user singing it back over the mic, and scores pitch accuracy.
    /// </summary>
    public sealed class SingingRaterService : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] int sampleRate = 24000;
        [SerializeField] string micDevice;

        MelodyNote[] _melody = ReferenceMelody.TwinkleOpening;

        void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public async Task<SingingResult> RunChallengeAsync(CancellationToken ct)
        {
            await PlayReferenceMelodyAsync(ct);
            await Task.Delay(300, ct);

            float totalDuration = ReferenceMelody.TotalDuration(_melody);
            string device = string.IsNullOrEmpty(micDevice) ? null : micDevice;
            int lengthSec = Mathf.CeilToInt(totalDuration) + 1;

            var clip = Microphone.Start(device, false, lengthSec, sampleRate);
            if (clip == null)
                return SingingResult.NoSignal;

            await Task.Delay(TimeSpan.FromSeconds(totalDuration), ct);

            int recordedSamples = Mathf.Clamp(Microphone.GetPosition(device), 0, clip.samples);
            if (Microphone.IsRecording(device))
                Microphone.End(device);

            if (recordedSamples <= 0)
                return SingingResult.NoSignal;

            var buffer = new float[recordedSamples];
            clip.GetData(buffer, 0);

            return SingingScorer.Score(buffer, sampleRate, _melody);
        }

        async Task PlayReferenceMelodyAsync(CancellationToken ct)
        {
            foreach (var note in _melody)
            {
                var tone = GenerateTone(note.FrequencyHz, note.DurationSeconds, sampleRate);
                audioSource.PlayOneShot(tone);
                await Task.Delay(TimeSpan.FromSeconds(note.DurationSeconds), ct);
            }
        }

        public static AudioClip GenerateTone(float frequencyHz, float durationSeconds, int sampleRate)
        {
            int sampleCount = Mathf.Max(1, (int)(durationSeconds * sampleRate));
            var samples = new float[sampleCount];
            float fadeSamples = sampleRate * 0.02f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float fade = Mathf.Min(1f, Mathf.Min(i, sampleCount - i) / Mathf.Max(1f, fadeSamples));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * 0.5f * fade;
            }

            var clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static string FeedbackFor(SingingResult result)
        {
            if (result.VoicedMatchedWindows == 0)
                return "I couldn't hear you sing — want to try again?";
            if (result.Score >= 80f)
                return "Wow, great pitch! You nailed it!";
            if (result.Score >= 50f)
                return "Nice try! Pretty close to the notes.";
            return "Good effort — let's try that again together.";
        }

        public static ExpressionId ExpressionFor(SingingResult result)
        {
            if (result.VoicedMatchedWindows == 0)
                return ExpressionId.Curious;
            if (result.Score >= 80f)
                return ExpressionId.Happy;
            if (result.Score >= 50f)
                return ExpressionId.Neutral;
            return ExpressionId.Sad;
        }
    }
}
