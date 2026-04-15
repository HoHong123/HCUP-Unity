#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 시간 관련 계산을 지원하는 유틸리티 클래스입니다.
 * 시간 계산 및 UI 표시를 위한 공통 시간 처리 기능을 제공합니다.
 *
 * 제공 기능 ::
 * - 쿨타임 남은 시간 계산
 * - 쿨타임 시작 Tick 계산
 * - 시간 준비 여부 확인
 * - 남은 시간 문자열 포맷 변환
 * - UTC 날짜 비교
 * =========================================================
 */
#endif

using System;

namespace HUtil.HTime {
    public static class TimeUtil {
        #region Remain Time
        public static TimeSpan GetRemaining(this DateTime utcNow, long nextAvailableUtcTicks) {
            if (nextAvailableUtcTicks <= 0) return TimeSpan.Zero;
            long diff = nextAvailableUtcTicks - utcNow.Ticks;
            return diff <= 0 ? TimeSpan.Zero : TimeSpan.FromTicks(diff);
        }

        public static bool IsReady(this DateTime utcNow, long nextAvailableUtcTicks) {
            return GetRemaining(utcNow, nextAvailableUtcTicks) <= TimeSpan.Zero;
        }

        public static string FormatRemaining(
            this TimeSpan remaining,
            string underOneHourFormat = "{0:00}:{1:00}",
            string oneHourOrMoreFormat = "{0}:{1:00}:{2:00}") {
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            int totalHours = (int)Math.Floor(remaining.TotalHours);
            int totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
            int minutes = remaining.Minutes;
            int seconds = remaining.Seconds;

            if (totalHours >= 1) return string.Format(oneHourOrMoreFormat, totalHours, minutes, seconds);

            return string.Format(underOneHourFormat, totalMinutes, seconds);
        }

        public static string FormatRemainingAuto(this TimeSpan remaining) {
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            // 1시간 미만이면 mm:ss, 이상이면 hh:mm:ss
            if (remaining.TotalHours >= 1) {
                int hour = (int)Math.Floor(remaining.TotalHours);
                return $"{hour:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
            }

            int minute = (int)Math.Floor(remaining.TotalMinutes);
            return $"{minute:00}:{remaining.Seconds:00}";
        }

        public static string ToTime(this float? seconds) {
            if (seconds == null) return string.Empty;
            return ToTime(seconds.Value);
        }
        public static string ToTime(this float seconds) {
            if (seconds < 0f) seconds = 0f;
            
            var ts = TimeSpan.FromSeconds(seconds);
            
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            
            int minute = (int)ts.TotalMinutes;
            return $"{minute:00}:{ts.Seconds:00}";
        }

        public static string FormatTimeMs(long milliseconds, string format = "{0:00}:{1:00}:{2:00}") {
            var ts = TimeSpan.FromMilliseconds(milliseconds);
            var min = ts.Minutes;
            var sec = ts.Seconds;
            var mili = ts.Milliseconds;
            return string.Format(format, min, sec, mili);
        }

        public static string FormatTimeMs(float milliseconds, string format = "{0:00}:{1:00}:{2:00}") {
            var ts = TimeSpan.FromMilliseconds(milliseconds);
            var min = ts.Minutes;
            var sec = ts.Seconds;
            var mili = ts.Milliseconds;
            return string.Format(format, min, sec, mili);
        }
        #endregion

        #region Add Tick
        public static long StartCooldownTicks(this DateTime utcNow, TimeSpan cooldown) {
            return utcNow.Add(cooldown).Ticks;
        }
        #endregion

        #region Check Date
        public static bool IsSameUtcDate(this DateTime a, DateTime b) {
            var utcA = a.Kind == DateTimeKind.Utc ? a : a.ToUniversalTime();
            var utcB = b.Kind == DateTimeKind.Utc ? b : b.ToUniversalTime();
            return utcA.Date == utcB.Date;
        }

        public static bool IsTodayUtc(this DateTime target) {
            var nowUtc = DateTime.UtcNow;
            var targetUtc = target.Kind == DateTimeKind.Utc ? target : target.ToUniversalTime();
            return nowUtc.Date == targetUtc.Date;
        }
        #endregion
    }
}