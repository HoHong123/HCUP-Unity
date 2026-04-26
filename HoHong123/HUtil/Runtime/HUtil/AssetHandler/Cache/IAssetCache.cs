#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 통합 계약 인터페이스. ISP 분리된 Reader/Writer/Releaser 의 합집합.
 *
 * 주요 기능 ::
 * IAssetReader + IAssetWriter + IAssetReleaser 를 한 인터페이스로 통합 노출.
 * OnAssetRemoved 이벤트 추가 — cache → loader 자동 release 연쇄의 trigger.
 *
 * 사용법 ::
 * AssetProvider 가 본 인터페이스로 cache 컴포넌트를 주입받아 조회/저장/해제 통합 호출.
 * 부분 책임만 필요하면 Reader/Writer/Releaser 한 갈래만 구현 가능.
 *
 * 주의 ::
 * OnAssetRemoved 는 실제 테이블 제거 시점에만 발생 (점유가 모두 비었을 때).
 * 단순 Release 호출은 점유 카운터 감소만 일으키고 이벤트는 안 발생할 수 있음.
 * =========================================================
 */
#endif

using System;

namespace HUtil.AssetHandler.Cache {
    public interface IAssetCache<TKey, TAsset> :
        IAssetReader<TKey, TAsset>,
        IAssetWriter<TKey, TAsset>,
        IAssetReleaser<TKey> {

        event Action<TKey, TAsset> OnAssetRemoved;
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
 * 2026-04-25 (최초 설계) :: IAssetCache 초기 구현
 * =========================================================
 * SOLID 의 I (Interface Segregation Principle) 가 코드 결정으로 살아있는 위치.
 * Reader / Writer / Releaser 3 갈래로 책임 분리 + 합집합으로 IAssetCache 정의.
 * AssetProvider 가 OnAssetRemoved 이벤트를 구독하여 releasable loader 의 source release 연쇄.
 * 이로써 Cache 와 Loader 의 결합도 0 (이벤트 한 줄로 묶임).
 * =========================================================
 */
#endif
