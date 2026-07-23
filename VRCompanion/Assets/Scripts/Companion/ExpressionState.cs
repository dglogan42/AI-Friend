namespace VRCompanion
{
    /// <summary>
    /// Facial / body expression states for the companion character.
    /// </summary>
    public enum ExpressionId
    {
        Neutral = 0,
        Happy,
        Curious,
        Listening,
        Thinking,
        Speaking,
        Surprised,
        Sad,
        Excited,
        Playful,
        Sleepy,
        Confused,
        Laughing,
        Bored,
        Determined,
        Embarrassed,
        // Intimate / adult-capable affect (used when CompanionContentSettings allows it)
        Affectionate,
        Flirty,
        Blush,
        Seductive,
        Intimate
    }

    /// <summary>
    /// Mutable expression snapshot used by controllers and UI.
    /// </summary>
    [System.Serializable]
    public struct ExpressionState
    {
        public ExpressionId Id;
        public float Intensity;

        public static ExpressionState Neutral => new ExpressionState
        {
            Id = ExpressionId.Neutral,
            Intensity = 0f
        };

        public ExpressionState(ExpressionId id, float intensity = 1f)
        {
            Id = id;
            Intensity = UnityEngine.Mathf.Clamp01(intensity);
        }
    }
}
