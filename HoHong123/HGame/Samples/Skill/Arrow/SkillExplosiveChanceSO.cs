using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "ExplosiveChance", menuName = "Game/Skill/Arrow/Explosive Chance", order = 1)]
    public class SkillExplosiveChanceSO : BaseSkillSO {
        public override bool CanOffer(SkillStats stats, int stacks) => stats.EnableExplosive && stacks < MaxStacks;
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddExplChanceStacks(add);
        }
    }
}