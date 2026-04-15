using HGame.Character;
using HGame.H2D.Map;

namespace HGame.World.EventAction {
    public sealed class HitEventAction : BaseEventAction {
        public override void Handle(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig target) {
            WorldEventManager.Instance.ReachHitPoint(point, target);
        }
    }
}