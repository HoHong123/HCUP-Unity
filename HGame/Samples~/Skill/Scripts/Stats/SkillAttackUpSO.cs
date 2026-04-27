using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "AttackUp", menuName = "Game/Skill/Stats/Attack Up", order = 0)]
    public class SkillAttackUpSO : BaseSkillSO {
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddAttackStacks(add);
        }
    }
}