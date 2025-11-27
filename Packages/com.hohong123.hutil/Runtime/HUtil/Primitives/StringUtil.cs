#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * string 추가 기능 스크립트입니다.
 * =========================================================
 */
#endif

using System;
using System.Text;
using System.Collections.Generic;

namespace HUtil.Formattable {
    public static class StringUtil {
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

        /// <summary>Seconds => 'HH:MM:SS' (Less than 1 hour format to 'MM:SS')</summary>
        public static string ToClock(this float seconds) {
            int tot = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.FloorToInt(seconds));
            int hh = tot / 3600;
            int mm = (tot % 3600) / 60;
            int ss = tot % 60;
            return (hh > 0) ? $"{hh:00}:{mm:00}:{ss:00}" : $"{mm:00}:{ss:00}";
        }
    }
}