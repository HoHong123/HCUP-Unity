using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "UltCooldown", menuName = "Game/Skill/Stats/Ult Cooldown", order = 3)]
    public class SkillUltCooldownSO : BaseSkillSO {
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddUltCoolStacks(add);
        }
    }
}