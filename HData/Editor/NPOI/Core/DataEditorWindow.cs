#if UNITY_EDITOR
/* =========================================================
 * NPOI 데이터 관리 Editor Window
 *
 * 특징 ::
 * - Unity Menu → HData/NPOI 에서 오픈
 * - IMGUI 사이드바(검색 + 항목 목록) + 우측 Inspector 패널 구성
 * - OdinMenuEditorWindow 제거 완료 (M4)
 *
 * 주의사항 ::
 * - 새 Loader 추가 시 _BuildEntries()에 항목만 추가
 * - cachedEditor는 선택 변경 / OnDisable 시 반드시 DestroyImmediate 필요
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HData.NPOI.Core.Editor;
using HData.NPOI.Localization;

namespace HData.NPOI.Core {
    public class DataEditorWindow : EditorWindow {
        const float WINDOW_WIDTH  = 1200f;
        const float WINDOW_HEIGHT = 700f;
        const float SIDEBAR_WIDTH = 200f;

        List<(string label, ScriptableObject target)> entries;
        string searchQuery   = "";
        int selectedIndex = -1;
        UnityEditor.Editor cachedEditor;
        Vector2 sidebarScroll;
        Vector2 contentScroll;

        #region Private - Editor Window Control
        [MenuItem("HCUP/Windows/Data Editor Window")]
        private static void OpenWindow() {
            var window = GetWindow<DataEditorWindow>("NPOI Data Editor");
            var main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(
                main.x + (main.width  - WINDOW_WIDTH)  * 0.5f,
                main.y + (main.height - WINDOW_HEIGHT) * 0.5f,
                WINDOW_WIDTH, WINDOW_HEIGHT);
            window.Show();
        }
        #endregion

        #region Private - Lifecycle
        private void OnEnable() {
            _BuildEntries();
        }

        private void OnDisable() {
            _DestroyCachedEditor();
        }
        #endregion

        #region Private - OnGUI
        private void OnGUI() {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            _DrawSidebar();
            _DrawVerticalSeparator();
            _DrawContent();
            EditorGUILayout.EndHorizontal();
        }

        private void _DrawSearchBar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("검색", EditorStyles.toolbarButton, GUILayout.Width(36));
            searchQuery = EditorGUILayout.TextField(
                searchQuery,
                EditorStyles.toolbarSearchField,
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22))) {
                searchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void _DrawSidebar() {
            EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_WIDTH), GUILayout.ExpandHeight(true));
            sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll);

            _DrawSearchBar();

            string query = searchQuery.ToLowerInvariant();
            for (int k = 0; k < entries.Count; k++) {
                string label = entries[k].label;
                if (!string.IsNullOrEmpty(query) && !label.ToLowerInvariant().Contains(query))
                    continue;

                bool isSelected = k == selectedIndex;
                if (isSelected) GUI.backgroundColor = new Color(0.24f, 0.49f, 0.91f);
                if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true))) {
                    _SelectEntry(k);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void _DrawVerticalSeparator() {
            var rect = GUILayoutUtility.GetRect(2f, 2f, GUILayout.Width(2f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        private void _DrawContent() {
            contentScroll = EditorGUILayout.BeginScrollView(
                contentScroll,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (selectedIndex >= 0 && cachedEditor != null) {
                cachedEditor.OnInspectorGUI();
            } else {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("항목을 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Private - Entry Management
        private void _BuildEntries() {
            entries = new List<(string, ScriptableObject)> {
                ("00. Localization", LocalizationTableLoader.Instance),
            };
        }

        private void _SelectEntry(int index) {
            if (selectedIndex == index) return;
            selectedIndex = index;
            _DestroyCachedEditor();

            var target = entries[index].target;
            if (target != null) {
                cachedEditor = UnityEditor.Editor.CreateEditor(target, typeof(ExcelLoaderEditor));
            }
        }

        private void _DestroyCachedEditor() {
            if (cachedEditor == null) return;
            DestroyImmediate(cachedEditor);
            cachedEditor = null;
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 "00. Localization" 항목 추가 (Phase 2)
 *
 * # 변경
 * - using HData.NPOI.Localization 추가
 * - _BuildEntries() : ("00. Localization", LocalizationTableLoader.Instance) 추가
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Samples 항목 제거 — LocalizationTableLoader 연동 전 빈 목록으로 초기화
 *
 * # 변경
 * - using HData.NPOI.Samples 제거
 * - _BuildEntries() : Sample / Goods 항목 삭제 → 빈 List 초기화
 * - Phase 2 완료 시 "00. Localization" + LocalizationTableLoader.Instance 추가 예정
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 CreateEditor 타입 명시 수정
 *
 * # 변경
 * - _SelectEntry() : Editor.CreateEditor(target) → CreateEditor(target, typeof(ExcelLoaderEditor))
 * - using HData.NPOI.Core.Editor 추가
 *
 * # 원인
 * - AssetDatabaseInstanceEditor / ExcelLoaderEditor 둘 다 open generic [CustomEditor]로 등록
 * - Unity open generic 간 우선순위 결정 신뢰 불가 → AssetDatabaseInstanceEditor가 당첨되어
 *   DrawDefaultInspector()만 그리고 Load/Import/Export 버튼이 표시되지 않는 문제
 * - 명시적 타입 전달로 모호성 제거
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 GoodsTableLoader 항목 추가
 *
 * # 변경
 * - _BuildEntries() : "01. Goods" → GoodsTableLoader.Instance 추가
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 컨벤션 정리 — 필드 접근제어자 제거 + camelCase 통일
 *
 * # 변경
 * - WindowWidth/Height/SidebarWidth → WINDOW_WIDTH/HEIGHT/SIDEBAR_WIDTH (const UPPER_SNAKE_CASE)
 * - private 필드 전체 접근제어자 제거 + 언더바 제거 (camelCase)
 *   (_entries → entries, _searchQuery → searchQuery, _selectedIndex → selectedIndex 등)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 MenuItem 경로 변경
 *
 * # 변경
 * - [MenuItem("HData/NPOI")] → [MenuItem("HData/Windows/Data Editor Window")]
 *   (HData 메뉴 하위 Windows 그룹으로 이동)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 M4 — OdinMenuEditorWindow → EditorWindow IMGUI 재작성
 *
 * # 변경
 * - OdinMenuEditorWindow 상속 제거 → EditorWindow 직접 상속
 * - BuildMenuTree() 제거 → OnGUI() 직접 구현 (검색 바 + 사이드바 + 컨텐츠 패널)
 * - Editor.CreateEditor(target).OnInspectorGUI() 임베딩 방식으로 우측 패널 구현
 * - using Sirenix.OdinInspector.Editor 제거
 *
 * =============================================================================
 */
#endif
