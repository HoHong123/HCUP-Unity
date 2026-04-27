#if UNITY_EDITOR
/* =========================================================
 * 데이터 사용 권한을 표현하는 Lease 인터페이스입니다.
 * Lease는 특정 데이터에 대한 사용 권한을 의미합니다.
 *
 * 주의사항 ::
 * Dispose 호출 시 반드시 Release 로직이 수행됩니다.
 * =========================================================
 */
#endif

using System;

namespace HUtil.Data {
    /// <summary>
    /// 구독권(핸들). Dispose 시 반드시 Release가 수행되어야 한다.
    /// </summary>
    public interface IDataLease<TKey, TData> : IDisposable {
        TKey Key { get; }
        TData Data { get; }
        int OwnerId { get; }
        bool IsValid { get; }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Key
 * 2. Data
 * 3. OwnerId
 * 4. IsValid
 *
 * 사용법 ::
 * 1. AcquireAsync 호출 시 Lease 형태로 반환됩니다.
 *
 * 기타 ::
 * 1. Dispose 호출 시 데이터 의존성이 해제됩니다.
 * =========================================================
 */
#endif