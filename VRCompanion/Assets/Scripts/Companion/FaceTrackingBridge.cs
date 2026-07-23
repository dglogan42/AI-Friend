using System;
using UnityEngine;
using VRCompanion.Speech;

namespace VRCompanion
{
    /// <summary>
    /// Wires whichever IFaceTrackingSource is available into ExpressionController.
    /// Live tracking (when present) overrides dialogue-driven expressions; when no
    /// source is available/streaming, CompanionController's existing dialogue-based
    /// SetExpression calls are untouched.
    /// </summary>
    public sealed class FaceTrackingBridge : MonoBehaviour
    {
        [SerializeField] ExpressionController expression;
        [SerializeField] MonoBehaviour[] sourceBehaviours;

        IFaceTrackingSource[] _sources = Array.Empty<IFaceTrackingSource>();
        bool _subscribed;

        /// <summary>Explicit wiring for runtime-built scenes (see CompanionBootstrap).</summary>
        public void Configure(ExpressionController target, params MonoBehaviour[] sources)
        {
            Unsubscribe();
            expression = target;
            sourceBehaviours = sources;
            BuildSources();
            Subscribe();
        }

        void Awake()
        {
            if (expression == null)
                expression = GetComponentInChildren<ExpressionController>();
            BuildSources();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void BuildSources()
        {
            _sources = new IFaceTrackingSource[sourceBehaviours?.Length ?? 0];
            for (int i = 0; i < _sources.Length; i++)
                _sources[i] = sourceBehaviours[i] as IFaceTrackingSource;
        }

        void Subscribe()
        {
            if (_subscribed || _sources == null)
                return;
            foreach (var s in _sources)
                if (s != null)
                    s.BlendshapesUpdated += OnBlendshapes;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed || _sources == null)
                return;
            foreach (var s in _sources)
                if (s != null)
                    s.BlendshapesUpdated -= OnBlendshapes;
            _subscribed = false;
        }

        void OnBlendshapes(FaceBlendshapeFrame frame) => expression?.ApplyBlendshapeFrame(frame);
    }
}
