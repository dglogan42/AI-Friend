using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VRCompanion.Dialogue;
using VRCompanion.Scenes;
using VRCompanion.Speech;
using VRCompanion.Singing;

namespace VRCompanion
{
    /// <summary>
    /// Main loop: listen → understand → express → speak → optional scene change.
    /// Works with stub ASR/TTS in Editor; swap interfaces for local models later.
    /// </summary>
    public sealed class CompanionController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] ExpressionController expression;
        [SerializeField] DialogueService dialogue;
        [SerializeField] SceneSwitcher scenes;
        [SerializeField] MonoBehaviour asrBehaviour;
        [SerializeField] MonoBehaviour ttsBehaviour;
        [SerializeField] MonoBehaviour realtimeBehaviour;
        [SerializeField] SingingRaterService singingRater;

        [Header("Input")]
        [SerializeField] Key pushToTalkKey = Key.Space;
        [SerializeField] Key singKey = Key.K;
        [SerializeField] bool autoGreetOnStart = true;

        IAsrService _asr;
        ITtsService _tts;
        IRealtimeConversationService _realtime;
        CancellationTokenSource _loopCts;
        bool _busy;

        public bool IsBusy => _busy;

        void Awake()
        {
            _asr = asrBehaviour as IAsrService ?? GetComponent<IAsrService>() ?? gameObject.AddComponent<StubAsrService>();
            _tts = ttsBehaviour as ITtsService ?? GetComponent<ITtsService>() ?? gameObject.AddComponent<StubTtsService>();
            _realtime = realtimeBehaviour as IRealtimeConversationService ?? GetComponent<IRealtimeConversationService>();
            if (expression == null)
                expression = GetComponentInChildren<ExpressionController>();
            if (dialogue == null)
                dialogue = GetComponent<DialogueService>() ?? gameObject.AddComponent<DialogueService>();
            if (scenes == null)
                scenes = FindFirstObjectByType<SceneSwitcher>();
            if (singingRater == null)
                singingRater = GetComponent<SingingRaterService>();
        }

        async void Start()
        {
            _loopCts = new CancellationTokenSource();
            if (autoGreetOnStart)
            {
                expression?.SetExpression(ExpressionId.Happy);
                await _tts.SpeakAsync("Hi! I'm your companion. Press Space to talk.", _loopCts.Token);
                expression?.SetExpression(ExpressionId.Neutral, 0.2f);
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[pushToTalkKey].wasPressedThisFrame && !_busy)
                _ = RunTurnAsync();
            if (keyboard[singKey].wasPressedThisFrame && !_busy && singingRater != null)
                _ = RunSingingChallengeAsync();
        }

        public async Task RunSingingChallengeAsync()
        {
            if (_busy || singingRater == null)
                return;

            _busy = true;
            var ct = _loopCts?.Token ?? CancellationToken.None;
            try
            {
                expression?.SetExpression(ExpressionId.Curious);
                var result = await singingRater.RunChallengeAsync(ct);

                expression?.SetExpression(SingingRaterService.ExpressionFor(result));
                await _tts.SpeakAsync(SingingRaterService.FeedbackFor(result), ct);
                expression?.SetExpression(ExpressionId.Neutral, 0.25f);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
            finally
            {
                _busy = false;
            }
        }

        public async Task RunTurnAsync()
        {
            if (_busy)
                return;

            _busy = true;
            var ct = _loopCts?.Token ?? CancellationToken.None;
            try
            {
                if (_realtime != null)
                {
                    expression?.SetExpression(ExpressionId.Listening);
                    string heardByRealtime = await _realtime.RunTurnAsync(ct);
                    expression?.SetExpression(ExpressionId.Neutral, 0.25f);
                    if (!string.IsNullOrWhiteSpace(heardByRealtime))
                        Debug.Log($"[CompanionController] (realtime) heard: {heardByRealtime}");
                    return;
                }

                expression?.SetExpression(ExpressionId.Listening);

                // Subscribe before listening so stub ASR final events are not missed.
                var heardTask = CaptureNextFinalTranscript(ct);
                await _asr.StartListeningAsync(ct);
                string heard = await heardTask;

                if (string.IsNullOrWhiteSpace(heard))
                {
                    expression?.SetExpression(ExpressionId.Curious);
                    await _tts.SpeakAsync("I didn't catch that.", ct);
                    return;
                }

                expression?.SetExpression(ExpressionId.Thinking);
                string sceneName = scenes != null ? scenes.CurrentDisplayName : "hub";
                var reply = await dialogue.ReplyAsync(heard, sceneName, ct);

                if (reply.SwitchToScene.HasValue && scenes != null)
                    scenes.SwitchTo(reply.SwitchToScene.Value);

                expression?.SetExpression(reply.Expression);
                await _tts.SpeakAsync(reply.Text, ct);
                expression?.SetExpression(ExpressionId.Neutral, 0.25f);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
            finally
            {
                _busy = false;
            }
        }

        Task<string> CaptureNextFinalTranscript(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<string>();
            void Handler(string text)
            {
                _asr.FinalTranscript -= Handler;
                tcs.TrySetResult(text ?? string.Empty);
            }

            _asr.FinalTranscript += Handler;
            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    _asr.FinalTranscript -= Handler;
                    tcs.TrySetCanceled(ct);
                });
            }

            return tcs.Task;
        }

        void OnDestroy()
        {
            _loopCts?.Cancel();
            _tts?.Stop();
        }
    }
}
