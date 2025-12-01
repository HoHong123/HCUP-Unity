#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 콜랙션 컨테이너에 적용할 수 있는 공통 유틸리티 기능들을 가진 클래스입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HUtil.Logger;

namespace HUtil.Collection {
    public static class CollectionUtil {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIndexInRange<T>(this IReadOnlyList<T> source, int index) => (uint)index < (uint)source.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRangeValid(int start, int length, int count) => (uint)start <= (uint)count && (uint)length <= (uint)(count - start);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRangeValid(this string s, int start, int length) => IsRangeValid(start, length, s.Length);

        #region Shuffle
        public static void Shuffle<T>(this IList<T> collection) {
            System.Random rand = new System.Random();

            int n = collection.Count;
            while (n > 1) {
                int k = rand.Next(--n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
        }
        #endregion

        #region Add Range
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> existingBook, Dictionary<TKey, TValue> newBook) {
            foreach (var kvp in newBook) {
                if (!existingBook.ContainsKey(kvp.Key))
                    existingBook.Add(kvp.Key, kvp.Value);
            }
        }

        public static void AddRange<T>(this Queue<T> que, IEnumerable<T> items) {
            foreach (var item in items) {
                que.Enqueue(item);
            }
        }

        public static void AddRange<T>(this Stack<T> stack, IEnumerable<T> items) {
            foreach (var item in items) {
                stack.Push(item);
            }
        }
        #endregion

        #region Random
        public static T RandomElement<T>(this IList<T> list) {
            if (list == null || list.Count == 0)
                HLogger.Throw(new InvalidOperationException("Empty list."));
            return list[_NextIndex(list.Count)];
        }

        public static bool TryRandomElement<T>(this IList<T> list, out T value) {
            if (list != null && list.Count > 0) {
                value = list[_NextIndex(list.Count)];
                return true;
            }
            value = default;
            return false;
        }

        public static KeyValuePair<TKey, TValue> RandomEntry<TKey, TValue>(this IDictionary<TKey, TValue> dict) {
            if (dict == null || dict.Count == 0)
                HLogger.Throw(new InvalidOperationException("Empty dictionary."));
            int target = _NextIndex(dict.Count);
            int k = 0;
            foreach (var kv in dict) {
                if (k++ == target)
                    return kv;
            }
            throw HLogger.Throw(new InvalidOperationException("Enumeration failed."));
        }

        public static TKey RandomKey<TKey, TValue>(this IDictionary<TKey, TValue> dict)
            => dict.RandomEntry().Key;

        public static TValue RandomValue<TKey, TValue>(this IDictionary<TKey, TValue> dict)
            => dict.RandomEntry().Value;

        public static bool TryRandomEntry<TKey, TValue>(this IDictionary<TKey, TValue> dict, out KeyValuePair<TKey, TValue> entry) {
            if (dict != null && dict.Count > 0) {
                entry = dict.RandomEntry();
                return true;
            }
            entry = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int _NextIndex(int count) => UnityEngine.Random.Range(0, count);
        #endregion

        /// <summary>
        /// Search target in ienumerable and nullify.
        /// </summary>
        /// <typeparam name="T">IList type</typeparam>
        /// <param name="array">Base list</param>
        /// <param name="target">Target element</param>
        /// <returns></returns>
        public static bool NullifyTarget<T>(this IList<T> array, T target) where T : class {
            for (int k = 0; k < array.Count; k++) {
                if (array[k] == target) {
                    array[k] = null;
                    return true;
                }
            }
            return false;
        }

        public static int IndexOfReference<T>(this IList<T> array, T target) where T : class {
            for (int k = 0; k < array.Count; k++) {
                if (array[k] == target) {
                    return k;
                }
            }
            return -1;
        }

        public static bool TryGetIndexOf<T>(this IList<T> array, T target, out int index) where T : class {
            for (int k = 0; k < array.Count; k++) {
                if (array[k] == target) {
                    index = k;
                    return true;
                }
            }
            index = -1;
            return false;
        }
    }
}