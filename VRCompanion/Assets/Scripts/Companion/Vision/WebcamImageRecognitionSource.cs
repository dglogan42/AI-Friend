using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VRCompanion.Diagnostics;

namespace VRCompanion.Vision
{
    /// <summary>
    /// Listens for vision label frames from Tools/run_robotics_tracker.py (UDP 5557).
    /// </summary>
    public sealed class WebcamImageRecognitionSource : MonoBehaviour
    {
        [SerializeField] int listenPort = 5557;
        [SerializeField] float staleAfterSeconds = 1f;

        UdpClient _client;
        float _lastPacketTime = -999f;
        float _lastArrival = -1f;
        readonly LatencyMeter _intervalMeter = new LatencyMeter("vision_interval");
        readonly LatencyMeter _procMeter = new LatencyMeter("vision_proc");

        public bool IsAvailable => _client != null && Time.unscaledTime - _lastPacketTime < staleAfterSeconds;
        public ImageRecognitionFrame Latest { get; private set; } = ImageRecognitionFrame.Empty;
        public LatencyMeter IntervalMeter => _intervalMeter;
        public LatencyMeter ProcMeter => _procMeter;
        public event Action<ImageRecognitionFrame> LabelsUpdated;

        void OnEnable()
        {
            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenPort));
                _client.Client.Blocking = false;
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[WebcamImageRecognitionSource] Could not bind UDP {listenPort}: {ex.Message}");
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

            byte[] latest = null;
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_client.Available > 0)
            {
                try { latest = _client.Receive(ref remote); }
                catch (SocketException) { break; }
            }

            if (latest == null)
                return;

            float now = Time.unscaledTime;
            _intervalMeter.RecordInterval(_lastArrival, now);
            _lastArrival = now;
            _lastPacketTime = now;

            if (TryParse(latest, out var frame))
            {
                if (frame.ProcMs > 0f)
                    _procMeter.RecordMs(frame.ProcMs);
                Latest = frame;
                LabelsUpdated?.Invoke(frame);
            }
        }

        public static bool TryParse(byte[] json, out ImageRecognitionFrame frame)
        {
            frame = ImageRecognitionFrame.Empty;
            try
            {
                var packet = JsonUtility.FromJson<VisionPacket>(System.Text.Encoding.UTF8.GetString(json));
                if (packet == null)
                    return false;

                var scores = new Dictionary<string, float>();
                if (packet.labels != null)
                {
                    foreach (var e in packet.labels)
                        if (!string.IsNullOrEmpty(e.n))
                            scores[e.n] = e.s;
                }

                frame = new ImageRecognitionFrame(scores.Count > 0, scores, packet.proc_ms);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebcamImageRecognitionSource] parse failed: {ex.Message}");
                return false;
            }
        }

        [Serializable]
        class LabelEntry { public string n; public float s; }

        [Serializable]
        class VisionPacket
        {
            public long t;
            public float proc_ms;
            public LabelEntry[] labels;
        }
    }
}
