using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRCompanion.Audio;
using VRCompanion.Diagnostics;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Dev stub TTS: logs text, plays a procedural chirp via AudioSource, and
    /// records speak latency for diagnostics. Swap for neural TTS later.
    /// </summary>
    public sealed class StubTtsService : MonoBehaviour, ITtsService
    {
        [SerializeField] float secondsPerWord = 0.28f;
        [SerializeField] float minSeconds = 0.45f;
        [SerializeField] bool playTone = true;
        [SerializeField] float toneAmplitude = 0.18f;
        [SerializeField] int sampleRate = 24000;

        CancellationTokenSource _cts;
        AudioSource _source;
        readonly LatencyMeter _speakMeter = new LatencyMeter("stub_tts");

        public bool IsSpeaking { get; private set; }
        public LatencyMeter SpeakLatency => _speakMeter;
        public event Action Started;
        public event Action Finished;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.85f;
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            Stop();
            if (string.IsNullOrWhiteSpace(text))
                return;

            IsSpeaking = true;
            Started?.Invoke();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            float duration = ToneSynthesizer.EstimateSpeechSeconds(text, secondsPerWord, minSeconds);
            float freq = ToneSynthesizer.PitchForText(text);
            Debug.Log($"[StubTTS] Speaking ({duration:0.00}s @ {freq:0}Hz): {text}");

            float t0 = Time.realtimeSinceStartup;
            try
            {
                if (playTone && _source != null)
                {
                    var clip = ToneSynthesizer.CreateTone(freq, duration, sampleRate, toneAmplitude);
                    _source.clip = clip;
                    _source.Play();
                }

                await Task.Delay(TimeSpan.FromSeconds(duration), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                float elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;
                _speakMeter.RecordMs(elapsedMs);
                if (_source != null && _source.isPlaying)
                    _source.Stop();
                IsSpeaking = false;
                Finished?.Invoke();
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            if (_source != null && _source.isPlaying)
                _source.Stop();
            IsSpeaking = false;
        }

        void OnDestroy() => _cts?.Cancel();
    }
}
