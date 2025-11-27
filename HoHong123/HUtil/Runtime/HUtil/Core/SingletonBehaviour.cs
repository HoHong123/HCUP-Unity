#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 싱글톤 패턴 지원 스크립트
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using BehaviourBase = Sirenix.OdinInspector.SerializedMonoBehaviour;
#else
using BehaviourBase = UnityEngine.MonoBehaviour;
#endif
using UnityEngine;
using HUtil.Logger;

namespace HUtil.Core {
    public class SingletonBehaviour<T> : BehaviourBase where T : SingletonBehaviour<T> {
#if ODIN_INSPECTOR
        [Title("Singleton")]
#endif
        [SerializeField]
        bool dontDestroyOnLoad;

        protected static T instance = null;
        public static T Instance {
            get {
                if (instance == null) {
                    instance = FindFirstObjectByType(typeof(T)) as T;
                    if (instance == null) {
                        HLogger.Log("Nothing " + instance.ToString());
                        return null;
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;


        // Use this for initialization
        protected virtual void Awake() {
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            }

            if (instance != null && instance != this) {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
        }
    }
}