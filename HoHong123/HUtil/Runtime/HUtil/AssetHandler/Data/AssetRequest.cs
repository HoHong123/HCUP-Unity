using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProvider 요청 단위를 표현하는 값 타입 스크립트입니다.
 *
 * 주의사항 ::
 * 1. ownerId가 없으면 owner 기반 해제 연결이 생기지 않습니다.
 * 2. key, load mode, fetch mode를 함께 넘겨야 의도가 분명해집니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Data {
    public readonly struct AssetRequest<TKey> {
        #region Properties
        public TKey Key { get; }
        public AssetOwnerId OwnerId { get; }
        public AssetLoadMode LoadMode { get; }
        public AssetFetchMode FetchMode { get; }
        public bool HasOwner => OwnerId.IsValid;
        #endregion

        #region Public - Constructors
        public AssetRequest(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            AssetOwnerId ownerId = default) {

            Key = key;
            LoadMode = loadMode;
            FetchMode = fetchMode;
            OwnerId = ownerId;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. key, owner, load mode, fetch mode를 묶습니다.
 * 2. owner 존재 여부를 HasOwner로 노출합니다.
 *
 * 사용법 ::
 * 1. provider.GetAsync 호출 시 요청 값을 명시적으로 넘길 때 사용합니다.
 * 2. 도메인 계층이 요청 의도를 구조체 하나로 전달할 수 있습니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. provider 내부 로직이 이 값을 기준으로 분기합니다.
 *
 * 기타 ::
 * 1. 읽기 전용 struct입니다.
 * 2. 요청 문맥 전달을 단순화하기 위한 값 타입입니다.
 * =========================================================
 */
#endif
