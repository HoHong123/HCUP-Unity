using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.Core;
using HUtil.Logger;

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

            public LogQue(
                int uid, PopLevel level,
                string title, string message,
                Action onClickOk,
                Action onClickCancel) {
                UID = uid;
                Level = level;
                Title = title;
                Message = message;
                OnClickOk = onClickOk;
                OnClickCancel = onClickCancel;
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


        public void ShowLog(PopLevel level, string title, string message, Action onClickOk = null, Action onClickCancel = null) {
            int uid = ++logCreatStack;
            background.SetActive(true);
            var wrapper = onClickCancel;
            wrapper += _SetTextPopup;
            logHistory.Enqueue(new(uid, level, title, message, onClickOk, wrapper));

            switch (level) {
            case PopLevel.Log: HLogger.Log($"[Log UID {uid}] {title} :: {message}"); break;
            case PopLevel.Warning: HLogger.Warning($"[Warning UID {uid}] {title} :: {message}"); break;
            case PopLevel.Alert: HLogger.Error($"[Alert UID {uid} ]  {title}  ::  {message}"); break;
            case PopLevel.Fatal: HLogger.Error($"[Fatal UID {uid} ]  {title}  ::  {message}"); break;
            default: HLogger.Error($"Log data invalid. Check log level({level.ToString()})"); break;
            }

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
            imgInstnace = Instantiate(imagePrefab, gameParent);
            imgInstnace.SetUi(texture);
            imgInstnace.OnClickPanel += onClick;
        }

        public void ShowVideo(string address, Action onClick = null, int width = 0, int height = 0) {
            background.SetActive(true);
            vidInstnace = Instantiate(videoPrefab, gameParent);
            vidInstnace.SetVideo(address, width, height);
            vidInstnace.OnClickPanel += onClick;
        }


        private void _SetTextPopup() {
            if (logHistory.Count == 0) {
                textInstance.Close();
                if (isAllCose) background.SetActive(false);
                return;
            }

            LogQue log = logHistory.Dequeue();
            textInstance.SetText(log.Title, log.Message, log.OnClickOk, log.OnClickCancel);
            textInstance.Open();
        }

#if UNITY_EDITOR
        int _testId = 0;
        [Button("Text Test")]
        private void _Test() {
            int id = _testId++;
            ShowLog(PopLevel.Log, "Test", $"Testing event {id}",
                () => {
                    Debug.Log($"[Popup {id}] Ok Called");
                },
                () => {
                    Debug.Log($"[Popup {id}] Cancel Called");
                });
        }
#endif
    }
}