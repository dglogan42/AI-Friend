using UnityEngine;
using VRCompanion.Speech;

namespace VRCompanion
{
    /// <summary>
    /// Drives companion expressions: real VRM face blend shapes when a compatible
    /// SkinnedMeshRenderer is present (see <see cref="FaceExpressionShapes"/>), falling
    /// back to a material color tint for the primitive stand-in otherwise.
    /// </summary>
    public sealed class ExpressionController : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] SkinnedMeshRenderer faceMesh;
        [SerializeField] float blendSpeed = 6f;

        ExpressionState _current = ExpressionState.Neutral;
        ExpressionState _target = ExpressionState.Neutral;
        MaterialPropertyBlock _block;
        int[] _faceShapeIndices;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public ExpressionState Current => _current;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
            if (faceMesh == null)
                faceMesh = FindFaceMesh();

            if (faceMesh != null)
            {
                _faceShapeIndices = new int[FaceExpressionShapes.AllShapes.Length];
                for (int i = 0; i < _faceShapeIndices.Length; i++)
                    _faceShapeIndices[i] = faceMesh.sharedMesh.GetBlendShapeIndex(FaceExpressionShapes.AllShapes[i]);
            }
        }

        SkinnedMeshRenderer FindFaceMesh()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null && smr.sharedMesh.GetBlendShapeIndex(FaceExpressionShapes.Neutral) >= 0)
                    return smr;
            }
            return null;
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
        /// (webcam/MediaPipe or, later, VIVE facial tracking), then routes it through
        /// the same SetExpression path used everywhere else — which now drives the
        /// VRM face mesh's blend shapes directly when one is present.
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
                ExpressionId.Affectionate => new Color(1f, 0.55f, 0.65f),
                ExpressionId.Flirty => new Color(1f, 0.4f, 0.75f),
                ExpressionId.Blush => new Color(1f, 0.5f, 0.55f),
                ExpressionId.Seductive => new Color(0.85f, 0.25f, 0.55f),
                ExpressionId.Intimate => new Color(0.95f, 0.35f, 0.5f),
                _ => new Color(0.85f, 0.85f, 0.9f)
            };
        }

        void ApplyVisual(ExpressionState state)
        {
            if (faceMesh != null)
                ApplyFaceBlendShapes(state);
            else
                ApplyColorTint(state);
        }

        void ApplyFaceBlendShapes(ExpressionState state)
        {
            string targetShape = FaceExpressionShapes.ShapeFor(state.Id);
            bool targetIsNeutral = targetShape == FaceExpressionShapes.Neutral;
            float targetWeight = targetIsNeutral ? 100f : state.Intensity * 100f;
            float neutralWeight = targetIsNeutral ? 0f : (1f - state.Intensity) * 100f;

            for (int i = 0; i < _faceShapeIndices.Length; i++)
            {
                int index = _faceShapeIndices[i];
                if (index < 0)
                    continue;

                string shape = FaceExpressionShapes.AllShapes[i];
                float weight = shape == targetShape ? targetWeight
                    : shape == FaceExpressionShapes.Neutral ? neutralWeight
                    : 0f;
                faceMesh.SetBlendShapeWeight(index, weight);
            }
        }

        void ApplyColorTint(ExpressionState state)
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
