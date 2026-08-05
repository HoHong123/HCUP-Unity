using System;
using System.Collections.Generic;
using UnityEngine;
using HCore;
using HGame.H2D.Map;
using HGame.Character;
using HInspector;

namespace HGame.World.EventAction {
    public class WorldEventManager : SingletonBehaviour<WorldEventManager> {
        [HTitle("Controllers")]
        [SerializeField]
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