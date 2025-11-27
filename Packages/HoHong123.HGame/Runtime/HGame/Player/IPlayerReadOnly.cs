using System;

namespace HGame.Player {
    public interface IPlayerReadOnly {
        public int Level { get; }
        public float Exp { get; }
        public float ExpToNext { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public int RandomDamage { get; }

        public event Action<int /* Level */> OnLevelUp;
        public event Action<float /* Exp */, float /* Next Exp */> OnExpChanged;
        public event Action<int /* Damage */> OnDamage;
        public event Action<int /* Heal */> OnHeal;
        public event Action<int /* Hp */> OnHpChanged;
        public event Action OnAttack;
        public event Action OnUltUsed;
        public event Action OnDeath;
    }
}