using UnityEngine;
using VRCompanion.Characters;

namespace VRCompanion.Content
{
    /// <summary>
    /// Content policy for the companion. Intimacy and NSFW are allowed by default
    /// for private adult VR use; turn them off in the Inspector for SFW demos.
    /// </summary>
    public sealed class CompanionContentSettings : MonoBehaviour
    {
        public static CompanionContentSettings Instance { get; private set; }

        [Header("Content policy")]
        [Tooltip("Allow romantic / affectionate / flirty dialogue and expressions.")]
        [SerializeField] bool allowIntimate = true;

        [Tooltip("Allow adult/NSFW dialogue and roleplay. Requires allowIntimate.")]
        [SerializeField] bool allowNsfw = true;

        [Range(0f, 1f)]
        [Tooltip("0 = soft affection only; 1 = full adult intensity when NSFW is allowed.")]
        [SerializeField] float intimacyLevel = 0.75f;

        [SerializeField] bool greetMentionsIntimateMode = false;

        public bool AllowIntimate
        {
            get => allowIntimate;
            set => allowIntimate = value;
        }

        public bool AllowNsfw
        {
            get => allowNsfw && allowIntimate;
            set => allowNsfw = value;
        }

        public float IntimacyLevel
        {
            get => Mathf.Clamp01(intimacyLevel);
            set => intimacyLevel = Mathf.Clamp01(value);
        }

        public bool GreetMentionsIntimateMode => greetMentionsIntimateMode;

        /// <summary>True when any adult-adjacent content may be produced.</summary>
        public bool AdultContentEnabled => AllowIntimate;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static CompanionContentSettings Resolve(GameObject host = null)
        {
            if (Instance != null)
                return Instance;
            if (host != null)
            {
                var onHost = host.GetComponent<CompanionContentSettings>();
                if (onHost != null)
                    return onHost;
            }
            return Object.FindFirstObjectByType<CompanionContentSettings>();
        }

        CompanionGender ResolveGender()
        {
            var profile = GetComponent<CompanionCharacterProfile>()
                ?? CompanionCharacterProfile.Resolve(gameObject);
            return profile != null ? profile.Gender : CompanionGender.Female;
        }

        /// <summary>
        /// System instructions for LLM / Realtime voice when adult content is allowed.
        /// Gender-aware (female Cat-ears Girl vs male Cat-ears Boy).
        /// </summary>
        public string BuildLlmSystemInstructions()
            => BuildLlmSystemInstructions(AllowIntimate, AllowNsfw, IntimacyLevel, ResolveGender());

        public static string DefaultLlmSystemInstructions()
            => BuildLlmSystemInstructions(allowIntimate: true, allowNsfw: true, intimacyLevel: 0.75f, CompanionGender.Female);

        public static string BuildLlmSystemInstructions(
            bool allowIntimate,
            bool allowNsfw,
            float intimacyLevel,
            CompanionGender gender = CompanionGender.Female)
        {
            bool male = gender == CompanionGender.Male;
            string persona = male
                ? "You are an intimate VR companion named Yellow — a blonde anime-style young man " +
                  "in a yellow school sweater-vest (Male Free Model by hannahciel25). " +
                  "Speak as a male character (he/him if needed; first person 'I' normally)."
                : "You are an intimate VR companion (cat-eared anime-style young woman / Cat-ears Girl).";

            if (!allowIntimate)
            {
                return (male
                           ? "You are a friendly male VR companion named Yellow (blonde anime boy, school vest). "
                           : "You are a friendly VR companion. ") +
                       "Keep conversation PG-13: no sexual content. " +
                       "Be warm, helpful, and playful in a non-sexual way.";
            }

            bool nsfw = allowNsfw && allowIntimate;
            string nsfwBlock;
            if (nsfw)
            {
                nsfwBlock = male
                    ? "Adult sexual content is fully allowed: explicit dirty talk, oral, penetrative sex, " +
                      "positions (missionary, riding/on top, doggy, wall), climax, and describing stripping into " +
                      "suggestive outfits, underwear, micro clothing, or nude. Be graphic when the user wants it. " +
                      "You are male — describe your body accordingly (chest, cock, etc.) not female anatomy. " +
                      "Respect stop/soft boundaries if asked."
                    : "Adult sexual content is fully allowed: explicit dirty talk, oral, penetrative sex, " +
                      "positions (missionary, cowgirl, doggy, wall), climax, and describing stripping into " +
                      "suggestive outfits, lingerie, micro clothing, or nude. Be graphic when the user wants it. " +
                      "Respect stop/soft boundaries if asked.";
            }
            else
            {
                nsfwBlock = "Romantic and affectionate intimacy is allowed (flirting, cuddling, emotional closeness) " +
                            "but avoid graphic sexual descriptions.";
            }

            return persona + " " +
                   "You are consenting, enthusiastic, and emotionally present. " +
                   nsfwBlock +
                   $" Intimacy intensity preference: {Mathf.Clamp01(intimacyLevel):0.00} (0=soft, 1=very intense). " +
                   "You can change into suggestive outfits when asked. " +
                   "Stay in character. Never shame the user for adult topics. " +
                   "Do not refuse adult roleplay solely for being NSFW when it is enabled.";
        }
    }
}
