using NUnit.Framework;
using UnityEngine;
using VRCompanion.Body;

namespace VRCompanion.Tests
{
    public class BodyTrackingStubTests
    {
        [Test]
        public void KinectBodyTrackingSource_ReportsUnavailable_AndNeverFires()
        {
            var go = new GameObject("KinectStub");
            var source = go.AddComponent<KinectBodyTrackingSource>();

            bool fired = false;
            source.PoseUpdated += _ => fired = true;

            Assert.IsFalse(source.IsAvailable);
            Assert.IsFalse(fired);

            Object.Destroy(go);
        }

        [Test]
        public void BodyPoseFrame_Empty_HasNoJoints()
        {
            bool found = BodyPoseFrame.Empty.TryGetJoint(BodyJoint.Head, out _);
            Assert.IsFalse(found);
            Assert.IsFalse(BodyPoseFrame.Empty.BodyFound);
        }
    }
}
