#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 의 중심 진입점 계약 인터페이스. 도메인 코드가 알아야 할 유일한 경계.
 *
 * 주요 기능 ::
 * GetAsync (AssetRequest / 4 인자) — 비동기 조회 (fetch mode 분기).
 * TryGet — 비동기 없이 cache 만 단순 조회.
 * Release / Release(key, ownerId) / ReleaseOwner / ReleaseAll / ClearCache — 점유 해제 5 갈래.
 * ClearStoreAsync — store 영속 데이터 비움.
 *
 * 사용법 ::
 * Repository / 도메인 코드가 본 인터페이스로 자산 조회. owner lifecycle 이 있는 경로는
 * ReleaseOwner 짝맞춤. lease 표현이 필요하면 위에 AssetLeaseManager 를 얹어 사용.
 *
 * 주의 ::
 * Provider 는 자산의 실제 Load/Cache/Validate/Release 책임을 소유하는 유일한 경계 — 실 보유자.
 * Subscription 의 IAssetLeaseManager / IAssetLease 는 위에 얹힌 선택 계층일 뿐, 실제 자산
 * 수명은 언제나 provider(cache) 의 Release 경로를 통해 종료. 오너 단위 일괄 해제와 전역
 * 해제는 provider 에만 존재 — lease 계층은 단일 key 짝맞춤만 제공.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;

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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (도입 + 주의사항 + 주요 기능 + 사용법 + 이벤트 + 기타 6 섹션, 상하단 분산) 을
 * 한 곳에 통합하여 §11 형틀 통일. 하단 Dev Log 영역 추가.
 * 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드 (기존 가드 유지).
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용. 상단/하단 분산 헤더를 한 곳으로 통합하여 reader 가
 * 한 화면에서 인터페이스 의도를 모두 파악 가능하도록.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: IAssetProvider 초기 구현
 * =========================================================
 * 도메인 계층이 source 세부 (Resources / Addressable / Store) 를 모르도록 감싸는 단일 경계.
 * generic key + asset 타입 지원. 두 GetAsync 오버로드 (4 인자 직접 / AssetRequest struct) 로
 * 호출자 의도 표현 자유도 확보. Release 5 갈래는 점유 단위 (단일 key / single owner / 전체)
 * 와 의미 (해제 vs cache 비움 vs store 비움) 의 조합. ReleaseOwner / ReleaseAll 같은 대량
 * 정리는 provider 에만 노출 (lease 계층은 단일 짝맞춤만).
 * =========================================================
 */
#endif
