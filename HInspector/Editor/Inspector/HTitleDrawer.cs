#if UNITY_EDITOR
// =============================================================================
// HTitleDrawer
// =============================================================================
// HTitle 시각 규격 (볼드 라벨 + 1px 구분선) 의 public IMGUI helper.
//
// 특징 ::
// HInspectorEditor 의 CustomEditor 경로 밖 (SettingsProvider, IMGUIContainer 등)
// 임의 IMGUI 영역에서 HTitle attribute 와 동일한 시각 효과를 적용할 수 있다.
// 시각 규격 본체 (_DrawTitleCore + 시각 상수) 를 단일 진입점으로 보유 —
// HInspectorEditor 도 본 helper 를 위임 호출해 DRY.
//
// 사용 예 ::
// HTitleDrawer.Draw("Snap Settings");
// EditorGUILayout.PropertyField(serialized.FindProperty("gridUnit"));
//
// 결과 ::
// Snap Settings
// ─────────────────
// [필드 출력]
//
// 주의사항 ::
// 시각 상수는 internal — 외부에서 변경 불가. 시각 규격 변경 시 본 클래스 단일
// 진입점에서만 수정.
// =============================================================================
using UnityEditor;
using UnityEngine;

namespace HInspector.Editor {
    public static class HTitleDrawer {
        #region Const
        const float TITLE_TOP_PADDING = 6f;
        const float TITLE_TO_LINE_GAP = 3f;
        const float TITLE_LINE_THICKNESS = 1f;
        const float TITLE_LINE_TO_FIELD_GAP = 4f;
        #endregion

        #region Fields
        static GUIStyle titleStyle;
        #endregion

        #region Public API
        public static void Draw(string title) {
            GUILayout.Space(TITLE_TOP_PADDING);
            Rect block = GUILayoutUtility.GetRect(0, _GetTitleBlockHeight(),
                                                  GUILayout.ExpandWidth(true));
            _DrawTitleCore(block, title);
            GUILayout.Space(TITLE_LINE_TO_FIELD_GAP);
        }
        #endregion

        #region Internal - HInspectorEditor 위임 호출용
        internal static void _DrawTitleCore(Rect blockRect, string title) {
            Rect titleRect = new Rect(blockRect.x, blockRect.y, blockRect.width,
                                       EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, title, _GetTitleStyle());

            float lineY = titleRect.yMax + TITLE_TO_LINE_GAP;
            Color lineColor = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.45f, 0.45f)
                : new Color(0.55f, 0.55f, 0.55f);
            Rect lineRect = new Rect(blockRect.x, lineY, blockRect.width, TITLE_LINE_THICKNESS);
            EditorGUI.DrawRect(lineRect, lineColor);
        }

        internal static float _GetTitleBlockHeight() {
            return EditorGUIUtility.singleLineHeight + TITLE_TO_LINE_GAP + TITLE_LINE_THICKNESS;
        }
        #endregion

        #region Private - Style cache
        static GUIStyle _GetTitleStyle() {
            if (titleStyle != null) return titleStyle;
            titleStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            return titleStyle;
        }
        #endregion
    }
}
#endif

// =============================================================================
// Dev Log
// =============================================================================
// 2026-05-08 (최초 설계) :: HTitle 시각 규격 외부 노출
//
//   변경 / HInspectorEditor 의 _DrawTitleCore 시각 규격을 별도 public class 로 추출.
//   이유 / SettingsProvider / IMGUIContainer 등 CustomEditor 경로 밖 IMGUI 영역에서
//          HTitle attribute 와 동일한 시각 효과 재사용 필요 (Phase 1-E NodeWindow Settings).
//   결과 / 시각 규격 단일 진입점 보존 (DRY). HInspectorEditor 가 본 helper 위임 호출.
//   주의 / 시각 상수 (TITLE_*) 는 internal — 외부 변경 불가. 시각 규격 변경 시 본 클래스 단일 위치.
// =============================================================================
