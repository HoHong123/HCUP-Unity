using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Sirenix.OdinInspector;
using HGame.Character;

namespace HGame.Player {
    // 기본 스탯 저장 클래스
    [Serializable]
    [CreateAssetMenu(
        fileName = "PlayerConfig",
        menuName = "Game/Player/Config")]
    public class PlayerConfig : BaseCharacterConfig {
        [Title("Leveling")]
        [SerializeField]
        float baseExp = 100;
        [SerializeField]
        float expMultiplier = 1.25f;

        [Title("Stats")]
        [SerializeField]
        int baseHp = 5;
        [SerializeField]
        int minDamage = 90;
        [SerializeField]
        int maxDamage = 100;
        [SerializeField]
        float attackCooldown = 0.2f;
        [SerializeField]
        float specialCooldown = 180f;
        [SerializeField]
        float critProbability = 20f;
        [SerializeField]
        float critMinRate = 120f;
        [SerializeField]
        float critMaxRate = 135f;

        public int BaseHp => baseHp;
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public float AttackCooldown => attackCooldown;
        public float SpecialCooldown => specialCooldown;
        public float CritProbability => critProbability;
        public float CritMinRate => critMinRate;
        public float CritMaxRate => critMaxRate;
        public float GetCritRate => Random.Range(critMinRate, critMaxRate);

        public float GetRequiredExpForLevel(int level) {
            var mul = Mathf.Pow(expMultiplier, Mathf.Max(0, level - 1));
            return Mathf.Ceil(baseExp * mul);
        }

        public int RollBaseDamage() {
            return Random.Range(minDamage, maxDamage);
        }
        
        public bool RollCrit(out float rate) {
            if (Random.value < critProbability * 0.01f) {
                rate = Random.Range(critMinRate, critMaxRate) * 0.01f;
                return true;
            }
            rate = 1f;
            return false;
        }
    }
} 