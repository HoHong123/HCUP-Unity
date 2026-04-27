#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * lease 발급 계약 인터페이스. IAssetProvider 의 owner 기능 구독을 강제하여 메모리 리크를
 * 방지하는 선택 계층.
 *
 * 주요 기능 ::
 * AcquireAsync(IAssetOwner / AssetOwnerId, key, loadMode, fetchMode) — lease 비동기 발급.
 *
 * 사용법 ::
 * 구독형 acquire/release 표현이 필요할 때 사용. 도메인 코드는 `using lease = await mgr.AcquireAsync(...)`
 * 패턴으로 acquire/release 짝맞춤을 컴파일러가 강제하게 만듦.
 *
 * 주의 ::
 * lease 계층은 선택 기능. provider 직접 호출도 동등 효과. 본 인터페이스는 Acquire/Dispose
 * 짝맞춤 (단일 key 단위) 만 제공 — 오너 단위 일괄 해제 (ReleaseOwner) 나 전역 해제 (ReleaseAll)
 * 는 IAssetProvider 직접 호출.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Subscription {
    public interface IAssetLeaseManager<TKey, TAsset> {
        UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            IAssetOwner owner,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            AssetOwnerId ownerId,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);
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
 * 2026-04-25 (최초 설계) :: IAssetLeaseManager 초기 구현
 * =========================================================
 * Provider 위에 얹는 IDisposable 표현 계층. 두 AcquireAsync 오버로드는 owner 객체 전달 vs
 * ownerId 직접 전달 두 경로 분리. 일반 provider 경계를 래핑하는 역할 — 파사드가 아니라
 * Dispose 편의 표현. 두 경계 (provider + leaseManager) 를 함께 참조하는 사용처가 자연스러움
 * (예: OnDestroy 에서 lease.Dispose + provider.ReleaseOwner).
 * =========================================================
 */
#endif
