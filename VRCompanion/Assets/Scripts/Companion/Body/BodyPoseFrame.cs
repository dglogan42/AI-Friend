using System.Collections.Generic;

namespace VRCompanion.Body
{
    /// <summary>Kinect SDK v2-style joint names — the set the official SDK reports.</summary>
    public enum BodyJoint
    {
        Head, Neck, SpineShoulder, SpineMid, SpineBase,
        ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
        ShoulderRight, ElbowRight, WristRight, HandRight,
        HipLeft, KneeLeft, AnkleLeft, FootLeft,
        HipRight, KneeRight, AnkleRight, FootRight,
    }

    public readonly struct JointPose
    {
        public readonly UnityEngine.Vector3 Position; // meters, sensor space
        public readonly float TrackingConfidence;     // 0..1

        public JointPose(UnityEngine.Vector3 position, float trackingConfidence)
        {
            Position = position;
            TrackingConfidence = trackingConfidence;
        }
    }

    /// <summary>One frame of body/skeleton tracking, source-agnostic.</summary>
    public readonly struct BodyPoseFrame
    {
        readonly IReadOnlyDictionary<BodyJoint, JointPose> _joints;

        public bool BodyFound { get; }

        public BodyPoseFrame(bool bodyFound, IReadOnlyDictionary<BodyJoint, JointPose> joints)
        {
            BodyFound = bodyFound;
            _joints = joints;
        }

        public bool TryGetJoint(BodyJoint joint, out JointPose pose)
        {
            if (_joints != null && _joints.TryGetValue(joint, out pose))
                return true;
            pose = default;
            return false;
        }

        public static readonly BodyPoseFrame Empty = new BodyPoseFrame(false, null);
    }
}
