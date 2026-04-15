#if UNITY_EDITOR
/* =========================================================
 * 순환 탐색을 지원하는 리스트 컬렉션 클래스입니다.
 * 내부 List를 기반으로 Pivot 인덱스를 중심으로 순환 이동 및 조회 기능을 제공합니다.
 *
 * 주의사항 ::
 * 1. Pivot은 항상 내부 리스트 범위를 기준으로 순환합니다.
 * 2. 리스트가 비어있는 상태에서 이동 함수 호출 시 동작하지 않습니다.
 * 3. index는 항상 유효 범위를 유지하도록 관리됩니다.
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
    public class CircularList<T> : IEnumerable<T>, IDisposable {
        #region Fields
        int index = 0;
        readonly List<T> list;
        #endregion

        #region Properties
        public int Count => list.Count;
        public int Pivot => index;
        public int NextPivot => (index + 1) % list.Count;
        public int PrevPivot => (index - 1 + list.Count) % list.Count;
        public bool IsAtFirst => index == 0;
        public bool IsAtLast => index == list.Count - 1 && list.Count > 0;
        public bool IsEmpty => list.Count == 0;
        public T CurrentItem => (list.Count > 0) ? list[index] : default;
        public List<T> Items => list;
        #endregion

        #region Public - Getters
        public override string ToString() =>
            $"[Circular<{typeof(T).Name}>] (Current Index: {index})\n" +
            $"{string.Join(",\n ", list.Select((item, k) => $"{k}. {item}"))}";
        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Public - Constroctor
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

        #region Public - Add
        public void Add(T item) => list.Add(item);
        public void AddRange(IEnumerable<T> items) => list.AddRange(items);
        #endregion

        #region Public - Remove
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

        #region Public - Peek
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

        #region Public - Move
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

        #region Public - Dispose
        [Obsolete("Change it to 'Dispose'")]
        public void Clear() => Dispose();
        public void Dispose() {
            list.Clear();
            index = 0;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* Dev Log
 * =========================================================
 * * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. MoveNext / MovePrev
 *    + Pivot 기준 순환 이동
 * 2. MoveTo / MoveBy
 *    + 특정 위치 또는 offset 기반 이동
 * 3. PeekOffset
 *    + Pivot 변경 없이 offset 위치 조회
 * 4. RemoveCurrent
 *    + 현재 Pivot 위치 요소 제거
 *
 * 사용법 ::
 * 1. CircularList<T> 생성 후 Add / AddRange로 요소를 추가합니다.
 * 2. MoveNext / MovePrev를 사용해 Pivot을 순환 이동합니다.
 * 3. CurrentItem을 통해 현재 Pivot 요소를 조회합니다.
 *
 * 기타 ::
 * 1. IEnumerable을 구현하여 foreach 사용이 가능합니다.
 * 2. Dispose 호출 시 내부 리스트를 Clear 합니다.
 * ==================================
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
 * =========================================================
 */
#endif