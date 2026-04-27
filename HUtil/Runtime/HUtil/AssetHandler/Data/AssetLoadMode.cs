#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Asset source 종류를 구분하는 열거형 (Resources / Addressable).
 *
 * 주요 기능 ::
 * loader 선택 기준. AssetProvider.loaderTable 의 키로 사용.
 *
 * 사용법 ::
 * AssetRequest 또는 GetAsync 의 인자로 전달. provider 가 등록된 loader 중 일치하는
 * LoadMode 의 loader 를 _ResolveLoader 로 찾아 위임.
 *
 * 주의 ::
 * fetch mode (CacheFirst / SourceOnly 등) 와 다른 개념. 확장 시 provider 매핑도 함께 검토.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Data {
    public enum AssetLoadMode : byte {
        Resources = 0,
        Addressable = 1,
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
 * 2026-04-25 (최초 설계) :: AssetLoadMode 초기 구현
 * =========================================================
 * loader 다형성을 enum 키로 표현. AssetProvider 가 Dictionary<AssetLoadMode, IAssetLoader>
 * 로 loader 를 등록하고, 요청 시 LoadMode 로 즉시 조회. byte 기반으로 메모리 절약.
 * =========================================================
 */
#endif
