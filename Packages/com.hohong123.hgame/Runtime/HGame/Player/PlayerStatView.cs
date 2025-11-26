using UnityEngine;
using HGame.Skill;

namespace HGame.Player {
    // (스탯 + 스킬) 최종수치 계산 클래스
    public sealed class PlayerStatView : MonoBehaviour {
        PlayerConfig config = null;
        SkillStats skill = null;

        public bool ExplosiveEnabled => skill.EnableExplosive;
        public float ExplosiveChance => skill.ExplosiveChance;
        public float ExplosiveDmgMul => skill.ExplosiveDamageMul;
        public float ExplosiveRadiusMul => skill.ExplosiveRadiusMul;
        public float KnockbackMul => skill.KnockbackMul;

        public bool FireEnabled => true;
        public float FireChance => 0;

        public bool FrostEnabled => true;
        public float FrostChance => 0;

        public bool ThunderEnabled => true;
        public float ThunderChance => 0;

        public bool PoisonEnabled => true;
        public float PoisonChance => 0;


        public bool BindOnce(PlayerConfig config, SkillStats skill) {
            if (this.config || this.skill)
                return false;
            this.config = config;
            this.skill = skill;
            return true;
        }

        /// <returns>Is critical, Final damage</returns>
        public (bool, int) FinalDamage() {
            int baseRoll = config.RollBaseDamage();
            bool isCrit = config.RollCrit(out float critRate);
            float mulAtk = skill.AttackMul;
            float dmg = baseRoll * mulAtk * critRate;
            return (isCrit, Mathf.Max(1, Mathf.RoundToInt(dmg)));
        }

        public float AttackCooldown() {
            float cd = config.AttackCooldown;
            float spd = skill.AttackSpeedMul;
            return Mathf.Max(0.01f, cd / Mathf.Max(0.0001f, spd));
        }

        public float UltCooldown() {
            float baseCd = config.SpecialCooldown;
            float mul = skill.UltCooldownMul;
            return Mathf.Max(0.01f, baseCd * Mathf.Max(0.0001f, mul));
        }
    }
}
