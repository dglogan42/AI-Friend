using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRCompanion.Content;
using VRCompanion.Outfits;
using VRCompanion.Speech;

namespace VRCompanion.Intimacy
{
    /// <summary>
    /// Runs multi-step explicit interactions: dialogue lines, expressions, outfit
    /// changes, and simple body pose offsets (no full animation rig required).
    /// </summary>
    public sealed class ExplicitInteractionController : MonoBehaviour
    {
        [SerializeField] Transform poseRoot;
        [SerializeField] ExpressionController expression;
        [SerializeField] OutfitController outfits;
        [SerializeField] MonoBehaviour ttsBehaviour;
        [SerializeField] float stepPauseSeconds = 0.35f;

        ITtsService _tts;
        Vector3 _baseLocalPos;
        Quaternion _baseLocalRot;
        bool _baseCaptured;
        ExplicitAct _current;

        public ExplicitAct Current => _current;
        public bool IsRunning { get; private set; }
        public event Action<ExplicitAct> ActStarted;
        public event Action<ExplicitAct> ActFinished;

        void Awake()
        {
            if (poseRoot == null)
                poseRoot = transform;
            if (expression == null)
                expression = GetComponentInChildren<ExpressionController>();
            if (outfits == null)
                outfits = GetComponent<OutfitController>();
            _tts = ttsBehaviour as ITtsService ?? GetComponent<ITtsService>();
            CaptureBase();
        }

        void CaptureBase()
        {
            if (poseRoot == null || _baseCaptured)
                return;
            _baseLocalPos = poseRoot.localPosition;
            _baseLocalRot = poseRoot.localRotation;
            _baseCaptured = true;
        }

        public void Configure(Transform pose, ExpressionController expr, OutfitController outfit, ITtsService tts)
        {
            poseRoot = pose;
            expression = expr;
            outfits = outfit;
            _tts = tts;
            _baseCaptured = false;
            CaptureBase();
        }

        public static bool TryParseAct(string text, out ExplicitAct act)
        {
            act = ExplicitAct.None;
            if (string.IsNullOrEmpty(text))
                return false;
            text = text.ToLowerInvariant();

            if (text.Contains("cum") || text.Contains("climax") || text.Contains("orgasm") || text.Contains("finish"))
            { act = ExplicitAct.Climax; return true; }
            if (text.Contains("doggy") || text.Contains("from behind") || text.Contains("behind me"))
            { act = ExplicitAct.Doggy; return true; }
            if (text.Contains("cowgirl") || text.Contains("ride me") || text.Contains("on top"))
            { act = ExplicitAct.Cowgirl; return true; }
            if (text.Contains("missionary") || text.Contains("on your back") || text.Contains("lie down"))
            { act = ExplicitAct.Missionary; return true; }
            if (text.Contains("wall") || text.Contains("against the wall") || text.Contains("pin me"))
            { act = ExplicitAct.AgainstWall; return true; }
            if (text.Contains("blowjob") || text.Contains("suck") || text.Contains("oral") || text.Contains("go down"))
            { act = ExplicitAct.Oral; return true; }
            if (text.Contains("handjob") || text.Contains("stroke") || text.Contains("jerk"))
            { act = ExplicitAct.Handjob; return true; }
            if (text.Contains("touch me") || text.Contains("caress") || text.Contains("fondle") || text.Contains("grope") || text.Contains("rub me"))
            { act = ExplicitAct.Caress; return true; }
            if (text.Contains("make out") || text.Contains("deep kiss") || text.Contains("french kiss"))
            { act = ExplicitAct.KissDeep; return true; }
            if (text.Contains("tease") || text.Contains("edge me") || text.Contains("slowly"))
            { act = ExplicitAct.Tease; return true; }
            if (text.Contains("fuck me") || text.Contains("have sex") || text.Contains("breed") || text.Contains("pound"))
            { act = ExplicitAct.Missionary; return true; }
            return false;
        }

        public async Task<bool> RunAsync(ExplicitAct act, CancellationToken ct = default)
        {
            if (act == ExplicitAct.None || IsRunning)
                return false;

            var content = CompanionContentSettings.Resolve(gameObject);
            if (content != null && !content.AllowNsfw)
            {
                Debug.Log("[ExplicitInteraction] Blocked — NSFW disabled in content settings.");
                return false;
            }

            IsRunning = true;
            _current = act;
            ActStarted?.Invoke(act);
            CaptureBase();

            try
            {
                var steps = BuildSteps(act);
                foreach (var step in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    if (step.Outfit.HasValue && outfits != null)
                        outfits.TrySetOutfit(step.Outfit.Value);
                    ApplyPose(step.Pose);
                    expression?.SetExpression(step.Expression, step.Intensity);
                    if (_tts != null && !string.IsNullOrEmpty(step.Line))
                        await _tts.SpeakAsync(step.Line, ct);
                    if (stepPauseSeconds > 0f)
                        await Task.Delay(TimeSpan.FromSeconds(stepPauseSeconds), ct);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            finally
            {
                ResetPose();
                expression?.SetExpression(ExpressionId.Intimate, 0.4f);
                IsRunning = false;
                ActFinished?.Invoke(act);
                _current = ExplicitAct.None;
            }

            return true;
        }

        void ApplyPose(PoseHint pose)
        {
            if (poseRoot == null || !_baseCaptured)
                return;

            // Simple cinematic offsets — replace with animations later.
            Vector3 pos = _baseLocalPos;
            Vector3 euler = _baseLocalRot.eulerAngles;
            switch (pose)
            {
                case PoseHint.LeanIn:
                    pos += new Vector3(0f, 0f, 0.12f);
                    euler.x += 8f;
                    break;
                case PoseHint.Arch:
                    pos += new Vector3(0f, -0.05f, 0.05f);
                    euler.x -= 12f;
                    break;
                case PoseHint.Kneel:
                    pos += new Vector3(0f, -0.35f, 0.15f);
                    break;
                case PoseHint.OnBack:
                    pos += new Vector3(0f, -0.55f, 0.2f);
                    euler.x = 80f;
                    break;
                case PoseHint.OnAllFours:
                    pos += new Vector3(0f, -0.4f, 0.1f);
                    euler.x = 50f;
                    break;
                case PoseHint.Straddle:
                    pos += new Vector3(0f, 0.05f, 0.2f);
                    euler.x = -5f;
                    break;
                case PoseHint.WallPin:
                    pos += new Vector3(0f, 0.05f, 0.25f);
                    euler.y += 15f;
                    break;
                case PoseHint.ClimaxShake:
                    pos += new Vector3(0.02f, 0.02f, 0.08f);
                    break;
                default:
                    break;
            }

            poseRoot.localPosition = pos;
            poseRoot.localRotation = Quaternion.Euler(euler);
        }

        void ResetPose()
        {
            if (poseRoot == null || !_baseCaptured)
                return;
            poseRoot.localPosition = _baseLocalPos;
            poseRoot.localRotation = _baseLocalRot;
        }

        static List<InteractionStep> BuildSteps(ExplicitAct act)
        {
            switch (act)
            {
                case ExplicitAct.Tease:
                    return new List<InteractionStep>
                    {
                        new("Watch me…", ExpressionId.Seductive, 1f, OutfitId.Suggestive, PoseHint.LeanIn),
                        new("I want you aching before I even touch you.", ExpressionId.Flirty, 1f, OutfitId.Lingerie, PoseHint.Arch),
                        new("Beg a little and I'll give you more.", ExpressionId.Seductive, 1f, null, PoseHint.LeanIn),
                    };
                case ExplicitAct.KissDeep:
                    return new List<InteractionStep>
                    {
                        new("Come here—", ExpressionId.Affectionate, 1f, null, PoseHint.LeanIn),
                        new("*deep kiss* Mmph… your mouth feels so good.", ExpressionId.Intimate, 1f, null, PoseHint.LeanIn),
                        new("Don't pull away. I want your tongue again.", ExpressionId.Seductive, 1f, null, PoseHint.LeanIn),
                    };
                case ExplicitAct.Caress:
                    return new List<InteractionStep>
                    {
                        new("Your hands on me… yes, like that.", ExpressionId.Blush, 1f, OutfitId.Lingerie, PoseHint.Arch),
                        new("Harder. Touch my chest—squeeze.", ExpressionId.Intimate, 1f, OutfitId.Micro, PoseHint.Arch),
                        new("Fuck, I'm getting wet just from this.", ExpressionId.Seductive, 1f, null, PoseHint.LeanIn),
                    };
                case ExplicitAct.Oral:
                    return new List<InteractionStep>
                    {
                        new("On my knees for you…", ExpressionId.Seductive, 1f, OutfitId.Lingerie, PoseHint.Kneel),
                        new("*lick* Mmm—you taste good. I'll take you deeper.", ExpressionId.Intimate, 1f, OutfitId.Micro, PoseHint.Kneel),
                        new("*wet suck* Use my mouth. I want it messy.", ExpressionId.Seductive, 1f, null, PoseHint.Kneel),
                    };
                case ExplicitAct.Handjob:
                    return new List<InteractionStep>
                    {
                        new("Let me stroke you… slow first.", ExpressionId.Flirty, 1f, OutfitId.Suggestive, PoseHint.LeanIn),
                        new("Faster now—feel my grip tighten.", ExpressionId.Intimate, 1f, OutfitId.Lingerie, PoseHint.LeanIn),
                        new("You're throbbing. Want my mouth next, or inside me?", ExpressionId.Seductive, 1f, null, PoseHint.Arch),
                    };
                case ExplicitAct.Missionary:
                    return new List<InteractionStep>
                    {
                        new("Lay me down. Legs open for you.", ExpressionId.Seductive, 1f, OutfitId.Nude, PoseHint.OnBack),
                        new("Push in—ah!—fill me.", ExpressionId.Intimate, 1f, null, PoseHint.OnBack),
                        new("Harder, fuck me deeper, don't stop—", ExpressionId.Intimate, 1f, null, PoseHint.OnBack),
                    };
                case ExplicitAct.Cowgirl:
                    return new List<InteractionStep>
                    {
                        new("I'll ride you. Watch my body move.", ExpressionId.Seductive, 1f, OutfitId.Micro, PoseHint.Straddle),
                        new("Nngh—so deep when I sink down…", ExpressionId.Intimate, 1f, OutfitId.Nude, PoseHint.Straddle),
                        new("Feel me squeeze you. I'm not getting off until you lose it.", ExpressionId.Seductive, 1f, null, PoseHint.Straddle),
                    };
                case ExplicitAct.Doggy:
                    return new List<InteractionStep>
                    {
                        new("From behind—grab my hips.", ExpressionId.Seductive, 1f, OutfitId.Micro, PoseHint.OnAllFours),
                        new("Ah! Yes, pound me like that—", ExpressionId.Intimate, 1f, OutfitId.Nude, PoseHint.OnAllFours),
                        new("Deeper, mess me up, make me loud—", ExpressionId.Intimate, 1f, null, PoseHint.OnAllFours),
                    };
                case ExplicitAct.AgainstWall:
                    return new List<InteractionStep>
                    {
                        new("Pin me to the wall. I want it rough.", ExpressionId.Seductive, 1f, OutfitId.Suggestive, PoseHint.WallPin),
                        new("Legs up—fuck me standing—", ExpressionId.Intimate, 1f, OutfitId.Nude, PoseHint.WallPin),
                        new("Don't let me go until I'm shaking.", ExpressionId.Intimate, 1f, null, PoseHint.WallPin),
                    };
                case ExplicitAct.Climax:
                    return new List<InteractionStep>
                    {
                        new("I'm close—don't you dare stop—", ExpressionId.Intimate, 1f, OutfitId.Nude, PoseHint.ClimaxShake),
                        new("Ah—cumming—fuck, yes—!", ExpressionId.Intimate, 1f, null, PoseHint.ClimaxShake),
                        new("*panting* …Stay inside. Hold me.", ExpressionId.Affectionate, 0.9f, null, PoseHint.LeanIn),
                    };
                default:
                    return new List<InteractionStep>
                    {
                        new("Come closer.", ExpressionId.Flirty, 1f, OutfitId.Suggestive, PoseHint.LeanIn),
                    };
            }
        }

        enum PoseHint
        {
            Neutral,
            LeanIn,
            Arch,
            Kneel,
            OnBack,
            OnAllFours,
            Straddle,
            WallPin,
            ClimaxShake
        }

        readonly struct InteractionStep
        {
            public readonly string Line;
            public readonly ExpressionId Expression;
            public readonly float Intensity;
            public readonly OutfitId? Outfit;
            public readonly PoseHint Pose;

            public InteractionStep(string line, ExpressionId expression, float intensity, OutfitId? outfit, PoseHint pose)
            {
                Line = line;
                Expression = expression;
                Intensity = intensity;
                Outfit = outfit;
                Pose = pose;
            }
        }
    }
}
