using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * cache 바깥 영속 저장소 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. store는 선택 계층이므로 없는 조합도 허용됩니다.
 * 2. asset 직렬화, 저장 경로, 삭제 규칙은 구현체 책임입니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Store {
    public interface IAssetStore<TKey, TAsset> {
        UniTask<bool> HasAsync(TKey key);
        UniTask<TAsset> LoadAsync(TKey key);
        UniTask SaveAsync(TKey key, TAsset asset);
        UniTask DeleteAsync(TKey key);
        UniTask ClearAsync();
    }
}
