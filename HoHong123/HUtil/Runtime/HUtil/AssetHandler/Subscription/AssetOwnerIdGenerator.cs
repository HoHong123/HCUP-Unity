using System.Threading;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetOwnerId 발급기 스크립트입니다.
 *
 * 주의사항 ::
 * 1. ownerId 생성과 해제 알림은 짝을 맞추는 것이 좋습니다.
 * 2. owner 객체 전달은 추적 보조 정보일 뿐 식별 자체는 id 값으로 합니다.
 * =========================================================
 */
#endif

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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 새 ownerId를 발급합니다.
 * 2. 생성과 해제 알림 이벤트를 제공합니다.
 *
 * 사용법 ::
 * 1. Awake 등 owner 수명 시작 지점에서 NewId를 호출합니다.
 * 2. OnDestroy 등 종료 지점에서 NotifyReleased를 호출합니다.
 *
 * 이벤트 ::
 * 1. OnIdCreated가 발급 시 발생합니다.
 * 2. OnIdReleased가 해제 통지 시 발생합니다.
 *
 * 기타 ::
 * 1. Interlocked 기반 단순 증가 값을 사용합니다.
 * 2. 전역 owner 추적 보조 도구입니다.
 * =========================================================
 */
#endif
