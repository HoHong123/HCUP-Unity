#if UNITY_EDITOR
/* =========================================================
 * 구독 기반 데이터 관리자를 정의하는 인터페이스입니다.
 * Subscriber가 특정 데이터를 요청하면 Lease 형태로 제공합니다.
 *
 * 주의사항 ::
 * Lease 패턴을 사용하여 데이터 의존성을 관리합니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;

namespace HUtil.Data {
    /// <summary>
    /// 구독 기반 데이터 제공자(매니저) 규격.
    /// </summary>
    public interface IDataLeaseManager<TKey, TData> {
        UniTask<IDataLease<TKey, TData>> AcquireAsync(IDataSubscriber subscriber, TKey key);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. AcquireAsync
 *    + 데이터 Lease 획득
 *
 * 사용법 ::
 * 1. IDataLeaseManager 구현체에서 Lease 생성 및 관리합니다.
 *
 * 기타 ::
 * 1. Subscriber 기반 데이터 구독 시스템입니다.
 * =========================================================
 */
#endif