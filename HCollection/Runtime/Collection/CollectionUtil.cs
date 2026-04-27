#if UNITY_EDITOR
/* =========================================================
 * 컬렉션(List, Dictionary, Queue, Stack 등)에 공통적으로
 * 사용할 수 있는 유틸리티 기능을 제공하는 정적 클래스입니다.
 *
 * 다양한 컬렉션 조작 및 변환 기능을 제공합니다.
 *
 * 주의사항 ::
 * 1. 일부 기능은 IList / IDictionary 인터페이스에 의존합니다.
 * 2. Random 관련 기능은 UnityEngine.Random을 사용합니다.
 * 3. null 입력 시 예외를 발생시키는 함수가 포함되어 있습니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HDiagnosis.Logger;

namespace HCollection {
    public static class CollectionUtil {
        #region Range Check
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIndexInRange<T>(this IReadOnlyList<T> source, int index) => (uint)index < (uint)source.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRangeValid(int start, int length, int count) => (uint)start <= (uint)count && (uint)length <= (uint)(count - start);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRangeValid(this string s, int start, int length) => IsRangeValid(start, length, s.Length);
        #endregion

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

        #region Nullify
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
        #endregion

        #region Search
        public static int IndexOfReference<T>(this IList<T> array, T target) where T : class {
            for (int k = 0; k < array.Count; k++) {
                if (array[k] == target) return k;
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
        #endregion

        #region Remove
        // List Removal
        public static int RemoveIf<T>(this IList<T> list, Predicate<T> predicate) {
            if (list == null) HLogger.Throw(new ArgumentNullException(nameof(list)));
            if (predicate == null) HLogger.Throw(new ArgumentNullException(nameof(predicate)));

            if (list is List<T> concrete) return concrete.RemoveAll(predicate);

            var removeCount = 0;
            for (var k = list.Count - 1; k > -1; k--) {
                if (!predicate(list[k])) continue;
                list.RemoveAt(k);
                removeCount++;
            }

            return removeCount;
        }

        // Array Removal
        public static T[] RemoveIf<T>(this T[] array, Predicate<T> predicate) {
            if (array == null) HLogger.Throw(new ArgumentNullException(nameof(array)));
            if (predicate == null) HLogger.Throw(new ArgumentNullException(nameof(predicate)));

            var result = new List<T>();
            for (var k = 0; k < array.Length; k++) {
                if (predicate(array[k])) continue;
                result.Add(array[k]);
            }

            return result.ToArray();
        }

        // Pair Data Removal
        public static int RemoveIf<TKey, TValue>(this IDictionary<TKey, TValue> dic, Predicate<TValue> predicate)
        where TKey : notnull {
            if (dic == null) HLogger.Throw(new ArgumentNullException(nameof(dic)));
            if (predicate == null) HLogger.Throw(new ArgumentNullException(nameof(predicate)));

            List<TKey> keysToRemove = null;

            foreach (var kv in dic) {
                if (!predicate(kv.Value)) continue;
                keysToRemove ??= new List<TKey>();
                keysToRemove.Add(kv.Key);
            }

            if (keysToRemove == null) return 0;

            var removed = 0;
            for (var k = 0; k < keysToRemove.Count; k++) {
                if (dic.Remove(keysToRemove[k])) removed++;
            }

            return removed;
        }
        #endregion

        #region ToList / ToHashSet
        public static List<T> ToListFast<T>(this IEnumerable<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is List<T> list) return list;
            if (source is ICollection<T> col) {
                var result = new List<T>(col.Count);
                result.AddRange(col);
                return result;
            }
            return new List<T>(source);
        }

        public static List<T> CloneList<T>(this IEnumerable<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is ICollection<T> col) {
                var result = new List<T>(col.Count);
                foreach (var item in col) result.Add(item);
                return result;
            }
            return new List<T>(source);
        }
        #endregion

        #region ToHashSet
        public static HashSet<T> ToHashSetFast<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return comparer == null ? new HashSet<T>(source) : new HashSet<T>(source, comparer);
        }
        #endregion

        #region ToStack

        /// <summary>
        /// Stack은 열거 순서대로 Push하면 마지막 원소가 Top이 됩니다.
        /// 예) [1,2,3] -> Push 1,2,3 => Pop 순서: 3,2,1
        /// </summary>
        public static Stack<T> ToStack<T>(this IEnumerable<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // Stack<T>(IEnumerable<T>) 는 열거 순서대로 push와 동일한 의미.
            // 다만 용량 사전 할당 최적화를 위해 ICollection<T> 경우 수동 구성.
            if (source is ICollection<T> col) {
                var stack = new Stack<T>(col.Count);
                foreach (var item in source) stack.Push(item);
                return stack;
            }
            return new Stack<T>(source);
        }
        #endregion

        #region ToQueue
        /// <summary>
        /// Queue는 열거 순서대로 Enqueue.
        /// 예) [1,2,3] => Dequeue 순서: 1,2,3
        /// </summary>
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is ICollection<T> col) {
                var queue = new Queue<T>(col.Count);
                foreach (var item in source) queue.Enqueue(item);
                return queue;
            }
            return new Queue<T>(source);
        }
        #endregion

        #region ToDictionary (index as key)
        /// <summary>
        /// 인덱스를 키로 Dictionary 생성.
        /// 예) ["a","b"] => {0:"a", 1:"b"}
        /// </summary>
        public static Dictionary<int, T> ToIndexDictionary<T>(
            this IEnumerable<T> source,
            int startIndex = 0) {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Dictionary<int, T> dict;
            if (source is ICollection<T> col) {
                dict = new Dictionary<int, T>(col.Count);
            }
            else {
                dict = new Dictionary<int, T>();
            }

            var index = startIndex;
            foreach (var item in source) {
                dict.Add(index, item);
                index++;
            }

            return dict;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Range Check
 *    + Index 및 Range 유효성 검사
 * 2. Shuffle
 *    + IList 랜덤 셔플
 * 3. AddRange
 *    + Dictionary / Queue / Stack 확장 추가
 * 4. Random
 *    + List / Dictionary 랜덤 요소 조회
 * 5. Remove
 *    + Predicate 기반 요소 제거
 * 6. Conversion
 *    + IEnumerable → List / HashSet / Stack / Queue 변환
 *
 * 사용법 ::
 * 1. 컬렉션 객체에 Extension Method 형태로 호출합니다.
 *    예) list.Shuffle();
 *
 * 2. RandomElement / RandomEntry로 랜덤 데이터를 조회합니다.
 *
 * 기타 ::
 * 1. 일부 메서드는 성능 최적화를 위해 AggressiveInlining을 사용합니다.
 * =========================================================
 */
#endif