using System;
using UnityEngine;

namespace VRCompanion.Body
{
    /// <summary>
    /// Placeholder for Kinect body/skeleton tracking.
    ///
    /// UNTESTED — no Kinect sensor is connected in this environment, and there are two
    /// genuinely different real integration paths depending on target OS:
    ///
    ///   1. Windows + Kinect for Windows SDK v2 (RECOMMENDED path): the official SDK
    ///      computes a 25-joint skeleton (Kinect20.dll) directly. This is
    ///      Windows-only — no Linux port exists. To finish this on Windows:
    ///        a. Add the Kinect v2 Unity plugin (Microsoft's Kinect.Unity sample, or
    ///           a NuGet/native-plugin wrapper around Kinect20.dll) to the project.
    ///        b. In Update(), poll BodyFrameSource for tracked bodies and map its
    ///           joint set into a BodyPoseFrame (the BodyJoint enum here already
    ///           matches the SDK v2 joint names).
    ///
    ///   2. Linux + libfreenect2 (installed on this machine via apt, but NOT wired
    ///      up here): libfreenect2 only exposes raw depth/color/IR streams — it does
    ///      NOT compute a skeleton. Real body tracking on Linux would need a
    ///      separate layer on top (e.g. NuiTrack, a paid third-party middleware, or
    ///      a custom pose-estimation model over the depth/RGB frames — similar in
    ///      spirit to how WebcamFaceTrackingSource uses MediaPipe for faces).
    ///
    /// IsAvailable always reports false here, so this is a safe no-op either way.
    /// </summary>
    public sealed class KinectBodyTrackingSource : MonoBehaviour, IBodyTrackingSource
    {
        public bool IsAvailable => false;
        public event Action<BodyPoseFrame> PoseUpdated;

        void Awake()
        {
            Debug.Log("[KinectBodyTrackingSource] Stub only — no Kinect sensor/SDK available in this build.");
            PoseUpdated = null;
        }
    }
}
