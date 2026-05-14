#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 태그 구조 검증 에디터 윈도우 (Phase 5).
 *
 * 특징 / 지원기능 ::
 * + 메뉴 Tools > HDialogue > Dialogue Tag Validator 로 열기
 * + 텍스트 입력 → Validate 버튼 → Error/Warning 컬러 목록 표시
 *
 * 주의사항 ::
 * DialogueTextValidator.Validate() 호출. 검증 로직은 Validator 클래스 관리.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HDialogue.Editor {
    public sealed class DialogueTextValidatorWindow : EditorWindow {
        #region 변수
        string rawText = string.Empty;
        IReadOnlyList<DialogueTextValidator.ValidationIssue> results;
        Vector2 scrollPos;
        #endregion

        #region Unity Life Cycle
        private void OnGUI() {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Raw Text", EditorStyles.boldLabel);
            rawText = EditorGUILayout.TextArea(rawText, GUILayout.Height(100));
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Validate", GUILayout.Height(28)))
                results = DialogueTextValidator.Validate(rawText);

            if (results == null) return;

            EditorGUILayout.Space(6);
            _DrawDivider();
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (results.Count == 0) {
                EditorGUILayout.HelpBox("태그 구조 이상 없음.", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int k = 0; k < results.Count; k++) {
                DialogueTextValidator.ValidationIssue issue = results[k];
                MessageType msgType = issue.Severity == DialogueTextValidator.IssueSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, msgType);
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Private
        [MenuItem("Tools/HDialogue/Dialogue Tag Validator")]
        private static void _Open() => GetWindow<DialogueTextValidatorWindow>("Dialogue Tag Validator");

        private static void _DrawDivider() {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(2);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.Editor.TextUI → HDialogue.Editor 패키지 이관
 *
 * # 변경
 * - namespace HUI.Editor.TextUI → HDialogue.Editor
 * - HUI/Editor/HUI/Text/Dialogue/ → HDialogue/Editor/ 이관
 * - 메뉴 경로 : Tools/HUI → Tools/HDialogue (패키지 정체성 반영)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueTextValidatorWindow Phase 5 베이스 코드 생성
 *
 * # 목적
 * - DialogueTextValidator 결과를 에디터 윈도우로 표시. IMGUI 기반.
 *
 * # 설계 결정
 * - IMGUI 선택: UIElements 불필요한 의존 추가 없이 간결하게 구현.
 * - results null 체크로 첫 진입 시 결과 패널 숨김.
 *
 * =============================================================================
 */
#endif
