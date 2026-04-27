#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 조회 책임만 분리한 ISP 계약 인터페이스.
 *
 * 주요 기능 ::
 * TryLoad(key) — 익명 점유로 조회.
 * TryLoad(key, ownerId) — owner 점유 등록과 함께 조회.
 * TryGet(key) — 점유 갱신 없이 단순 존재 확인.
 *
 * 사용법 ::
 * IAssetCache 가 본 인터페이스 + IAssetWriter + IAssetReleaser 를 합집합으로 노출.
 * 읽기 전용 cache decorator 구현 시 본 인터페이스만 구현 가능.
 *
 * 주의 ::
 * TryLoad 는 점유 등록 (AnonymousDependency++ 또는 Owners.Add) 을 동반.
 * TryGet 은 단순 조회 (점유 갱신 없음). 두 메서드의 의미를 구현체에서 일관되게 유지.
 * =========================================================
 */
#endif

using HUtil.AssetHandler.Subscription;

namespace HUtil.AssetHandler.Cache {
    public interface IAssetReader<TKey, TAsset> {
        bool TryLoad(TKey key, out TAsset asset);
        bool TryLoad(TKey key, AssetOwnerId ownerId, out TAsset asset);
        bool TryGet(TKey key, out TAsset asset);
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
 * 2026-04-25 (최초 설계) :: IAssetReader 초기 구현
 * =========================================================
 * IAssetCache 의 ISP (Interface Segregation Principle) 분리. Reader / Writer / Releaser 를
 * 별도 계약으로 분리하여 부분 책임만 노출하는 cache decorator 구현이 가능하도록.
 * 합집합인 IAssetCache 는 OnAssetRemoved 이벤트만 추가.
 * =========================================================
 */
#endif
