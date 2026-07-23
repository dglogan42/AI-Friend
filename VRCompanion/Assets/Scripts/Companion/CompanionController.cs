using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VRCompanion.Dialogue;
using VRCompanion.Scenes;
using VRCompanion.Speech;
using VRCompanion.Singing;
using VRCompanion.Outfits;
using VRCompanion.Intimacy;

namespace VRCompanion
{
    /// <summary>
    /// Main loop: listen → understand → express → speak → optional scene / outfit / explicit act.
    /// Works with stub ASR/TTS in Editor; swap interfaces for local models later.
    /// </summary>
    public sealed class CompanionController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] ExpressionController expression;
        [SerializeField] DialogueService dialogue;
        [SerializeField] SceneSwitcher scenes;
        [SerializeField] OutfitController outfits;
        [SerializeField] ExplicitInteractionController explicitActs;
        [SerializeField] MonoBehaviour asrBehaviour;
        [SerializeField] MonoBehaviour ttsBehaviour;
        [SerializeField] MonoBehaviour realtimeBehaviour;
        [SerializeField] SingingRaterService singingRater;

        [Header("Input")]
        [SerializeField] Key pushToTalkKey = Key.Space;
        [SerializeField] Key singKey = Key.K;
        [SerializeField] Key cycleOutfitKey = Key.O;
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
            if (outfits == null)
                outfits = GetComponent<OutfitController>() ?? gameObject.AddComponent<OutfitController>();
            if (explicitActs == null)
                explicitActs = GetComponent<ExplicitInteractionController>()
                    ?? gameObject.AddComponent<ExplicitInteractionController>();
        }

        async void Start()
        {
            _loopCts = new CancellationTokenSource();
            if (autoGreetOnStart)
            {
                expression?.SetExpression(ExpressionId.Flirty);
                await _tts.SpeakAsync(
                    "Hi! I'm your companion. Press Space to talk — we can keep it sweet or get intimate.",
                    _loopCts.Token);
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
            if (keyboard[cycleOutfitKey].wasPressedThisFrame && !_busy && outfits != null)
                CycleOutfitHotkey();
        }

        void CycleOutfitHotkey()
        {
            if (outfits == null)
                return;
            int next = ((int)outfits.Current + 1) % System.Enum.GetValues(typeof(OutfitId)).Length;
            if (!outfits.TrySetOutfit((OutfitId)next))
                outfits.TrySetOutfit(OutfitId.Default);
            expression?.SetExpression(ExpressionId.Flirty);
            Debug.Log($"[CompanionController] Outfit hotkey → {outfits.Current}");
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

                if (reply.Outfit.HasValue && outfits != null)
                    outfits.TrySetOutfit(reply.Outfit.Value);

                expression?.SetExpression(reply.Expression);
                await _tts.SpeakAsync(reply.Text, ct);

                // Full multi-step explicit scene after the lead-in line.
                if (reply.Act.HasValue && reply.Act.Value != ExplicitAct.None && explicitActs != null)
                    await explicitActs.RunAsync(reply.Act.Value, ct);

                expression?.SetExpression(ExpressionId.Intimate, 0.35f);
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
