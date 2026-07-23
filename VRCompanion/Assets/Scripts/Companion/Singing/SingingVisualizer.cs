using UnityEngine;

namespace VRCompanion.Singing
{
    /// <summary>
    /// Draws the player's mic input as a live oscillating waveform while the singing
    /// challenge is recording, using a <see cref="LineRenderer"/> in front of the companion.
    /// Flattens to a still line whenever there's no active challenge.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SingingVisualizer : MonoBehaviour
    {
        [SerializeField] SingingRaterService rater;
        [SerializeField] LineRenderer line;
        [SerializeField] int pointCount = 64;
        [SerializeField] float width = 1f;
        [SerializeField] float amplitudeScale = 0.3f;

        void Awake()
        {
            if (line == null)
                line = GetComponent<LineRenderer>();
            if (rater == null)
                rater = GetComponentInParent<SingingRaterService>();

            line.positionCount = Mathf.Max(2, pointCount);
            line.useWorldSpace = false;
            Flatten();
        }

        void OnEnable()
        {
            if (rater != null)
                rater.LiveAudioFrame += HandleLiveAudioFrame;
        }

        void OnDisable()
        {
            if (rater != null)
                rater.LiveAudioFrame -= HandleLiveAudioFrame;
            Flatten();
        }

        void HandleLiveAudioFrame(float[] samples, float detectedHz)
        {
            var points = WaveformVisualizer.Downsample(samples, line.positionCount);
            for (int i = 0; i < points.Length; i++)
                line.SetPosition(i, PointAt(i, points[i]));
        }

        void Flatten()
        {
            if (line == null)
                return;
            for (int i = 0; i < line.positionCount; i++)
                line.SetPosition(i, PointAt(i, 0f));
        }

        Vector3 PointAt(int index, float amplitude)
        {
            float x = (index / (float)(line.positionCount - 1) - 0.5f) * width;
            return new Vector3(x, amplitude * amplitudeScale, 0f);
        }
    }
}
