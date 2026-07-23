using UnityEngine;

namespace VRCompanion.Audio
{
    /// <summary>
    /// Procedural tones for stub TTS / UI feedback without external audio assets.
    /// </summary>
    public static class ToneSynthesizer
    {
        public static AudioClip CreateTone(
            float frequencyHz,
            float durationSeconds,
            int sampleRate = 24000,
            float amplitude = 0.2f,
            bool fade = true)
        {
            durationSeconds = Mathf.Max(0.02f, durationSeconds);
            sampleRate = Mathf.Max(8000, sampleRate);
            int count = Mathf.CeilToInt(durationSeconds * sampleRate);
            var data = new float[count];
            float twoPiF = 2f * Mathf.PI * frequencyHz;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f;
                if (fade)
                {
                    float attack = Mathf.Clamp01(t / 0.02f);
                    float release = Mathf.Clamp01((durationSeconds - t) / 0.04f);
                    env = attack * release;
                }

                // Soft square-ish formant blend so speech stubs sound less pure-sine.
                float s = Mathf.Sin(twoPiF * t);
                float h = 0.35f * Mathf.Sin(twoPiF * 2f * t);
                data[i] = amplitude * env * (s + h);
            }

            var clip = AudioClip.Create($"tone_{frequencyHz:0}", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Map text length to a short chirp sequence duration and base pitch.</summary>
        public static float EstimateSpeechSeconds(string text, float secondsPerWord = 0.28f, float minSeconds = 0.45f)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0f;
            int words = Mathf.Max(1, text.Split(new[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length);
            return Mathf.Max(minSeconds, words * secondsPerWord);
        }

        public static float PitchForText(string text)
        {
            // Stable pseudo-random pitch per phrase so replies are distinguishable.
            unchecked
            {
                int hash = (text ?? string.Empty).GetHashCode();
                float u = (hash & 0xFFFF) / 65535f;
                return Mathf.Lerp(220f, 520f, u);
            }
        }
    }
}
