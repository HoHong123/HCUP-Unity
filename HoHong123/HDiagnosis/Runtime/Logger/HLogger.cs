#if UNITY_EDITOR
/* =========================================================
 * 프로젝트 공용 로그 시스템입니다.
 * Unity Debug 로그를 확장하여 일관된 로그 포맷을 제공합니다.
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

namespace HDiagnosis.Logger {
    public class HLogger {
        #region Const
        const int MAX_QUE_SIZE = 1000;
        #endregion

        #region Event
        public static event Action<LogEntry> OnLogPublished;
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
                case LogLevel.Log: return "#7ED957";
                case LogLevel.Warn: return "#FFD54F";
                case LogLevel.Error: return "#FF5252";
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
            LogEntry entry = new(LogLevel.Log, _UtcNow, message, "", target ? target.GetInstanceID() : null);
            _Publish(entry);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(entry, target);
#endif

            if (popupActivate) {
                // TODO :: Connect with local PopupManager
            }
        }

        public static void Warning(string message, GameObject target = null, bool popupActivate = false) {
            LogEntry entry = new(LogLevel.Warn, _UtcNow, message, "", target ? target.GetInstanceID() : null);
            _Publish(entry);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(entry, target);
#endif

            if (popupActivate) {
                // TODO :: Connect with local PopupManager
            }
        }

        public static void Error(string message, GameObject target = null, bool showPopup = false, string debug = "") {
            LogEntry entry = new(LogLevel.Error, _UtcNow, message, debug, target ? target.GetInstanceID() : null);
            _Publish(entry);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(entry, target);
#endif

            if (showPopup) {
                // TODO :: Connect with local PopupManager
            }
        }

        public static void Exception(Exception ex, string extra = "") {
            string msg = string.IsNullOrEmpty(extra) ? ex.ToString() : $"{extra}\n{ex}";
            LogEntry entry = new(LogLevel.Error, _UtcNow, msg, "", null);
            _Publish(entry);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ConsoleLog(entry, null);
#endif
        }

        public static Exception Throw(Exception ex, string extra = "", bool doThrow = true) {
            Exception(ex, extra);
            if (doThrow) throw ex;
            return null;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message = "Assertion failed", GameObject target = null) {
            if (condition) return;
            Debug.Assert(false, message, target);
        }

        public static void SendLogsToServer() {
            // TODO :: Implement server communication to send logs
        }
        #endregion

        #region Private
        private static void _Publish(LogEntry entry) {
#if !UNITY_EDITOR
            logQue.Enqueue(entry);
            if (logQue.Count > MAX_QUE_SIZE) logQue.Dequeue();
#endif
            OnLogPublished?.Invoke(entry);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void _ConsoleLog(LogEntry entry, GameObject target) {
            string formatted = entry.ToConsoleString();

            if (entry.Level == LogLevel.Log) {
                if (target == null) Debug.Log(formatted);
                else Debug.Log(formatted, target);
                return;
            }

            if (entry.Level == LogLevel.Warn) {
                if (target == null) Debug.LogWarning(formatted);
                else Debug.LogWarning(formatted, target);
                return;
            }

            if (entry.Level == LogLevel.Error) {
                if (target == null) Debug.LogError(formatted);
                else Debug.LogError(formatted, target);
            }
        }
#endif
        #endregion
    }
}
