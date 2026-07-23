using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Dev stub: logs speech and waits proportional to text length (no audio).
    /// </summary>
    public sealed class StubTtsService : MonoBehaviour, ITtsService
    {
        [SerializeField] float secondsPerWord = 0.28f;
        [SerializeField] float minSeconds = 0.6f;

        CancellationTokenSource _cts;

        public bool IsSpeaking { get; private set; }
        public event Action Started;
        public event Action Finished;

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            Stop();
            if (string.IsNullOrWhiteSpace(text))
                return;

            IsSpeaking = true;
            Started?.Invoke();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            int words = Mathf.Max(1, text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length);
            float duration = Mathf.Max(minSeconds, words * secondsPerWord);
            Debug.Log($"[StubTTS] Speaking ({duration:0.0}s): {text}");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(duration), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                IsSpeaking = false;
                Finished?.Invoke();
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            IsSpeaking = false;
        }

        void OnDestroy() => _cts?.Cancel();
    }
}
