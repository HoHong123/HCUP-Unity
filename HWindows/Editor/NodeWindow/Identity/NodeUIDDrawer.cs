#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- NodeUID 한 줄 GUID 표시 전용 PropertyDrawer.
 *
 * 특징 ::
 * NodeUID.guid 필드가 [HideInInspector] 이므로 hasVisibleChildren == false.
 * HDictionaryDrawer 의 _IsContainerType == false → simple cell 경로 →
 * EditorGUI.PropertyField 가 본 Drawer 를 호출.
 * 32자 GUID 를 SelectableLabel 로 한 줄 표시 (Ctrl+C 복사 가능, 편집 불가).
 * None(= string.Empty) 상태는 "(None)" 회색 이탤릭으로 표시.
 *
 * 주의사항 ::
 * HDictionaryDrawer 코드를 수정하지 않는 대신 본 Drawer 가 6 지점 전담.
 * 향후 NodeUID 자식을 직접 iterate 하는 경로는 본 Drawer 를 거쳐야 정합.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using HWindows.NodeWindow.Identity;
using UnityEditor;
using UnityEngine;

namespace HWindows.Editor.NodeWindow.Identity {
    [CustomPropertyDrawer(typeof(NodeUID))]
    public class NodeUIDDrawer : PropertyDrawer {
        #region Const
        const string GUID_FIELD = "guid";
        const string NONE_LABEL = "(None)";
        #endregion

        #region Static Cache
        static GUIStyle noneStyle;
        #endregion

        #region PropertyDrawer Overrides
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty guidProp = property.FindPropertyRelative(GUID_FIELD);
            string guid = guidProp != null ? (guidProp.stringValue ?? string.Empty) : string.Empty;

            Rect contentRect = (label != null && label != GUIContent.none)
                ? EditorGUI.PrefixLabel(position, label)
                : position;

            if (string.IsNullOrEmpty(guid)) {
                EditorGUI.LabelField(contentRect, NONE_LABEL, _GetNoneStyle());
                return;
            }

            EditorGUI.SelectableLabel(contentRect, guid, EditorStyles.textField);
        }
        #endregion

        #region Private
        static GUIStyle _GetNoneStyle() {
            if (noneStyle != null) return noneStyle;
            noneStyle = new GUIStyle(EditorStyles.label) {
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray }
            };
            return noneStyle;
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.09 NodeUIDDrawer 베이스 코드 생성
 *
 * # 목적
 * - NodeUID struct 의 Inspector foldout 구조를 한 줄 GUID SelectableLabel 로 교체.
 * - NodeUID.guid 에 [HideInInspector] 를 적용해 hasVisibleChildren == false 를 만들어
 *   HDictionaryDrawer._IsContainerType 이 false 를 반환 → simple cell 경로로 유도.
 *   이후 EditorGUI.PropertyField 가 본 Drawer 를 호출하게 됨.
 *
 * # 사용 흐름
 * - 6 지점 자동 적용: rootUID 필드, BaseNode.uid 필드, nodes/layouts/foldout/openSizes
 *   HDictionary Key 셀.
 * - 유효 GUID: 32자 hex SelectableLabel (드래그 선택 + Ctrl+C 복사, 편집 불가).
 * - None 상태: "(None)" 회색 이탤릭 LabelField.
 *
 * =============================================================================
 */
#endif
