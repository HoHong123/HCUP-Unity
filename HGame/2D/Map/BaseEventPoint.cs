#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 특정 오브젝트와 충돌 이벤트를 담당하는 스크립트를 구현했습니다.
 * 
 * ** 사용법 **
 * 1. 상속으로 제네릭 타입에 충돌 후 가져올 컴포넌트를 지정합니다.
 * 2. 물리 충돌이 발생하면 충돌 오브젝트의 태그를 비교합니다.
 * + 태그 사용유무 조절가능 ('useTag' 설정)
 * 3. 충돌 후 선정한 컴포넌트를 가져오고 대리자를 통해 연결된 모든 오브젝트에 전달합니다.
 * 4. 에디터상 이벤트 포인트의 기즈모를 활성화할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using Util.Logger;
using Util.OdinCompat;

namespace HGame._2D.Map {
    public abstract partial class BaseEventPoint<T> : MonoBehaviour where T : MonoBehaviour {
#if !ODIN_INSPECTOR
        [HeaderOrTitle("Filter")]
        [SerializeField]
        protected bool useTag = true;
        [SerializeField]
        protected string targetTag;

        [HeaderOrTitle("Collider")]
        [SerializeField]
        protected Collider2D eventCollider;

#if UNITY_EDITOR
        [HeaderOrTitle("Debug")]
        [SerializeField]
        protected bool useDebug = true;
        [SerializeField]
        protected Color debugColor = Color.red;
#endif
#endif

        public string TargetTag => targetTag;

        public event Action<T> OnEventCollision;
        public event Action<T> OnEventTrigger;


        protected virtual void OnCollisionEnter2D(Collision2D collision) {
            if (useTag && !collision.transform.CompareTag(TargetTag)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEventCollision?.Invoke(target);
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision) {
            if (useTag && !collision.CompareTag(TargetTag)) return;
            if (!collision.transform.TryGetComponent(out T target)) return;
            OnEventTrigger?.Invoke(target);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!eventCollider) TryGetComponent(out eventCollider);
            if (!string.IsNullOrEmpty(targetTag)) {
                var all = UnityEditorInternal.InternalEditorUtility.tags;
                bool ok = Array.IndexOf(all, targetTag) >= 0;
                if (!ok) {
                    HLogger.Error($"{name}: '{targetTag}' 태그가 Tag Manager에 없습니다.", gameObject);
                    targetTag = string.Empty;
                }
            }
        }

        private void OnDrawGizmosSelected() {
            if (!useDebug) return;
            Gizmos.color = debugColor;
            Gizmos.DrawCube(eventCollider.bounds.center, eventCollider.bounds.size);
        }
#endif
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH 18. 09. 25
 * 1. 디버깅 시스템 업데이트
 * + 디버그 활성화 기능 추가
 * + 디버그 컬러 설정 가능
 * 2. 태그 사용설정 추가
 */
#endif