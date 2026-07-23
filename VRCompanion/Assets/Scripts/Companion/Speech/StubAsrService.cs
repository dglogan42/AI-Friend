using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Dev stub: after a short "listen" delay emits a canned transcript.
    /// Press Space / A button in play mode to trigger a fake utterance via CompanionController.
    /// </summary>
    public sealed class StubAsrService : MonoBehaviour, IAsrService
    {
        [SerializeField] float fakeListenSeconds = 1.2f;
        [SerializeField] string[] cannedPhrases =
        {
            "Hello, how are you?",
            "Can we go to the cafe?",
            "Wear something sexy",
            "Put on lingerie",
            "Fuck me doggy",
            "Show me the shop.",
            "Get naked",
            "Tell me a short story."
        };

        int _phraseIndex;
        CancellationTokenSource _cts;

        public bool IsListening { get; private set; }
        public event Action<string> PartialTranscript;
        public event Action<string> FinalTranscript;

        public async Task StartListeningAsync(CancellationToken ct = default)
        {
            if (IsListening)
                return;

            IsListening = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                string phrase = cannedPhrases[_phraseIndex++ % cannedPhrases.Length];
                PartialTranscript?.Invoke(phrase.Substring(0, Mathf.Min(6, phrase.Length)) + "…");
                await Task.Delay(TimeSpan.FromSeconds(fakeListenSeconds), _cts.Token);
                FinalTranscript?.Invoke(phrase);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                IsListening = false;
            }
        }

        public Task StopListeningAsync()
        {
            _cts?.Cancel();
            IsListening = false;
            return Task.CompletedTask;
        }

        void OnDestroy() => _cts?.Cancel();
    }
}
