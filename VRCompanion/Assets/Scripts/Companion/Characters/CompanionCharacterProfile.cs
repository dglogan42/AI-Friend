using UnityEngine;

namespace VRCompanion.Characters
{
    /// <summary>
    /// Which companion body to spawn and how dialogue / LLM should address them.
    /// Female = CatEarsGirl VRM; Male = CatEarsBoy VRM when present, else a taller
    /// procedural stand-in (drop a VRM at Resources/Characters/CatEarsBoy/CatEarsBoy).
    /// </summary>
    public sealed class CompanionCharacterProfile : MonoBehaviour
    {
        public const string FemaleResourcePath = "Characters/CatEarsGirl/CatEarsGirl";
        public const string MaleResourcePath = "Characters/CatEarsBoy/CatEarsBoy";

        public const string PlayerPrefsKey = "VRCompanion.CharacterGender";

        [SerializeField] CompanionGender gender = CompanionGender.Female;

        public CompanionGender Gender
        {
            get => gender;
            set => gender = value;
        }

        public bool IsMale => gender == CompanionGender.Male;
        public bool IsFemale => gender == CompanionGender.Female;

        /// <summary>
        /// Male model: "Yellow" (Male Free Model) by hannahciel25 on VRoid Hub —
        /// https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638
        /// </summary>
        public const string MaleModelSourceUrl =
            "https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638";

        public const string MaleModelCreator = "hannahciel25";
        public const string MaleModelTitle = "Male Free Model / Yellow";

        public string DisplayName => gender == CompanionGender.Male
            ? "Yellow"
            : "Cat-ears Girl";

        public string ResourcesPath => gender == CompanionGender.Male
            ? MaleResourcePath
            : FemaleResourcePath;

        /// <summary>~standing height used for HUD / singing visualizer placement.</summary>
        public float ApproximateHeight => gender == CompanionGender.Male ? 1.65f : 1.456f;

        public static CompanionCharacterProfile Resolve(GameObject host = null)
        {
            if (host != null)
            {
                var onHost = host.GetComponent<CompanionCharacterProfile>();
                if (onHost != null)
                    return onHost;
            }
            return Object.FindFirstObjectByType<CompanionCharacterProfile>();
        }

        public void SavePreference()
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, (int)gender);
            PlayerPrefs.Save();
        }

        public void LoadPreference()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
                gender = (CompanionGender)PlayerPrefs.GetInt(PlayerPrefsKey, 0);
        }

        /// <summary>Env override: VRCOMPANION_GENDER=male|female (wins over PlayerPrefs when set).</summary>
        public static CompanionGender ResolveStartupGender(CompanionGender inspectorDefault)
        {
            string env = System.Environment.GetEnvironmentVariable("VRCOMPANION_GENDER");
            if (!string.IsNullOrEmpty(env))
            {
                env = env.Trim().ToLowerInvariant();
                if (env is "male" or "m" or "boy" or "man")
                    return CompanionGender.Male;
                if (env is "female" or "f" or "girl" or "woman")
                    return CompanionGender.Female;
            }

            if (PlayerPrefs.HasKey(PlayerPrefsKey))
                return (CompanionGender)PlayerPrefs.GetInt(PlayerPrefsKey, (int)inspectorDefault);

            return inspectorDefault;
        }
    }
}
