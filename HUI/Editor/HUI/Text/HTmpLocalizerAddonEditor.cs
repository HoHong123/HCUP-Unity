#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HTmpLocalizerAddon 컴포넌트의 Custom Editor.
 * Localization ID, UseOriginalText, FitWidth 필드를 Inspector에 노출한다.
 *
 * 사용 ::
 * HTmpLocalizerAddon 컴포넌트를 Inspector에서 선택하면 자동 적용된다.
 * =========================================================
 */

using UnityEditor;
using UnityEngine;
using HUI.TextUI;

namespace HUI.Editor.TextUI {
    [CustomEditor(typeof(HTmpLocalizerAddon))]
    public class HTmpLocalizerAddonEditor : UnityEditor.Editor {
        private SerializedProperty localizationIdProp;
        private SerializedProperty useOriginalTextProp;
        private SerializedProperty originalTextModeProp;
        private SerializedProperty fitWidthProp;

        private void OnEnable() {
            localizationIdProp = serializedObject.FindProperty("localizationId");
            useOriginalTextProp = serializedObject.FindProperty("useOriginalText");
            originalTextModeProp = serializedObject.FindProperty("originalTextMode");
            fitWidthProp = serializedObject.FindProperty("fitWidth");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(localizationIdProp, new GUIContent("Localization ID"));

            EditorGUILayout.PropertyField(useOriginalTextProp, new GUIContent("Use Original Text"));

            if (useOriginalTextProp.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(originalTextModeProp, new GUIContent("Original Text Mode"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(fitWidthProp, new GUIContent("Fit Width", "텍스트 preferred width로 RectTransform 크기를 맞춘다. 비활성 상태에서도 동작."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.06.29 HTmpLocalizerAddonEditor 베이스 코드 생성
 *
 * # 목적
 * - HTmpLocalizerAddon 컴포넌트(TMP 기반)의 필드를 Inspector에서 편집하는 Custom Editor
 *
 * # 사용 흐름
 * - HTmpLocalizerAddon 컴포넌트를 Inspector에서 선택하면 [CustomEditor(typeof(HTmpLocalizerAddon))]로 자동 적용
 *
 * =============================================================================
 */
#endif
