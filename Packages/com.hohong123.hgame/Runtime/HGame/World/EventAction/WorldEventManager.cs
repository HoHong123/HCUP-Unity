using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.Core;
using HUtil._2D.Map;
using HGame.Character;

namespace HGame.World.EventAction {
    public class WorldEventManager : SingletonBehaviour<WorldEventManager> {
        [Title("Controllers")]
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
            foreach (var point in endpoints) {
                UnregisterEndPoint(point);
            }
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