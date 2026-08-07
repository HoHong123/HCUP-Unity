using System;
using System.Collections.Generic;
using UnityEngine;
using HCore;
using HGame.H2D.Map;
using HGame.Character;

namespace HGame.World.EventAction {
    public class WorldEventManager : SingletonBehaviour<WorldEventManager> {
        // HashSet<> 은 Unity 가 직렬화하지 못하는 타입이라 [SerializeField] 는 Inspector 에 아무 효과가 없다
        // (readonly 인터라 애초에 Inspector 편집 대상도 아니다). 오인 방지를 위해 제거.
        readonly HashSet<BaseEventPoint<ICharacterCommand>> endpoints = new();

        public event Action<BaseEventPoint<ICharacterCommand>, BaseCharacterConfig> OnReachHitPoint;
        public event Action<BaseEventPoint<ICharacterCommand>, BaseCharacterConfig> OnReachEndPoint;


        public void RegisterEndPoint(BaseEventPoint<ICharacterCommand> endPoint) {
            if (!endPoint || endpoints.Contains(endPoint)) return;
            endpoints.Add(endPoint);
        }
        public bool UnregisterEndPoint(BaseEventPoint<ICharacterCommand> endPoint) {
            return endpoints.Remove(endPoint);
        }
        public void UnregisterAllEndPoint() {
            // 순회 중 Remove 는 요소가 1개 이상이면 InvalidOperationException — 일괄 비움으로 대체.
            endpoints.Clear();
        }


        public void ReachEndPoint(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig character) {
            if (!point || !character) return;
            OnReachEndPoint?.Invoke(point, character);
        }

        public void ReachHitPoint(BaseEventPoint<ICharacterCommand> point, BaseCharacterConfig character) {
            if (!point || !character) return;
            OnReachHitPoint?.Invoke(point, character);
        }
    }
}