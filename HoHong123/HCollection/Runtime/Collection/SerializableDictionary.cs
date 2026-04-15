#if UNITY_EDITOR
/* =========================================================
 * Unity 기본 직렬화가 지원하지 않는 Dictionary를
 * Inspector에서 직렬화할 수 있도록 만든 래퍼 클래스입니다.
 *
 * Dictionary 데이터를 List<Entry> 형태로 변환하여
 * Unity Serialization 시스템과 호환되도록 합니다.
 *
 * 주의사항 ::
 * 1. 중복 Key가 존재할 경우 마지막 값이 우선 적용됩니다.
 * 2. OnBeforeSerialize / OnAfterDeserialize 과정에서
 *    Dictionary와 List가 상호 변환됩니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HCollection {
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
        #region Nested Class
        [Serializable]
        private struct Entry {
            public TKey Key;
            public TValue Value;
        }
        #endregion

        #region Fields
        [SerializeField]
        List<Entry> entries = new();

#if UNITY_EDITOR
        [SerializeField]
        bool logDuplicateKeyWarning = true;
#endif
        #endregion

        #region Public - Serialization
        public void OnBeforeSerialize() {
            entries.Clear();

            foreach (var kv in this) {
                entries.Add(new Entry {
                    Key = kv.Key,
                    Value = kv.Value
                });
            }
        }

        public void OnAfterDeserialize() {
            Clear();

            if (entries == null || entries.Count == 0) return;

            for (var k = 0; k < entries.Count; k++) {
                var entry = entries[k];

                // Key가 null일 수 있는 타입(예: string, UnityEngine.Object)인 경우 방어
                if (EqualityComparer<TKey>.Default.Equals(entry.Key, default)) {
#if UNITY_EDITOR
                    // default(TKey)가 유효 키일 수도 있어 경고를 강제하진 않음.
#endif
                }

                if (ContainsKey(entry.Key)) {
#if UNITY_EDITOR
                    if (logDuplicateKeyWarning) {
                        Debug.LogWarning(
                            $"[SerializableDictionary] Duplicate key detected. " +
                            $"Key='{entry.Key}'. Last value wins. Index={k}");
                    }
#endif
                    this[entry.Key] = entry.Value; // “마지막 값 우선”
                    continue;
                }

                Add(entry.Key, entry.Value);
            }
        }
        #endregion

        #region Public - Add
        public bool TryAddOrReplace(TKey key, TValue value) {
            if (ContainsKey(key)) {
                this[key] = value;
                return false;
            }

            Add(key, value);
            return true;
        }
        #endregion

        #region Public - Get
        public TValue GetValueOrDefault(TKey key, TValue defaultValue = default) {
            return TryGetValue(key, out var value) ? value : defaultValue;
        }
        #endregion

#if UNITY_EDITOR
        #region Debug
        public IReadOnlyList<(TKey Key, TValue Value)> DebugSnapshot() {
            var list = new List<(TKey, TValue)>(Count);
            foreach (var kvp in this) list.Add((kvp.Key, kvp.Value));
            return list;
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 
 * 주요 기능 ::
 * 1. OnBeforeSerialize
 *    + Dictionary 데이터를 Entry List로 변환
 * 2. OnAfterDeserialize
 *    + Entry List 데이터를 Dictionary로 복원
 * 3. TryAddOrReplace
 *    + Key 존재 시 값 교체 / 없으면 신규 추가
 * 4. GetValueOrDefault
 *    + Key 미존재 시 기본값 반환
 *
 * 사용법 ::
 * 1. Unity Inspector에서 Dictionary 데이터를 저장할 때 사용합니다.
 * 2. 일반 Dictionary처럼 접근 가능합니다.
 *
 * 기타 ::
 * 1. DebugSnapshot()을 통해 디버그용 데이터 확인이 가능합니다.
 * =========================================================
 */
#endif