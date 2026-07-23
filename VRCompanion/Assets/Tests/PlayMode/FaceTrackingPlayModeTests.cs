using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRCompanion;
using VRCompanion.Speech;

namespace VRCompanion.Tests
{
    public class FaceTrackingPlayModeTests
    {
        [UnityTest]
        public IEnumerator WebcamSource_SyntheticSmilePacket_DrivesHappyExpression()
        {
            new GameObject("CompanionBootstrap").AddComponent<CompanionBootstrap>();

            ExpressionController expression = null;
            for (int i = 0; i < 60 && expression == null; i++)
            {
                expression = Object.FindFirstObjectByType<ExpressionController>();
                yield return null;
            }
            Assert.IsNotNull(expression, "ExpressionController was not found in the bootstrap scene.");

            // Let the auto-greet settle so it's not mid-transition when we check state.
            yield return new WaitForSeconds(2.5f);

            // Synthetic packet matching Tools/FaceTracking/webcam_face_tracker.py's format —
            // no live webcam needed, this only exercises the Unity-side parsing/mapping.
            string json = "{\"t\":1,\"faceFound\":true,\"shapes\":[" +
                           "{\"n\":\"mouthSmileLeft\",\"s\":0.9}," +
                           "{\"n\":\"mouthSmileRight\",\"s\":0.9}]}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using (var udp = new UdpClient())
            {
                udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, 5555));
            }

            float timeout = 3f;
            while (expression.Current.Id != ExpressionId.Happy && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(ExpressionId.Happy, expression.Current.Id,
                "Expression did not switch to Happy after a synthetic high-smile blendshape packet.");
        }

        [Test]
        public void WebcamFaceTrackingSource_ParsesPacket_IntoBlendshapeFrame()
        {
            string json = "{\"t\":1,\"faceFound\":true,\"shapes\":[{\"n\":\"jawOpen\",\"s\":0.55}]}";
            bool ok = WebcamFaceTrackingSource.TryParse(Encoding.UTF8.GetBytes(json), out var frame);

            Assert.IsTrue(ok);
            Assert.IsTrue(frame.FaceFound);
            Assert.AreEqual(0.55f, frame.Get("jawOpen"), 0.001f);
            Assert.AreEqual(0f, frame.Get("missingShape"), 0.001f);
        }
    }
}
