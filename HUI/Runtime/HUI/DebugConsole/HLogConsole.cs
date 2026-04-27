using HCore;
using HInspector;
using HDiagnosis.Logger;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HUI.DebugConsole {
    public partial class HLogConsole : SingletonBehaviour<HLogConsole>, IBasicPanel {
        #region Fields
        [HTitle("Panel")]
        [SerializeField]
        GameObject panelRoot;

        [HTitle("View")]
        [SerializeField]
        HLogRecycleView recycleView;
        [SerializeField]
        TextMeshProUGUI fpsText;
        [SerializeField]
        TextMeshProUGUI networkText;

        [HTitle("Buttons")]
        [SerializeField]
        Button openButton;
        [SerializeField]
        Button closeButton;
        [SerializeField]
        Button clearButton;
        [SerializeField]
        Button saveButton;

        [HTitle("Filter : Level")]
        [SerializeField]
        Toggle showLogToggle;
        [SerializeField]
        Toggle showWarnToggle;
        [SerializeField]
        Toggle showErrorToggle;

        [HTitle("Filter : Source")]
        [SerializeField]
        Toggle showHLoggerToggle;
        [SerializeField]
        Toggle showUnityToggle;

        [HTitle("Runtime")]
        [SerializeField]
        bool runInBuild = true;
        [SerializeField]
        int maxConsoleEntries = 2000;

        [HTitle("Save")]
        [SerializeField]
        string editorSaveFolder = "Logs";

        readonly List<HLogCellData> entries = new();
        readonly List<HLogCellData> filteredEntries = new();
        readonly Dictionary<string, int> pendingUnityEchoCountByCondition = new();

        bool isFollowingLatest = true;
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
            HLogger.Log(message, target);
        }

        public static void Warning(string message, GameObject target = null) {
            HLogger.Warning(message, target);
        }

        public static void Error(string message, GameObject target = null, string debug = "") {
            HLogger.Error(message, target, false, debug);
        }
        #endregion

#if UNITY_EDITOR
        [HTitle("Debug")]
        int debugStack = 1;

        [ContextMenu("Send HLog.Log")]
        private void _MakeHLog() => HLogger.Log($"{debugStack++}. HLog Called");
        [ContextMenu("Send HLog.Warn")]
        private void _MakeHWarn() => HLogger.Warning($"{debugStack++}. HWarning Called");
        [ContextMenu("Send HLog.Error")]
        private void _MakeHError() => HLogger.Error($"{debugStack++}. HError Called");
        [ContextMenu("Send HLog.Fatal")]
        private void _MakeHFatal() => HLogger.Throw(new System.Exception(), $"{debugStack++}. HThrow Called", false);

        [ContextMenu("Send Debug.Log")]
        private void _MakeLog() => Debug.Log($"{debugStack++}. Log Called");
        [ContextMenu("Send Debug.Warn")]
        private void _MakeWarn() => Debug.LogWarning($"{debugStack++}. Warning Called");
        [ContextMenu("Send Debug.Error")]
        private void _MakeError() => Debug.LogError($"{debugStack++}. Error Called");

        #region Generate Random
        [SerializeField]
        float randomDelay = 1f;
        [SerializeField, HReadOnly]
        bool isRandomRunning;
        Coroutine randomLogRoutine;

        [ContextMenu("Start Random Log (1/sec)")]
        private void _StartRandomLog() {
            if (isRandomRunning) return;
            isRandomRunning = true;
            randomLogRoutine = StartCoroutine(_RandomLogRoutine());
        }

        [ContextMenu("Stop Random Log")]
        private void _StopRandomLog() {
            if (!isRandomRunning) return;
            isRandomRunning = false;

            if (randomLogRoutine != null) {
                StopCoroutine(randomLogRoutine);
            }

            randomLogRoutine = null;
        }

        private IEnumerator _RandomLogRoutine() {
            while (isRandomRunning) {
                _RunRandomOnce();
                yield return new WaitForSeconds(randomDelay);
            }
        }

        private void _RunRandomOnce() {
            int random = Random.Range(0, 7);

            try {
                switch (random) {
                case 0: _MakeHLog(); break;
                case 1: _MakeHWarn(); break;
                case 2: _MakeHError(); break;
                case 3: _MakeHFatal(); break;
                case 4: _MakeLog(); break;
                case 5: _MakeWarn(); break;
                case 6: _MakeError(); break;
                default:
                    Debug.LogError($"[HLogConsole] Invalid random value :: {random}");
                    break;
                }
            }
            catch (System.Exception e) {
                Debug.LogException(e);
            }
        }
        #endregion
#endif
    }
}
