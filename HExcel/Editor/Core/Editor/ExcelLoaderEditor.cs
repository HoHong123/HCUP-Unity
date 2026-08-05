#if UNITY_EDITOR
/* =========================================================
 * ExcelLoader<T> CustomEditor — Odin 종속 제거 후 IMGUI 구현
 *
 * 특징 ::
 * - [FilePath], [ValueDropdown], [OnValueChanged], [Button], [ShowIf] 대체
 * - open generic [CustomEditor(typeof(ExcelLoader<>), true)] 적용
 * - ExcelLoader의 open generic 특성상 멤버 접근은 Reflection 경유
 * - 섹션 타이틀은 HInspector HTitleDrawer 시각 규격(볼드 라벨 + 1px 구분선)으로 통일
 *
 * 주의사항 ::
 * - internal 멤버(LoadExcelFile, GetSheet, Sheets, IsAvailable)도
 *   Reflection으로 접근 (open generic cast 불가 우회)
 * =========================================================
 */

using System.Collections.Generic;
using System.Reflection;
using HInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace HExcel.Core.Editor {
    [CustomEditor(typeof(ExcelLoader<>), true)]
    public class ExcelLoaderEditor : UnityEditor.Editor {
        const float SECTION_GAP = 8f;

        SerializedProperty excelFileAssetProp;
        SerializedProperty sheetNameProp;
        SerializedProperty dataOutputPathProp;

        string[] previewHeaders;
        string[][] previewRows;
        bool previewDirty = true;
        Vector2 dataScrollPos;

        private void OnEnable() {
            excelFileAssetProp  = serializedObject.FindProperty("excelFileAsset");
            sheetNameProp       = serializedObject.FindProperty("sheetName");
            dataOutputPathProp  = serializedObject.FindProperty("dataOutputPath");
            previewDirty        = true;
            _CallLoadExcelFile();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            _DrawLoaderSaveSection();
            _DrawFilePathRow();
            _DrawSheetDropdown();
            _DrawDataOutputPathRow();
            _DrawActionButtons();

            serializedObject.ApplyModifiedProperties();

            _RefreshPreviewIfNeeded();
            _DrawDataPreview();
        }

        #region Private - Draw Sections
        private void _DrawLoaderSaveSection() {
            var prop = target.GetType().GetProperty("IsLoadedFromAsset", FLAGS);
            if (prop == null || (bool)prop.GetValue(target)) return;

            EditorGUILayout.HelpBox(
                "Loader가 저장되지 않았습니다. 저장하면 엑셀 파일·시트·경로 설정이 다음 세션에서도 유지됩니다.",
                MessageType.Warning);
            if (GUILayout.Button("Loader 저장하기", GUILayout.Height(36))) {
                _CallMethod("CreateAsset");
            }
            EditorGUILayout.Space(6);
        }

        private void _DrawFilePathRow() {
            HTitleDrawer.Draw("엑셀 파일");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(excelFileAssetProp, typeof(DefaultAsset), GUIContent.none);
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                _CallLoadExcelFile();
            }
        }

        private void _DrawSheetDropdown() {
            var sheets = _GetSheets();
            if (sheets == null || sheets.Count == 0) return;

            EditorGUILayout.Space(SECTION_GAP);
            HTitleDrawer.Draw("시트 선택");

            string current = sheetNameProp.stringValue;
            int currentIndex = sheets.IndexOf(current);
            if (currentIndex < 0) {
                currentIndex = 0;
                sheetNameProp.stringValue = sheets[0];
                serializedObject.ApplyModifiedProperties();
                _CallGetSheet();
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup(currentIndex, sheets.ToArray());
            if (EditorGUI.EndChangeCheck()) {
                sheetNameProp.stringValue = sheets[selected];
                serializedObject.ApplyModifiedProperties();
                _CallGetSheet();
            }
        }

        private void _DrawDataOutputPathRow() {
            if (dataOutputPathProp == null) return;

            EditorGUILayout.Space(SECTION_GAP);
            HTitleDrawer.Draw("데이터 출력 경로");
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string newPath = EditorGUILayout.TextField(dataOutputPathProp.stringValue);
            if (EditorGUI.EndChangeCheck()) {
                dataOutputPathProp.stringValue = newPath;
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("...", GUILayout.Width(28))) {
                string folder = EditorUtility.OpenFolderPanel("데이터 출력 폴더 선택", "Assets", "");
                if (!string.IsNullOrEmpty(folder)) {
                    string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
                    string selected = folder.Replace('\\', '/');
                    if (selected.StartsWith(dataPath)) {
                        dataOutputPathProp.stringValue = "Assets" + selected.Substring(dataPath.Length);
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void _DrawActionButtons() {
            EditorGUILayout.Space(SECTION_GAP);
            HTitleDrawer.Draw("실행");
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Excel", GUILayout.Height(50))) {
                _CallLoadExcelFile();
            }

            EditorGUI.BeginDisabledGroup(!_GetIsAvailable());
            if (GUILayout.Button("Import Data", GUILayout.Height(50))) {
                _CallMethod("ImportData");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Export Data as Excel", GUILayout.Height(50))) {
                _CallMethod("ExportData");
            }
        }
        #endregion

        #region Private - Reflection Helpers
        static readonly BindingFlags FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private List<string> _GetSheets() {
            var prop = target.GetType().GetProperty("Sheets", FLAGS);
            return prop?.GetValue(target) as List<string>;
        }

        private bool _GetIsAvailable() {
            var prop = target.GetType().GetProperty("IsAvailable", FLAGS);
            if (prop == null) return false;
            return (bool)prop.GetValue(target);
        }

        private void _CallLoadExcelFile() {
            _CallMethod("LoadExcelFile");
            previewDirty = true;
        }

        private void _CallGetSheet() {
            _CallMethod("GetSheet");
            previewDirty = true;
        }

        private void _CallMethod(string methodName) {
            target.GetType()
                  .GetMethod(methodName, FLAGS)
                  ?.Invoke(target, null);
        }
        #endregion

        #region Private - Data Preview
        private void _RefreshPreviewIfNeeded() {
            if (!previewDirty) return;
            previewDirty   = false;
            previewHeaders = target.GetType().GetMethod("GetPreviewHeaders", FLAGS)
                                   ?.Invoke(target, null) as string[];
            previewRows    = target.GetType().GetMethod("GetPreviewRows", FLAGS)
                                   ?.Invoke(target, new object[] { 200 }) as string[][];
        }

        private void _DrawDataPreview() {
            if (previewHeaders == null || previewHeaders.Length == 0) return;
            if (previewRows    == null || previewRows.Length    == 0) return;

            EditorGUILayout.Space(SECTION_GAP);
            HTitleDrawer.Draw($"데이터 미리보기 — {previewRows.Length}행");

            float colWidth    = Mathf.Clamp(
                (EditorGUIUtility.currentViewWidth - 48f) / previewHeaders.Length,
                60f, 120f);
            float tableHeight = Mathf.Min(previewRows.Length * 18f + 24f, 300f);
            dataScrollPos = EditorGUILayout.BeginScrollView(dataScrollPos, GUILayout.Height(tableHeight));

            // 헤더 행
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", EditorStyles.toolbarButton, GUILayout.Width(28f));
            foreach (var h in previewHeaders)
                GUILayout.Label(h, EditorStyles.toolbarButton, GUILayout.Width(colWidth));
            EditorGUILayout.EndHorizontal();

            // 데이터 행
            for (int k = 0; k < previewRows.Length; k++) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label((k + 1).ToString(), GUILayout.Width(28f));
                foreach (var cell in previewRows[k])
                    GUILayout.Label(cell, GUILayout.Width(colWidth));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion

    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.04 HTitle 시각 규격 적용
 *
 * # 변경
 * - 섹션 헤더(boldLabel)를 HTitleDrawer.Draw()로 대체 :: 엑셀 파일 / 시트 선택 / 데이터 출력 경로 / 실행(신설) / 데이터 미리보기 5개 타이틀 블록.
 * - 섹션 간 여백을 SECTION_GAP(8f) 상수로 통일 — 조건부 섹션(시트 선택 등)은 가드 통과 후 여백 삽입 (숨김 시 공백 잔류 방지).
 * - asmdef :: HCUP.HInspector.Editor 참조 추가 — HExcel.Editor → HInspector 패키지 의존 발생.
 * - 본 에디터는 ExcelLoader<> 파생 전체(HcupLocalization / HUnityLocalization 등)에 공통 적용됨.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Sprite Preview 제거 — SpritePreviewAttribute/Drawer 로 분리
 *
 * # 변경
 * - iconColIndex / spriteCache / addrHandles / ICON_ROW_HEIGHT 필드 제거
 * - OnDisable() 제거 (_ClearSpriteCache 불필요)
 * - _RefreshPreviewIfNeeded() : icon 컬럼 탐지 + 프리워밍 로직 제거
 * - _DrawDataPreview() : 단순 텍스트 루프로 복원
 * - #region Private - Sprite Preview 전체 제거
 *
 * # 이관 대상
 * - SpritePreviewAttribute.cs + SpritePreviewDrawer.cs (동일 폴더)
 *   GoodsData.Icon 에 [SpritePreview] 태그 부착으로 인스펙터 미리보기 제공
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Loader 저장 섹션 + 데이터 출력 경로 행 추가
 *
 * # 추가
 * - dataOutputPathProp : ExcelLoader.dataOutputPath 직렬화 프로퍼티
 * - _DrawLoaderSaveSection() : IsLoadedFromAsset == false 일 때 경고 HelpBox + "Loader 저장하기" 버튼
 *   (AssetDatabaseInstanceEditor._DrawCreateAssetSection 역할 대체 — ExcelLoaderEditor 가 완전 대체하므로 직접 구현)
 * - _DrawDataOutputPathRow() : 데이터 출력 경로 TextField + "..." 폴더 피커 버튼
 *   선택된 OS 절대경로를 "Assets/..." Unity 상대경로로 변환하여 저장
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 _DrawSheetDropdown 첫 시트 자동 적용
 *
 * # 변경
 * - currentIndex < 0 (저장된 sheetName 없음) 시 index=0 으로 강제 설정 후
 *   sheetNameProp.stringValue, ApplyModifiedProperties(), _CallGetSheet() 즉시 호출
 *
 * # 원인
 * - EndChangeCheck는 사용자가 드롭다운을 직접 변경할 때만 발화
 * - 시트가 1개인 Excel(ex. 900000_Goods.xlsx "Goods" 시트)에서 드롭다운 조작 불가
 *   → sheetName 필드가 공백 유지 → sheet == null → IsAvailable = false → Import Data 비활성
 * - 변경 감지 루프 외부에서 초기 상태를 직접 처리하여 해결
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 excelFilePathProp → excelFileAssetProp (ObjectField 교체)
 *
 * # 변경
 * - excelFilePathProp (string TextField + "..." 버튼) → excelFileAssetProp (DefaultAsset ObjectField)
 * - FindProperty("excelFilePath") → FindProperty("excelFileAsset")
 * - _DrawFilePathRow() : Project 창 드래그 앤 드롭 ObjectField 로 교체
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 데이터 미리보기 패널 추가
 *
 * # 추가
 * - previewHeaders / previewRows / previewDirty / dataScrollPos 필드
 * - _RefreshPreviewIfNeeded() : previewDirty 시에만 Reflection으로 시트 데이터 재파싱
 * - _DrawDataPreview() : 헤더 + 데이터 행을 스크롤 가능한 테이블로 렌더링
 * - _CallLoadExcelFile / _CallGetSheet → previewDirty = true 설정 추가
 *
 * # 결정
 * - Import 없이 시트 선택만으로 미리보기 갱신 (GetPreviewHeaders/GetPreviewRows 사용)
 * - previewDirty 패턴으로 매 프레임 NPOI 파싱 방지
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 컨벤션 정리 — private 필드 접근제어자 제거 + camelCase 적용
 *
 * # 변경
 * - private SerializedProperty _excelFilePathProp → excelFilePathProp (접근제어자 제거, 언더바 제거)
 * - private SerializedProperty _sheetNameProp → sheetNameProp (동일)
 * - private static readonly FLAGS → static readonly FLAGS (접근제어자 제거)
 * - FindProperty 키 원복 : "excelFilePath", "sheetName" (ExcelLoader 필드명과 동기화)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 M3 — Odin CustomEditor 제거 후 IMGUI 재작성
 *
 * # 변경
 * - open generic [CustomEditor(typeof(ExcelLoader<>), true)] 적용
 * - ExcelLoader internal 멤버 전체 Reflection 경유 접근
 * - [FilePath], [ValueDropdown], [Button], [ShowIf] IMGUI 대체 구현
 *
 * =============================================================================
 */
#endif
