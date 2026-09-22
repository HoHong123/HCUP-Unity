#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 캐시에 올라온 항목 하나의 점유 현황을 담는 진단용 스냅샷입니다.
 *
 * 특징 / 지원기능 ::
 * key 하나에 대해 총 점유 수와 그 key 를 잡고 있는 소유자 목록을 담습니다.
 * + 리소스 기준 뷰(Resource Ownership)의 원본입니다. 소유자 기준은 AssetHoldingSnapshot 이 맡습니다
 *
 * 주의사항 ::
 * 에디터 진단 전용이라 통째로 #if UNITY_EDITOR 로 감쌉니다.
 * Key 는 문자열입니다. 캐시의 TKey 가 무엇이든 ToString 으로 지웁니다.
 * 진단 창이 제네릭 타입을 특정할 수 없기 때문입니다.
 * TotalCount 는 캐시가 제거 판정에 쓰는 소유자 수, Owners 는 ownerTable 을 뒤집은 목록입니다. 둘이 다르면 인덱스 불일치입니다.
 * =========================================================
 */

using System.Collections.Generic;

namespace HResource.Cache {
    public readonly struct AssetOccupancySnapshot {
        #region Fields
        public readonly string Key;
        public readonly int TotalCount;
        public readonly IReadOnlyList<AssetOwnerOccupancy> Owners;
        #endregion

        #region 생성자
        public AssetOccupancySnapshot(string key, int totalCount, IReadOnlyList<AssetOwnerOccupancy> owners) {
            Key = key;
            TotalCount = totalCount;
            Owners = owners;
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-23 (수정) :: 소유자 기준 뷰를 AssetHoldingSnapshot 으로 분리
 *
 * 변경 ::
 * 이 스냅샷은 key 기준 뷰만 맡는다. TotalCount 의 출처가 캐시의 OwnerCount 로 바뀌었다.
 *
 * 이유 ::
 * 캐시가 key 마다 소유자 목록을 들지 않게 되어 Owners 는 ownerTable 을 뒤집어 만든다.
 * 수와 목록의 출처가 달라졌으므로 TotalCount 는 더 이상 Owners.Count 의 사본이 아니라 대조 값이다.
 *
 * 주의 ::
 * 아래 09-04 의 "모양을 key 중심 하나로 통일" 결정은 이 항목으로 바뀌었다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.04 AssetOccupancySnapshot 베이스 코드 생성
 *
 * # 목적
 * - 캐시 내부의 점유 현황을 제네릭을 지운 형태로 에디터에 넘긴다.
 *
 * # 설계 결정
 * - 스냅샷 모양을 key 중심 하나로 통일했다. 소유자 중심 뷰는 에디터에서 뒤집어 만든다.
 *   런타임 공개 표면을 하나만 늘리고, 두 패널이 같은 원본에서 나와 서로 어긋날 수 없다.
 * - TotalCount 를 캡처 시점에 담는다. 표시 측이 매 프레임 다시 세지 않는다.
 *
 * =============================================================================
 */
#endif
