using System;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시의 통합 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 읽기, 쓰기, 해제 계약을 함께 구현해야 합니다.
 * 2. 실제 제거 시점은 OnAssetRemoved 이벤트로 전달됩니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Cache {
    public interface IAssetCache<TKey, TAsset> :
        IAssetReader<TKey, TAsset>,
        IAssetWriter<TKey, TAsset>,
        IAssetReleaser<TKey> {

        event Action<TKey, TAsset> OnAssetRemoved;
    }
}
