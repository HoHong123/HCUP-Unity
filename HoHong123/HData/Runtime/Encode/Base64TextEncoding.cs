#if UNITY_EDITOR
/* =========================================================
 * Base64 기반 텍스트 인코딩 / 디코딩 유틸리티입니다.
 *
 * 특징 ::
 * Base64 문자열 앞에 Prefix를 추가하여 데이터 형식 검증 및 식별을 지원합니다.
 *
 * 목적 ::
 * PlayerPrefs 등 문자열 기반 저장소에 byte[] 데이터를 안전하게 저장하기 위함입니다.
 * =========================================================
 */
#endif

using System;

namespace HUtil.Encode {
    public sealed class Base64TextEncoding : ITextEncoding {
        #region Fields
        readonly string prefix;
        #endregion

        #region Public - Constructors
        public Base64TextEncoding(string prefix = "") {
            this.prefix = prefix ?? string.Empty;
        }
        #endregion

        #region Public - Encode
        public string Encode(byte[] data) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(data, "[Base64TextEncoding] Encode data array is null.");
#endif
            return prefix + Convert.ToBase64String(data);
        }
        #endregion

        #region Public - Decode
        public bool TryDecode(string text, out byte[] data) {
            data = Array.Empty<byte>();

            if (string.IsNullOrEmpty(text)) return false;

            if (!string.IsNullOrEmpty(prefix)) {
                if (!text.StartsWith(prefix, StringComparison.Ordinal)) return false;
                text = text.Substring(prefix.Length);
            }

            try {
                data = Convert.FromBase64String(text);
                return true;
            }
            catch {
                return false;
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * Encode
 *  + byte[] → Base64 string
 * TryDecode
 *  + Base64 string → byte[]
 *
 * 옵션 ::
 * prefix
 *  + 데이터 식별 문자열
 *
 * 사용법 ::
 * var encoding = new Base64TextEncoding("KEY::");
 * string encoded = encoding.Encode(bytes);
 *
 * 기타 ::
 * PlayerPrefs 저장 시스템과 함께 사용됩니다.
 * =========================================================
 */
#endif
