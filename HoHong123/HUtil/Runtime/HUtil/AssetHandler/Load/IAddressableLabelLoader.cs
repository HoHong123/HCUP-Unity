using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Addressable label 조회 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 단일 key 단일 asset 계약과는 다른 query 경계입니다.
 * 2. 조회 방식마다 결과 의미와 release 단위가 다릅니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public interface IAddressableLabelLoader<TAsset> {
        UniTask<IList<TAsset>> LoadAllAsync(string label);
        UniTask<TAsset> LoadFirstAsync(string label);
        UniTask<TAsset> LoadSingleAsync(string label);
        UniTask<TAsset> LoadByIndexAsync(string label, int index);

        bool ReleaseAllByLabel(string label);
        bool ReleaseFirstByLabel(string label);
        bool ReleaseSingleByLabel(string label);
        bool ReleaseByLabelIndex(string label, int index);
        void ReleaseAll();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. all, first, single, index 조회 계약을 제공합니다.
 * 2. label query release 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. label 검색이 필요한 Addressable 기능에서만 참조합니다.
 * 2. 일반 AssetProvider 대신 별도 query 도구로 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 구현체가 handle 관리와 release를 담당합니다.
 *
 * 기타 ::
 * 1. Addressable 전용 계약입니다.
 * 2. token/address loader와 역할을 의도적으로 분리합니다.
 * =========================================================
 */
#endif
