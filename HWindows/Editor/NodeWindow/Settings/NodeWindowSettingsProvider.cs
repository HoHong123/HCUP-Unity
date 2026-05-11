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
// SnapSettingsChanged<bool> 로 showGrid 값 직달 → GridBackground.display 동기화 + HGraphCanvas 갱신.
//
// 노출 항목 (P1E-9 A 채택) ::
// Snap Settings  / NodeSnapSettings 3 필드 편집
// UID Registry 섹션 제거 — NodeUIDRegistry 삭제 (GUID 방식 전환, 2026-05-09)
//
// 시각 ::
// HTitleDrawer.Draw 로 그룹 헤더 (P1E-θ).
// =============================================================================
using HInspector.Editor;
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
        // bool 파라미터 = so.FindProperty("showGrid").boolValue — ApplyModifiedProperties 타이밍 이슈 방지.
        // HGraphCanvas 가 구독해 GridBackground.display 동기화 + 시각 갱신.
        internal static event Action<bool> SnapSettingsChanged;
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
                // ApplyModifiedProperties 전에 값을 읽어야 SerializedProperty 기준 최신값 보장.
                bool showGrid = so.FindProperty("showGrid").boolValue;
                so.ApplyModifiedProperties();
                settings.Save();
                SnapSettingsChanged?.Invoke(showGrid);
            }
        }

        #endregion
    }
}
#endif

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.12 Show Grid 미적용 버그픽스 — SnapSettingsChanged Action → Action<bool>
 *
 * # 변경
 * - SnapSettingsChanged 이벤트 시그니처: Action → Action<bool>.
 * - _DrawSnapSettings: ApplyModifiedProperties 전에 so.FindProperty("showGrid").boolValue 를 캡처.
 *   showGrid 값을 Invoke 인자로 직접 전달.
 *
 * # 이유
 * - ApplyModifiedProperties 이후 ScriptableSingleton C# 필드 갱신이 지연될 수 있음.
 *   NodeSnapSettings.instance.ShowGrid 를 콜백에서 읽을 때 old 값을 반환하는 경우.
 * - SerializedProperty.boolValue (so 기준) 는 PropertyField 가 수정한 직후 값이므로 타이밍 무관 신뢰 가능.
 *
 * # 결과
 * - _OnSnapSettingsChanged(bool showGrid) 가 올바른 값 수신.
 *   HGraphCanvas 에서 gridBackground.style.display 갱신 정상화.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 UID Registry UI 섹션 제거
 *
 * # 삭제
 * - _DrawUIDRegistry() 메서드 제거
 * - DrawSettingsGUI 내 _DrawUIDRegistry() 호출 제거
 * - using HWindows.Editor.NodeWindow.Identity 제거
 * - NodeUIDRegistry 삭제에 따른 의존 정리
 *
 * =============================================================================
 */
// =============================================================================
// (이하 이전 엔트리 — 원래 형식 보존)
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
