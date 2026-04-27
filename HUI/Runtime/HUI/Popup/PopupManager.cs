#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 모든 Popup 시스템의 공통 베이스 매니저입니다.
 * Text / Image / Video Popup을 관리하며 로그 메시지 큐 시스템을 제공합니다.
 *
 * 주의사항 ::
 * 1. PopupManager는 SingletonBehaviour 기반으로 동작합니다.
 * 2. Text Popup은 Queue 구조로 순차적으로 표시됩니다.
 * 3. Popup Background는 활성 Popup 여부에 따라 자동 제어됩니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HCore;
using HDiagnosis.Logger;

namespace HUI.Popup {
    public abstract class PopupManager<T> : SingletonBehaviour<T> where T : PopupManager<T> {
        #region Class
        [Serializable]
        public class LogQue {
            [ShowInInspector]
            public int UID { get; private set; }
            [ShowInInspector]
            public PopLevel Level { get; private set; }
            
            [ShowInInspector]
            public string Title { get; private set; }
            [ShowInInspector]
            public string Message { get; private set; }

            public Action OnClickOk { get; private set; }
            public Action OnClickCancel { get; private set; }

            [ShowInInspector]
            public string OkText { get; private set; } = null;
            [ShowInInspector]
            public string CancelText { get; private set; } = null;

            public LogQue(
                int uid, PopLevel level,
                string title, string message,
                Action onClickOk, Action onClickCancel,
                string okTxt, string cancelTxt) {
                UID = uid;
                Level = level;
                Title = title;
                Message = message;
                OnClickOk = onClickOk;
                OnClickCancel = onClickCancel;
                OkText = okTxt;
                CancelText = cancelTxt;
            }
        }
        #endregion

        #region Member
        [Title("UI")]
        [SerializeField]
        protected GameObject background;

        [Title("Prefab")]
        [SerializeField]
        protected TextPopup textPrefab;
        [SerializeField]
        protected ImagePopup imagePrefab;
        [SerializeField]
        protected VideoPopup videoPrefab;

        [Title("Parents")]
        [SerializeField]
        protected Transform poolParent;
        [SerializeField]
        protected Transform logParent;
        [SerializeField]
        protected Transform gameParent;

        [Title("Logs")]
        [SerializeField]
        protected Queue<LogQue> logHistory = new();

        protected TextPopup textInstance = null;
        protected ImagePopup imgInstnace = null;
        protected VideoPopup vidInstnace = null;

        protected int logCreatStack = 0;


        protected bool isAllCose => gameParent.childCount + logHistory.Count == 0;
        #endregion


        public void ShowLog(
            PopLevel level,
            string title, string message,
            Action onClickOk = null, Action onClickCancel = null,
            string okTxt = null, string cancelTxt = null) {
            int uid = ++logCreatStack;
            switch (level) {
            case PopLevel.Log: HLogger.Log($"[Log UID {uid}] {title} :: {message}"); break;
            case PopLevel.Warning: HLogger.Warning($"[Warning UID {uid}] {title} :: {message}"); break;
            case PopLevel.Alert: HLogger.Error($"[Alert UID {uid} ]  {title}  ::  {message}"); break;
            case PopLevel.Fatal: HLogger.Error($"[Fatal UID {uid} ]  {title}  ::  {message}"); break;
            default: HLogger.Error($"Log data invalid. Check log level({level.ToString()})"); return;
            }

            background.SetActive(true);
            var wrapper = onClickCancel;
            wrapper += _SetTextPopup;
            logHistory.Enqueue(new(uid, level, title, message, onClickOk, wrapper, okTxt, cancelTxt));

            // Create one text popup
            if (textInstance == null) {
                textInstance = Instantiate(textPrefab, logParent);
                textInstance.Close();
            }

            if (!textInstance.IsActive) {
                _SetTextPopup();
            }
        }

        public void ShowImage(Sprite sprite, Action onClick = null) => ShowImage(sprite.texture, onClick);
        public void ShowImage(Texture texture, Action onClick = null) {
            background.SetActive(true);
            _DisposeImageInstance();

            imgInstnace = Instantiate(imagePrefab, gameParent);
            imgInstnace.SetUi(texture);
            if (onClick != null) imgInstnace.OnClickPanel += onClick;
        }

        public void ShowVideo(string address, Action onClick = null, int width = 0, int height = 0) {
            background.SetActive(true);
            _DisposeVideoInstance();

            vidInstnace = Instantiate(videoPrefab, gameParent);
            vidInstnace.SetVideo(address, width, height);
            if (onClick != null) vidInstnace.OnClickPanel += onClick;
        }

        // 싱글톤 파괴 시 자식 팝업 인스턴스와 큐에 남은 외부 Action 참조를 모두 끊는다.
        protected override void OnDestroy() {
            _DisposeImageInstance();
            _DisposeVideoInstance();

            if (textInstance != null) {
                Destroy(textInstance.gameObject);
                textInstance = null;
            }

            logHistory.Clear();
            base.OnDestroy();
        }

        private void _DisposeImageInstance() {
            if (imgInstnace == null) return;
            Destroy(imgInstnace.gameObject);
            imgInstnace = null;
        }

        private void _DisposeVideoInstance() {
            if (vidInstnace == null) return;
            Destroy(vidInstnace.gameObject);
            vidInstnace = null;
        }


        private void _SetTextPopup() {
            if (logHistory.Count == 0) {
                textInstance.Close();
                if (isAllCose) background.SetActive(false);
                return;
            }

            LogQue log = logHistory.Dequeue();
            textInstance.SetText(log.Title, log.Message, log.OnClickOk, log.OnClickCancel, log.OkText, log.CancelText);
            textInstance.Open();
        }

#if UNITY_EDITOR
        protected bool _IsPlaying => Application.isPlaying;
        int _testId = 0;

        [TitleGroup("Test (Play in run-time)")]
        [BoxGroup("Test (Play in run-time)/Text")]
        [Button("Test Text Popup"), EnableIf(nameof(_IsPlaying))]
        private void _Test() {
            int id = _testId++;
            ShowLog(PopLevel.Log, "Test", $"Testing event {id}",
                () => { Debug.Log($"[Popup {id}] Ok Called"); },
                () => { Debug.Log($"[Popup {id}] Cancel Called"); });
        }
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. Text Popup 로그를 Queue 기반으로 순차 표시합니다.
 * 2. Image / Video Popup을 생성하여 표시합니다.
 * 3. Popup Background 활성 상태를 자동 관리합니다.
 *
 * 사용법 ::
 * 1. ShowLog()를 호출하여 Text Popup 메시지를 표시합니다.
 * 2. ShowImage() 또는 ShowVideo()를 통해 미디어 Popup을 생성합니다.
 *
 * 기타 ::
 * 1. Popup 로그는 Queue<LogQue> 구조로 관리됩니다.
 * 2. TextPopup 인스턴스는 최초 1회 생성 후 재사용됩니다.
 * =========================================================
 */
#endif
