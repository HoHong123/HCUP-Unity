#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 리스트/배열 필드의 표시 방식을 제어하는 Attribute입니다.
 * Unity 기본 리스트 위에 기본 펼침 상태, 편집 잠금 등 옵션을 추가합니다.
 *
 * 사용 예 ::
 * [HListDrawer]
 * public List<int> values;
 *
 * 옵션 사용 ::
 * [HListDrawer(DefaultExpandedState = true)]
 * public List<SfxClip> clips;
 *
 * [HListDrawer(IsReadOnly = true)]
 * public SfxClip[] catalog;
 *
 * 주의사항 ::
 * List<T> 또는 T[] 필드에만 의미가 있으며, 그 외 필드 타입에서는 무시됩니다.
 * 옵션 이름은 Odin ListDrawerSettings와 1:1 정렬되어 있어 점진 마이그레이션 및
 * Odin AttributeProcessor 어댑터 매핑을 단순화합니다.
 *
 * Phase 정책 ::
 * Phase 1에서는 DefaultExpandedState와 IsReadOnly만 드로어가 처리합니다.
 * DraggableItems 이하 옵션은 API 계약을 미리 확정하기 위한 예약 필드이며,
 * 실제 사용 케이스가 발생할 때 드로어에 구현합니다.
 * =========================================================
 */
#endif

using System;

namespace HInspector {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HListDrawerAttribute : HInspectorAttribute {
        // 드로어가 처리하는 옵션
        public bool DefaultExpandedState { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;

        // API 예약. 드로어는 현재 미구현. 사용해도 무시됨.
        [Obsolete]
        public bool DraggableItems { get; set; } = true;
        [Obsolete]
        public bool ShowIndexLabels { get; set; } = false;
        [Obsolete]
        public bool HideAddButton { get; set; } = false;
        [Obsolete]
        public bool HideRemoveButton { get; set; } = false;
        [Obsolete]
        public int NumberOfItemsPerPage { get; set; } = 0;

        public HListDrawerAttribute(int order = -30) : base(order) { }
    }
}
