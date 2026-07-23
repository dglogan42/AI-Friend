using System;
using System.Collections.Generic;

namespace VRCompanion.Speech
{
    /// <summary>
    /// One frame of ARKit-style named blendshape scores (0..1), source-agnostic
    /// (webcam/MediaPipe, VIVE OpenXR facial tracking, etc.).
    /// </summary>
    public readonly struct FaceBlendshapeFrame
    {
        readonly IReadOnlyDictionary<string, float> _scores;

        public bool FaceFound { get; }

        public FaceBlendshapeFrame(bool faceFound, IReadOnlyDictionary<string, float> scores)
        {
            FaceFound = faceFound;
            _scores = scores;
        }

        public float Get(string blendshapeName)
        {
            if (_scores != null && _scores.TryGetValue(blendshapeName, out float v))
                return v;
            return 0f;
        }

        public static readonly FaceBlendshapeFrame Empty = new FaceBlendshapeFrame(false, null);
    }
}
