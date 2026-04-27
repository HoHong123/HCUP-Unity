#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetOwnerId 발급기 (정적 클래스). Interlocked 기반 thread-safe 단조 증가.
 *
 * 주요 기능 ::
 * NewId(owner) — 새 ownerId 발급 + OnIdCreated 이벤트 발생.
 * NotifyReleased(ownerId) — 해제 통지 + OnIdReleased 이벤트 발생 (실제 해제는 호출자 책임).
 *
 * 사용법 ::
 * Awake 등 owner 수명 시작 지점에서 NewId(this) 호출. OnDestroy 등 종료 지점에서
 * provider.ReleaseOwner + NotifyReleased 짝맞춤. owner 객체는 추적 보조 정보로만 전달
 * (식별 자체는 id 값).
 *
 * 주의 ::
 * NewId / NotifyReleased 짝을 맞추는 것이 좋음. 미짝맞춤 시 OnIdReleased 가 안 발생해
 * 외부 추적 도구가 owner 수명을 놓침.
 * =========================================================
 */
#endif

using System.Threading;

namespace HUtil.AssetHandler.Subscription {
    public static class AssetOwnerIdGenerator {
        #region Fields
        static int nextId = 0;
        #endregion

        #region Events
        public static event System.Action<AssetOwnerId, object> OnIdCreated;
        public static event System.Action<AssetOwnerId> OnIdReleased;
        #endregion

        #region Public - Generate
        public static AssetOwnerId NewId(object owner = null) {
            var ownerId = new AssetOwnerId(Interlocked.Increment(ref nextId));
            OnIdCreated?.Invoke(ownerId, owner);
            return ownerId;
        }

        public static void NotifyReleased(AssetOwnerId ownerId) {
            if (!ownerId.IsValid) return;
            OnIdReleased?.Invoke(ownerId);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항) 에 "주요 기능 / 사용법" 섹션 추가하여 §11 형틀 통일.
 * 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetOwnerIdGenerator 초기 구현
 * =========================================================
 * 정적 카운터 + Interlocked.Increment 로 동시 발급 안전성. owner 객체 전달은 OnIdCreated
 * 이벤트의 추적 보조 정보 (외부 분석 도구가 "이 ownerId 가 어느 객체에 발급됐나" 추적용).
 * 실제 식별은 id 값으로만 수행. NotifyReleased 는 외부 통지만 — 실제 자산 해제는
 * provider.ReleaseOwner(ownerId) 가 별도로 수행.
 * =========================================================
 */
#endif
