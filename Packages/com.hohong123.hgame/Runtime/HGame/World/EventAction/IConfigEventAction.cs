using HGame.Character;
using HUtil._2D.Map;

namespace HGame.World.EventAction {
    public interface IConfigEventAction {
        public void Handle(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig monster);
    }
}