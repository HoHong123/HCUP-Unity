#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/* =========================================================
 * @Jason - PKH
 * HAudio 저작 도구 3종을 탭으로 묶은 호스트 창입니다.
 *
 * 주요 기능 ::
 * Catalogs / Generator / Enum 세 패널을 탭으로 전환하며 그립니다.
 * 각 패널은 EditorWindow 가 아니라 [Serializable] 클래스이며, 이 창이 [SerializeField] 로
 * 들고 있어 도메인 리로드 후에도 패널 설정이 유지됩니다.
 *
 * 사용법 ::
 * HCUP/Audio 아래 기존 메뉴 3개가 그대로 남아 있고, 각각 이 창을 해당 탭으로 엽니다.
 * 손에 익은 진입 경로를 유지하기 위한 의도이며, 탭이 늘어도 진입점은 그대로입니다.
 *
 * 주의 ::
 * 1. 패널은 EditorWindow 를 상속하지 않으므로 Repaint 는 Draw 에서 받은 host 를 통해 부릅니다.
 * 2. 패널 클래스는 파일 하나씩 분리되어 있습니다 (AudioCatalogEditorPanel /
 *    AudioCatalogGeneratorPanel / AudioClipEnumPanel). 이 창은 탭 전환만 담당합니다.
 * 3. Sound Data Diagnostics 는 Play Mode 전용 관측 창이라 성격이 달라 합치지 않았습니다.
 * =========================================================
 */

namespace HAudio.Editor {
    public sealed class SoundToolsWindow : EditorWindow {
        #region Types
        public enum Tab {
            Catalogs = 0,
            Generator = 1,
            Enum = 2,
        }
        #endregion

        #region Constants
        static readonly string[] TAB_LABELS = { "Sound Catalogs", "Catalog Generator", "Enum Generator" };
        #endregion

        #region Serialized
        [SerializeField]
        Tab activeTab = Tab.Catalogs;

        // 패널을 [SerializeField] 로 들어야 도메인 리로드 후에도 각 탭의 입력값이 남는다.
        [SerializeField]
        AudioCatalogEditorPanel catalogsPanel = new();
        [SerializeField]
        AudioCatalogGeneratorPanel generatorPanel = new();
        [SerializeField]
        AudioClipEnumPanel enumPanel = new();
        #endregion

        #region Menu
        // 기존 메뉴 경로를 유지한다 — 각각 통합 창을 해당 탭으로 연다.
        [MenuItem("HCUP/Audio/Sound Catalog Editor")]
        public static void OpenCatalogs() => Open(Tab.Catalogs);

        [MenuItem("HCUP/Audio/Sound Catalog Generator")]
        public static void OpenGenerator() => Open(Tab.Generator);

        [MenuItem("HCUP/Audio/Sound Clip Enum Generator")]
        public static void OpenEnum() => Open(Tab.Enum);

        public static void Open(Tab tab) {
            var window = GetWindow<SoundToolsWindow>();
            window.titleContent = new GUIContent("Sound Tools");
            window.activeTab = tab;
            window.Show();
        }
        #endregion

        #region Unity Life Cycle
        private void OnEnable() {
            // 패널은 Unity 메시지를 받지 못하므로 호스트가 대신 전달한다.
            catalogsPanel ??= new AudioCatalogEditorPanel();
            generatorPanel ??= new AudioCatalogGeneratorPanel();
            enumPanel ??= new AudioClipEnumPanel();

            catalogsPanel.OnEnable();
        }

        private void OnGUI() {
            _DrawTabBar();
            EditorGUILayout.Space(4);

            switch (activeTab) {
            case Tab.Catalogs:
                catalogsPanel.Draw(this);
                break;
            case Tab.Generator:
                generatorPanel.Draw(this);
                break;
            case Tab.Enum:
                enumPanel.Draw(this);
                break;
            default:
                EditorGUILayout.HelpBox($"Unknown tab: {activeTab}", MessageType.Error);
                break;
            }
        }
        #endregion

        #region Draw
        private void _DrawTabBar() {
            EditorGUILayout.Space(4);
            int next = GUILayout.Toolbar((int)activeTab, TAB_LABELS, GUILayout.Height(24));
            if (next == (int)activeTab) return;

            activeTab = (Tab)next;
            // 탭을 옮기면 이전 탭의 잔여 컨트롤 포커스가 새 탭의 첫 필드로 새는 것을 막는다.
            GUI.FocusControl(null);
        }
        #endregion
    }
}
#endif
