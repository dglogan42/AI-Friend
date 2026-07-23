namespace VRCompanion
{
    /// <summary>
    /// Maps each <see cref="ExpressionId"/> to one of the VRoid-standard "Fcl_ALL_*"
    /// preset face blend shapes, so <see cref="ExpressionController"/> can drive a VRM
    /// character's SkinnedMeshRenderer directly instead of a color tint. Pure/stateless
    /// — no Unity dependency — so it's directly unit-testable.
    /// </summary>
    public static class FaceExpressionShapes
    {
        public const string Neutral = "Fcl_ALL_Neutral";
        public const string Joy = "Fcl_ALL_Joy";
        public const string Angry = "Fcl_ALL_Angry";
        public const string Sorrow = "Fcl_ALL_Sorrow";
        public const string Fun = "Fcl_ALL_Fun";
        public const string Surprised = "Fcl_ALL_Surprised";

        public static readonly string[] AllShapes = { Neutral, Joy, Angry, Sorrow, Fun, Surprised };

        /// <summary>Closest preset expression shape for a given companion ExpressionId.</summary>
        public static string ShapeFor(ExpressionId id) => id switch
        {
            ExpressionId.Happy => Joy,
            ExpressionId.Excited => Joy,
            ExpressionId.Curious => Surprised,
            ExpressionId.Surprised => Surprised,
            ExpressionId.Sad => Sorrow,
            ExpressionId.Sleepy => Sorrow,
            ExpressionId.Embarrassed => Sorrow,
            ExpressionId.Playful => Fun,
            ExpressionId.Laughing => Fun,
            ExpressionId.Confused => Angry,
            ExpressionId.Determined => Angry,
            ExpressionId.Affectionate => Joy,
            ExpressionId.Flirty => Fun,
            ExpressionId.Blush => Sorrow, // soft downturn / shy preset on VRoid
            ExpressionId.Seductive => Fun,
            ExpressionId.Intimate => Joy,
            // Neutral, Listening, Thinking, Speaking, Bored have no distinct preset.
            _ => Neutral,
        };
    }
}
