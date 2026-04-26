#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProvider 요청 단위를 표현하는 readonly struct (값 객체).
 *
 * 주요 기능 ::
 * Key + LoadMode + FetchMode + OwnerId 4 인자를 한 덩어리로 묶기.
 * HasOwner 프로퍼티 (OwnerId.IsValid 위임) 로 owner 인지 여부 단축 표현.
 *
 * 사용법 ::
 * provider.GetAsync(AssetRequest) 오버로드의 인자. 도메인 코드가 요청 의도를 한 곳에 응축
 * 하여 호출 지점에서 의도 (CacheFirst / SourceOnly / owner-aware / 익명 등) 를 명시.
 *
 * 주의 ::
 * readonly struct 라 heap 할당 0. ownerId 가 default(=None) 면 owner 기반 해제 연결 없음.
 * key/loadMode/fetchMode 를 함께 넘겨야 의도가 명확.
 * =========================================================
 */
#endif

using HUtil.AssetHandler.Subscription;

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
 * 2026-04-25 (최초 설계) :: AssetRequest 초기 구현
 * =========================================================
 * 4 인자 readonly struct 로 요청 문맥을 단일 값 객체화. heap 할당 0 + 의도 응축이 핵심.
 * provider.GetAsync 의 두 오버로드 (4 인자 직접 / 1 인자 struct) 가 동일 흐름으로 수렴 —
 * 도메인 측에서 의도가 또렷하면 struct 형태로 전달, 짧은 호출이면 직접 인자로 전달.
 * HasOwner 프로퍼티는 OwnerId.IsValid 의 의미적 alias (호출 시 가독성 향상).
 * =========================================================
 */
#endif
