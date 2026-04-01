using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 source loader 공통 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 하나의 key로 하나의 asset을 가져오는 축을 전제로 합니다.
 * 2. release 책임이 필요한 loader는 별도 계약을 함께 구현해야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoader<TKey, TAsset> {
        AssetLoadMode LoadMode { get; }
        UniTask<TAsset> LoadAsync(TKey key);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. load mode 노출 계약을 제공합니다.
 * 2. 비동기 LoadAsync 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. provider가 load mode별 loader를 등록할 때 사용합니다.
 * 2. Resources와 Addressable loader가 이 인터페이스를 구현합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 실제 로드 결과는 provider 흐름에 전달됩니다.
 *
 * 기타 ::
 * 1. 가장 작은 공통 분모의 로더 경계입니다.
 * 2. query 성격 기능은 별도 계약으로 분리합니다.
 * =========================================================
 */
#endif
