#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 저장 책임만 분리한 ISP 계약 인터페이스.
 *
 * 주요 기능 ::
 * Save(key, asset) — 익명 점유로 저장.
 * Save(key, asset, ownerId) — owner 점유 등록과 함께 저장.
 *
 * 사용법 ::
 * AssetProvider 가 source 로드 후 cache 에 결과 저장 시 호출.
 * IAssetCache 합집합의 일부.
 *
 * 주의 ::
 * owner 가 있는 저장은 점유 등록 정책 (Owners.Add + ownerTable 역인덱스) 을 동반.
 * null/invalid asset 처리는 구현체 정책 (MemoryAssetCache 는 false 반환 + Reject).
 * =========================================================
 */
#endif

using HUtil.AssetHandler.Subscription;

namespace HUtil.AssetHandler.Cache {
    public interface IAssetWriter<TKey, TAsset> {
        bool Save(TKey key, TAsset asset);
        bool Save(TKey key, TAsset asset, AssetOwnerId ownerId);
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
 * 2026-04-25 (최초 설계) :: IAssetWriter 초기 구현
 * =========================================================
 * IAssetCache 의 ISP 분리 (Reader/Writer/Releaser 3 갈래). 저장 단일 책임만 노출.
 * 두 Save 오버로드는 owner 인지 여부에 따른 점유 등록 정책 차이를 시그니처로 표현.
 * =========================================================
 */
#endif
