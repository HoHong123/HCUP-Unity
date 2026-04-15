#if UNITY_EDITOR
/* =========================================================
 * 호출 위치 및 스택 정보를 출력하기 위한 에디터 전용 디버깅 유틸리티 클래스입니다.
 *
 * 특징 ::
 * Conditional("UNITY_EDITOR") 적용 빌드 시 모든 호출 코드가 제거됩니다.
 *
 * 목적 ::
 * 함수 호출 흐름 및 스택 추적을 간단히 하기 위함입니다.
 * =========================================================
 */
#endif

using System.Diagnostics;
using HDiagnosis.Logger;

namespace HDiagnosis.HDebug {
    public static class HDebug {
        #region Public - Callers
        [Conditional("UNITY_EDITOR")]
        public static void LogCaller(string message = "") {
            HLogger.Log(_LogInternal(message));
        }

        [Conditional("UNITY_EDITOR")]
        public static void WarningCaller(string message = "") {
            HLogger.Warning(_LogInternal(message));
        }

        [Conditional("UNITY_EDITOR")]
        public static void ErrorCaller(string message = "") {
            HLogger.Error(_LogInternal(message));
        }

        [Conditional("UNITY_EDITOR")]
        public static void StackTraceLog(string message = "", int frameCount = 3) {
            HLogger.Log(_GetFormattedStackTrace(frameCount, message));
        }

        [Conditional("UNITY_EDITOR")]
        public static void StackTraceWarning(string message = "", int frameCount = 3) {
            HLogger.Warning(_GetFormattedStackTrace(frameCount, message));
        }

        [Conditional("UNITY_EDITOR")]
        public static void StackTraceError(string message = "", int frameCount = 3) {
            HLogger.Error(_GetFormattedStackTrace(frameCount, message));
        }
        #endregion

#if UNITY_EDITOR
        #region Private - Trace
        private static string _LogInternal(string message) {
            var trace = new StackTrace(2, false);
            var frames = trace.GetFrames();

            if (frames == null || frames.Length == 0) {
                return $"[Unknown] {message}";
            }

            var method = frames[0]?.GetMethod();
            var className = method?.DeclaringType?.Name ?? "UnknownClass";
            var methodName = method?.Name ?? "UnknownMethod";

            return $"[DEBUG ({className}.{methodName})] {message}";
        }

        private static string _GetFormattedStackTrace(int maxFrames = 3, string message = "") {
            var trace = new StackTrace(2, false);
            var frames = trace.GetFrames();

            // null check
            if (frames == null || frames.Length == 0) return "[DEBUG] No stack trace available";

            // stack frame legnth check.
            int frameCount = UnityEngine.Mathf.Min(maxFrames, frames.Length);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(message)) sb.AppendLine(message);

            for (int k = 0; k < frameCount; k++) {
                var method = frames[k].GetMethod();
                var className = method.DeclaringType?.Name ?? "UnknownClass";
                var methodName = method.Name;

                sb.AppendLine($"{k + 1}. {className}.{methodName}()");
            }
            
            return sb.ToString();
        }
        #endregion
#else
        private static string _LogInternal(string message) => message;
        private static string _GetFormattedStackTrace(int maxFrames = 3, string message = "") => message;
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * LogCaller
 * WarningCaller
 * ErrorCaller
 *
 * StackTraceLog
 * StackTraceWarning
 * StackTraceError
 *
 * 사용법 ::
 * HDebug.LogCaller("message");
 * HDebug.StackTraceLog("message", depth);
 *
 * 기타 ::
 * HLogger와 함께 사용되는 디버깅 유틸리티입니다.
 * =========================================================
 */
#endif