using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VRCompanion.Diagnostics;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Listens for blendshape frames sent by Tools/FaceTracking/webcam_face_tracker.py
    /// (MediaPipe Face Landmarker over UDP, localhost by default). Run that script
    /// separately; this component only consumes its output.
    /// </summary>
    public sealed class WebcamFaceTrackingSource : MonoBehaviour, IFaceTrackingSource
    {
        [SerializeField] int listenPort = 5555;
        [SerializeField] float staleAfterSeconds = 1f;

        UdpClient _client;
        float _lastPacketTime = -999f;
        float _lastArrival = -1f;
        readonly LatencyMeter _intervalMeter = new LatencyMeter("face_interval");
        readonly LatencyMeter _procMeter = new LatencyMeter("face_proc");

        public bool IsAvailable => _client != null && Time.unscaledTime - _lastPacketTime < staleAfterSeconds;
        public LatencyMeter IntervalMeter => _intervalMeter;
        public LatencyMeter ProcMeter => _procMeter;
        public event Action<FaceBlendshapeFrame> BlendshapesUpdated;

        void OnEnable()
        {
            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenPort));
                _client.Client.Blocking = false;
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[WebcamFaceTrackingSource] Could not bind UDP port {listenPort}: {ex.Message}. Webcam tracking disabled.");
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
                BlendshapesUpdated?.Invoke(frame);
            }
        }

        public static bool TryParse(byte[] json, out FaceBlendshapeFrame frame)
            => TryParse(json, out frame, out _);

        public static bool TryParse(byte[] json, out FaceBlendshapeFrame frame, out float procMs)
        {
            frame = FaceBlendshapeFrame.Empty;
            procMs = 0f;
            try
            {
                var packet = JsonUtility.FromJson<FacePacket>(System.Text.Encoding.UTF8.GetString(json));
                if (packet == null)
                    return false;

                var scores = new Dictionary<string, float>();
                if (packet.shapes != null)
                {
                    foreach (var entry in packet.shapes)
                        scores[entry.n] = entry.s;
                }

                procMs = packet.proc_ms;
                frame = new FaceBlendshapeFrame(packet.faceFound, scores);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebcamFaceTrackingSource] Failed to parse packet: {ex.Message}");
                return false;
            }
        }

        [Serializable]
        internal class BlendshapeEntry
        {
            public string n;
            public float s;
        }

        [Serializable]
        internal class FacePacket
        {
            public long t;
            public bool faceFound;
            public float proc_ms;
            public BlendshapeEntry[] shapes;
        }
    }
}
