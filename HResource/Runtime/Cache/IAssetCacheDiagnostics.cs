#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 캐시가 에디터 진단 도구에 점유 현황을 내주는 계약입니다.
 *
 * 특징 / 지원기능 ::
 * 제네릭을 지운 표면입니다. 진단 창이 TKey / TAsset 을 몰라도 캐시를 다룰 수 있습니다.
 * + CaptureOccupancy 는 호출자가 준 버퍼를 채웁니다. 매 리페인트마다 할당하지 않기 위함입니다
 * + ForceReleaseOwner 는 점유를 강제로 내려놓습니다. orphan 판정은 호출자가 집니다
 *
 * 주의사항 ::
 * 에디터 진단 전용이라 통째로 #if UNITY_EDITOR 로 감쌉니다. 빌드 공개 표면은 늘지 않습니다.
 * 구현체는 AssetCacheDiagnosticsRegistry 에 자신을 등록해야 창이 찾을 수 있습니다.
 *
 * 사용 ::
 * List<AssetOccupancySnapshot> buffer = new();
 * diagnostics.CaptureOccupancy(buffer);
 * =========================================================
 */

using System.Collections.Generic;

namespace HResource.Cache {
    public interface IAssetCacheDiagnostics {
        /// <summary> 진단 창에 표시할 캐시 식별 문자열 </summary>
        string CacheLabel { get; }

        /// <summary> 현재 캐시에 올라와 있는 항목 수 </summary>
        int EntryCount { get; }

        /// <summary> 호출자가 준 버퍼를 비우고 현재 점유 현황으로 채운다 </summary>
        void CaptureOccupancy(List<AssetOccupancySnapshot> buffer);

        /// <summary> 이 소유자의 점유를 강제 해제. 반환값은 회수한 key 수. 생존 판정은 호출자 몫 </summary>
        int ForceReleaseOwner(int ownerId);
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-06 (수정) :: ForceReleaseOwner 추가
 * =============================================================================
 * 변경 ::
 * int ownerId 로 그 소유자의 점유를 강제 해제하는 메서드를 넣었다.
 *
 * 이유 ::
 * 워처가 orphan 을 지우려면 신원을 지목해야 하는데 AssetOwnerId 는 어셈블리 밖에서 만들 수 없다.
 *
 * 결과 ::
 * 워처 툴바의 Orphan Clean 이 이 메서드로 정리한다.
 *
 * 주의 ::
 * 생존을 묻지 않는다. 판정은 호출자 몫이다.
 * 이 계약은 통째로 에디터 전용이라 빌드 공개 표면은 늘지 않는다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.04 IAssetCacheDiagnostics 베이스 코드 생성
 *
 * # 목적
 * - 진단 창이 살아있는 캐시의 점유를 읽을 유일한 경로를 만든다.
 *
 * # 설계 결정
 * - 반환값 대신 버퍼를 받는다. 창에 자동 리페인트를 붙이므로 초당 수 회 호출된다.
 *   매번 리스트를 새로 만들면 에디터에서 불필요한 GC 압력이 된다.
 * - IAssetCache 를 상속하지 않는 독립 계약이다. 캐시가 아닌 것도 진단만 내줄 수 있고,
 *   런타임 캐시 계약이 진단 때문에 오염되지 않는다.
 *
 * =============================================================================
 */
#endif
