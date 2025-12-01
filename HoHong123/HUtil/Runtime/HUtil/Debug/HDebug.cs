#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 함수 스택을 기록하고 디버깅하는 기능을 제공하는 스크립트입니다.
 * 탐색할 함수 스택 깊이를 설정하거나 출력할 로그 레벨을 설정할 수 있습니다.
 * =========================================================
 */
#endif

using System.Diagnostics;
using HUtil.Logger;

namespace HUtil.Diagnosis {
    public static class HDebug {
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


#if UNITY_EDITOR
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
#else
        private static string _LogInternal(string message) => message;
        private static string _GetFormattedStackTrace(int maxFrames = 3, string message = "") => message;
#endif
    }
}

#if UNITY_EDITOR
/* @Jason
 * This is a utility class that tracks stack memory.
 * @Jason
 * Can be replaced by 'Assert'.
 */
#endif