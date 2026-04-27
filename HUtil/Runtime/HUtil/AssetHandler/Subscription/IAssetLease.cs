#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * lease 결과 계약 인터페이스 (IDisposable 기반). 자산 단일 점유의 표현 계층.
 *
 * 주요 기능 ::
 * Key / Asset / OwnerId / IsValid 4 프로퍼티 + Dispose ()  (자동 release).
 *
 * 사용법 ::
 * AssetLeaseManager.AcquireAsync 의 반환 타입. 도메인 코드는 `using lease = await ...` 으로
 * acquire/release 짝맞춤을 컴파일러가 강제하게 만듦. Dispose 호출이 provider.Release(key, ownerId)
 * 위임으로 연결.
 *
 * 주의 ::
 * lease 는 선택 계층 — provider 직접 사용도 가능. lease 자체는 자산을 복제 소유하지 않음.
 * 실제 reference counting 은 provider(cache) 에 있고 lease 는 한 점의 수명 핸들일 뿐.
 * 동일 key 에 다수 lease 가 발급되어도 자산 실체는 provider 쪽에 한 벌만 존재.
 * =========================================================
 */
#endif

using System;

namespace HUtil.AssetHandler.Subscription {
    public interface IAssetLease<TKey, TAsset> : IDisposable {
        TKey Key { get; }
        TAsset Asset { get; }
        AssetOwnerId OwnerId { get; }
        bool IsValid { get; }
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
 * 2026-04-25 (최초 설계) :: IAssetLease 초기 구현
 * =========================================================
 * Provider 의 acquire/release 짝을 IDisposable 패턴으로 표현하기 위한 계약. Dispose 가
 * provider.Release(key, ownerId) 를 위임하므로 lease 자체는 자산 소유권을 가지지 않음.
 * 실제 reference counting 은 cache 한 곳에 단일 보유. lease 는 그 위에 얹힌 표현 계층 —
 * 동일 key 다수 lease 발급되어도 자산 실체는 한 벌. provider 직접 호출과 동등한 효과를
 * 컴파일러가 강제하는 형태로 노출하는 것이 lease 의 본질적 가치.
 * =========================================================
 */
#endif
