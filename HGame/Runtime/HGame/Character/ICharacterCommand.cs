namespace HGame.Character {
    public interface ICharacterCommand {
        public void Heal(int heal);
        public void TakeDamage(int damage);
        public void Attack();
        public void OnHitTarget(ICharacterCommand target);
    }
}