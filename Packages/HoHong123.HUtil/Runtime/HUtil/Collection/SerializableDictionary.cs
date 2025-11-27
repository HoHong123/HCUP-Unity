#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity 직렬화를 위한 Dictionary 래퍼입니다.
 * 내부적으로 keys/values 리스트를 유지하고 직렬화 훅에서 동기화합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HUtil.Collection {
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver {
        [SerializeField]
        List<TKey> keys = new();
        [SerializeField]
        List<TValue> values = new();

        readonly Dictionary<TKey, TValue> dictionary = new();

        public Dictionary<TKey, TValue> Dictionary => dictionary;
        public int Count => dictionary.Count;
        public void Clear() => dictionary.Clear();
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool Remove(TKey key) => dictionary.Remove(key);
        public void Add(TKey key, TValue value) => dictionary[key] = value;
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public IEnumerable<KeyValuePair<TKey, TValue>> Pairs() => dictionary;


        public void OnBeforeSerialize() {
            if (!Application.isPlaying) {
                dictionary.Clear();
                int count = Mathf.Min(keys.Count, values.Count);
                for (int i = 0; i < count; i++) {
                    TKey key = keys[i];
                    TValue val = values[i];
                    if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, val);
                }
            }
            else {
                keys.Clear();
                values.Clear();
                foreach (var pair in dictionary) {
                    keys.Add(pair.Key);
                    values.Add(pair.Value);
                }
            }
        }

        public void OnAfterDeserialize() {
            dictionary.Clear();
            int count = Mathf.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++) {
                TKey key = keys[i];
                TValue val = values[i];
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, val);
            }

            // 런타임 메모리 최적화를 위해 복원 끝났으면 리스트 메모리 해제
            if (Application.isPlaying) {
                // Size = 0
                keys.Clear();
                values.Clear();
                // Capacity = 0
                keys.TrimExcess();
                values.TrimExcess();
            }
        }
    }
}
