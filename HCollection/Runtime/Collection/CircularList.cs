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
using HDiagnosis.Logger;

namespace HCollection {
    public class CircularList<T> : IEnumerable<T>, IDisposable {
        #region Fields
        int index = 0;
        readonly List<T> list;
        #endregion

        #region Properties
        public int Count => list.Count;
        public int Pivot => index;
        // 빈 리스트 가드 — IsAtLast 와 동일 규약. 가드 없이는 Count==0 에서 DivideByZero. [CASE EDGE-3]
        public int NextPivot => (list.Count > 0) ? (index + 1) % list.Count : 0;
        public int PrevPivot => (list.Count > 0) ? (index - 1 + list.Count) % list.Count : 0;
        public bool IsAtFirst => index == 0;
        public bool IsAtLast => index == list.Count - 1 && list.Count > 0;
        public bool IsEmpty => list.Count == 0;
        // pivot 선지정(deferred) 상태에서 Count <= index 인 동안의 접근을 막는 범위 가드. [CASE COR-2]
        public T CurrentItem => ((uint)index < (uint)list.Count) ? list[index] : default;
        // 원본 List 노출 시 외부 제거가 Pivot 보정(_AdjustPivotAfterRemove)을 우회한다. [CASE NEG-3]
        public IReadOnlyList<T> Items => list;
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
            // 요소가 즉시 확정되는 생성자이므로 pivot 을 유효 범위로 clamp. [CASE NEG-2]
            index = (this.list.Count > 0) ? Math.Clamp(pivot, 0, this.list.Count - 1) : 0;
        }
        public CircularList(CircularList<T> list) {
            this.list = new(list.Items);
            index = list.index;
        }
        /// <summary>
        /// pivot 선지정 생성자. size 는 capacity 이며 요소는 이후 Add 로 채운다 — Add 가 pivot
        /// 이상으로 채워지기 전까지 CurrentItem 은 default 를 반환한다 (ParallexLayer 사용 패턴).
        /// </summary>
        public CircularList(int pivot, int size) {
            this.list = new(size);
            index = Math.Max(0, pivot);   // 음수 pivot 만 차단 — 초과 pivot 은 deferred 채움 패턴 허용
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
            if ((uint)index >= (uint)list.Count) return;   // 빈 리스트 + pivot 선지정 상태 방어
            list.RemoveAt(index);
            if (index >= list.Count) index = 0;
        }

        public void RemoveAt(int index) {
            if (index < 0 || index > list.Count - 1) return;
            list.RemoveAt(index);
            _AdjustPivotAfterRemove(index);
        }

        public bool Remove(T item) {
            int removeIndex = list.IndexOf(item);
            if (removeIndex < 0) return false;
            list.RemoveAt(removeIndex);
            _AdjustPivotAfterRemove(removeIndex);
            return true;
        }

        // 제거 위치 기준 Pivot 보정. Pivot 앞이 지워지면 한 칸 당기고,
        // 보정 후에도 범위를 벗어나면(마지막 요소 제거 등) 0으로 순환.
        private void _AdjustPivotAfterRemove(int removedIndex) {
            if (list.Count == 0) {
                index = 0;
                return;
            }
            if (removedIndex < index) index--;
            if (index >= list.Count) index = 0;
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
 * @Jason - PKH 29. May. 2025 [LOG-20250529-1]
 * - Implement 'IEnumerable'.
 * ==================================
 * @Jason - PKH 24. Jun. 2025 [LOG-20250624-1]
 * - Add extra constructors.
 * ==================================
 * @Jason - PKH 05. Sep. 2025 [LOG-20250905-1]
 * - Add regions in script.
 * ==================================
 * > 이전 엔트리는 docs/history/HCollection/Runtime/Collection/CircularList.md 참조 (총 4 엔트리)
 * =========================================================
 */
#endif