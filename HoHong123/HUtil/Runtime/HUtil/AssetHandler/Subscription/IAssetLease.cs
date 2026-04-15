using System;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * lease 결과 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. lease는 선택 계층이며 필수 구조는 아닙니다.
 * 2. Dispose 의미를 명확히 알고 사용해야 합니다.
 * =========================================================
 */
#endif

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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. key, asset, ownerId 노출 계약을 제공합니다.
 * 2. 유효성 확인과 Dispose 계약을 제공합니다.
 *
 * 사용법 ::
 * 1. lease manager가 반환하는 구독 결과로 사용합니다.
 * 2. 사용이 끝나면 Dispose로 release를 연결합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. Dispose 호출이 release 흐름과 연결됩니다.
 *
 * 기타 ::
 * 1. IDisposable 기반 경계입니다.
 * 2. owner-aware provider를 래핑하는 보조 표현입니다.
 * 3. Dispose는 내부적으로 provider.Release(Key, OwnerId)를 호출할 뿐이며,
 *    lease 자체는 자산을 복제 소유하지 않습니다. 실제 reference counting은 provider(cache)에 있습니다.
 * 4. 따라서 동일 key에 대해 다수의 lease가 발급되어도 자산 실체는 provider 쪽에 단 한 벌만 존재합니다.
 * =========================================================
 */
#endif
