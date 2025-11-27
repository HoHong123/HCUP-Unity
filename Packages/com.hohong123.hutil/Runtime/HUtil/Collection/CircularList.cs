#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 순환 리스트입니다.
 * 순환 탐색, 반환, 삽입, 삭제, 출력, 제거 기능이 구현되어 있습니다.
 * =========================================================
 */
#endif

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HUtil.Logger;

namespace HUtil.Collection {
    [Serializable]
    public class CircularList<T> : IEnumerable<T> {
        int index = 0;
        readonly List<T> list;

        public int Count => list.Count;
        public int Pivot => index;
        public int NextPivot => (index + 1) % list.Count;
        public int PrevPivot => (index - 1 + list.Count) % list.Count;
        public bool IsAtFirst => index == 0;
        public bool IsAtLast => index == list.Count - 1 && list.Count > 0;
        public bool IsEmpty => list.Count == 0;
        public T CurrentItem => (list.Count > 0) ? list[index] : default;
        public List<T> Items => list;

        public override string ToString() =>
            $"[Circular<{typeof(T).Name}>] (Current Index: {index})\n" +
            $"{string.Join(",\n ", list.Select((item, i) => $"{i}. {item}"))}";

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Constroctor
        public CircularList() {
            list = new();
            index = 0;
        }
        public CircularList(int pivot, IEnumerable<T> list) {
            this.list = new(list);
            index = pivot;
        }
        public CircularList(CircularList<T> list) {
            this.list = new(list.Items);
            index = list.index;
        }
        public CircularList(int pivot, int size) {
            this.list = new(size);
            index = pivot;
        }
        public CircularList(int size) {
            this.list = new(size);
            index = 0;
        }
        #endregion

        #region Add
        public void Add(T item) => list.Add(item);
        public void AddRange(IEnumerable<T> items) => list.AddRange(items);
        #endregion

        #region Remove
        public void RemoveCurrent() {
            if (list.Count == 0) return;
            list.RemoveAt(index);
            if (index >= list.Count) index = 0;
        }

        public void RemoveAt(int index) {
            if (index < 0 || index > list.Count - 1) return;
            list.RemoveAt(index);
            if (index >= list.Count) this.index = 0;
        }

        public bool Remove(T item) {
            return list.Remove(item);
        }
        #endregion

        #region Peek
        /// <summary>
        /// Returns the element at the position offset from the pivot without changing the pivot.
        /// 피봇을 바꾸지 않고, 피봇에서 offset만큼 이동한 위치의 요소를 반환.
        /// </summary>
        public T PeekOffset(int offset) {
            if (list.Count == 0) return default;
            int size = list.Count;
            int peek = ((index + (offset % size)) + size) % size;
            return list[peek];
        }
        #endregion

        #region Move
        public void MoveToFirst() {
            index = 0;
        }

        public void MoveNext() {
            if (list.Count == 0) return;
            index = (index + 1) % list.Count;
        }

        public void MovePrev() {
            if (list.Count == 0) return;
            index = (index - 1 + list.Count) % list.Count;
        }

        public void MoveToLast() {
            if (list.Count == 0) return;
            index = list.Count - 1;
        }

        public void MoveTo(int index) {
            if (index < 0 || index > list.Count - 1) {
                HLogger.Exception(new IndexOutOfRangeException(), $"Input index is '{index}'");
                return;
            }
            this.index = index;
        }

        public void MoveTo(T target) {
            int pivot = list.IndexOf(target);
            if (pivot >= 0) index = pivot;
        }

        /// <summary>
        /// Moves an offset from the pivot.
        /// 현재 피봇에서 offset만큼 이동(좌우)합니다.
        /// </summary>
        public void MoveBy(int offset) {
            if (list.Count == 0) return;
            int size = list.Count;
            index = ((index + (offset % size)) + size) % size;
        }
        #endregion

        #region Dispose
        public void Clear() {
            list.Clear();
            index = 0;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH 16. May. 2025.
 * 1. Create class.
 * ==================================
 * @Jason - PKH 29. May. 2025
 * 1. Implement 'IEnumerable'.
 * 2. Edit 'ToString' format.
 * 3. Add pivot position boolean properties.
 * 4. Add constructors.
 * ==================================
 * @Jason - PKH 24. Jun. 2025
 * 1. Add extra constructors.
 * ==================================
 * @Jason - PKH 05. Sep. 2025
 * 1. Add regions in script.
 * 2. Add 'MoveBy' feature.
 * 3. Add 'MovePrev' feature.
 * 4. Fixing 'MoveTo' exception condition.
 * 5. Add 'PeekTo' feature.
 * 6. Set readonly on 'list' variable.
 * *** MAJOR CHANGE *****
 * 7. Remove generic target from 'T = class' to 'None'
 * + Now 'CircularList' can be used on any objects.
 */
#endif