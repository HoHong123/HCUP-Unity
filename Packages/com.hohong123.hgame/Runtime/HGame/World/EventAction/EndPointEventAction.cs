using HGame.Character;
using HUtil._2D.Map;

namespace HGame.World.EventAction {
    public sealed class EndPointEventAction : BaseEventAction {
        public override void Handle(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig target) {
            WorldEventManager.Instance.ReachEndPoint(point, target);
        }
    }
}