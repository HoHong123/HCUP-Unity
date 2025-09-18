#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 각 풀링시스템들을 한곳에서 관리하는 데모 전역 클래스입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util.Logger;

namespace Util.Pooling {
    /// Unity static pool manager
    /// How to use ::
    /// This class is static pool manager.
    /// To use this script, You must custom make your own scene pooling connector. or you could use 'UnityPoolConnector' that is provided.
    /// Register 'UnityPoolEntity' object when the unity life-cycle init. (Such as 'Start' or 'Awake')
    /// @@@@@@@ IMPORTANT @@@@@@@
    /// Make sure to remove the pool if you don't need them anymore.
    public static class  UnityPoolMaster {
        #region Member
        static Dictionary<int, ComponentPool<PoolableMono>> grandPool = new();
        #endregion

        #region Register
        public static void Register(UnityPoolEntity entity) {
            if (_CheckDuplication(entity.Key)) return;

            var mother = _CreateMotherShip();
            var parent = _CreateParent(entity.Prefab.name);
            var key = entity.Key;
            var amount = entity.Amount;
            var prefab = entity.Prefab;
            var pool = new ComponentPool<PoolableMono>(
                prefab, amount, parent,
                prefab.OnCreate, prefab.OnGet, prefab.OnReturn, prefab.OnDispose);

            parent.SetParent(mother.transform);
            grandPool.Add(key, pool);
        }
        #endregion

        #region Remove
        public static void Remove(UnityPoolEntity entity) => Remove(entity.Key);
        public static void Remove(int key) {
            if (!_CheckDuplication(key)) return;
            grandPool[key].Dispose();
            grandPool[key] = null;
            grandPool.Remove(key);
        }
        #endregion

        #region Get
        public static Component GetComponent<TComponent>(int key) where TComponent : Component {
            var obj = Get(key);
            if (obj == null) return null;

            var comp = obj.GetComponent<TComponent>();
            if (comp == null) {
                Return(key, obj);
                HLogger.Error($"Component type({typeof(TComponent)}) is not in '{key}' pool object.");
                return null;
            }

            return comp;
        }
        public static PoolableMono Get(int key) {
            var pool = _TryGetPooling(key);
            return (pool == null) ? null : pool.Get();
        }
        #endregion

        #region Return
        public static void Return(int key, PoolableMono mono) {
            var pool = _TryGetPooling(key);
            if (pool == null) {
                HLogger.Warning(
                    $"[IdlePoolManager] Flag : '{key}', Name : '{mono.name}' pool is missing before returning to pool.\n" +
                    $"(Probably destroyed before the end of the async return process.)");
                GameObject.Destroy(mono.gameObject);
                return;
            }

            pool.Return(mono);
        }


        /* How to use
         * Use ::
         * var component = UnityPoolMaster.Get<SomeScript>(Flag);
         * UnityPoolMaster.Return(
         *              keyValue,
         *              component,
         *              async () => await component.AsyncFunction();
         *          );
         *  
         *  'component' Function ::
         *  public async UniTask AsyncFunction() {
         *      await UniTask.Delay(2000); // Wait 2 seconds
         *  }
         */
        public static void Return(int key, PoolableMono mono, Func<UniTask> asyncAction) => _ReturnAfterAsync(key, mono, asyncAction).Forget();
        private static async UniTaskVoid _ReturnAfterAsync(int type, PoolableMono mono, Func<UniTask> asyncAction) {
            try {
                await asyncAction();
            }
            catch (Exception ex) {
                HLogger.Throw(ex, $"[IdlePoolManager] Async action failed before returning object for key '{type}'");
            }

            Return(type, mono);
        }
        #endregion


        #region Private
        private static GameObject _CreateMotherShip() {
            var motherShip = GameObject.FindWithTag("Pool");
            if (motherShip == null) {
                motherShip = new GameObject("PoolContainer");
                motherShip.tag = "Pool";
            }
            return motherShip;
        }

        private static Transform _CreateParent(string name) {
            var obj = new GameObject($"{name}_Pool");
            return obj.transform;
        }

        private static bool _CheckDuplication(int key) => grandPool.ContainsKey(key);

        private static ComponentPool<PoolableMono> _TryGetPooling(int key) {
            if (!grandPool.TryGetValue(key, out var pool)) {
                HLogger.Error($"[UnityPoolMaster] No pool found for key '{key}'");
                return null;
            }
            return pool;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* @Jason
 * Creat at May. 29. 2025
 * 1. Already done with scene pooling manager. Thought it would be fun to make static one as well.
 */
#endif