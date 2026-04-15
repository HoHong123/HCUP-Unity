using UnityEngine;
using HDiagnosis.Logger;

namespace HUI.DebugConsole {
    public partial class HLogConsole {
        #region Protected
        protected override void Awake() {
#if !UNITY_EDITOR
            if (!runInBuild) {
                Destroy(gameObject);
                return;
            }
#endif
            base.Awake();
            _InitializePanelState();
            _RefreshVisibleEntries();
        }

        protected void OnEnable() {
            _BindUi();
            HLogger.OnLogPublished += _OnHLoggerLogPublished;
            Application.logMessageReceived += _OnUnityLogReceived;
        }

        protected void OnDisable() {
            HLogger.OnLogPublished -= _OnHLoggerLogPublished;
            Application.logMessageReceived -= _OnUnityLogReceived;
            _UnbindUi();
        }

        protected void Update() {
            _UpdateFps();
            _UpdateNetwork();
        }
        #endregion
    }
}
