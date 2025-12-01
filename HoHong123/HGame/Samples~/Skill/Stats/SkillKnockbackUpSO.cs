using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "KnockbackUp", menuName = "Game/Skill/Stats/Knockback Up", order = 4)]
    public class SkillKnockbackUpSO : BaseSkillSO {
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddKnockbackStacks(add);
        }
    }
}