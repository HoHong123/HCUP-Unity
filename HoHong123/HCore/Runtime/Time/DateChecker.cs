#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * UTC 기준 날짜 변경 여부를 확인하는 클래스입니다.
 * 마지막 확인 시각을 암호화하여 PlayerPrefs에 저장합니다.
 *
 * 기능 ::
 * - 마지막 저장된 UTC 날짜 관리
 * - 새로운 UTC 날짜 여부 판별
 * - 날짜 차이 계산
 * =========================================================
 */
#endif

using System;
using System.Text;
using HData.Encode;
using HData.Encrypt;
using HUtil.Data.Load;

namespace HCore.HTime {
    public sealed class DateChecker {
        #region Const
        const string LAST_CHECK_KEY = "date_checker_last_utc_ticks";
        #endregion

        #region Fields
        readonly IEncryptor encryptor;
        readonly ITextEncoding encoding;
        #endregion

        #region Properties
        public bool IsNewDay { get; private set; } = false;
        public DateTime LastSavedUtc { get; private set; } = DateTime.MinValue;
        public DateTime LastCheckedUtc { get; private set; } = DateTime.MinValue;
        #endregion

        #region Public - Constructor
        public DateChecker(IEncryptor encryptor, ITextEncoding encoding, bool evaluateImmediately = true) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(encryptor, "[DateChecker] encryptor is null.");
            UnityEngine.Assertions.Assert.IsNotNull(encoding, "[DateChecker] encoding is null.");
#endif
            this.encryptor = encryptor;
            this.encoding = encoding;

            _LoadLastUtc();

            if (evaluateImmediately) EvaluateNewDayAndSave();
        }
        #endregion

        #region Public - Save Data
        public void EvaluateNewDayAndSave() {
            _EvaluateNewDay();
            _SaveLastUtc(LastCheckedUtc);
        }

        /// <summary> 입력 UTC 날짜와 마지막 저장 UTC 날짜의 일수 차이(signed)를 반환 </summary>
        public int GetDayDifferenceFromLastUtc(DateTime targetUtc) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsTrue(targetUtc.Kind == DateTimeKind.Utc, "[DateChecker] targetUtc.Kind must be Utc.");
#endif
            if (LastSavedUtc == DateTime.MinValue) return 0;
            return (targetUtc.Date - LastSavedUtc.Date).Days;
        }

        public void ClearSavedStamp() {
            PlayerPrefsHandler.DeleteKey(LAST_CHECK_KEY);

            LastSavedUtc = DateTime.MinValue;
            LastCheckedUtc = DateTime.MinValue;
            IsNewDay = true;
        }
        #endregion

        #region Private - Checker
        private static bool _IsDifferentUtcDate(DateTime firstDate, DateTime secondDate) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsTrue(firstDate.Kind == DateTimeKind.Utc, "[DateChecker] firstDate.Kind must be Utc.");
            UnityEngine.Assertions.Assert.IsTrue(secondDate.Kind == DateTimeKind.Utc, "[DateChecker] secondDate.Kind must be Utc.");
#endif
            return firstDate.Date != secondDate.Date;
        }
        #endregion

        #region Private - Calculate Day
        /// <summary> UTC 기준으로 새 날인지 확인 </summary>
        private void _EvaluateNewDay() {
            LastCheckedUtc = DateTime.UtcNow;
            IsNewDay = LastSavedUtc == DateTime.MinValue || _IsDifferentUtcDate(LastSavedUtc, LastCheckedUtc);
        }
        #endregion

        #region Private - Save/Load
        private void _SaveLastUtc(DateTime utc) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsTrue(utc.Kind == DateTimeKind.Utc, "[DateChecker] utc.Kind must be Utc.");
#endif
            var plainBytes = Encoding.UTF8.GetBytes(utc.Ticks.ToString());
            var cipherBytes = encryptor.Encrypt(plainBytes);
            var text = encoding.Encode(cipherBytes);

            PlayerPrefsHandler.SetString(LAST_CHECK_KEY, text);

            LastSavedUtc = utc;
        }

        private void _LoadLastUtc() {
            if (!PlayerPrefsHandler.HasKey(LAST_CHECK_KEY)) {
                LastSavedUtc = DateTime.MinValue;
                return;
            }

            var text = PlayerPrefsHandler.GetString(LAST_CHECK_KEY, string.Empty);
            if (string.IsNullOrEmpty(text)) {
                LastSavedUtc = DateTime.MinValue;
                return;
            }

            if (!encoding.TryDecode(text, out var cipherBytes)) {
                LastSavedUtc = DateTime.MinValue;
                return;
            }

            if (!encryptor.TryDecrypt(cipherBytes, out var plainBytes)) {
                LastSavedUtc = DateTime.MinValue;
                return;
            }

            var ticksString = Encoding.UTF8.GetString(plainBytes);
            if (!long.TryParse(ticksString, out var ticks)) {
                LastSavedUtc = DateTime.MinValue;
                return;
            }

            LastSavedUtc = new DateTime(ticks, DateTimeKind.Utc);
        }
        #endregion
    }
}
