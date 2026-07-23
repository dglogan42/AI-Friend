using System;

namespace VRCompanion.Body
{
    /// <summary>A live source of body/skeleton pose data. Implementations must be safe
    /// to enable when their hardware/backend is absent — IsAvailable should report
    /// false and PoseUpdated should simply never fire.</summary>
    public interface IBodyTrackingSource
    {
        bool IsAvailable { get; }
        event Action<BodyPoseFrame> PoseUpdated;
    }
}
