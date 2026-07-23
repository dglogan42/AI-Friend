using System;

namespace VRCompanion.Speech
{
    /// <summary>
    /// A live source of facial blendshape data (webcam, headset add-on, etc.).
    /// Implementations must be safe to enable when their hardware/backend is absent —
    /// IsAvailable should report false and BlendshapesUpdated should simply never fire.
    /// </summary>
    public interface IFaceTrackingSource
    {
        bool IsAvailable { get; }
        event Action<FaceBlendshapeFrame> BlendshapesUpdated;
    }
}
