using HDiagnosis.Logger;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.NodeWindow.Editor
{
    public sealed class HGraphCanvas : GraphView
    {
        private const string USS_PATH =
            "Assets/01_Scripts/HCUP-Unity/HoHong123/HWindows/Editor/NodeWindow/UI/HGraphWindow.uss";

        public HGraphCanvas()
        {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            LoadStyleSheet();

            this.StretchToParentSize();
        }

        private void LoadStyleSheet()
        {
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (sheet == null)
            {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] HGraphWindow.uss not found at path {USS_PATH}. " +
                    "Grid/style will fall back to GraphView defaults. " +
                    "Verify package installation or USS rename.");
                return;
            }
            styleSheets.Add(sheet);
        }
    }
}
