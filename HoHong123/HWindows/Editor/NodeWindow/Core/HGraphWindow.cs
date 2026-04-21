using UnityEditor;
using UnityEngine;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphWindow : EditorWindow {
        [MenuItem("Window/HWindows/Node Window/Graph Editor")]
        public static void Open() {
            HGraphWindow window = GetWindow<HGraphWindow>();
            window.titleContent = new GUIContent("Graph Editor");
            window.minSize = new Vector2(400, 300);
        }

        private HGraphCanvas _canvas;

        private void CreateGUI() {
            _canvas = new HGraphCanvas();
            rootVisualElement.Add(_canvas);
        }

        private void OnDisable() {
            // L1: no cleanup needed. Reserved as guard point for L2+ event/subscription unhooks.
        }
    }
}
