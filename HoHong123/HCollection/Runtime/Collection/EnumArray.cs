#if UNITY_EDITOR
/* =========================================================
 * Enum 값을 인덱스로 사용하는 배열 래퍼 클래스입니다.
 * Enum → int 변환을 통해 배열 접근을 수행합니다.
 *
 * 주의사항 ::
 * 1. Enum 값은 반드시 0..N-1 형태의 연속 값이어야 합니다.
 * 2. Enum 값이 배열 범위를 벗어나면 Index 오류가 발생할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HCollection {
    [Serializable]
    public sealed class EnumArray<TEnum, TValue>
        where TEnum : unmanaged, Enum {
        #region Fields
        [SerializeField]
        TValue[] values;
        #endregion

        #region Properties
        public int Length => values?.Length ?? 0;
        #endregion

        #region Public - Getters
        public TValue this[TEnum key] {
            get {
                return values[Convert.ToInt32(key)];
            }
            set {
                values[Convert.ToInt32(key)] = value;
            }
        }

        public TValue[] GetRawValues() {
            return values;
        }

        public bool TryGetValue(TEnum key, out TValue value) {
            var index = Convert.ToInt32(key);
            if ((uint)index >= (uint)values.Length) {
                value = default;
                return false;
            }

            value = values[index];
            return true;
        }

        private static int _GetEnumCount() {
            // EquipRarityType처럼 0..N-1 연속 enum을 전제
            return Enum.GetValues(typeof(TEnum)).Length;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. this[TEnum key]
 *    + Enum 키로 배열 값 접근
 * 2. TryGetValue
 *    + Enum 키 기반 안전 조회
 * 3. GetRawValues
 *    + 내부 배열 직접 접근
 *
 * 사용법 ::
 * 1. EnumArray<EnumType, TValue> 형태로 생성합니다.
 * 2. enum 값을 키로 사용하여 배열처럼 접근합니다.
 *
 * 기타 ::
 * 1. 내부적으로 Convert.ToInt32를 사용하여 Enum을 Index로 변환합니다.
 * =========================================================
 */
#endif