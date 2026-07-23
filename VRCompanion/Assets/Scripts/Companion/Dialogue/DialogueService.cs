using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCompanion.Dialogue
{
    /// <summary>
    /// Lightweight rule-based replies. Replace with a local / remote LLM later.
    /// </summary>
    public sealed class DialogueService : MonoBehaviour
    {
        [SerializeField] float thinkSeconds = 0.4f;

        public async Task<DialogueReply> ReplyAsync(string userText, string sceneId, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(thinkSeconds), ct);
            string text = (userText ?? string.Empty).Trim().ToLowerInvariant();

            if (text.Contains("cafe") || text.Contains("café") || text.Contains("coffee"))
            {
                return new DialogueReply(
                    "Sure — let's visit the café. I can almost smell the espresso.",
                    ExpressionId.Happy,
                    CompanionSceneId.Cafe);
            }

            if (text.Contains("shop") || text.Contains("store") || text.Contains("buy"))
            {
                return new DialogueReply(
                    "Okay! The shop is this way. Want me to show you something cute?",
                    ExpressionId.Curious,
                    CompanionSceneId.Shop);
            }

            if (text.Contains("hello") || text.Contains("hi ") || text == "hi")
            {
                return new DialogueReply(
                    "Hi! I'm your VR companion. Talk to me, or ask to visit the café or shop.",
                    ExpressionId.Happy,
                    null);
            }

            if (text.Contains("story"))
            {
                return new DialogueReply(
                    "Once, a cat-eared companion waited in a quiet arcade… until someone said hello.",
                    ExpressionId.Speaking,
                    null);
            }

            string sceneHint = string.IsNullOrEmpty(sceneId) ? "here" : sceneId;
            return new DialogueReply(
                $"I'm listening ({sceneHint}). You said: \"{userText}\". Try asking about the café or shop.",
                ExpressionId.Curious,
                null);
        }
    }

    public readonly struct DialogueReply
    {
        public readonly string Text;
        public readonly ExpressionId Expression;
        public readonly CompanionSceneId? SwitchToScene;

        public DialogueReply(string text, ExpressionId expression, CompanionSceneId? switchToScene)
        {
            Text = text;
            Expression = expression;
            SwitchToScene = switchToScene;
        }
    }

    public enum CompanionSceneId
    {
        Hub = 0,
        Cafe,
        Shop
    }
}
