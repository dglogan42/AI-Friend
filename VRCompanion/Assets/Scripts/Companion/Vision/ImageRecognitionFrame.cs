using System.Collections.Generic;

namespace VRCompanion.Vision
{
    public readonly struct ImageRecognitionFrame
    {
        public readonly bool HasLabels;
        public readonly IReadOnlyDictionary<string, float> Scores;
        public readonly float ProcMs;

        public static ImageRecognitionFrame Empty { get; } =
            new ImageRecognitionFrame(false, new Dictionary<string, float>(), 0f);

        public ImageRecognitionFrame(bool hasLabels, IReadOnlyDictionary<string, float> scores, float procMs)
        {
            HasLabels = hasLabels;
            Scores = scores ?? new Dictionary<string, float>();
            ProcMs = procMs;
        }

        public float Get(string name, float fallback = 0f)
            => Scores != null && Scores.TryGetValue(name, out var v) ? v : fallback;
    }
}
