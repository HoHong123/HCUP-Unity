using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(
        fileName = "AttackSpeedUp",
        menuName = "Game/Skill/Stats/Attack Speed Up",
        order = 1)]
    public class SkillAttackSpeedUpSO : BaseSkillSO {
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddAttackSpeedStacks(add);
        }
    }
}