using System;
using UnityEngine;

namespace Util.Pooling {
    public class ComponentPool<T> : BasePool<T> where T : MonoBehaviour {
        private readonly T prefab;
        private readonly Transform parent;


        public ComponentPool(
            T prefab, int initialSize = 5, Transform parent = null,
            Action<T> onCreate = null, Action<T> onGet = null,
            Action<T> onReturn = null, Action<T> onDispose = null)
            : base(onCreate, onGet, onReturn, onDispose) {
            this.prefab = prefab;
            this.parent = parent;
            Init(initialSize);
        }


        protected override T Create() {
            var obj = GameObject.Instantiate(prefab.gameObject, parent);
            var component = obj.GetComponent<T>();
            onCreate?.Invoke(component);
            return component;
        }
    }
}

#if UNITY_EDITOR
/* @Jason - PKH
 * 
 */
#endif