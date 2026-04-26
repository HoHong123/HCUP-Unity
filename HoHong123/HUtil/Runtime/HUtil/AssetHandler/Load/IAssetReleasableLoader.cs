#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * source release 가 필요한 loader 를 위한 확장 계약 인터페이스 (IAssetLoader 상속).
 *
 * 주요 기능 ::
 * Release(key) — key 단위 source 핸들 정리.
 * ReleaseAll() — 전체 source 핸들 정리.
 *
 * 사용법 ::
 * Addressable 같이 명시 release 가 필요한 loader 만 본 인터페이스 구현. AssetProvider 는
 * 등록된 loader 중 본 인터페이스 구현체만 releasableLoaders List 로 별도 관리하여
 * cache 제거 시 (OnAssetRemoved) source release 연쇄.
 *
 * 주의 ::
 * cache release 와 source release 는 다른 책임. Resources 같이 release 가 불필요한 loader 는
 * 본 인터페이스 미구현 (IAssetLoader 만 구현). provider 는 release 연쇄 시 본 인터페이스
 * 구현체만 순회하여 dispatch 비용 0.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public interface IAssetReleasableLoader<TKey, TAsset> : IAssetLoader<TKey, TAsset> {
        bool Release(TKey key);
        void ReleaseAll();
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
 * 2026-04-25 (최초 설계) :: IAssetReleasableLoader 초기 구현
 * =========================================================
 * IAssetLoader 의 확장 — release 책임이 있는 loader 만 추가 구현. provider 가 등록된
 * loader 들을 두 List 로 분리 (모든 loader / releasable loader 만) 하여 release 연쇄 시
 * release 가능한 것들만 순회. Resources 같은 release 불필요 loader 는 본 계약 미구현으로
 * release 순회에서 자연 제외 (성능 + 의도 표현 동시 달성).
 * =========================================================
 */
#endif
