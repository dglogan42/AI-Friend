using System.Collections.Generic;

namespace VRCompanion.Singing
{
    public readonly struct MelodyNote
    {
        public readonly string Name;
        public readonly float FrequencyHz;
        public readonly float DurationSeconds;

        public MelodyNote(string name, float frequencyHz, float durationSeconds)
        {
            Name = name;
            FrequencyHz = frequencyHz;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>Short, public-domain melodies for the singing rater's call-and-response.</summary>
    public static class ReferenceMelody
    {
        // "Twinkle Twinkle Little Star", opening phrase.
        public static readonly MelodyNote[] TwinkleOpening =
        {
            new MelodyNote("C4", 261.63f, 0.6f),
            new MelodyNote("C4", 261.63f, 0.6f),
            new MelodyNote("G4", 392.00f, 0.6f),
            new MelodyNote("G4", 392.00f, 0.6f),
        };

        public static float TotalDuration(IReadOnlyList<MelodyNote> melody)
        {
            float total = 0f;
            foreach (var n in melody)
                total += n.DurationSeconds;
            return total;
        }

        /// <summary>Which note is expected to be sounding at time t (seconds) into the melody.</summary>
        public static bool TryGetNoteAtTime(IReadOnlyList<MelodyNote> melody, float t, out MelodyNote note)
        {
            float cursor = 0f;
            foreach (var n in melody)
            {
                if (t >= cursor && t < cursor + n.DurationSeconds)
                {
                    note = n;
                    return true;
                }
                cursor += n.DurationSeconds;
            }
            note = default;
            return false;
        }
    }
}
