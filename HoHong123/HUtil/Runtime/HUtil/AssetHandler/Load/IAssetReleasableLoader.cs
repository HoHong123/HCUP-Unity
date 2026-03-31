#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * source release가 필요한 loader 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. cache release와 source release는 다른 책임입니다.
 * 2. Addressable 같은 source만 선택적으로 이 경계를 구현합니다.
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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. key 단위 source release 계약을 제공합니다.
 * 2. 전체 source release 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. provider가 cache 제거 후 source handle 정리까지 연결할 때 사용합니다.
 * 2. release가 필요 없는 loader는 이 인터페이스를 구현하지 않아도 됩니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. provider release 흐름과 함께 호출됩니다.
 *
 * 기타 ::
 * 1. 기존 loader 계약을 확장합니다.
 * 2. source 수명 책임을 분리하기 위한 보조 경계입니다.
 * =========================================================
 */
#endif
