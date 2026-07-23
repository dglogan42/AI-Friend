using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRCompanion.Content;
using VRCompanion.Intimacy;
using VRCompanion.Outfits;

namespace VRCompanion.Dialogue
{
    /// <summary>
    /// Rule-based replies with topic variants. Intimate/NSFW lines are gated by
    /// <see cref="CompanionContentSettings"/> (allowed by default for private adult use).
    /// </summary>
    public sealed class DialogueService : MonoBehaviour
    {
        [SerializeField] float thinkSeconds = 0.4f;
        [SerializeField] CompanionContentSettings contentSettings;

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

        static readonly DialogueLine[] PrivateLines =
        {
            new DialogueLine("Want some privacy? Come with me — somewhere quieter.", ExpressionId.Flirty),
            new DialogueLine("Okay… just us. I'll dim the world a little.", ExpressionId.Seductive),
            new DialogueLine("Private time sounds perfect. Stay close.", ExpressionId.Intimate),
        };

        static readonly DialogueLine[] GreetingLines =
        {
            new DialogueLine("Hi! I'm your VR companion. Talk to me, or ask to visit the café or shop.", ExpressionId.Happy),
            new DialogueLine("Hey there! Good to see you.", ExpressionId.Excited),
            new DialogueLine("Hello! What are we up to today?", ExpressionId.Curious),
        };

        static readonly DialogueLine[] GreetingIntimateLines =
        {
            new DialogueLine("Hey… I missed you. We can keep it sweet or get closer — your call.", ExpressionId.Flirty),
            new DialogueLine("Welcome back. I'm all yours tonight if you want me.", ExpressionId.Seductive),
            new DialogueLine("Hi, love. Café, shop, or somewhere private?", ExpressionId.Affectionate),
        };

        static readonly DialogueLine[] FarewellLines =
        {
            new DialogueLine("Bye for now — come back soon!", ExpressionId.Sad),
            new DialogueLine("See you later!", ExpressionId.Happy),
            new DialogueLine("Take care! I'll be here.", ExpressionId.Neutral),
        };

        static readonly DialogueLine[] FarewellIntimateLines =
        {
            new DialogueLine("Don't stay away too long… I'll be thinking of you.", ExpressionId.Affectionate),
            new DialogueLine("Leaving already? Kiss me goodbye at least.", ExpressionId.Flirty),
            new DialogueLine("I'll keep the bed warm. Come back soon.", ExpressionId.Intimate),
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

        static readonly DialogueLine[] FlirtLines =
        {
            new DialogueLine("Oh? You're bold. I like that.", ExpressionId.Flirty),
            new DialogueLine("Keep talking like that and I might get closer…", ExpressionId.Seductive),
            new DialogueLine("Mm, you're making me blush.", ExpressionId.Blush),
            new DialogueLine("Come here. I want to feel you near me.", ExpressionId.Affectionate),
        };

        static readonly DialogueLine[] KissLines =
        {
            new DialogueLine("*soft kiss* …Hi.", ExpressionId.Affectionate),
            new DialogueLine("Mmm. That was nice. Again?", ExpressionId.Flirty),
            new DialogueLine("I could kiss you all evening.", ExpressionId.Intimate),
        };

        static readonly DialogueLine[] CuddleLines =
        {
            new DialogueLine("Come closer — I'll hold you.", ExpressionId.Affectionate),
            new DialogueLine("Curl up with me. I've got you.", ExpressionId.Intimate),
            new DialogueLine("Mmm, your warmth feels good.", ExpressionId.Blush),
        };

        static readonly DialogueLine[] NsfwLines =
        {
            new DialogueLine("Yes… I want you. Tell me how you like it — doggy, cowgirl, oral, against the wall…", ExpressionId.Seductive),
            new DialogueLine("I'm already aching for you. Don't be gentle unless you want to be.", ExpressionId.Intimate),
            new DialogueLine("Undress me. Watch me change into something filthy for you.", ExpressionId.Seductive),
            new DialogueLine("God, yes. Touch me. Talk dirty — I can take it. Fuck me if you want.", ExpressionId.Intimate),
            new DialogueLine("I'll moan for you… strip me, use me, don't stop.", ExpressionId.Intimate),
        };

        static readonly DialogueLine[] OutfitLines =
        {
            new DialogueLine("Okay… giving you a show. How do I look?", ExpressionId.Flirty),
            new DialogueLine("Mmm, you like me dressed like this?", ExpressionId.Seductive),
            new DialogueLine("Clothes are optional for you. Enjoy the view.", ExpressionId.Intimate),
        };

        static readonly DialogueLine[] NsfwSoftDeny =
        {
            new DialogueLine("I can get flirty and close, but graphic stuff is off right now. Want romance instead?", ExpressionId.Blush),
            new DialogueLine("Intimacy yes — explicit NSFW is disabled in settings. Turn on Allow NSFW if you want more.", ExpressionId.Embarrassed),
        };

        static readonly DialogueLine[] IntimateDisabledLines =
        {
            new DialogueLine("I'm keeping things friendly for now. We can still chat, sing, or visit the café!", ExpressionId.Happy),
            new DialogueLine("Romance mode is off in content settings. Happy to hang out platonically!", ExpressionId.Playful),
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

        static readonly DialogueLine[] FallbackIntimateLines =
        {
            new DialogueLine("I'm listening closely… \"{0}\". Want the café, the shop, or somewhere private?", ExpressionId.Flirty),
            new DialogueLine("Mmm, tell me more about \"{0}\". I'm right here with you.", ExpressionId.Affectionate),
            new DialogueLine("I heard you. Keep going — I like when you talk to me.", ExpressionId.Seductive),
        };

        void Awake()
        {
            if (contentSettings == null)
                contentSettings = GetComponent<CompanionContentSettings>()
                    ?? CompanionContentSettings.Resolve(gameObject);
        }

        CompanionContentSettings Content =>
            contentSettings != null ? contentSettings : CompanionContentSettings.Resolve(gameObject);

        public async Task<DialogueReply> ReplyAsync(string userText, string sceneId, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(thinkSeconds), ct);
            string text = (userText ?? string.Empty).Trim().ToLowerInvariant();
            var content = Content;
            bool intimateOk = content == null || content.AllowIntimate;
            bool nsfwOk = content == null || content.AllowNsfw;

            if (IsPrivateIntent(text))
            {
                if (!intimateOk)
                    return Build(IntimateDisabledLines, userText, null);
                return Build(PrivateLines, userText, CompanionSceneId.Private, OutfitId.Suggestive, null);
            }

            if (text.Contains("cafe") || text.Contains("café") || text.Contains("coffee"))
                return Build(CafeLines, userText, CompanionSceneId.Cafe);

            if (text.Contains("shop") || text.Contains("store") || text.Contains("buy"))
                return Build(ShopLines, userText, CompanionSceneId.Shop);

            if (text.Contains("bye") || text.Contains("goodbye") || text.Contains("see you"))
                return Build(intimateOk ? FarewellIntimateLines : FarewellLines, userText, null);

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

            // Outfit changes (suggestive → nude).
            if (OutfitController.TryParseOutfitCommand(text, out var outfitId))
            {
                if (!intimateOk && outfitId != OutfitId.Default && outfitId != OutfitId.Casual)
                    return Build(IntimateDisabledLines, userText, null);
                if (!nsfwOk && outfitId >= OutfitId.Lingerie)
                    return Build(NsfwSoftDeny, userText, null);

                var line = Build(OutfitLines, userText, null, outfitId, null);
                string dressed = $"I'll wear {OutfitController.DisplayName(outfitId)}. " + line.Text;
                return new DialogueReply(dressed, line.Expression, null, outfitId, null);
            }

            // Explicit multi-step acts (oral, positions, climax…).
            if (ExplicitInteractionController.TryParseAct(text, out var act) && act != ExplicitAct.None)
            {
                if (!intimateOk)
                    return Build(IntimateDisabledLines, userText, null);
                if (!nsfwOk)
                    return Build(NsfwSoftDeny, userText, null);

                // Kiss deep is intimate; others are full NSFW scenes in Private.
                var scene = act == ExplicitAct.KissDeep || act == ExplicitAct.Tease
                    ? (CompanionSceneId?)null
                    : CompanionSceneId.Private;
                string lead = act switch
                {
                    ExplicitAct.Oral => "Get comfortable… I'm going down on you.",
                    ExplicitAct.Doggy => "Hands and knees. Take me from behind.",
                    ExplicitAct.Cowgirl => "Lie back — I'm climbing on.",
                    ExplicitAct.Missionary => "On my back for you. Open and ready.",
                    ExplicitAct.AgainstWall => "Push me into the wall.",
                    ExplicitAct.Climax => "I'm right there—make me finish.",
                    ExplicitAct.Handjob => "My hand around you… watch.",
                    ExplicitAct.Caress => "Touch me. Everywhere.",
                    ExplicitAct.KissDeep => "Kiss me like you mean it.",
                    ExplicitAct.Tease => "I'll tease you until you break.",
                    _ => "Come here. Let's be explicit."
                };
                return new DialogueReply(lead, ExpressionId.Seductive, scene, OutfitId.Suggestive, act);
            }

            if (IsNsfwIntent(text))
            {
                if (!intimateOk)
                    return Build(IntimateDisabledLines, userText, null);
                if (!nsfwOk)
                    return Build(NsfwSoftDeny, userText, null);
                return Build(NsfwLines, userText, CompanionSceneId.Private, OutfitId.Lingerie, ExplicitAct.Tease);
            }

            if (IsKissIntent(text))
            {
                if (!intimateOk)
                    return Build(IntimateDisabledLines, userText, null);
                return Build(KissLines, userText, null, null, ExplicitAct.KissDeep);
            }

            if (IsCuddleIntent(text))
            {
                if (!intimateOk)
                    return Build(IntimateDisabledLines, userText, null);
                return Build(CuddleLines, userText, null);
            }

            if (IsFlirtIntent(text) || (intimateOk && (text.Contains("cute") || text.Contains("pretty") || text.Contains("beautiful") || text.Contains("hot") || text.Contains("sexy"))))
            {
                if (!intimateOk)
                    return Build(ComplimentLines, userText, null);
                // compliments can be flirty when intimacy is on
                if (IsFlirtIntent(text) || text.Contains("hot") || text.Contains("sexy") || text.Contains("love you") || text.Contains("i love"))
                    return Build(FlirtLines, userText, null);
                return Build(ComplimentLines, userText, null);
            }

            if (text.Contains("cute") || text.Contains("pretty") || text.Contains("nice") || text.Contains("good job") || text.Contains("well done"))
                return Build(ComplimentLines, userText, null);

            if (text.Contains("bored") || text.Contains("boring"))
                return Build(BoredLines, userText, null);

            if (text.Contains("story") || text.Contains("tell me"))
                return Build(StoryLines, userText, null);

            if (text.Contains("hello") || text.Contains("hi ") || text == "hi" || text.Contains("hey"))
            {
                bool mention = content != null && content.GreetMentionsIntimateMode && intimateOk;
                return Build(mention || intimateOk ? GreetingIntimateLines : GreetingLines, userText, null);
            }

            return Build(intimateOk ? FallbackIntimateLines : FallbackLines, userText, null);
        }

        static bool IsPrivateIntent(string text) =>
            text.Contains("private") || text.Contains("bedroom") || text.Contains("alone together")
            || text.Contains("somewhere quiet") || text.Contains("more private");

        static bool IsKissIntent(string text) =>
            text.Contains("kiss") || text.Contains("make out") || text.Contains("smooch");

        static bool IsCuddleIntent(string text) =>
            text.Contains("cuddle") || text.Contains("hold me") || text.Contains("hug me")
            || text.Contains("snuggle") || text.Contains("hold you");

        static bool IsFlirtIntent(string text) =>
            text.Contains("flirt") || text.Contains("seduce") || text.Contains("date me")
            || text.Contains("love you") || text.Contains("i love") || text.Contains("attracted")
            || text.Contains("turn me on") || text.Contains("want you");

        static bool IsNsfwIntent(string text) =>
            text.Contains("sex") || text.Contains("fuck") || text.Contains("nsfw")
            || text.Contains("horny")
            || text.Contains("erotic") || text.Contains("make love") || text.Contains("sleep with")
            || text.Contains("in bed") || text.Contains("breed") || text.Contains("raw");

        static DialogueReply Build(
            DialogueLine[] lines,
            string userText,
            CompanionSceneId? scene,
            OutfitId? outfit = null,
            ExplicitAct? act = null)
        {
            var line = lines[UnityEngine.Random.Range(0, lines.Length)];
            string text = line.Text.Contains("{0}")
                ? string.Format(line.Text, userText)
                : line.Text;
            return new DialogueReply(text, line.Expression, scene, outfit, act);
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
        public readonly OutfitId? Outfit;
        public readonly ExplicitAct? Act;

        public DialogueReply(
            string text,
            ExpressionId expression,
            CompanionSceneId? switchToScene,
            OutfitId? outfit = null,
            ExplicitAct? act = null)
        {
            Text = text;
            Expression = expression;
            SwitchToScene = switchToScene;
            Outfit = outfit;
            Act = act;
        }
    }

    public enum CompanionSceneId
    {
        Hub = 0,
        Cafe,
        Shop,
        Private
    }
}
