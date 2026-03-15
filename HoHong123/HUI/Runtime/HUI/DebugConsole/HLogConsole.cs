using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HUtil.Core;

namespace HUI.DebugConsole {
    public partial class HLogConsole : SingletonBehaviour<HLogConsole>, IBasicPanel {
        #region Serialized Fields
        [Header("Panel")]
        [SerializeField]
        GameObject panelRoot;

        [Header("View")]
        [SerializeField]
        HLogRecycleView recycleView;
        [SerializeField]
        TextMeshProUGUI fpsText;
        [SerializeField]
        TextMeshProUGUI networkText;

        [Header("Buttons")]
        [SerializeField]
        Button openButton;
        [SerializeField]
        Button closeButton;
        [SerializeField]
        Button clearButton;
        [SerializeField]
        Button saveButton;

        [Header("Filter : Level")]
        [SerializeField]
        Toggle showLogToggle;
        [SerializeField]
        Toggle showWarnToggle;
        [SerializeField]
        Toggle showErrorToggle;

        [Header("Filter : Source")]
        [SerializeField]
        Toggle showHLoggerToggle;
        [SerializeField]
        Toggle showUnityToggle;

        [Header("Runtime")]
        [SerializeField]
        bool runInBuild = true;
        [SerializeField]
        int maxConsoleEntries = 2000;

        [Header("Save")]
        [SerializeField]
        string editorSaveFolder = "Logs";
        #endregion

        #region Fields
        readonly List<HLogCellData> entries = new();
        readonly List<HLogCellData> filteredEntries = new();
        readonly Dictionary<string, int> pendingUnityEchoCountByCondition = new();

        float fpsInterval = 0.5f;
        float fpsTimer;
        int fpsFrameCount;
        float fpsAccumulatedDelta;
        float networkInterval = 1f;
        float networkTimer;
        #endregion

        #region Property
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool RunInBuild => runInBuild;
        public string EditorSaveFolder => editorSaveFolder;
        #endregion

        #region Public
        public static void Log(string message, GameObject target = null) {
            HUtil.Logger.HLogger.Log(message, target);
        }

        public static void Warning(string message, GameObject target = null) {
            HUtil.Logger.HLogger.Warning(message, target);
        }

        public static void Error(string message, GameObject target = null, string debug = "") {
            HUtil.Logger.HLogger.Error(message, target, false, debug);
        }
        #endregion
    }
}
