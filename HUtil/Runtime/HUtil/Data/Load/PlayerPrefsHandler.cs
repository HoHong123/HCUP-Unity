#if UNITY_EDITOR
/* =========================================================
 * PlayerPrefs 데이터를 안전하게 저장/조회하기 위한 래퍼 유틸리티 클래스입니다.
 * Key와 Value는 Base64 인코딩을 통해 저장됩니다.
 *
 * 주의사항 ::
 * 1. 저장 데이터가 손상된 경우 자동으로 기본값으로 복구됩니다.
 * =========================================================
 */
#endif

using System.Text;
using System.Globalization;
using UnityEngine;
using HData.Encode;

namespace HUtil.Data.Load {
    public static class PlayerPrefsHandler {
        #region Const
        // prefix를 두면 나중에 마이그레이션/디버그 시 구분이 쉽습니다.
        const string KEY_PREFIX = "JPXKEY::";
        const string VALUE_PREFIX = "JPXVLU::";
        #endregion

        #region Private - Fields
        static readonly Base64TextEncoding KeyEncoding = new Base64TextEncoding(KEY_PREFIX);
        static readonly Base64TextEncoding ValueEncoding = new Base64TextEncoding(VALUE_PREFIX);
        #endregion

        #region Public - Utility
        public static bool HasKey(string key) => PlayerPrefs.HasKey(_EncodeKey(key));
        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(_EncodeKey(key));
        public static void DeleteAll() => PlayerPrefs.DeleteAll();
        #endregion

        #region Public - Int
        public static int GetInt(string key, int defaultValue = 0) {
            var encodedKey = _EncodeKey(key);
            var plain = _GetPlainOrDefault(encodedKey, defaultValue.ToString(CultureInfo.InvariantCulture));

            if (int.TryParse(plain, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;

            // 파싱 실패 시도 기본값 복구
            SetInt(key, defaultValue);
            return defaultValue;
        }

        public static void SetInt(string key, int value) {
            var encodedKey = _EncodeKey(key);
            PlayerPrefs.SetString(
                encodedKey,
                _EncodeValue(value.ToString(CultureInfo.InvariantCulture))
            );
            PlayerPrefs.Save();
        }
        #endregion

        #region Public - Float
        public static float GetFloat(string key, float defaultValue = 0f) {
            var encodedKey = _EncodeKey(key);
            var plain = _GetPlainOrDefault(encodedKey, defaultValue.ToString(CultureInfo.InvariantCulture));

            if (float.TryParse(plain, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;

            // 파싱 실패 시도 기본값 복구
            SetFloat(key, defaultValue);
            return defaultValue;
        }

        public static void SetFloat(string key, float value) {
            var encodedKey = _EncodeKey(key);
            PlayerPrefs.SetString(
                encodedKey,
                _EncodeValue(value.ToString(CultureInfo.InvariantCulture))
            );
            PlayerPrefs.Save();
        }
        #endregion

        #region Public - Bool (Int Proxy)
        public static bool GetBool(string key, bool defaultValue = false) {
            return GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public static void SetBool(string key, bool value) {
            SetInt(key, value ? 1 : 0);
        }
        #endregion

        #region Public - String
        public static string GetString(string key, string defaultValue = "") {
            var encodedKey = _EncodeKey(key);
            return _GetPlainOrDefault(encodedKey, defaultValue ?? string.Empty);
        }

        public static void SetString(string key, string value) {
            var encodedKey = _EncodeKey(key);
            PlayerPrefs.SetString(encodedKey, _EncodeValue(value ?? string.Empty));
            PlayerPrefs.Save();
        }
        #endregion

        #region Private - Encode
        private static string _EncodeKey(string key) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(key), "[PlayerPrefsHandler] Key is null or empty.");
#endif
            return KeyEncoding.Encode(Encoding.UTF8.GetBytes(key));
        }

        private static string _EncodeValue(string plain) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(plain, "[PlayerPrefsHandler] Value string is null.");
#endif
            return ValueEncoding.Encode(Encoding.UTF8.GetBytes(plain));
        }

        private static bool _TryDecodeValue(string encoded, out string plain) {
            plain = string.Empty;
            if (!ValueEncoding.TryDecode(encoded, out var bytes)) return false;

            try {
                plain = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch {
                return false;
            }
        }
        #endregion

        #region Private - Core
        private static string _GetOrCreateEncodedValue(string encodedKey, string defaultPlainValue) {
            if (!PlayerPrefs.HasKey(encodedKey)) {
                var encodedValue = _EncodeValue(defaultPlainValue);
                PlayerPrefs.SetString(encodedKey, encodedValue);
                PlayerPrefs.Save();
                return encodedValue;
            }
            return PlayerPrefs.GetString(encodedKey);
        }

        private static string _GetPlainOrDefault(string encodedKey, string defaultPlainValue) {
            var encodedValue = _GetOrCreateEncodedValue(encodedKey, defaultPlainValue);
            if (_TryDecodeValue(encodedValue, out var plain)) return plain;
            // 값이 깨졌거나 prefix/포맷이 바뀐 경우: 안전하게 기본값으로 복구
            var fallbackEncoded = _EncodeValue(defaultPlainValue);
            PlayerPrefs.SetString(encodedKey, fallbackEncoded);
            PlayerPrefs.Save();
            return defaultPlainValue;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Int / Float / Bool / String 저장 및 조회
 * 2. Base64 인코딩 저장
 * 3. 데이터 손상 자동 복구
 *
 * 사용법 ::
 * 1. PlayerPrefsHandler.SetInt(key,value)
 * 2. PlayerPrefsHandler.GetInt(key)
 *
 * 기타 ::
 * 1. 내부적으로 PlayerPrefs.Save()가 자동 호출됩니다.
 * =========================================================
 */
#endif