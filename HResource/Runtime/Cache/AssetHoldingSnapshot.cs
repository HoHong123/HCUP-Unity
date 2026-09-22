#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 소유자 한 명이 잡고 있는 key 목록을 담는 진단용 스냅샷입니다.
 *
 * 특징 / 지원기능 ::
 * 소유자 기준 뷰(Owner Tracker)의 원본입니다. 캐시의 ownerTable 을 그대로 옮깁니다.
 * + key 기준 뷰는 AssetOccupancySnapshot 이 따로 맡습니다
 *
 * 주의사항 ::
 * 에디터 진단 전용이라 통째로 #if UNITY_EDITOR 로 감쌉니다. 빌드에는 들어가지 않습니다.
 * Keys 는 문자열입니다. 캐시의 TKey 를 ToString 으로 지웁니다.
 * 점유가 유무라 잡고 있는 key 수는 곧 Keys 의 개수입니다.
 * =========================================================
 */

using System.Collections.Generic;

namespace HResource.Cache {
    public readonly struct AssetHoldingSnapshot {
        #region Fields
        public readonly int OwnerId;
        public readonly IReadOnlyList<string> Keys;
        #endregion

        #region 생성자
        public AssetHoldingSnapshot(int ownerId, IReadOnlyList<string> keys) {
            OwnerId = ownerId;
            Keys = keys;
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-23 (최초 설계) :: AssetHoldingSnapshot 생성
 *
 * 변경 ::
 * 소유자 기준 스냅샷을 새로 만들었다. IAssetCacheDiagnostics.CaptureHoldings 가 채운다.
 *
 * 이유 ::
 * 캐시가 key 마다 소유자 목록을 들지 않게 되면서(MemoryAssetCache 09-23) 소유자 목록의 정본이
 * ownerTable 이 됐다. 소유자 기준 뷰는 그것을 그대로 옮기면 되므로 key 기준 스냅샷을 에디터에서
 * 다시 뒤집을 이유가 없다.
 *
 * 주의 ::
 * 두 방향 스냅샷은 Scan 시점이 따로라 서로 다른 순간을 보여줄 수 있다.
 * =============================================================================
 */
#endif
