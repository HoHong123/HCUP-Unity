using UnityEngine;
using HGame.Character;
using HUtil._2D.Map;

namespace HGame.World.EventAction {
    public abstract class BaseEventAction : MonoBehaviour, IConfigEventAction {
        public abstract void Handle(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig monster);
    }
}