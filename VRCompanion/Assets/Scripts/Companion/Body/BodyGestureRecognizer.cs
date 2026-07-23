using System.Collections.Generic;
using UnityEngine;

namespace VRCompanion.Body
{
    /// <summary>
    /// Pure joint-based gesture recognition (no camera ML). Works on BodyPoseFrame.
    /// </summary>
    public static class BodyGestureRecognizer
    {
        public const string HandsUp = "hands_up";
        public const string TPose = "t_pose";
        public const string LeanLeft = "lean_left";
        public const string LeanRight = "lean_right";

        public static Dictionary<string, float> Evaluate(BodyPoseFrame frame)
        {
            var scores = new Dictionary<string, float>();
            if (!frame.BodyFound)
                return scores;

            if (!frame.TryGetJoint(BodyJoint.Head, out var headJ) ||
                !frame.TryGetJoint(BodyJoint.SpineBase, out var hipsJ) ||
                !frame.TryGetJoint(BodyJoint.ShoulderLeft, out var shLJ) ||
                !frame.TryGetJoint(BodyJoint.ShoulderRight, out var shRJ) ||
                !frame.TryGetJoint(BodyJoint.WristLeft, out var wrLJ) ||
                !frame.TryGetJoint(BodyJoint.WristRight, out var wrRJ))
                return scores;

            if (headJ.TrackingConfidence < 0.15f || hipsJ.TrackingConfidence < 0.15f)
                return scores;

            var head = headJ.Position;
            var hips = hipsJ.Position;
            var shL = shLJ.Position;
            var shR = shRJ.Position;
            var wrL = wrLJ.Position;
            var wrR = wrRJ.Position;

            float shoulderY = (shL.y + shR.y) * 0.5f;
            float handsUp = 0f;
            if (wrL.y > shoulderY + 0.12f) handsUp += 0.5f;
            if (wrR.y > shoulderY + 0.12f) handsUp += 0.5f;
            scores[HandsUp] = Mathf.Clamp01(handsUp);

            float armSpan = Vector3.Distance(wrL, wrR);
            float shoulderSpan = Mathf.Max(0.05f, Vector3.Distance(shL, shR));
            float wristYAlign = 1f - Mathf.Clamp01(Mathf.Abs(wrL.y - wrR.y) / 0.25f);
            float tpose = Mathf.Clamp01((armSpan / (shoulderSpan * 2.2f)) * wristYAlign);
            float heightMatch = 1f - Mathf.Clamp01((Mathf.Abs(wrL.y - shoulderY) + Mathf.Abs(wrR.y - shoulderY)) * 0.5f / 0.2f);
            scores[TPose] = Mathf.Clamp01(tpose * heightMatch);

            float lean = head.x - hips.x;
            scores[LeanLeft] = Mathf.Clamp01((-lean - 0.05f) / 0.15f);
            scores[LeanRight] = Mathf.Clamp01((lean - 0.05f) / 0.15f);

            return scores;
        }
    }
}
