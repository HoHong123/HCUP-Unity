using System;
using UnityEngine;
using HGame.Character;

namespace HGame.Player {
    // 플레이어 스탯 상태 소유 크래스
    public sealed class PlayerStatus : MonoBehaviour, IPlayerReadOnly, IPlayerCommand {
        PlayerConfig config = null;

        public int Level { get; private set; } = 1;
        public float Exp { get; private set; } = 0;
        public float ExpToNext { get; private set; } = 0;
        public int Hp { get; private set; } = 5;
        public int MaxHp => config.BaseHp;
        public int RandomDamage => config.RollBaseDamage();

        public event Action<int /* Level */> OnLevelUp;
        public event Action<float /* Exp */, float /* Next Exp */> OnExpChanged;
        public event Action<int /* Damage */> OnDamage;
        public event Action<int /* Heal */> OnHeal;
        public event Action<int /* New Hp */> OnHpChanged;
        public event Action OnAttack;
        public event Action OnUltUsed;
        public event Action OnDeath;


        public void Init(PlayerConfig config, int startLevel = 1, float startExp = 0) {
            this.config = config;
            Level = Mathf.Max(1, startLevel);
            Exp = Mathf.Max(0, startExp);
            ExpToNext = config.GetRequiredExpForLevel(Level);
            Hp = config.BaseHp;
            OnExpChanged?.Invoke(Exp, ExpToNext);
        }

        public void GainExp(float gainExp) {
            if (gainExp <= 0f) return;
            Exp += gainExp;

            while (Exp >= ExpToNext) {
                Exp -= ExpToNext;

                Level++;
                ExpToNext = config.GetRequiredExpForLevel(Level);
                OnLevelUp?.Invoke(Level);

                OnExpChanged?.Invoke(Exp, ExpToNext);
            }

            OnExpChanged?.Invoke(Exp, ExpToNext);
        }

        public void TakeDamage(int damage) {
            if (damage <= 0f || Hp <= 0f) return;
            Hp = Mathf.Max(0, Hp - damage);
            OnDamage?.Invoke(damage);
            OnHpChanged?.Invoke(Hp);
            if (Hp <= 0f) {
                OnDeath?.Invoke();
                // GameOver
            }
        }

        public void Heal(int heal) {
            if (heal <= 0f || Hp <= 0f) return;
            int prev = Hp;
            Hp = Mathf.Min(MaxHp, Hp + heal);
            int final = Hp - prev;
            OnHeal?.Invoke(final);
            OnHpChanged?.Invoke(Hp);
        }


        public void OnHitTarget(ICharacterCommand target) {
            if (Hp <= 0f) return;
            target?.TakeDamage(RandomDamage);
        }

        public void Attack() {
            if (Hp <= 0f) return;
            OnAttack?.Invoke();
        }

        public void UseUlt() {
            if (Hp <= 0f) return;
            OnUltUsed?.Invoke();
        }
    }
}