#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 카탈로그 검증 에디터 창.
 *
 * 특징 / 지원기능 ::
 * + HCUP > Dialogue > Catalogue Validator 메뉴 진입
 * + DialogueCatalogSO 오브젝트 필드 + Validate 버튼
 * + Error(빨강) / Warning(노랑) / Clean(초록) 결과 표시
 * + 스크롤 뷰로 다수 이슈 탐색 가능
 *
 * 주의사항 ::
 * DialogueCatalogValidator.Validate() 결과를 단순 표시만. 수정 기능 없음.
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine;

namespace HDialogue.Editor {
    public sealed class DialogueCatalogValidatorWindow : EditorWindow {
        [MenuItem("HCUP/Dialogue/Catalogue Validator")]
        static void _Open() => GetWindow<DialogueCatalogValidatorWindow>("Catalogue Validator");

        DialogueCatalogSO catalog;
        DialogueValidationReport? report;
        Vector2 scrollPos;

        static readonly Color PASS_COLOR = new(0.2f, 0.8f, 0.2f);
        static readonly Color ERROR_COLOR = new(0.9f, 0.25f, 0.2f);
        static readonly Color WARN_COLOR = new(0.95f, 0.75f, 0.0f);

        void OnGUI() {
            EditorGUILayout.Space(6f);
            catalog = (DialogueCatalogSO)EditorGUILayout.ObjectField(
                "Catalogue", catalog, typeof(DialogueCatalogSO), false
            );
            EditorGUILayout.Space(4f);

            GUI.enabled = catalog != null;
            if (GUILayout.Button("Validate", GUILayout.Height(28f))) {
                report = DialogueCatalogValidator.Validate(catalog);
            }
            GUI.enabled = true;

            if (report == null) return;

            EditorGUILayout.Space(6f);
            _DrawSummary(report.Value);
            EditorGUILayout.Space(4f);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            _DrawIssueSection("Errors", report.Value.Errors, ERROR_COLOR);
            _DrawIssueSection("Warnings", report.Value.Warnings, WARN_COLOR);
            EditorGUILayout.EndScrollView();
        }

        private void _DrawSummary(DialogueValidationReport r) {
            bool pass = r.IsValid;
            Color c = pass ? PASS_COLOR : ERROR_COLOR;
            string label = pass
                ? $"✓  PASS — {r.Warnings.Count} warning(s)"
                : $"✗  FAIL — {r.Errors.Count} error(s), {r.Warnings.Count} warning(s)";
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = c;
            GUILayout.Label(label, style);
        }

        static void _DrawIssueSection(string header, System.Collections.Generic.List<DialogueValidationIssue> issues, Color color) {
            if (issues == null || issues.Count == 0) return;
            EditorGUILayout.Space(4f);
            GUILayout.Label(header, EditorStyles.boldLabel);
            foreach (DialogueValidationIssue issue in issues) {
                _DrawIssue(issue, color);
            }
        }

        static void _DrawIssue(DialogueValidationIssue issue, Color color) {
            string uidPart = issue.NodeUID.IsValid ? $" [{issue.NodeUID.Value[..8]}]" : "";
            string text = $"[{issue.Code}]{uidPart}  {issue.Message}";
            var style = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
            style.normal.textColor = color;
            GUILayout.Label(text, style);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueCatalogValidatorWindow 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 3 — Catalogue Validator 에디터 창
 *
 * # 설계 결정
 * - GUIStyle을 OnGUI 내 new로 생성: 에디터 창이라 프레임 수 낮고 생성 비용 무시 가능
 * - report를 nullable struct로 보관 — 버튼 클릭 전 결과 없음 상태 구분
 * - GUI.enabled = catalog != null 으로 null SO 클릭 방지 (Validate 내 null 처리가 있어도)
 *
 * =============================================================================
 */
#endif
