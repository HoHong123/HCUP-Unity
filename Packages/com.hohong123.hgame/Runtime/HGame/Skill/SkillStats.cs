using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Skill {
    public class SkillStats : MonoBehaviour {
        // 모든 스탯은 곱연산 처리중. 
        [Title("Multipliers (1 = No change)")]
        [SerializeField]
        float attackMul = 1f;
        [SerializeField]
        float attackSpeedMul = 1f;
        [SerializeField]
        float ultCooldownMul = 1f;
        [SerializeField]
        float knockbackMul = 1f;

        [Title("Explosive (Multiplier)")]
        [SerializeField]
        bool enableExplosive = false;
        [SerializeField]
        float explosiveChance = 0.1f; // 0~1
        [SerializeField]
        float explosiveDamageMul = 1f;
        [SerializeField]
        float explosiveRadiusMul = 1f;

        public float AttackMul => attackMul;
        public float AttackSpeedMul => attackSpeedMul;
        public float UltCooldownMul => ultCooldownMul;
        public float KnockbackMul => knockbackMul;

        public bool EnableExplosive => enableExplosive;
        public float ExplosiveChance => explosiveChance;
        public float ExplosiveDamageMul => explosiveDamageMul;
        public float ExplosiveRadiusMul => explosiveRadiusMul;

        public void AddAttackStacks(int stacks) => attackMul = 1f + SkillConst.ATK_MULT_STACK * stacks;
        public void AddAttackSpeedStacks(int stacks) => attackSpeedMul = 1f + SkillConst.ATK_SPEED_MULT_STACK * stacks;
        public void AddUltCoolStacks(int stacks) => ultCooldownMul = 1f - SkillConst.ULT_COOLDOWN_STACK * stacks;
        public void AddKnockbackStacks(int stacks) => knockbackMul = 1f + SkillConst.KNOCKBACK_MULT_STACK * stacks;

        public void UnlockExplosive() => enableExplosive = true;
        public void AddExplChanceStacks(int stacks) => explosiveChance += SkillConst.EXPLODE_CHANCE_STACK * stacks;
        public void AddExplDamageStacks(int stacks) => explosiveDamageMul = 1f + SkillConst.EXPLODE_DMG_STACK * stacks;
        public void AddExplRadiusStacks(int stacks) => explosiveRadiusMul = 1f + SkillConst.EXPLODE_RADIUS_STACK * stacks;

        public void ResetAll() {
            attackMul = attackSpeedMul = explosiveDamageMul = explosiveRadiusMul = 1f;
            ultCooldownMul = 1f;
            knockbackMul = 1f;
            enableExplosive = false;
            explosiveChance = 0f;
        }

        // TODO :: If this game use server-side user database, Require to json parsing this state to save the progress.
    }
}
