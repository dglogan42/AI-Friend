using UnityEngine;
using VRCompanion.Speech;

namespace VRCompanion
{
    /// <summary>
    /// Drives a simple visual stand-in for companion expressions.
    /// Replace material tint / blend shapes with a real character later.
    /// </summary>
    public sealed class ExpressionController : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] float blendSpeed = 6f;

        ExpressionState _current = ExpressionState.Neutral;
        ExpressionState _target = ExpressionState.Neutral;
        MaterialPropertyBlock _block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public ExpressionState Current => _current;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        void Update()
        {
            float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
            _current.Intensity = Mathf.Lerp(_current.Intensity, _target.Intensity, t);
            if (_current.Id != _target.Id && _current.Intensity < 0.05f)
                _current.Id = _target.Id;

            ApplyVisual(_current);
        }

        public void SetExpression(ExpressionId id, float intensity = 1f)
        {
            _target = new ExpressionState(id, intensity);
            if (_current.Id != id)
                _current.Intensity = 0f;
        }

        public void SetExpression(ExpressionState state) => SetExpression(state.Id, state.Intensity);

        /// <summary>
        /// Approximates a discrete ExpressionId from continuous live blendshapes
        /// (webcam/MediaPipe or, later, VIVE facial tracking). Placeholder mapping
        /// until the primitive stand-in is replaced by a real blendshape-driven mesh,
        /// at which point this should drive skinned-mesh blendshapes directly instead.
        /// </summary>
        public void ApplyBlendshapeFrame(FaceBlendshapeFrame frame)
        {
            if (!frame.FaceFound)
                return;

            float smile = Mathf.Max(frame.Get("mouthSmileLeft"), frame.Get("mouthSmileRight"));
            float jawOpen = frame.Get("jawOpen");
            float browDown = Mathf.Max(frame.Get("browDownLeft"), frame.Get("browDownRight"));
            float browUp = Mathf.Max(frame.Get("browOuterUpLeft"), frame.Get("browOuterUpRight"));

            if (jawOpen > 0.4f && smile > 0.35f)
                SetExpression(ExpressionId.Laughing, Mathf.Max(jawOpen, smile));
            else if (jawOpen > 0.4f)
                SetExpression(ExpressionId.Surprised, jawOpen);
            else if (smile > 0.35f)
                SetExpression(ExpressionId.Happy, smile);
            else if (browDown > 0.35f)
                SetExpression(ExpressionId.Sad, browDown);
            else if (browUp > 0.35f)
                SetExpression(ExpressionId.Curious, browUp);
            else
                SetExpression(ExpressionId.Neutral, 0.3f);
        }

        Color ColorFor(ExpressionId id)
        {
            return id switch
            {
                ExpressionId.Happy => new Color(1f, 0.85f, 0.2f),
                ExpressionId.Curious => new Color(0.4f, 0.9f, 1f),
                ExpressionId.Listening => new Color(0.5f, 0.75f, 1f),
                ExpressionId.Thinking => new Color(0.7f, 0.55f, 1f),
                ExpressionId.Speaking => new Color(0.35f, 1f, 0.55f),
                ExpressionId.Surprised => new Color(1f, 0.55f, 0.2f),
                ExpressionId.Sad => new Color(0.45f, 0.55f, 0.85f),
                ExpressionId.Excited => new Color(1f, 0.45f, 0.65f),
                ExpressionId.Playful => new Color(1f, 0.6f, 0.85f),
                ExpressionId.Sleepy => new Color(0.6f, 0.6f, 0.75f),
                ExpressionId.Confused => new Color(0.9f, 0.75f, 0.3f),
                ExpressionId.Laughing => new Color(1f, 0.75f, 0.1f),
                ExpressionId.Bored => new Color(0.55f, 0.55f, 0.55f),
                ExpressionId.Determined => new Color(0.9f, 0.3f, 0.25f),
                ExpressionId.Embarrassed => new Color(1f, 0.6f, 0.7f),
                _ => new Color(0.85f, 0.85f, 0.9f)
            };
        }

        void ApplyVisual(ExpressionState state)
        {
            if (targetRenderer == null)
                return;

            Color c = Color.Lerp(ColorFor(ExpressionId.Neutral), ColorFor(state.Id), state.Intensity);
            targetRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            _block.SetColor(ColorId, c);
            targetRenderer.SetPropertyBlock(_block);
        }
    }
}
