using System;
using UnityEngine;

namespace HUtil.Pooling {
    public class ComponentPool<T> : BasePool<T> where T : Component {
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
            GameObject obj;
            if (prefab) {
                obj = GameObject.Instantiate(prefab.gameObject, parent);
            }
            else {
                obj = new GameObject(typeof(T).Name, typeof(T));
                obj.transform.SetParent(parent);
            }
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