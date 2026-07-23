using System;
using System.IO;

namespace VRCompanion.Speech
{
    /// <summary>
    /// Reads the OpenAI API key from outside the Unity project (never baked into
    /// Assets/StreamingAssets or a build) — see ~/.vrcompanion/secrets/.
    /// </summary>
    public static class OpenAiSecrets
    {
        public static string ApiKeyPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".vrcompanion", "secrets", "openai_api_key.txt");

        public static bool TryGetApiKey(out string apiKey)
        {
            apiKey = null;
            try
            {
                if (!File.Exists(ApiKeyPath))
                    return false;
                apiKey = File.ReadAllText(ApiKeyPath).Trim();
                return !string.IsNullOrEmpty(apiKey);
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
