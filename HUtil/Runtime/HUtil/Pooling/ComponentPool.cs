#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity Component 타입을 대상으로 사용하는 풀링 클래스입니다.
 *
 * 특징 ::
 * - 특정 Component 타입을 기준으로 풀링
 * - Prefab 기반 생성 지원
 * - 부모 Transform 지정 가능
 *
 * 생성 방식 ::
 * 1. prefab이 존재할 경우 Instantiate
 * 2. prefab이 없을 경우 GameObject + Component 생성
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HUtil.Pooling {
    public class ComponentPool<T> : BasePool<T> where T : Component {
        #region Fields
        private readonly T prefab;
        private readonly Transform parent;
        #endregion

        #region Public - Constructors
        public ComponentPool(
            T prefab, int initialSize = 5, Transform parent = null,
            Action<T> onCreate = null, Action<T> onGet = null,
            Action<T> onReturn = null, Action<T> onDispose = null)
            : base(onCreate, onGet, onReturn, onDispose) {
            this.prefab = prefab;
            this.parent = parent;
            Init(initialSize);
        }
        #endregion

        #region Protected - Create
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
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * Component 기반 풀링
 *
 * 생성 방식 ::
 * prefab 존재 → Instantiate
 * prefab 없음 → GameObject + Component 생성
 *
 * 옵션 ::
 * parent
 *  + 생성된 객체 부모 지정
 *
 * 사용법 ::
 * var pool = new ComponentPool<MyComponent>(prefab);
 *
 * 기타 ::
 * MonoBehaviour 기반 풀링을 지원합니다.
 * =========================================================
 */
#endif