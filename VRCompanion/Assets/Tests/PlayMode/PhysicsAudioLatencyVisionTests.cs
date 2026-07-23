using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VRCompanion.Audio;
using VRCompanion.Body;
using VRCompanion.Diagnostics;
using VRCompanion.Speech;
using VRCompanion.Vision;

namespace VRCompanion.Tests
{
    public class PhysicsAudioLatencyVisionTests
    {
        [Test]
        public void Physics_BounceHeight_MatchesKinematics()
        {
            // v=4.905 m/s under g=9.81 → apex ~1.225 m
            float h = CompanionPhysics.BounceHeightMeters(4.905f, -9.81f);
            Assert.AreEqual(1.225f, h, 0.02f);
            float t = CompanionPhysics.TimeToApexSeconds(4.905f, -9.81f);
            Assert.AreEqual(0.5f, t, 0.02f);
        }

        [Test]
        public void Physics_ApplyProjectDefaults_SetsGravity()
        {
            CompanionPhysics.ApplyProjectDefaults();
            Assert.AreEqual(CompanionPhysics.DefaultGravityY, Physics.gravity.y, 0.001f);
        }

        [Test]
        public void AudioMeter_RmsAndPeak_OnSine()
        {
            var samples = new float[2400];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 440f * i / 24000f);

            float rms = AudioMeter.Rms(samples);
            float peak = AudioMeter.Peak(samples);
            Assert.Greater(rms, 0.3f);
            Assert.Less(rms, 0.4f);
            Assert.AreEqual(0.5f, peak, 0.02f);
            Assert.IsTrue(AudioMeter.IsVoiced(samples));
            Assert.IsFalse(AudioMeter.IsVoiced(new float[100]));
        }

        [Test]
        public void ToneSynthesizer_EstimateAndPitch_AreStable()
        {
            float d = ToneSynthesizer.EstimateSpeechSeconds("hello there friend");
            Assert.Greater(d, 0.5f);
            float p1 = ToneSynthesizer.PitchForText("hello");
            float p2 = ToneSynthesizer.PitchForText("hello");
            Assert.AreEqual(p1, p2);
            Assert.GreaterOrEqual(p1, 220f);
            Assert.LessOrEqual(p1, 520f);
        }

        [Test]
        public void LatencyMeter_RollingAverage_AndPercentile()
        {
            var m = new LatencyMeter("test", 16);
            for (int i = 1; i <= 10; i++)
                m.RecordMs(i * 10f);
            Assert.AreEqual(10, m.Count);
            Assert.AreEqual(55f, m.AverageMs, 0.01f);
            Assert.GreaterOrEqual(m.PercentileMs(0.95f), 90f);
        }

        [Test]
        public void FacePacket_ParsesProcMs()
        {
            const string json = "{\"t\":12,\"faceFound\":true,\"proc_ms\":17.5,\"shapes\":[{\"n\":\"jawOpen\",\"s\":0.4}]}";
            bool ok = WebcamFaceTrackingSource.TryParse(Encoding.UTF8.GetBytes(json), out var frame, out float proc);
            Assert.IsTrue(ok);
            Assert.IsTrue(frame.FaceFound);
            Assert.AreEqual(17.5f, proc, 0.01f);
            Assert.AreEqual(0.4f, frame.Get("jawOpen"), 0.001f);
        }

        [Test]
        public void BodyPacket_ParsesProcMs_AndGesturesHandsUp()
        {
            // Wrists above shoulders.
            var joints = new Dictionary<BodyJoint, JointPose>
            {
                [BodyJoint.Head] = new JointPose(new Vector3(0, 1.7f, 0), 1f),
                [BodyJoint.SpineBase] = new JointPose(new Vector3(0, 0.9f, 0), 1f),
                [BodyJoint.ShoulderLeft] = new JointPose(new Vector3(0.2f, 1.4f, 0), 1f),
                [BodyJoint.ShoulderRight] = new JointPose(new Vector3(-0.2f, 1.4f, 0), 1f),
                [BodyJoint.WristLeft] = new JointPose(new Vector3(0.25f, 1.7f, 0), 1f),
                [BodyJoint.WristRight] = new JointPose(new Vector3(-0.25f, 1.7f, 0), 1f),
            };
            var frame = new BodyPoseFrame(true, joints);
            var g = BodyGestureRecognizer.Evaluate(frame);
            Assert.Greater(g[BodyGestureRecognizer.HandsUp], 0.9f);
        }

        [Test]
        public void VisionPacket_ParsesLabels()
        {
            const string json = "{\"t\":1,\"proc_ms\":3.2,\"labels\":[{\"n\":\"person_present\",\"s\":0.8},{\"n\":\"motion\",\"s\":0.1}]}";
            bool ok = WebcamImageRecognitionSource.TryParse(Encoding.UTF8.GetBytes(json), out var frame);
            Assert.IsTrue(ok);
            Assert.IsTrue(frame.HasLabels);
            Assert.AreEqual(0.8f, frame.Get("person_present"), 0.001f);
            Assert.AreEqual(3.2f, frame.ProcMs, 0.001f);
        }
    }
}
