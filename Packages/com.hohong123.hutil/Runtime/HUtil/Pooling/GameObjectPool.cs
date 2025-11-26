using System;
using UnityEngine;

namespace HUtil.Pooling {
    public class GameObjectPool : BasePool<GameObject> {
        readonly GameObject prefab;
        readonly Transform parent;

        public GameObjectPool(
            GameObject prefab, int initialSize = 5, Transform parent = null,
            Action<GameObject> onCreate = null, Action<GameObject> onGet = null,
            Action<GameObject> onReturn = null, Action<GameObject> onDispose = null) 
            : base(onCreate, onGet, onReturn, onDispose) {
            this.prefab = prefab;
            this.parent = parent;
            Init(initialSize);
        }

        protected override GameObject Create() {
            var obj = GameObject.Instantiate(prefab, parent);
            onCreate?.Invoke(obj);
            return obj;
        }
    }
}

#if UNITY_EDITOR
/* @Jason - PKH
 * 
 */
#endif