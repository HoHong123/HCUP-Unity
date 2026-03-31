using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * asset 조회와 해제 진입 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 단순 조회와 owner 기반 해제 의미를 함께 이해하고 사용해야 합니다.
 * 2. TryGet과 GetAsync의 의도를 혼동하지 않아야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Provider {
    public interface IAssetProvider<TKey, TAsset> {
        UniTask<TAsset> GetAsync(AssetRequest<TKey> request);
        UniTask<TAsset> GetAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            AssetOwnerId ownerId = default);

        bool TryGet(TKey key, out TAsset asset);

        bool Release(TKey key);
        bool Release(TKey key, AssetOwnerId ownerId);
        int ReleaseOwner(AssetOwnerId ownerId);
        void ReleaseAll();
        void ClearCache();

        UniTask ClearStoreAsync();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 요청 구조체 기반 GetAsync를 제공합니다.
 * 2. 직접 인자 기반 GetAsync를 제공합니다.
 * 3. key, owner, 전체 해제 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. repository와 도메인 코드가 공통 진입점으로 사용합니다.
 * 2. owner lifecycle이 있는 경로는 ReleaseOwner를 같이 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 구현체가 cache와 loader 이벤트를 내부에서 연결할 수 있습니다.
 *
 * 기타 ::
 * 1. 도메인 계층이 source 세부를 모르도록 감싸는 경계입니다.
 * 2. generic key와 asset 타입을 지원합니다.
 * =========================================================
 */
#endif
