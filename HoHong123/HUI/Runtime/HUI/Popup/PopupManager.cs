using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.Core;
using HUtil.Logger;

namespace HUtil.UI.Popup {
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
            public Action OnClickAction { get; private set; }

            public LogQue(
                int uid, PopLevel level,
                string title, string message,
                Action onClickAction = null) {
                UID = uid;
                Level = level;
                Title = title;
                Message = message;
                OnClickAction = onClickAction;
            }
        }
        #endregion

        #region Member
        [Title("UI")]
        [SerializeField]
        protected GameObject background;
        [SerializeField]
        protected GameObject spinner;

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

        protected int creatStack = 0;


        protected bool isAllCose => gameParent.childCount + logHistory.Count == 0;
        #endregion


        public void ShowSpinner() => spinner.SetActive(true);
        public void HideSpinner() => spinner.SetActive(false);

        public void ShowLog(PopLevel level, string title, string message, Action onClickCancel = null) {
            int uid = ++creatStack;
            background.SetActive(true);
            logHistory.Enqueue(new(uid, level, title, message,onClickCancel));

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
                textInstance.OnClickCancel += _SetTextPopup;
                textInstance.Close();
            }

            if (!textInstance.IsActive) {
                _SetTextPopup();
            }
        }

        public void ShowImage(Sprite sprite, Action onClick = null) => ShowImage(sprite.texture, onClick);
        public void ShowImage(Texture texture, Action onClick = null) {
            imgInstnace = Instantiate(imagePrefab, gameParent);
            imgInstnace.SetUi(texture);
            imgInstnace.OnClickPanel += onClick;
        }

        public void ShowVideo(string address, Action onClick = null, int width = 0, int height = 0) {
            vidInstnace = Instantiate(videoPrefab, gameParent);
            vidInstnace.SetVideo(address, width, height);
            vidInstnace.OnClickPanel += onClick;
        }


        private void _SetTextPopup() {
            if (logHistory.Count == 0) {
                textInstance.Close();
                background.SetActive(false);
                return;
            }

            LogQue log = logHistory.Dequeue();
            textInstance.SetText(log.Title, log.Message, log.OnClickAction);
            textInstance.Open();
        }
    }
}