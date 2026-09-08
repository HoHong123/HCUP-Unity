using UnityEngine;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 소유자 GameObject 의 파괴를 AssetLeashManager 에 알리는 내부 전용 프로브.
 *
 * 주요 기능 ::
 * OnDestroy 시점에 OnGameObjectDestroyed 발화. 소유자별 회수 콜백이 여기에 붙는다.
 *
 * 사용법 ::
 * 직접 붙이지 않는다. AssetLeashManager 가 Component 소유자를 처음 볼 때 자동으로 AddComponent 한다.
 *
 * 주의 ::
 * hideFlags 의 DontSave 가 없으면 씬/프리팹에 직렬화되어 다음 로드 때 소유자 없는 프로브가 되살아난다.
 *
 * 유일한 사각 :: Destroy(component)
 * 프로브는 GameObject 파괴만 본다. Destroy(gameObject) 는 잡지만, Destroy(component) 로
 * 소유자 컴포넌트만 지우면 GameObject 와 프로브가 살아있어 통지가 오지 않는다.
 * 그 점유는 provider 폐기까지 남으며 에디터 Owner Watcher 의 ORPHAN 행으로만 드러난다.
 * 잡으려면 매 프레임 소유자 순회가 필요한데 그 비용은 이 사각의 발생 빈도에 비해 크다.
 * 소유자 컴포넌트만 떼어낼 일이 있으면 그 전에 Release / ReleaseOwner 를 직접 부를 것.
 * =========================================================
 */
#endif

namespace HResource.Subscription {
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class OwnerLeashProbe : MonoBehaviour {
        #region Events
        internal event System.Action OnGameObjectDestroyed;
        #endregion

        #region Unity
        void Awake() => hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;

        void OnDestroy() {
            OnGameObjectDestroyed?.Invoke();
            OnGameObjectDestroyed = null;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-04 (최초 설계) :: 파괴 통지 프로브
 *
 * 변경 ::
 * 소유자 GameObject 에 붙어 OnDestroy 를 AssetLeashManager 로 중계하는 내부 컴포넌트 신설.
 *
 * 이유 ::
 * Unity 는 임의 객체의 파괴에 대한 전역 훅을 주지 않는다. 폴링 없이 파괴 시점을 알려면
 * 파괴될 GameObject 위에 있어야 한다. 매 프레임 순회를 피하기 위한 유일한 수단이다.
 *
 * 결과 ::
 * 소비자가 Release 를 잊고 파괴되어도 그 소유자의 점유가 자동 회수된다.
 *
 * 주의 ::
 * 이것은 안전망이다. 정상 플로우는 소비자가 다 쓴 시점에 Release 를 부르는 것이다.
 * =========================================================
 */
#endif
