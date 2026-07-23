using UnityEngine;
using VRCompanion.Body;
using VRCompanion.Speech;
using VRCompanion.Vision;

namespace VRCompanion.Diagnostics
{
    /// <summary>
    /// On-screen latency / tracking summary (Editor + dev builds).
    /// Toggle with F3.
    /// </summary>
    public sealed class CompanionDiagnosticsHud : MonoBehaviour
    {
        [SerializeField] bool visible = true;
        [SerializeField] KeyCode toggleKey = KeyCode.F3;

        WebcamFaceTrackingSource _face;
        WebcamBodyTrackingSource _body;
        WebcamImageRecognitionSource _vision;
        StubTtsService _tts;
        float _nextLog;

        void Awake()
        {
            _face = GetComponent<WebcamFaceTrackingSource>();
            _body = GetComponent<WebcamBodyTrackingSource>();
            _vision = GetComponent<WebcamImageRecognitionSource>();
            _tts = GetComponent<StubTtsService>();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;

            // Occasional console sample so headless runs still show latency.
            if (Time.unscaledTime >= _nextLog)
            {
                _nextLog = Time.unscaledTime + 5f;
                if (_face != null && _face.IsAvailable)
                    Debug.Log($"[diag] face {_face.IntervalMeter} proc={_face.ProcMeter.AverageMs:0.0}ms");
                if (_body != null && _body.IsAvailable)
                    Debug.Log($"[diag] body {_body.IntervalMeter} proc={_body.ProcMeter.AverageMs:0.0}ms gestures={_body.LastGestures.Count}");
                if (_vision != null && _vision.IsAvailable)
                    Debug.Log($"[diag] vision {_vision.IntervalMeter} proc={_vision.ProcMeter.AverageMs:0.0}ms person={_vision.Latest.Get("person_present"):0.00}");
            }
        }

        void OnGUI()
        {
            if (!visible)
                return;

            const int w = 420;
            var rect = new Rect(12, 12, w, 160);
            GUI.Box(rect, "Companion diagnostics (F3)");
            GUILayout.BeginArea(new Rect(20, 36, w - 16, 130));
            GUILayout.Label(Line("face", _face != null && _face.IsAvailable, _face?.IntervalMeter, _face?.ProcMeter));
            GUILayout.Label(Line("body", _body != null && _body.IsAvailable, _body?.IntervalMeter, _body?.ProcMeter));
            GUILayout.Label(Line("vision", _vision != null && _vision.IsAvailable, _vision?.IntervalMeter, _vision?.ProcMeter));
            if (_tts != null)
                GUILayout.Label($"tts speak avg={_tts.SpeakLatency.AverageMs:0.0}ms last={_tts.SpeakLatency.LastMs:0.0}ms");
            if (_vision != null && _vision.IsAvailable)
                GUILayout.Label($"vision person={_vision.Latest.Get("person_present"):0.00} motion={_vision.Latest.Get("motion"):0.00} hand={_vision.Latest.Get("hand_raised_hint"):0.00}");
            GUILayout.EndArea();
        }

        static string Line(string name, bool up, LatencyMeter interval, LatencyMeter proc)
        {
            if (!up || interval == null)
                return $"{name}: down";
            return $"{name}: up  interval avg={interval.AverageMs:0.0}ms p95={interval.PercentileMs(0.95f):0.0}ms  proc avg={proc?.AverageMs ?? 0f:0.0}ms";
        }
    }
}
