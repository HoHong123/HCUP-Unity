#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HTmpText 컴포넌트의 Custom Editor.
 * Localization 설정과 FitWidth를 Inspector 상단에 그리고, 이후 TMP 기본 Inspector를 이어 그린다.
 *
 * 사용 ::
 * HTmpText 컴포넌트를 Inspector에서 선택하면 자동 적용된다.
 * =========================================================
 */

using UnityEditor;
using UnityEngine;
using HUI.TextUI;
using TMPro.EditorUtilities;

namespace HUI.Editor.TextUI {
    [CustomEditor(typeof(HTmpText))]
    public class HTmpTextEditor : TMP_EditorPanelUI {
        private SerializedProperty useLocalizationProp;
        private SerializedProperty localizationIdProp;
        private SerializedProperty useOriginalTextProp;
        private SerializedProperty originalTextModeProp;
        private SerializedProperty fitWidthProp;

        protected override void OnEnable() {
            base.OnEnable();

            useLocalizationProp = serializedObject.FindProperty("useLocalization");
            localizationIdProp = serializedObject.FindProperty("localizationId");
            useOriginalTextProp = serializedObject.FindProperty("useOriginalText");
            originalTextModeProp = serializedObject.FindProperty("originalTextMode");
            fitWidthProp = serializedObject.FindProperty("fitWidth");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            _DrawLocalizationSettings();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4);

            base.OnInspectorGUI();
        }

        private void _DrawLocalizationSettings() {
            EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useLocalizationProp, new GUIContent("Use Localization"));

            if (useLocalizationProp.boolValue) {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(localizationIdProp, new GUIContent("Localization ID"));

                EditorGUILayout.PropertyField(useOriginalTextProp, new GUIContent("Use Original Text"));

                if (useOriginalTextProp.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(originalTextModeProp, new GUIContent("Original Text Mode"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(fitWidthProp, new GUIContent("Fit Width", "텍스트 preferred width로 RectTransform 크기를 맞춘다. 비활성 상태에서도 동작."));

            _DrawSeparator();
        }

        private static void _DrawSeparator() {
            EditorGUILayout.Space(2);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.06.29 HTmpTextEditor 베이스 코드 생성
 *
 * # 목적
 * - HTmpText(TMP_Text 기반) 컴포넌트의 Localization 설정 + FitWidth를 Inspector에서 편집하는 Custom Editor
 * - TMP_EditorPanelUI 상속으로 TMP 기본 Inspector UI 재사용
 *
 * # 사용 흐름
 * - HTmpText 컴포넌트를 Inspector에서 선택하면 [CustomEditor(typeof(HTmpText))] 어트리뷰트로 자동 적용
 *
 * =============================================================================
 */
#endif
