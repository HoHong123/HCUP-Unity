#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetOwnerId 발급기 (정적 클래스). Interlocked 기반 thread-safe 단조 증가.
 *
 * 주요 기능 ::
 * NewId(owner) - 새 ownerId 발급 + OnIdCreated 이벤트 발생.
 * NotifyReleased(ownerId) - 해제 통지 + OnIdReleased 이벤트 발생 (실제 해제는 호출자 책임).
 *
 * 사용법 ::
 * 소비자는 이 클래스를 직접 부르지 않는다. NewId / NotifyReleased 는 internal 이며
 * AssetLeashManager 가 지문을 발급하고 회수할 때만 호출한다. owner 객체는 추적 보조
 * 정보로만 전달되고 식별 자체는 id 값이다.
 *
 * 주의 ::
 * NewId / NotifyReleased 짝을 맞추는 것이 좋음. 미짝맞춤 시 OnIdReleased 가 안 발생해
 * 외부 추적 도구가 owner 수명을 놓침.
 * 정적 이벤트는 플레이 진입 시 리셋된다 - 구독자는 재구독 경로를 스스로 가져야 함.
 * =========================================================
 */
#endif

using System.Threading;

namespace HResource.Subscription {
    public static class AssetOwnerIdGenerator {
        #region Fields
        static int nextId = 0;
        #endregion

        #region Events
        // 페이로드가 int 다. AssetOwnerId 로 두면 이 공개 이벤트를 구독하는 것만으로
        // 어셈블리 밖에서 살아있는 남의 신원을 손에 넣을 수 있고, 캐시 계층의 공개
        // Release(key, ownerId) 와 짝지어 남의 점유를 내려놓는 수단이 된다.
        // 진단 계층은 값만 있으면 되므로 AssetOwnerOccupancy 와 같은 규칙을 따른다.
        public static event System.Action<int, object> OnIdCreated;
        public static event System.Action<int> OnIdReleased;

        // Domain Reload 비활성 시 id 카운터·구독이 플레이 세션을 넘어 잔존하는 것을 차단.
        // 주의 :: 여기서 이벤트를 비우면 [InitializeOnLoad] 로 붙는 구독자는 스스로 복구하지 못한다.
        // 에디터 워처(AssetOwnerIdWatchRegistry)는 AfterAssembliesLoaded 에서 재구독하도록 짝을 맞춰 두었다.
        // 이 리셋의 시점(SubsystemRegistration)을 바꾸면 그 순서 보장이 깨지므로 함께 검토할 것.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() {
            nextId = 0;
            OnIdCreated = null;
            OnIdReleased = null;
        }
        #endregion

        #region Public - Generate
        // internal 이고 owner 기본값이 없다. 공개 + owner 생략이 가능하면 어셈블리 밖에서
        // NewId() 로 소유자 없는 유효 id 를 뽑아 캐시에 직접 점유를 만들 수 있었다.
        // 발급은 AssetLeashManager 한 곳을 지나야 하고, 그곳은 항상 owner 를 넘긴다.
        internal static AssetOwnerId NewId(object owner) {
            var ownerId = new AssetOwnerId(Interlocked.Increment(ref nextId));
            OnIdCreated?.Invoke(ownerId.Value, owner);
            return ownerId;
        }

        internal static void NotifyReleased(AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return;
            OnIdReleased?.Invoke(ownerId.Value);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * 
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetOwnerIdGenerator 초기 구현
 * 
 * 정적 카운터 + Interlocked.Increment 로 동시 발급 안전성. owner 객체 전달은 OnIdCreated
 * 이벤트의 추적 보조 정보 (외부 분석 도구가 "이 ownerId 가 어느 객체에 발급됐나" 추적용).
 * 실제 식별은 id 값으로만 수행. NotifyReleased 는 외부 통지만 - 실제 자산 해제는
 * provider.ReleaseOwner(ownerId) 가 별도로 수행.
 * =========================================================
 */
#endif
