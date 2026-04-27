#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * source loader 공통 계약 인터페이스. 한 key 로 한 asset 을 비동기 로드.
 *
 * 주요 기능 ::
 * LoadMode 프로퍼티 (Resources / Addressable 구분).
 * LoadAsync(key) — 비동기 로드 진입점.
 *
 * 사용법 ::
 * AssetProvider 가 loaderTable[LoadMode] = loader 로 등록 후 _ResolveLoader 로 조회.
 * Resources / Addressable 구현체가 본 인터페이스를 구현. release 책임이 있는 loader 는
 * IAssetReleasableLoader 를 추가 구현.
 *
 * 주의 ::
 * 하나의 key 로 하나의 asset 을 가져오는 축을 전제. 라벨 기반 다중 로드 같은 query 성격
 * 기능은 별도 계약 (예: IAddressableLabelLoader) 으로 분리.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoader<TKey, TAsset> {
        AssetLoadMode LoadMode { get; }
        UniTask<TAsset> LoadAsync(TKey key);
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
 * 2026-04-25 (최초 설계) :: IAssetLoader 초기 구현
 * =========================================================
 * 가장 작은 공통 분모의 loader 경계. AssetProvider 가 source 를 추상화하기 위한 Strategy.
 * LoadMode 프로퍼티로 multi-loader 환경 (한 provider 가 Resources + Addressable 동시 지원)
 * 에서 자기 식별. release / query 같은 추가 책임은 별도 인터페이스로 분리.
 * =========================================================
 */
#endif
