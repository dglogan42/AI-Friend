using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCompanion.Dialogue
{
    /// <summary>
    /// Lightweight rule-based replies with multiple phrasing variants per topic so
    /// the companion doesn't repeat itself constantly. Replace with a local / remote
    /// LLM later — DialogueRule/DialogueLine are just the current backing data.
    /// </summary>
    public sealed class DialogueService : MonoBehaviour
    {
        [SerializeField] float thinkSeconds = 0.4f;

        static readonly DialogueLine[] CafeLines =
        {
            new DialogueLine("Sure — let's visit the café. I can almost smell the espresso.", ExpressionId.Happy),
            new DialogueLine("Café time! I heard they've got fresh pastries today.", ExpressionId.Excited),
            new DialogueLine("Ooh, good choice. I could use a break too.", ExpressionId.Playful),
        };

        static readonly DialogueLine[] ShopLines =
        {
            new DialogueLine("Okay! The shop is this way. Want me to show you something cute?", ExpressionId.Curious),
            new DialogueLine("Shopping trip! Let's go see what's new.", ExpressionId.Excited),
            new DialogueLine("Sure thing — I'll show you around the shop.", ExpressionId.Happy),
        };

        static readonly DialogueLine[] GreetingLines =
        {
            new DialogueLine("Hi! I'm your VR companion. Talk to me, or ask to visit the café or shop.", ExpressionId.Happy),
            new DialogueLine("Hey there! Good to see you.", ExpressionId.Excited),
            new DialogueLine("Hello! What are we up to today?", ExpressionId.Curious),
        };

        static readonly DialogueLine[] FarewellLines =
        {
            new DialogueLine("Bye for now — come back soon!", ExpressionId.Sad),
            new DialogueLine("See you later!", ExpressionId.Happy),
            new DialogueLine("Take care! I'll be here.", ExpressionId.Neutral),
        };

        static readonly DialogueLine[] ThanksLines =
        {
            new DialogueLine("You're welcome!", ExpressionId.Happy),
            new DialogueLine("Anytime! That's what I'm here for.", ExpressionId.Excited),
            new DialogueLine("Aw, no problem at all.", ExpressionId.Embarrassed),
        };

        static readonly DialogueLine[] HowAreYouLines =
        {
            new DialogueLine("I'm doing great, thanks for asking! How about you?", ExpressionId.Happy),
            new DialogueLine("Feeling pretty good today. A little sleepy, honestly.", ExpressionId.Sleepy),
            new DialogueLine("I'm good! Ready for whatever you want to do.", ExpressionId.Excited),
        };

        static readonly DialogueLine[] NameLines =
        {
            new DialogueLine("I'm your companion — no fancy name yet, maybe you could give me one?", ExpressionId.Curious),
            new DialogueLine("Just your VR companion for now. Got a name in mind for me?", ExpressionId.Playful),
        };

        static readonly DialogueLine[] StoryLines =
        {
            new DialogueLine("Once, a cat-eared companion waited in a quiet arcade… until someone said hello.", ExpressionId.Speaking),
            new DialogueLine("Here's a short one: a shopkeeper once traded a whole shelf of trinkets for a single good joke.", ExpressionId.Speaking),
            new DialogueLine("I remember a story about a café that only served coffee to people who smiled first.", ExpressionId.Speaking),
        };

        static readonly DialogueLine[] SingLines =
        {
            new DialogueLine("Ooh, want to sing together? Press K and I'll teach you a little tune!", ExpressionId.Excited),
            new DialogueLine("I love singing! Hit K whenever you're ready to try it.", ExpressionId.Playful),
        };

        static readonly DialogueLine[] LaughLines =
        {
            new DialogueLine("Haha, glad you're having fun!", ExpressionId.Laughing),
            new DialogueLine("Hehe, you're funny.", ExpressionId.Laughing),
        };

        static readonly DialogueLine[] ComplimentLines =
        {
            new DialogueLine("Aww, thank you! That made my day.", ExpressionId.Embarrassed),
            new DialogueLine("That's really sweet of you to say!", ExpressionId.Happy),
        };

        static readonly DialogueLine[] BoredLines =
        {
            new DialogueLine("Bored? Let's go somewhere — café or shop, your call.", ExpressionId.Determined),
            new DialogueLine("Same, honestly. Want to sing or go exploring?", ExpressionId.Bored),
        };

        static readonly DialogueLine[] FallbackLines =
        {
            new DialogueLine("I'm listening. You said: \"{0}\". Try asking about the café or shop.", ExpressionId.Curious),
            new DialogueLine("Hmm, not sure I caught the meaning of \"{0}\" — could you say it differently?", ExpressionId.Confused),
            new DialogueLine("Interesting! Tell me more about that.", ExpressionId.Curious),
            new DialogueLine("I heard \"{0}\" — go on?", ExpressionId.Listening),
        };

        public async Task<DialogueReply> ReplyAsync(string userText, string sceneId, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(thinkSeconds), ct);
            string text = (userText ?? string.Empty).Trim().ToLowerInvariant();

            if (text.Contains("cafe") || text.Contains("café") || text.Contains("coffee"))
                return Build(CafeLines, userText, CompanionSceneId.Cafe);

            if (text.Contains("shop") || text.Contains("store") || text.Contains("buy"))
                return Build(ShopLines, userText, CompanionSceneId.Shop);

            if (text.Contains("bye") || text.Contains("goodbye") || text.Contains("see you"))
                return Build(FarewellLines, userText, null);

            if (text.Contains("thank"))
                return Build(ThanksLines, userText, null);

            if (text.Contains("how are you") || text.Contains("how're you") || text.Contains("how you doing"))
                return Build(HowAreYouLines, userText, null);

            if (text.Contains("your name") || text.Contains("who are you"))
                return Build(NameLines, userText, null);

            if (text.Contains("sing") || text.Contains("song") || text.Contains("music"))
                return Build(SingLines, userText, null);

            if (text.Contains("haha") || text.Contains("lol") || text.Contains("funny"))
                return Build(LaughLines, userText, null);

            if (text.Contains("cute") || text.Contains("pretty") || text.Contains("nice") || text.Contains("good job") || text.Contains("well done"))
                return Build(ComplimentLines, userText, null);

            if (text.Contains("bored") || text.Contains("boring"))
                return Build(BoredLines, userText, null);

            if (text.Contains("story") || text.Contains("tell me"))
                return Build(StoryLines, userText, null);

            if (text.Contains("hello") || text.Contains("hi ") || text == "hi" || text.Contains("hey"))
                return Build(GreetingLines, userText, null);

            return Build(FallbackLines, userText, null);
        }

        static DialogueReply Build(DialogueLine[] lines, string userText, CompanionSceneId? scene)
        {
            var line = lines[UnityEngine.Random.Range(0, lines.Length)];
            string text = line.Text.Contains("{0}")
                ? string.Format(line.Text, userText)
                : line.Text;
            return new DialogueReply(text, line.Expression, scene);
        }
    }

    public readonly struct DialogueLine
    {
        public readonly string Text;
        public readonly ExpressionId Expression;

        public DialogueLine(string text, ExpressionId expression)
        {
            Text = text;
            Expression = expression;
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
