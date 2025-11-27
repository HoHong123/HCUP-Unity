namespace HGame.Skill {
    public static class SkillConst {
        public static readonly float[] RarityWeights = {
            60f, // Normal
            25f, // Common
            12f, // Rare
            3f // Epic
        };

        public const int SKILL_CHOICE_COUNT = 3;

        // 기본 증감(스택당)
        public const float ATK_MULT_STACK = 0.1f; // +10%
        public const float ATK_SPEED_MULT_STACK = 0.05f; // +5%
        public const float ULT_COOLDOWN_STACK = 0.02f; // -2% (곱연산)
        public const float KNOCKBACK_MULT_STACK = 0.5f; // +5%

        public const float EXPLODE_CHANCE_STACK = 0.01f; // +1%p
        public const float EXPLODE_DMG_STACK = 0.1f; // +10% (곱연산)
        public const float EXPLODE_RADIUS_STACK = 0.05f; // +5% (곱연산)
    }
}
