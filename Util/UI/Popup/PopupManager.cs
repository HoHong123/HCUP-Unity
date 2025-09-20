#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 각 프로젝트의 팝업을 담당할 매니지먼트 스크립트입니다.
 * 
 * ** 사용법 **
 * 1. 해당 스크립트를 상속 받는 각 팝업매니저 스크립트를 생성합니다.
 * 2. 자식 팝업매니저를 컴포넌트로 가지는 빈 오브젝트를 생성합니다.
 * 3. 각 팝업창이 출력될 부모 빈 오브젝트들을 생성합니다.
 * + 각 레벨마다 하나씩 존재하는 것을 추천합니다.
 * 4. 팝업 발생시 배경을 담당할 UI 오브젝트를 'background' 값에 등록합니다.
 * 5. 기본 '텍스트, 이미지, 비디오' 팝업을 등록합니다. (Demo파일에 들어있습니다. 필요시 원하는 팝업을 커스텀하시면 됩니다.)
 * 
 * ** 설명 **
 * 1. TextPopup의 경우, 사용 빈도가 높은 편으로 고려하여 하나를 생성한 후 재사용합니다.
 * 2. 이미지와 비디오 팝업의 경우, 사용 빈도가 낮을 것으로 생각하여 필요할때 객체를 생성하고 사용 후 파괴하도록 구현했습니다.
 * 3. 'logParent'는 일반 게임 로그가 아닌 디버깅, 알람, 경고 용도의 로그를 가지는 부모 오브젝트 입니다.
 * 4. 'gameParent'는 게임에 사용될 의도된 팝업창이 올라가는 부모 오브젝트 입니다.
 * 5. 'logHistory'는 팝업창이 반복 재사용될때 다음 출력할 데이터를 저장하는 컨테이너입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Util.Logger;
using Util.Core;
using Util.OdinCompat;

namespace Util.UI.Popup {
    public abstract class PopupManager<T> : SingletonBehaviour<T> where T : PopupManager<T> {
        #region Class
        [Serializable]
        public class LogQue {
            public int UID { get; private set; }
            public PopLevel Level { get; private set; }
            public string Title { get; private set; }
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

            public override string ToString() =>
                $"UID :: {UID}\n" +
                $"Level :: {Level}\n" +
                $"Title :: {Title}\n" +
                $"Message :: {Message}";
        }
        #endregion

        #region Member
        [HeaderOrTitle("UI")]
        [SerializeField]
        protected GameObject background;

        [HeaderOrTitle("Prefab")]
        [SerializeField]
        protected TextPopup textPrefab;
        [SerializeField]
        protected ImagePopup imagePrefab;
        [SerializeField]
        protected VideoPopup videoPrefab;

        [HeaderOrTitle("Parents")]
        [SerializeField]
        protected Transform logParent;
        [SerializeField]
        protected Transform gameParent;

        [HeaderOrTitle("Logs")]
        [SerializeField]
        protected Queue<LogQue> logHistory = new();

        protected TextPopup textInstance = null;
        protected ImagePopup imgInstnace = null;
        protected VideoPopup vidInstnace = null;

        protected int creatStack = 0;


        protected bool _IsAllCose => gameParent.childCount + logHistory.Count == 0;
        #endregion


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