#if UNITY_EDITOR
/* =========================================================
 * 프로젝트 공용 로그 시스템입니다.
 * Unity Debug 로그를 확장하여 일관된 로그 포맷을 제공합니다.
 *
 * 특징 ::
 * 로그 레벨 관리
 * 색상 로그 출력
 * 로그 큐 저장
 * GameObject 연결 로그
 * =========================================================
 */
#endif

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if !UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace HUtil.Logger {
    public class HLogger {
        #region Const
        const int MAX_QUE_SIZE = 1000;
        #endregion

        #region Nested Class
        public readonly struct LogEntry {
            public readonly LogLevel Level;
            public readonly DateTimeOffset Timestamp;
            public readonly string Message;
            public readonly string Debug;
            public readonly int? TargetInstanceId;

            public LogEntry(
                LogLevel level,
                DateTimeOffset timestamp,
                string message,
                string debug,
                int? targetInstanceId) {
                Level = level;
                Timestamp = timestamp;
                Message = message;
                Debug = debug;
                TargetInstanceId = targetInstanceId;
            }

            public string ToConsoleString() {
                string levelTag = _GetColoredLevelTag(Level);
                string head = $"{levelTag} [{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] ";
                if (string.IsNullOrEmpty(Debug)) return $"{head}{Message}";
                string debugHead = $"{levelTag} Debug :: ";
                return $"{head}{Message}\n{debugHead}{Debug}";
            }

            private static string _GetColoredLevelTag(LogLevel level) {
                string tag = $"@{(int)level} [{level}]";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return $"<color={_GetLevelColor(level)}>{tag}</color>";
#else
                return tag;
#endif
            }

            private static string _GetLevelColor(LogLevel level) {
                switch (level) {
                case LogLevel.Log: return "#7ED957"; // Light Green
                case LogLevel.Warn: return "#FFD54F"; // Yellow
                case LogLevel.Error: return "#FF5252"; // Red
                default: return "#7ED957";
                }
            }
        }
        #endregion

        #region Log
#if !UNITY_EDITOR
        readonly static Queue<LogEntry> logQue = new();
#endif
        #endregion

        #region Property
        static DateTimeOffset _UtcNow => DateTimeOffset.Now;
        #endregion

        #region Public - Call Logger
        public static void Log(string message, GameObject target = null, bool popupActivate = false) {
#if !UNITY_EDITOR
            _Enqueue(new LogEntry(LogLevel.Log, _UtcNow, message, "", target ? target.GetInstanceID() : null));
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(LogLevel.Log, message, "", target);
#endif

            if (popupActivate) {
                // TODO :: Connect with local PopupManager
                //PopupManager.Instance.AddAlert("Log", message);
            }
        }

        public static void Warning(string message, GameObject target = null, bool popupActivate = false) {
#if !UNITY_EDITOR
            _Enqueue(new LogEntry(LogLevel.Warn, _UtcNow, message, "", target ? target.GetInstanceID() : null));
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(LogLevel.Warn, message, "", target);
#endif

            if (popupActivate) {
                // TODO :: Connect with local PopupManager
                //PopupManager.Instance.AddAlert("Warning", message);
            }
        }

        public static void Error(string message, GameObject target = null, bool showPopup = false, string debug = "") {
#if !UNITY_EDITOR
            _Enqueue(new LogEntry(LogLevel.Error, _UtcNow, message, debug, target ? target.GetInstanceID() : null));
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(LogLevel.Error, message, debug, target);
#endif

            if (showPopup) {
                //PopupManager.Instance.AddAlert("Error", message);
            }
        }

        public static void Exception(Exception ex, string extra = "") {
            string msg = string.IsNullOrEmpty(extra) ? ex.ToString() : $"{extra}\n{ex}";

#if !UNITY_EDITOR
            _Enqueue(new LogEntry(LogLevel.Error, _UtcNow, msg, "", null));
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(LogLevel.Error, msg, "", null);
#endif
        }

        public static Exception Throw(Exception ex, string extra = "") {
            Exception(ex, extra);
            throw ex;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message = "Assertion failed", GameObject target = null) {
            if (condition) return;
            Debug.Assert(false, message, target);
        }
        #endregion

        public static void SendLogsToServer() {
            // TODO :: Implement server communication to send logs
        }

#if !UNITY_EDITOR
        private static void _Enqueue(LogEntry entry) {
            logQue.Enqueue(entry);
            if (logQue.Count > MAX_QUE_SIZE) logQue.Dequeue();
        }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region Private - Print Log
        private static void _ConsoleLog(LogLevel level, string message, string debug, GameObject target) {
            string formatted = new LogEntry(level, _UtcNow, message, debug, target ? target.GetInstanceID() : null).ToConsoleString();

            if (level == LogLevel.Log) {
                if (target == null)
                    Debug.Log(formatted);
                else
                    Debug.Log(formatted, target);
                return;
            }
            else if (level == LogLevel.Warn) {
                if (target == null)
                    Debug.LogWarning(formatted);
                else
                    Debug.LogWarning(formatted, target);
                return;
            }
            else if (level == LogLevel.Error) {
                if (target == null)
                    Debug.LogError(formatted);
                else
                    Debug.LogError(formatted, target);
            }
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH 15.02.26
 * 로그 메시지에 색상 추가
 * =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * Log
 * Warning
 * Error
 * Exception
 * Assert
 * SendLogsToServer
 *
 * 사용법 ::
 * HLogger.Log("message");
 * HLogger.Error("error");
 *
 * 기타 ::
 * HDebug 및 프로젝트 전반의 로그 시스템 기반입니다.
 * =========================================================
 */
#endif