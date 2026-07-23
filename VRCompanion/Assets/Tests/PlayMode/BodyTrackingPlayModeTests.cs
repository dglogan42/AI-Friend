using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion.Body;

namespace VRCompanion.Tests
{
    public class BodyTrackingPlayModeTests
    {
        [Test]
        public void WebcamBodyTrackingSource_ParsesPacket_IntoBodyPoseFrame()
        {
            string json = "{\"t\":1,\"bodyFound\":true,\"joints\":[" +
                           "{\"n\":\"Head\",\"x\":0.1,\"y\":1.6,\"z\":-0.2,\"c\":0.9}," +
                           "{\"n\":\"WristLeft\",\"x\":-0.3,\"y\":1.1,\"z\":-0.1,\"c\":0.75}]}";

            bool ok = WebcamBodyTrackingSource.TryParse(Encoding.UTF8.GetBytes(json), out var frame);

            Assert.IsTrue(ok);
            Assert.IsTrue(frame.BodyFound);

            Assert.IsTrue(frame.TryGetJoint(BodyJoint.Head, out var head));
            Assert.AreEqual(new Vector3(0.1f, 1.6f, -0.2f), head.Position);
            Assert.AreEqual(0.9f, head.TrackingConfidence, 0.001f);

            Assert.IsTrue(frame.TryGetJoint(BodyJoint.WristLeft, out var wrist));
            Assert.AreEqual(0.75f, wrist.TrackingConfidence, 0.001f);

            Assert.IsFalse(frame.TryGetJoint(BodyJoint.AnkleRight, out _), "Joint not present in the packet should not be found.");
        }

        [Test]
        public void WebcamBodyTrackingSource_NoBody_ReturnsBodyFoundFalse()
        {
            string json = "{\"t\":1,\"bodyFound\":false,\"joints\":[]}";

            bool ok = WebcamBodyTrackingSource.TryParse(Encoding.UTF8.GetBytes(json), out var frame);

            Assert.IsTrue(ok);
            Assert.IsFalse(frame.BodyFound);
        }

        [UnityTest]
        public IEnumerator WebcamBodyTrackingSource_SyntheticPacket_RaisesPoseUpdatedAndBecomesAvailable()
        {
            var go = new GameObject("BodyTrackingTest");
            var source = go.AddComponent<WebcamBodyTrackingSource>();
            yield return null;

            BodyPoseFrame? received = null;
            source.PoseUpdated += frame => received = frame;

            string json = "{\"t\":1,\"bodyFound\":true,\"joints\":[{\"n\":\"SpineBase\",\"x\":0,\"y\":0.9,\"z\":0,\"c\":0.8}]}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using (var udp = new UdpClient())
            {
                udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, 5556));
            }

            float timeout = 3f;
            while (received == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(received, "PoseUpdated did not fire after a synthetic UDP packet.");
            Assert.IsTrue(received.Value.BodyFound);
            Assert.IsTrue(source.IsAvailable);

            Object.Destroy(go);
        }
    }
}
