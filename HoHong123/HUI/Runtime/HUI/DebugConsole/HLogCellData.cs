using System;
using UnityEngine;
using HUtil.Logger;

namespace HUI.DebugConsole {
    public enum HLogSource : byte {
        HLogger,
        Unity,
    }

    public sealed class HLogCellData {
        public HLogSource Source { get; }
        public LogLevel Level { get; }
        public DateTimeOffset Timestamp { get; }
        public string Message { get; }
        public string Debug { get; }
        public int? TargetInstanceId { get; }
        public string DisplayText { get; }
        public string ClipboardText { get; }

        public HLogCellData(
            HLogSource source,
            LogLevel level,
            DateTimeOffset timestamp,
            string message,
            string debug,
            int? targetInstanceId) {
            Source = source;
            Level = level;
            Timestamp = timestamp;
            Message = message;
            Debug = debug;
            TargetInstanceId = targetInstanceId;
            DisplayText = _BuildDisplayText();
            ClipboardText = _BuildClipboardText();
        }

        private string _BuildDisplayText() {
            string header = $"[{Timestamp:HH:mm:ss}] [{Source}] [{Level}]";
            if (string.IsNullOrEmpty(Debug)) return $"{header} {Message}";
            return $"{header} {Message}\nDebug :: {Debug}";
        }

        private string _BuildClipboardText() {
            string header = $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Source}] [{Level}]";
            if (string.IsNullOrEmpty(Debug)) return $"{header} {Message}";
            return $"{header} {Message}\nDebug :: {Debug}";
        }

        public static LogLevel ToLogLevel(LogType logType) {
            switch (logType) {
            case LogType.Warning:
                return LogLevel.Warn;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                return LogLevel.Error;
            default:
                return LogLevel.Log;
            }
        }
    }
}
