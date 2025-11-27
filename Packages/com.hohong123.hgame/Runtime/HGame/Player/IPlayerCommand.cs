namespace HGame.Player {
    public interface IPlayerCommand : Character.ICharacterCommand {
        public void GainExp(float amount);
        public void UseUlt();
    }
}