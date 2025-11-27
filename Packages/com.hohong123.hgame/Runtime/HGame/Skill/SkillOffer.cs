namespace HGame.Skill {
    [System.Serializable]
    public readonly struct SkillOffer {
        public readonly BaseSkillSO Skill;
        public readonly SkillRarity Rarity;

        public SkillOffer(BaseSkillSO skill, SkillRarity rarity) {
            Skill = skill;
            Rarity = rarity;
        }
    }
}