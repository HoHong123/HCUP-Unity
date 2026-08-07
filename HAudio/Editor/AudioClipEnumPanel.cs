#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using HAudio.Core;

/* =========================================================
 * @Jason - PKH
 * 카탈로그에서 게임 쪽 AudioClips enum + 재생 확장 메서드를 생성하는 탭 패널입니다.
 *
 * 주요 기능 ::
 * AudioAuthoringSettingsSO 를 편집하고, 그 설정으로 enum + 확장 메서드 2파일을 방출합니다.
 * "Add All In Folder" 로 폴더 하위 카탈로그를 목록에 채워 넣을 수 있습니다.
 *
 * 사용법 ::
 * SoundToolsWindow 의 "Enum" 탭으로 표시됩니다.
 * 설정 에셋이 없으면 이 패널에서 바로 만들 수 있습니다.
 *
 * 주의 ::
 * 1. 이 패널은 설정을 스스로 들고 있지 않는다. 인스펙터 클립 드롭다운이 같은 목록을 봐야
 *    enum 과 어긋나지 않으므로, 설정의 소유자는 에셋(AudioAuthoringSettingsSO)이다.
 * 2. 대상 카탈로그를 전체 스캔하지 않고 명시 목록으로 받는다. 전체 스캔은 HCUP 샘플·
 *    테스트 카탈로그까지 잡아 게임 enum 을 오염시킨다.
 * 3. 이름 충돌은 생성 실패로 끝낸다 — 자동 개명하지 않는다.
 * 4. 생성물은 프로젝트별 산출물이다. HCUP 안에 생성하면 다른 프로젝트의 enum 을 덮어쓴다.
 * =========================================================
 */

namespace HAudio.Editor {
    [Serializable]
    public sealed class AudioClipEnumPanel {
        #region Serialized
        // 스캔 대상 폴더만 창 상태로 남긴다. 저작 결과가 아니라 조작 편의값이다.
        [SerializeField]
        DefaultAsset scanFolder;
        #endregion

        #region Runtime
        readonly System.Collections.Generic.List<string> logs = new();
        Vector2 panelScroll;
        Vector2 logsScroll;

        [NonSerialized]
        EditorWindow host;
        [NonSerialized]
        AudioAuthoringSettingsSO settings;
        #endregion

        #region Panel
        public void Draw(EditorWindow owner) {
            host = owner;

            using (var sv = new EditorGUILayout.ScrollViewScope(panelScroll)) {
                panelScroll = sv.scrollPosition;

                _DrawHeader();

                if (!_DrawSettingsSlot()) {
                    _DrawLogs();
                    return;
                }

                _DrawSources();
                _DrawOutput();
                _DrawActions();
                _DrawLogs();
            }
        }
        #endregion

        #region Draw
        private void _DrawHeader() {
            EditorGUILayout.LabelField("Audio Clip Enum Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "카탈로그의 token(\"{uid}_{name}\") 에서 enum 과 재생 확장 메서드를 생성합니다.\n" +
                "생성물은 프로젝트별 산출물이므로 반드시 게임 어셈블리 폴더에 출력하세요 — " +
                "HCUP 안에 생성하면 다른 프로젝트의 enum 을 덮어씁니다.",
                MessageType.Info);
            EditorGUILayout.Space(6);
        }

        /// <summary> 설정 에셋 슬롯. 편집할 대상이 없으면 false 를 돌려 이후 섹션을 그리지 않는다. </summary>
        private bool _DrawSettingsSlot() {
            if (settings == null) settings = AudioAuthoringSettingsSO.Find();

            settings = (AudioAuthoringSettingsSO)EditorGUILayout.ObjectField(
                "Authoring Settings", settings, typeof(AudioAuthoringSettingsSO), false);

            if (settings != null) {
                EditorGUILayout.Space(8);
                return true;
            }

            EditorGUILayout.HelpBox(
                "저작 설정 에셋이 없습니다. 이 에셋이 enum 생성기와 인스펙터 클립 드롭다운의 " +
                "공통 원천입니다 — 없으면 드롭다운도 비어 있습니다.",
                MessageType.Warning);

            if (GUILayout.Button("Create Settings Asset...", GUILayout.Height(24))) _CreateSettings();

            EditorGUILayout.Space(8);
            return false;
        }

        private void _DrawSources() {
            EditorGUILayout.LabelField("Source Catalogs", EditorStyles.boldLabel);

            var catalogs = settings.Catalogs;
            for (int k = 0; k < catalogs.Count; k++) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUI.BeginChangeCheck();
                    var next = (AudioCatalogSO)EditorGUILayout.ObjectField(
                        catalogs[k], typeof(AudioCatalogSO), false);
                    if (EditorGUI.EndChangeCheck()) {
                        settings.SetCatalogAt(k, next);
                        _MarkSettingsDirty();
                    }

                    if (GUILayout.Button("−", GUILayout.Width(24))) {
                        settings.RemoveCatalogAt(k);
                        _MarkSettingsDirty();
                        k--;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("+ Add Slot")) {
                    settings.AddCatalogSlot();
                    _MarkSettingsDirty();
                }

                if (GUILayout.Button("Clear")) {
                    settings.ClearCatalogs();
                    _MarkSettingsDirty();
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope()) {
                scanFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                    "Scan Folder", scanFolder, typeof(DefaultAsset), false);

                GUI.enabled = scanFolder;
                if (GUILayout.Button("Add All In Folder", GUILayout.Width(140))) _AddAllInFolder();
                GUI.enabled = true;
            }

            EditorGUILayout.Space(8);
        }

        private void _DrawOutput() {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var folder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Script Output Folder", settings.OutputFolder, typeof(DefaultAsset), false);
            string ns = EditorGUILayout.TextField("Namespace (optional)", settings.NamespaceName);
            string typeName = EditorGUILayout.TextField("Enum Type Name", settings.EnumTypeName);

            if (EditorGUI.EndChangeCheck()) {
                settings.SetOutputFolder(folder);
                settings.SetNamespaceName(ns);
                settings.SetEnumTypeName(typeName);
                _MarkSettingsDirty();
            }

            EditorGUILayout.Space(6);
        }

        private void _DrawActions() {
            string outputPath = settings.OutputFolderPath;
            bool canGenerate =
                settings.Catalogs.Count > 0 &&
                !string.IsNullOrWhiteSpace(outputPath) &&
                !string.IsNullOrWhiteSpace(settings.EnumTypeName);

            GUI.enabled = canGenerate;
            if (GUILayout.Button("Generate Enum + Play Extensions", GUILayout.Height(28))) {
                _Generate(outputPath);
            }
            GUI.enabled = true;

            EditorGUILayout.Space(8);
        }

        private void _DrawLogs() {
            EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);

            using (var sv = new EditorGUILayout.ScrollViewScope(logsScroll, GUILayout.Height(160))) {
                logsScroll = sv.scrollPosition;
                for (int k = 0; k < logs.Count; k++) {
                    EditorGUILayout.LabelField(logs[k], EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (GUILayout.Button("Clear Logs")) {
                logs.Clear();
                _Repaint();
            }
        }
        #endregion

        #region Operations
        private void _CreateSettings() {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Audio Authoring Settings",
                "AudioAuthoringSettings",
                "asset",
                "저작 설정 에셋을 만들 위치를 고르세요. 프로젝트당 1개만 두세요.");

            if (string.IsNullOrEmpty(path)) return;

            var created = ScriptableObject.CreateInstance<AudioAuthoringSettingsSO>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            settings = created;
            logs.Add($"[Settings] Created :: {path}");
            _Repaint();
        }

        private void _AddAllInFolder() {
            string folderPath = AssetDatabase.GetAssetPath(scanFolder);
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                logs.Add($"[Scan] Failed :: not a folder. path={folderPath}");
                _Repaint();
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioCatalogSO", new[] { folderPath });
            int added = 0;

            foreach (string guid in guids) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(assetPath);
                if (!settings.TryAddCatalog(catalog)) continue;

                added++;
                logs.Add($"[Scan] Added :: {assetPath}");
            }

            if (added > 0) _MarkSettingsDirty();

            logs.Add($"[Scan] Done :: added={added}, total={settings.Catalogs.Count}");
            _Repaint();
        }

        private void _Generate(string outputPath) {
            var targets = new System.Collections.Generic.List<AudioCatalogSO>();
            var catalogs = settings.Catalogs;
            for (int k = 0; k < catalogs.Count; k++) {
                if (catalogs[k]) targets.Add(catalogs[k]);
            }

            if (targets.Count < 1) {
                logs.Add("[Enum] Failed :: no valid catalog in the list.");
                _Repaint();
                return;
            }

            var result = AudioClipEnumGenerator.Generate(
                targets, outputPath, settings.NamespaceName, settings.EnumTypeName);

            if (!result.Success) {
                // 이름 충돌은 자동 개명하지 않고 실패로 끝낸다 (설계 결정). 저작자가 원본
                // 파일명을 고치는 것이 유일한 해소 경로이며, 그래야 이름이 의도와 일치한다.
                logs.Add($"[Enum] Failed :: {result.Errors.Count} error(s)");
                foreach (string error in result.Errors) logs.Add($"[Enum]   {error}");
                _Repaint();
                return;
            }

            // 카탈로그가 바뀌지 않았어도 목록 캐시를 버린다 — 생성 직후 드롭다운이
            // 옛 목록을 보여주면 저작자가 방금 만든 항목을 못 고른다.
            AudioClipDropdownSource.Invalidate();

            logs.Add($"[Enum] Done :: catalogs={targets.Count}, members={result.MemberCount}");
            logs.Add($"[Enum]   {result.EnumFilePath}");
            logs.Add($"[Enum]   {result.ExtensionFilePath}");
            _Repaint();
        }

        private void _MarkSettingsDirty() {
            if (settings == null) return;

            EditorUtility.SetDirty(settings);
            AudioClipDropdownSource.Invalidate();
        }

        private void _Repaint() {
            if (host != null) host.Repaint();
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 설정 소유권을 AudioAuthoringSettingsSO 로 이관
 *
 * # 변경
 * - sourceCatalogs / outputFolder / namespaceName / enumTypeName 직렬화 필드 제거.
 *   전부 설정 에셋에서 읽고 쓴다. 창에는 scanFolder(조작 편의값)만 남는다.
 * - 설정 에셋이 없을 때 패널에서 바로 만드는 경로 추가.
 * - 설정·생성 후 AudioClipDropdownSource.Invalidate 호출.
 *
 * # 이유
 * - 인스펙터 클립 드롭다운이라는 두 번째 소비자가 생겼다. 두 소비자가 같은 목록을 봐야
 *   enum 과 드롭다운이 어긋나지 않으므로, 설정의 소유자가 창일 수 없다.
 * - 창 상태는 저장소에 남지 않아 다른 PC 에서 enum 재생성 결과가 달라진다.
 *   enum 은 게임의 공개 API 표면이라 재현 가능해야 한다.
 *
 * =============================================================================
 */
#endif
