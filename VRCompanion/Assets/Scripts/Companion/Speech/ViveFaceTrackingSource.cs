using System;
using UnityEngine;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Placeholder for the HTC VIVE Full Face Tracker (OpenXR extension
    /// XR_HTC_facial_tracking, eye + lip expression blendshapes).
    ///
    /// UNTESTED: this environment has neither the VIVE OpenXR Unity package
    /// (com.htc.upm.vive.openxr, HTC's own scoped registry) nor the physical
    /// tracker/headset, so IsAvailable always reports false and no event ever
    /// fires — safe no-op. To finish this once you have the hardware:
    ///   1. Add the VIVE OpenXR plugin package to Packages/manifest.json
    ///      (see developer.vive.com/resources/openxr/unity for the registry URL).
    ///   2. Enable the HTC facial tracking OpenXR feature in
    ///      Project Settings > XR Plug-in Management > OpenXR.
    ///   3. In Update(), poll the extension's eye/lip expression API and map
    ///      its blendshape set into a FaceBlendshapeFrame (names differ
    ///      slightly from ARKit/MediaPipe's — a mapping table will be needed).
    /// </summary>
    public sealed class ViveFaceTrackingSource : MonoBehaviour, IFaceTrackingSource
    {
        public bool IsAvailable => false;
        public event Action<FaceBlendshapeFrame> BlendshapesUpdated;

        void Awake()
        {
            Debug.Log("[ViveFaceTrackingSource] Stub only — no VIVE OpenXR facial tracking extension available in this build.");
            // Reference the event so the compiler doesn't warn it's unused in this stub.
            BlendshapesUpdated = null;
        }
    }
}
