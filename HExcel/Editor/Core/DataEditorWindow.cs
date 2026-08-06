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
 * - 새 Loader 추가 시 [DataEditorEntry("NN. 라벨")] 어트리뷰트만 부착 (TypeCache 자동 발견)
 * - cachedEditor는 선택 변경 / OnDisable 시 반드시 DestroyImmediate 필요
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using HExcel.Core.Editor;
using HDiagnosis.Logger;

namespace HExcel.Core {
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
        GUIStyle sidebarItemStyle;   // 좌측 정렬 사이드바 버튼 (OnGUI 시점 lazy 생성 — EditorStyles는 OnGUI 밖 접근 불가)
        bool sidebarItemStyleProSkin; // 스타일을 만들 때의 에디터 테마. 테마가 바뀌면 스타일을 다시 만든다.

        static readonly Color SELECTED_TINT = new Color(0.24f, 0.49f, 0.91f);

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

            // _BuildEntries 가 실패해도 OnGUI 는 계속 호출된다.
            if (entries != null) {
                GUIStyle itemStyle = _GetSidebarItemStyle();
                string query = searchQuery.ToLowerInvariant();

                // 종전에는 매 항목마다 Color.white 로 되돌려, 바깥 컨텍스트의 배경색을 덮어썼다.
                Color previousBackground = GUI.backgroundColor;

                for (int k = 0; k < entries.Count; k++) {
                    string label = entries[k].label;
                    if (!string.IsNullOrEmpty(query) && !label.ToLowerInvariant().Contains(query))
                        continue;

                    GUI.backgroundColor = (k == selectedIndex) ? SELECTED_TINT : previousBackground;
                    if (GUILayout.Button(label, itemStyle, GUILayout.ExpandWidth(true))) {
                        _SelectEntry(k);
                    }
                }

                GUI.backgroundColor = previousBackground;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // 두 가지를 같이 고친다.
        //
        // (1) 형태 :: EditorStyles.toolbarButton 은 툴바 스트립 안에서만 배경이 그려지는 납작한 스타일이다.
        //     목록 행으로 쓰면 버튼 크롬이 없어 라벨처럼 보이고, normal 배경 텍스처가 사실상 비어 있어
        //     GUI.backgroundColor 선택 틴트도 곱해질 대상이 없어 표시되지 않는다.
        //     프로젝트의 다른 창들은 toolbarButton 을 전부 툴바 안에서만 쓴다 — 여기만 예외였다.
        //
        // (2) 색 :: EditorStyles 는 에디터 테마가 바뀌면 통째로 재생성되는데, 종전 코드는 ??= 로 한 번만
        //     캐싱해 무효화 경로가 없었다. 테마를 바꾼 뒤에는 옛 스킨의 textColor 를 그대로 그린다.
        //     테마를 추적해 재생성하고, 상태별 textColor 를 명시해 스킨 상태에 기대지 않는다.
        private GUIStyle _GetSidebarItemStyle() {
            if (sidebarItemStyle != null && sidebarItemStyleProSkin == EditorGUIUtility.isProSkin)
                return sidebarItemStyle;

            sidebarItemStyleProSkin = EditorGUIUtility.isProSkin;

            sidebarItemStyle = new GUIStyle(GUI.skin.button) {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(8, 8, 4, 4),
                margin    = new RectOffset(2, 2, 1, 1),
                fixedHeight = 22f,
                wordWrap  = false,
            };

            Color textColor = sidebarItemStyleProSkin
                ? new Color(0.83f, 0.83f, 0.83f)
                : new Color(0.10f, 0.10f, 0.10f);

            sidebarItemStyle.normal.textColor  = textColor;
            sidebarItemStyle.hover.textColor   = textColor;
            sidebarItemStyle.focused.textColor = textColor;
            sidebarItemStyle.active.textColor  = textColor;

            return sidebarItemStyle;
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
            entries = new List<(string, ScriptableObject)>();

            foreach (var type in TypeCache.GetTypesWithAttribute<DataEditorEntryAttribute>()) {
                if (type.IsAbstract) continue;

                var attribute = (DataEditorEntryAttribute)type.GetCustomAttributes(typeof(DataEditorEntryAttribute), false)[0];
                var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (instanceProperty == null) {
                    HLogger.Error($"[DataEditorWindow] '{type.Name}' 에 static Instance 프로퍼티가 없습니다. AssetDatabaseInstance<T> 상속 여부를 확인하세요.");
                    continue;
                }

                var loader = instanceProperty.GetValue(null) as ScriptableObject;
                if (loader == null) {
                    HLogger.Error($"[DataEditorWindow] '{type.Name}'.Instance 가 ScriptableObject 를 반환하지 않았습니다.");
                    continue;
                }

                entries.Add((attribute.Label, loader));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.label, b.label));
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
 * @Jason - PKH 2026.08.05 사이드바 항목이 버튼으로 보이지 않고 글자색이 스킨과 어긋나던 문제 수정

 * # 원인
 * - 형태 :: 항목 스타일의 기반이 EditorStyles.toolbarButton 이었다. 이 스타일은 툴바 스트립
 *   안에서만 배경이 그려지는 납작한 스타일이라, 목록 행으로 쓰면 버튼 크롬이 없어 라벨처럼
 *   보인다. normal 배경 텍스처가 비어 있어 GUI.backgroundColor 선택 틴트도 표시되지 않았다.
 *   (프로젝트의 다른 창들은 toolbarButton 을 전부 툴바 안에서만 사용 — 이 지점만 예외)
 * - 색 :: sidebarItemStyle 을 ??= 로 한 번만 캐싱해 무효화 경로가 없었다. EditorStyles 는
 *   에디터 테마 변경 시 통째로 재생성되므로, 테마를 바꾼 뒤에는 옛 스킨의 textColor 가 남는다.

 * # 변경
 * - _GetSidebarItemStyle() 신설. 기반을 GUI.skin.button 으로 교체하고 좌측 정렬·패딩·행 높이 지정.
 * - EditorGUIUtility.isProSkin 을 추적해 테마가 바뀌면 스타일을 재생성.
 * - normal/hover/focused/active 의 textColor 를 명시해 스킨 상태에 기대지 않도록 고정.
 * - 선택 틴트를 SELECTED_TINT 상수로 분리. 매 항목 Color.white 대입 대신 이전 배경색을 저장·복원.
 * - entries null 가드 추가 (_BuildEntries 실패 시에도 OnGUI 는 계속 호출된다).

 * =============================================================================
 * @Jason - PKH 2026.07.04 사이드바 라벨 좌측 정렬
 *
 * # 변경
 * - sidebarItemStyle 필드 추가 — toolbarButton 기반 + MiddleLeft 정렬 (OnGUI lazy 생성)
 * - _DrawSidebar 항목 버튼 스타일을 sidebarItemStyle 로 교체
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 _BuildEntries TypeCache 자동 발견 전환
 *
 * # 변경
 * - 하드코딩 목록 → TypeCache.GetTypesWithAttribute<DataEditorEntryAttribute> 스캔
 * - Label Ordinal 정렬. static Instance 프로퍼티 리플렉션 획득 (FlattenHierarchy)
 *
 * # 이유
 * - HUnityLocalization.Editor 가 본 어셈블리를 참조하므로 하드코딩 시 순환 참조
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 "00. Localization" 항목 추가 (Phase 2)
 *
 * # 변경
 * - using HExcel.Localization 추가
 * - _BuildEntries() : ("00. Localization", LocalizationTableLoader.Instance) 추가
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Samples 항목 제거 — LocalizationTableLoader 연동 전 빈 목록으로 초기화
 *
 * # 변경
 * - using HExcel.Samples 제거
 * - _BuildEntries() : Sample / Goods 항목 삭제 → 빈 List 초기화
 * - Phase 2 완료 시 "00. Localization" + LocalizationTableLoader.Instance 추가 예정
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 CreateEditor 타입 명시 수정
 *
 * # 변경
 * - _SelectEntry() : Editor.CreateEditor(target) → CreateEditor(target, typeof(ExcelLoaderEditor))
 * - using HExcel.Core.Editor 추가
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
