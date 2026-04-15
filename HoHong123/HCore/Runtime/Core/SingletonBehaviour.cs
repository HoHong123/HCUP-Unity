#if UNITY_EDITOR
/* =========================================================
 * MonoBehaviour 기반 싱글톤 패턴을 구현하기 위한 베이스 클래스입니다.
 * 제네릭 타입 T를 통해 특정 컴포넌트를 싱글톤으로 관리하며 프로젝트 전역에서 접근 가능한 Instance를 제공합니다.
 *
 * 주의사항 ::
 * 1. 동일 타입의 컴포넌트가 여러 개 존재할 경우, 최초 인스턴스를 제외한 나머지는 Destroy됩니다.
 * 2. Instance 접근 시 씬에서 자동 검색이 수행됩니다.
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using BehaviourBase = Sirenix.OdinInspector.SerializedMonoBehaviour;
#else
using BehaviourBase = UnityEngine.MonoBehaviour;
#endif
using UnityEngine;
using HDiagnosis.Logger;
using HInspector;

namespace HCore {
    public class SingletonBehaviour<T> : BehaviourBase where T : SingletonBehaviour<T> {
        [HTitle("Singleton")]
        [SerializeField]
        bool dontDestroyOnLoad;

        protected static T instance = null;
        public static T Instance {
            get {
                if (instance == null) {
                    instance = FindFirstObjectByType(typeof(T)) as T;
                    if (instance == null) {
                        HLogger.Log("Instance is null");
                        return null;
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;


        // Use this for initialization
        protected virtual void Awake() {
            if (instance != null && instance != this) {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            }
        }

        // 자기 자신일 때만 static 참조를 해제한다. 다른 인스턴스를 덮어쓰는 사고 방지.
        protected virtual void OnDestroy() {
            if (instance == this) {
                instance = null;
            }
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. Singleton Instance 제공
 *    + Instance 프로퍼티를 통해 전역 접근 지원
 * 2. 중복 인스턴스 제거
 *    + 동일 타입 컴포넌트가 존재할 경우
 *      최초 인스턴스를 제외하고 Destroy 처리
 * 3. DontDestroyOnLoad 지원
 *    + 옵션 활성화 시 씬 변경 시에도 유지
 *
 * 사용법 ::
 * 1. SingletonBehaviour<T>를 상속한 클래스를 작성합니다.
 * 2. Instance를 통해 싱글톤 객체에 접근합니다.
 *
 * 기타 ::
 * 1. Instance가 null인 경우 씬에서 자동 검색을 수행합니다.
 * =========================================================
 */
#endif