#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- CinematicInstruction 인라인 PropertyDrawer.
 *
 * 특징 / 지원기능 ::
 * + Inspector 리스트 항목을 Verb·Target·Arg 3컬럼 한 줄로 표시
 * + Pose/Face/Slot: Arg 텍스트 필드 + 회색 placeholder (포즈키/방향/슬롯키)
 * + Show/Hide/Shake/Bounce: Arg 필드 대신 "(no arg)" 비활성 레이블 + arg 자동 초기화
 *
 * 주의사항 ::
 * GUIStyle 지연 초기화 — EditorStyles 접근은 OnGUI 최초 호출 이후에만 안전.
 * Verb 변경으로 hasArg 여부가 바뀌면 arg 필드가 빈 문자열로 자동 초기화됨.
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine;

namespace HDialogue.Editor {
    [CustomPropertyDrawer(typeof(CinematicInstruction))]
    public sealed class CinematicInstructionDrawer : PropertyDrawer {
        #region Fields
        const float VERB_RATIO = 0.25f;
        const float TARGET_RATIO = 0.35f;
        const float COL_GAP = 3f;

        static GUIStyle placeholderStyle;
        static GUIStyle noArgStyle;
        #endregion

        #region Override
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty verbProp = property.FindPropertyRelative("verb");
            SerializedProperty targetProp = property.FindPropertyRelative("targetCharacterKey");
            SerializedProperty argProp = property.FindPropertyRelative("arg");

            _CalcRects(position, out Rect verbRect, out Rect targetRect, out Rect argRect);

            EditorGUI.PropertyField(verbRect, verbProp, GUIContent.none);
            EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);

            PortraitVerb verb = (PortraitVerb)verbProp.enumValueIndex;
            if (_HasArg(verb)) {
                _DrawArgField(argRect, argProp, _ArgPlaceholder(verb));
            } else {
                if (!string.IsNullOrEmpty(argProp.stringValue)) argProp.stringValue = string.Empty;
                EditorGUI.LabelField(argRect, "(no arg)", _NoArgStyle());
            }

            EditorGUI.EndProperty();
        }
        #endregion

        #region Private — Layout
        private static void _CalcRects(Rect pos, out Rect verbRect, out Rect targetRect, out Rect argRect) {
            float verbWidth = Mathf.Floor(pos.width * VERB_RATIO);
            float targetWidth = Mathf.Floor(pos.width * TARGET_RATIO);
            float argWidth = pos.width - verbWidth - targetWidth - COL_GAP * 2f;

            verbRect = new Rect(pos.x, pos.y, verbWidth, pos.height);
            targetRect = new Rect(pos.x + verbWidth + COL_GAP, pos.y, targetWidth, pos.height);
            argRect = new Rect(pos.x + verbWidth + targetWidth + COL_GAP * 2f, pos.y, argWidth, pos.height);
        }
        #endregion

        #region Private — Arg Field
        private static void _DrawArgField(Rect rect, SerializedProperty argProp, string placeholder) {
            string controlId = "CinematicArg_" + argProp.propertyPath;
            GUI.SetNextControlName(controlId);

            EditorGUI.BeginChangeCheck();
            string newVal = EditorGUI.TextField(rect, argProp.stringValue);
            if (EditorGUI.EndChangeCheck()) argProp.stringValue = newVal;

            if (string.IsNullOrEmpty(argProp.stringValue) && GUI.GetNameOfFocusedControl() != controlId) {
                EditorGUI.LabelField(
                    new Rect(rect.x + 2f, rect.y, rect.width - 2f, rect.height),
                    placeholder,
                    _PlaceholderStyle());
            }
        }

        private static bool _HasArg(PortraitVerb verb) {
            return verb == PortraitVerb.Pose || verb == PortraitVerb.Face || verb == PortraitVerb.Slot;
        }

        private static string _ArgPlaceholder(PortraitVerb verb) {
            return verb switch {
                PortraitVerb.Pose => "pose key",
                PortraitVerb.Face => "direction",
                PortraitVerb.Slot => "slot key",
                _ => string.Empty
            };
        }
        #endregion

        #region Private — Styles
        private static GUIStyle _PlaceholderStyle() {
            if (placeholderStyle != null) return placeholderStyle;
            placeholderStyle = new GUIStyle(EditorStyles.label) {
                fontStyle = FontStyle.Italic
            };
            placeholderStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            return placeholderStyle;
        }

        private static GUIStyle _NoArgStyle() {
            if (noArgStyle != null) return noArgStyle;
            noArgStyle = new GUIStyle(EditorStyles.label) {
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft
            };
            noArgStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            return noArgStyle;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (최초 설계) :: CinematicInstructionDrawer 신규 생성
 *
 * # 변경
 * - Verb·Target·Arg 3컬럼 인라인 한 줄 PropertyDrawer.
 * - Pose/Face/Slot: Arg 텍스트 필드 + placeholder (포즈키/방향/슬롯키).
 * - Show/Hide/Shake/Bounce: Arg 자동 초기화 + "(no arg)" 비활성 레이블.
 * - GUIStyle 정적 지연 초기화 (placeholderStyle / noArgStyle).
 *
 * # 이유
 * - HCUP-2.4.0 Phase 3-A — Odin 없는 환경에서 CinematicInstruction 한 줄 편집.
 * - 기본 Inspector(리스트 항목당 3행)에서 인라인 1행으로 밀도 개선.
 *
 * =============================================================================
 */
#endif
