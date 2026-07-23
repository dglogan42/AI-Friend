using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VRCompanion.Diagnostics;

namespace VRCompanion.Body
{
    /// <summary>
    /// Listens for joint frames sent by Tools/BodyTracking/webcam_body_tracker.py
    /// (MediaPipe Pose over UDP, localhost by default). Run that script separately;
    /// this component only consumes its output.
    ///
    /// This is the Linux-friendly alternative to real Kinect body tracking (see
    /// KinectBodyTrackingSource's doc comment for why Kinect itself isn't viable
    /// here): a regular webcam plus MediaPipe Pose stands in for the Kinect SDK's
    /// skeleton output, reduced to the same BodyJoint set on the Python side.
    /// </summary>
    public sealed class WebcamBodyTrackingSource : MonoBehaviour, IBodyTrackingSource
    {
        [SerializeField] int listenPort = 5556;
        [SerializeField] float staleAfterSeconds = 1f;

        UdpClient _client;
        float _lastPacketTime = -999f;
        float _lastArrival = -1f;
        readonly LatencyMeter _intervalMeter = new LatencyMeter("body_interval");
        readonly LatencyMeter _procMeter = new LatencyMeter("body_proc");
        Dictionary<string, float> _lastGestures = new Dictionary<string, float>();

        public bool IsAvailable => _client != null && Time.unscaledTime - _lastPacketTime < staleAfterSeconds;
        public LatencyMeter IntervalMeter => _intervalMeter;
        public LatencyMeter ProcMeter => _procMeter;
        public IReadOnlyDictionary<string, float> LastGestures => _lastGestures;
        public event Action<BodyPoseFrame> PoseUpdated;

        void OnEnable()
        {
            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenPort));
                _client.Client.Blocking = false;
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[WebcamBodyTrackingSource] Could not bind UDP port {listenPort}: {ex.Message}. Webcam body tracking disabled.");
                _client = null;
            }
        }

        void OnDisable()
        {
            _client?.Close();
            _client = null;
        }

        void Update()
        {
            if (_client == null)
                return;

            // Drain the socket each frame, keep only the most recent packet.
            byte[] latest = null;
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_client.Available > 0)
            {
                try
                {
                    latest = _client.Receive(ref remote);
                }
                catch (SocketException)
                {
                    break;
                }
            }

            if (latest == null)
                return;

            float now = Time.unscaledTime;
            _intervalMeter.RecordInterval(_lastArrival, now);
            _lastArrival = now;
            _lastPacketTime = now;

            if (TryParse(latest, out var frame, out float procMs))
            {
                if (procMs > 0f)
                    _procMeter.RecordMs(procMs);
                _lastGestures = BodyGestureRecognizer.Evaluate(frame);
                PoseUpdated?.Invoke(frame);
            }
        }

        public static bool TryParse(byte[] json, out BodyPoseFrame frame)
            => TryParse(json, out frame, out _);

        public static bool TryParse(byte[] json, out BodyPoseFrame frame, out float procMs)
        {
            frame = BodyPoseFrame.Empty;
            procMs = 0f;
            try
            {
                var packet = JsonUtility.FromJson<BodyPacket>(System.Text.Encoding.UTF8.GetString(json));
                if (packet == null)
                    return false;

                var joints = new Dictionary<BodyJoint, JointPose>();
                if (packet.joints != null)
                {
                    foreach (var entry in packet.joints)
                    {
                        if (Enum.TryParse(entry.n, out BodyJoint joint))
                            joints[joint] = new JointPose(new Vector3(entry.x, entry.y, entry.z), entry.c);
                    }
                }

                procMs = packet.proc_ms;
                frame = new BodyPoseFrame(packet.bodyFound, joints);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebcamBodyTrackingSource] Failed to parse packet: {ex.Message}");
                return false;
            }
        }

        [Serializable]
        internal class JointEntry
        {
            public string n;
            public float x;
            public float y;
            public float z;
            public float c;
        }

        [Serializable]
        internal class BodyPacket
        {
            public long t;
            public bool bodyFound;
            public float proc_ms;
            public JointEntry[] joints;
        }
    }
}
