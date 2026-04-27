#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 문자열 관련 확장 기능을 제공하는 유틸리티 클래스입니다.
 *
 * 기능 ::
 * - Query String 파싱
 * - Base64 인코딩 / 디코딩
 * - 숫자 포맷팅
 * - 숫자 단위 축약 표시
 * - 시간 포맷 변환
 * - 문자열 필터링
 * =========================================================
 */
#endif

using System;
using System.Text;
using System.Collections.Generic;

namespace HData.Formattable {
    public static class StringUtil {
        #region Parsing Query
        public static Dictionary<string, string> ParseQueryString(this string query) {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();

            query = query.TrimStart('?');
            string[] pairs = query.Split('&');
            foreach (string pair in pairs) {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2) {
                    string key = Uri.UnescapeDataString(keyValue[0]);
                    string value = Uri.UnescapeDataString(keyValue[1]);
                    queryParams[key] = value;
                }
            }

            return queryParams;
        }
        #endregion

        #region Base64 Parsing
        public static string EncodeStringToBase64(this string plainText) {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            string base64EncodedData = Convert.ToBase64String(plainTextBytes);
            return base64EncodedData;
        }

        public static string DecodeBase64ToString(string base64EncodedData) {
            byte[] decodedBytes = Convert.FromBase64String(base64EncodedData);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);
            return decodedString;
        }
        #endregion

        #region Format Numbers to String
        /// <summary>
        /// Formats the given formattable value by inserting a comma(',')
        /// as a thousands separator every three digits.
        /// </summary>
        public static string FormatNumber<T>(this T number, int decimalCount = 0) where T : struct, IFormattable {
            return number.ToString($"N{decimalCount}", null);
        }

        /// <summary>
        /// Converts a numeric value into a shortened string representation with units 
        /// such as K (thousand), M (million), B (billion), or T (trillion),
        /// or their Korean equivalents like 천, 백만, 십억, 조.
        /// </summary>
        public static string NumToAlpha<T>(this T number, bool useRound = false, bool useKoreanUnit = false) where T : struct, IConvertible {
            double num = Convert.ToDouble(number);

            double Format(double value) {
                double temp = value * 10;
                return useRound ? Math.Round(temp) / 10.0 : Math.Floor(temp) / 10.0;
            }

            // Ps. Compilar ignore under line.
            if (num >= 1_000_000_000_000)
                return $"{Format(num / 1_000_000_000_000.0):0.0}" + (useKoreanUnit ? "조" : "T");
            else if (num >= 1_000_000_000)
                return $"{Format(num / 1_000_000_000.0):0.0}" + (useKoreanUnit ? "십억" : "B");
            else if (num >= 1_000_000)
                return $"{Format(num / 1_000_000.0):0.0}" + (useKoreanUnit ? "백만" : "M");
            else if (num >= 10_000)
                return $"{Format(num / 1_000.0):0.0}" + (useKoreanUnit ? "천" : "K");

            return num.ToString("0");
        }
        #endregion

        #region Format Numbers to Time
        /// <summary>Seconds => 'HH:MM:SS' (Less than 1 hour format to 'MM:SS')</summary>
        public static string ToClock(this float seconds) {
            int tot = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.FloorToInt(seconds));
            int hh = tot / 3600;
            int mm = (tot % 3600) / 60;
            int ss = tot % 60;
            return (hh > 0) ? $"{hh:00}:{mm:00}:{ss:00}" : $"{mm:00}:{ss:00}";
        }

        public static string ToClock(this TimeSpan remain) {
            float seconds = (float)Math.Max(0.0, remain.TotalSeconds);
            return seconds.ToClock();
        }
        #endregion

        #region Filter
        public static string FilterText(this string src, string key) {
            if (string.IsNullOrWhiteSpace(key)) return src;

            var lines = src.Split('\n');
            var sb = new StringBuilder();
            foreach (var k in lines) {
                if (k.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    sb.AppendLine(k);
            }

            return sb.ToString();
        }
        #endregion

        #region Check Uni-NewLine
        public static bool IsMultiLine(this string value) {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.Contains('\n')) return true; // Unix
            if (value.Contains('\r')) return true; // 구형 Mac, 기타
            return false;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * QueryString 파싱
 * Base64 문자열 변환
 * 숫자 포맷팅
 * 숫자 단위 축약 표시
 * 시간 포맷 변환
 * 문자열 필터링
 *
 * 주요 API ::
 * ParseQueryString
 * FormatNumber
 * NumToAlpha
 * ToClock
 * FilterText
 *
 * 사용법 ::
 * 1000000.NumToAlpha()
 * 90f.ToClock()
 *
 * 기타 ::
 * 프로젝트 전반에서 사용하는 문자열 유틸리티 모음입니다.
 * =========================================================
 */
#endif