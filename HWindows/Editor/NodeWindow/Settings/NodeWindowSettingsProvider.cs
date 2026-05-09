#if UNITY_EDITOR
// =============================================================================
// NodeWindowSettingsProvider
// =============================================================================
// Project Settings > HCUP > Node Window 페이지 + HGraphWindow Toolbar 사이드패널의
// 공유 IMGUI 그리기 코드. Phase 1-E (2026-05-08) 도입.
//
// 특징 ::
// SettingsProvider (Project Settings 페이지) 와 IMGUIContainer (HGraphWindow 사이드패널)
// 양쪽이 internal static DrawSettingsGUI 헬퍼 호출 — DRY 단일 진입점 (P1E-7).
// SnapSettingsChanged event 로 GridBackground.visible 동기화 + HGraphCanvas 갱신.
//
// 노출 항목 (P1E-9 A 채택) ::
// Snap Settings  / NodeSnapSettings 3 필드 편집
// UID Registry   / NodeUIDRegistry NextValue + Issued Count (ReadOnly, P1E-8)
//
// 시각 ::
// HTitleDrawer.Draw 로 두 그룹 헤더 (P1E-θ).
// =============================================================================
using HInspector.Editor;
using HWindows.Editor.NodeWindow.Identity;
using System;
using UnityEditor;
using UnityEngine;

namespace HWindows.Editor.NodeWindow.Settings {
    static class NodeWindowSettingsProvider {
        #region Const
        const string SETTINGS_PATH = "Project/HCUP/Node Window";
        #endregion

        #region Events
        // SettingsProvider GUI 또는 사이드패널 GUI 에서 NodeSnapSettings 변경 시 발화.
        // HGraphCanvas 가 구독해 GridBackground.visible 동기화 + 시각 갱신.
        internal static event Action SnapSettingsChanged;
        #endregion

        #region SettingsProvider 등록
        [SettingsProvider]
        public static SettingsProvider Create() {
            return new SettingsProvider(SETTINGS_PATH, SettingsScope.Project) {
                label = "Node Window",
                guiHandler = DrawSettingsGUI,
                keywords = new[] { "HCUP", "Node", "Snap", "Grid", "UID" }
            };
        }
        #endregion

        #region Internal - HGraphWindow 사이드패널 측 공유 호출
        // SettingsProvider.guiHandler 와 IMGUIContainer 양쪽이 호출 — DRY (P1E-7).
        internal static void DrawSettingsGUI(string searchContext) {
            _DrawSnapSettings();
            EditorGUILayout.Space(8);
            _DrawUIDRegistry();
        }
        #endregion

        #region Private - 그룹별 그리기
        static void _DrawSnapSettings() {
            HTitleDrawer.Draw("Snap Settings");

            NodeSnapSettings settings = NodeSnapSettings.instance;
            SerializedObject so = new SerializedObject(settings);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(so.FindProperty("gridUnit"));
            EditorGUILayout.PropertyField(so.FindProperty("showGrid"));
            EditorGUILayout.PropertyField(so.FindProperty("mode"));

            if (EditorGUI.EndChangeCheck()) {
                so.ApplyModifiedProperties();
                settings.Save();
                SnapSettingsChanged?.Invoke();
            }
        }

        static void _DrawUIDRegistry() {
            HTitleDrawer.Draw("UID Registry (ReadOnly)");

            NodeUIDRegistry registry = NodeUIDRegistry.instance;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Next UID", registry.PeekNext());
            EditorGUILayout.IntField("Issued Count", registry.PeekNext() - 1);
            EditorGUI.EndDisabledGroup();
        }
        #endregion
    }
}
#endif

// =============================================================================
// Dev Log
// =============================================================================
// 2026-05-08 (최초 설계) :: Phase 1-E P1E-α/θ + Q4 D 채택
//
//   변경 / SettingsProvider + DrawSettingsGUI 공유 헬퍼 + SnapSettingsChanged event.
//   이유 / Q4 D — Project Settings 페이지 + HGraphWindow Toolbar 사이드패널 양쪽 진입점.
//          DRY 단일 진입점 (P1E-7) — DrawSettingsGUI 가 SettingsProvider.guiHandler 와
//          IMGUIContainer 양쪽에서 호출.
//   결과 / 한 인스턴스 SerializedObject 양쪽 자동 동기. HGraphCanvas 가 SnapSettingsChanged
//          구독해 GridBackground.visible + 시각 갱신.
//   주의 / NodeUIDRegistry 영역은 EditorGUI.BeginDisabledGroup 으로 ReadOnly (P1E-8).
//          Reset 버튼 미배치 — Phase 0 의 "삭제 UID 재사용 금지" 데이터 무결성 보호.
//          PeekNext() 반환형 = int (NodeUID 아님) — .Value 접근 불필요.
// =============================================================================
