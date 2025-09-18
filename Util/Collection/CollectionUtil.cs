#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 콜랙션 컨테이너에 적용할 수 있는 공통 유틸리티 기능들을 가진 클래스입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Util.Logger;

namespace Util.Collection {
    public static class CollectionUtil {
        public static List<T> SafeGetRange<T>(this List<T> list, int index, int count) {
            if (list == null || index >= list.Count) return new List<T>();
            int safeCount = Math.Min(count, list.Count - index);
            return list.GetRange(index, safeCount);
        }

        public static void Shuffle<T>(this IList<T> collection) {
            if (collection == null) HLogger.Throw(new ArgumentException(nameof(collection)));
            Random rand = new Random();
            int n = collection.Count;
            while (n > 1) {
                int k = rand.Next(--n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
        }

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