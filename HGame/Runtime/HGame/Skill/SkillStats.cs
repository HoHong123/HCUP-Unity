using UnityEngine;
using HInspector;

namespace HGame.Skill {
    public class SkillStats : MonoBehaviour {
        // 모든 스탯은 곱연산 처리중. 
        [HTitle("Multipliers (1 = No change)")]
        [SerializeField]
        float attackMul = 1f;
        [SerializeField]
        float attackSpeedMul = 1f;
        [SerializeField]
        float ultCooldownMul = 1f;
        [SerializeField]
        float knockbackMul = 1f;

        [HTitle("Explosive (Multiplier)")]
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

        // 호출부(Samples~/Skill/Scripts/**)가 전부 증분(add)을 넘기므로 절대 대입이 아닌 누적(+=/-=)으로 통일한다.
        // 절대 대입이면 같은 add 값이 반복 호출될 때 스택 2 이상에서 값이 증가하지 않는다.
        public void AddAttackStacks(int stacks) => attackMul += SkillConst.ATK_MULT_STACK * stacks;
        public void AddAttackSpeedStacks(int stacks) => attackSpeedMul += SkillConst.ATK_SPEED_MULT_STACK * stacks;
        public void AddUltCoolStacks(int stacks) => ultCooldownMul -= SkillConst.ULT_COOLDOWN_STACK * stacks;
        public void AddKnockbackStacks(int stacks) => knockbackMul += SkillConst.KNOCKBACK_MULT_STACK * stacks;

        public void UnlockExplosive() => enableExplosive = true;
        public void AddExplChanceStacks(int stacks) => explosiveChance += SkillConst.EXPLODE_CHANCE_STACK * stacks;
        public void AddExplDamageStacks(int stacks) => explosiveDamageMul += SkillConst.EXPLODE_DMG_STACK * stacks;
        public void AddExplRadiusStacks(int stacks) => explosiveRadiusMul += SkillConst.EXPLODE_RADIUS_STACK * stacks;

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
