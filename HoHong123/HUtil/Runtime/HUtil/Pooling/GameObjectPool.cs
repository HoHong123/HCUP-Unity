#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * GameObject를 대상으로 사용하는 풀링 클래스입니다.
 *
 * 특징 ::
 * - Prefab 기반 GameObject 풀링
 * - 부모 Transform 지정 가능
 * - BasePool 구조를 기반으로 동작
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HUtil.Pooling {
    public class GameObjectPool : BasePool<GameObject> {
        #region Fields
        readonly GameObject prefab;
        readonly Transform parent;
        #endregion

        #region Public - Constructors
        public GameObjectPool(
            GameObject prefab, int initialSize = 5, Transform parent = null,
            Action<GameObject> onCreate = null, Action<GameObject> onGet = null,
            Action<GameObject> onReturn = null, Action<GameObject> onDispose = null) 
            : base(onCreate, onGet, onReturn, onDispose) {
            this.prefab = prefab;
            this.parent = parent;
            Init(initialSize);
        }
        #endregion

        #region Protected - Create
        protected override GameObject Create() {
            var obj = GameObject.Instantiate(prefab, parent);
            onCreate?.Invoke(obj);
            return obj;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * GameObject Prefab 풀링
 *
 * 생성 방식 ::
 * Instantiate(prefab)
 *
 * 옵션 ::
 * parent
 *  + 생성된 GameObject 부모 지정
 *
 * 사용법 ::
 * var pool = new GameObjectPool(prefab, 10);
 *
 * 기타 ::
 * 가장 기본적인 Unity 오브젝트 풀링 구현입니다.
 * =========================================================
 */
#endif