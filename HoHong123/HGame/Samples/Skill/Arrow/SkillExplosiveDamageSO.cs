using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "ExplosiveDamage", menuName = "Game/Skill/Arrow/Explosive Damage", order = 1)]
    public class SkillExplosiveDamageSO : BaseSkillSO {
        public override bool CanOffer(SkillStats stats, int stacks) => stats.EnableExplosive && stacks < MaxStacks;
        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int cur) {
            int add = GrantFor(rarity);
            if (TryAddStacks(ref cur, add, MaxStacks))
                stats.AddExplDamageStacks(add);
        }
    }
}