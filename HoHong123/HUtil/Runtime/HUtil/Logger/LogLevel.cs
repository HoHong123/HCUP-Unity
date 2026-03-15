#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로그 시스템에서 사용하는 로그 레벨 열거형입니다.
 * =========================================================
 */
#endif

namespace HUtil.Logger {
    public enum LogLevel : byte {
        Debug,
        Log,
        Warn,
        Error,
        Fatal,
        Assert,
    }
}