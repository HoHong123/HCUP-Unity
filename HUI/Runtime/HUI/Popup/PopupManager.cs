#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 모든 Popup 시스템의 공통 베이스 매니저입니다.
 * Text / Image / Video / AwaitCover Popup을 관리하며 로그 메시지 큐 시스템을 제공합니다.
 *
 * 주의사항 ::
 * 1. PopupManager는 SingletonBehaviour 기반으로 동작합니다.
 * 2. Text Popup은 Queue 구조로 순차적으로 표시됩니다.
 * 3. Popup Background는 활성 Popup 여부에 따라 자동 제어됩니다.
 * 4. 커버 팝업은 호출자별 참조 카운트로 유지되며, await 오버로드는 결과와 무관하게 finally 로 내립니다.
 * 5. 커버 타임아웃은 대기만 끊습니다. 작업까지 취소하려면 taskCts 를 넘겨야 합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HCore;
using HDiagnosis.Logger;
using HInspector;

namespace HUI.Popup {
    public abstract class PopupManager<T> : SingletonBehaviour<T> where T : PopupManager<T> {
        #region Class
        [Serializable]
        public class LogQue {
            int uid = 0;
            PopLevel level;

            string title;
            string message;

            string okText;
            string cancelText;


            public int UID => uid;
            public PopLevel Level => level;

            public string Title => title;
            public string Message => message;

            public string OkText => okText;
            public string CancelText => cancelText;

            public Action OnClickOk { get; private set; }
            public Action OnClickCancel { get; private set; }

            // 큐 진행 콜백을 합성하면 OnClickOk 는 절대 null 이 아니게 된다.
            // 호출자가 실제로 OK 를 원했는지는 이 플래그로 따로 보존한다.
            public bool HasOk { get; private set; }

            public LogQue(
                int uid, PopLevel level,
                string title, string message,
                Action onClickOk, Action onClickCancel,
                string okTxt, string cancelTxt,
                bool hasOk) {
                this.uid = uid;
                this.level = level;
                this.title = title;
                this.message = message;
                this.okText = okTxt;
                this.cancelText = cancelTxt;
                OnClickOk = onClickOk;
                OnClickCancel = onClickCancel;
                HasOk = hasOk;
            }
        }
        #endregion

        #region Member
        [HTitle("UI")]
        [SerializeField]
        protected GameObject background;

        [HTitle("Prefab")]
        [SerializeField]
        protected TextPopup textPrefab;
        [SerializeField]
        protected ImagePopup imagePrefab;
        [SerializeField]
        protected VideoPopup videoPrefab;
        [SerializeField]
        protected AwaitCoverPopup coverPrefab;

        [HTitle("Parents")]
        [SerializeField]
        protected Transform poolParent;
        [SerializeField]
        protected Transform logParent;
        [SerializeField]
        protected Transform gameParent;

        // Unity 는 Queue<T> 를 직렬화하지 않고 LogQue 는 Action 필드를 가진다 —
        // [SerializeField] 는 무효였으므로 제거한다.
        protected Queue<LogQue> logHistory = new();

        protected TextPopup textInstance = null;
        protected ImagePopup imgInstance = null;
        protected VideoPopup vidInstance = null;
        protected AwaitCoverPopup coverInstance = null;

        // 커버는 여러 비동기 작업이 겹칠 수 있어 호출자별 참조 카운트로 수명을 정한다.
        readonly Dictionary<object, int> coverCallers = new();

        protected int logCreatStack = 0;

        // 콜백 재진입으로 큐가 무한히 자라는 것을 막는 상한.
        protected const int MAX_LOG_QUEUE = 256;

        protected const float DEFAULT_COVER_TIMEOUT_SECONDS = 30f;


        // 종전: gameParent.childCount 기반. Destroy 가 프레임 종료 후에 적용되고 "닫혔지만 살아있는"
        // 자식도 계수되어, 이미지/비디오 팝업을 닫아도 배경이 영원히 남았다. 인스턴스 활성 여부로 판정한다.
        protected bool IsAllClosed =>
            logHistory.Count == 0
            && (textInstance == null || !textInstance.IsActive)
            && (imgInstance == null || !imgInstance.IsActive)
            && (vidInstance == null || !vidInstance.IsActive)
            && (coverInstance == null || !coverInstance.IsActive);
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

            // 콜백 안에서 ShowLog 를 다시 부르는 재진입에 상한이 없어 큐가 무한히 자랄 수 있었다.
            if (logHistory.Count >= MAX_LOG_QUEUE) {
                HLogger.Error($"[Popup] Log queue limit ({MAX_LOG_QUEUE}) reached. Dropping '{title}'.");
                return;
            }

            if (background != null) background.SetActive(true);
            // 큐 진행 콜백은 OK/Cancel 양쪽에 결합해야 한다 — Cancel 에만 걸려 있던 종전 코드는
            // OK 를 누르면 큐가 영구 정체되고 background 가 입력을 막았다.
            bool hasOk = onClickOk != null;
            var okWrapper = onClickOk;
            okWrapper += _SetTextPopup;
            var cancelWrapper = onClickCancel;
            cancelWrapper += _SetTextPopup;
            logHistory.Enqueue(new(uid, level, title, message, okWrapper, cancelWrapper, okTxt, cancelTxt, hasOk));

            // Create one text popup
            if (textInstance == null) {
                textInstance = Instantiate(textPrefab, logParent);
                textInstance.OnClosed += _OnPopupClosed;
                textInstance.Close();
            }

            if (!textInstance.IsActive) {
                _SetTextPopup();
            }
        }

        public void ShowImage(Sprite sprite, Action onClick = null) {
            if (sprite == null) {
                HLogger.Error("[Popup] ShowImage: sprite is null.");
                return;
            }
            ShowImage(sprite.texture, onClick);
        }

        public void ShowImage(Texture texture, Action onClick = null) {
            if (background != null) background.SetActive(true);
            _DisposeImageInstance();

            imgInstance = Instantiate(imagePrefab, gameParent);
            imgInstance.OnClosed += _OnPopupClosed;   // 닫힘을 매니저가 알아야 배경을 내릴 수 있다
            imgInstance.SetUi(texture);
            if (onClick != null) imgInstance.OnClickPanel += onClick;
        }

        public void ShowVideo(string address, Action onClick = null, int width = 0, int height = 0) {
            if (background != null) background.SetActive(true);
            _DisposeVideoInstance();

            vidInstance = Instantiate(videoPrefab, gameParent);
            vidInstance.OnClosed += _OnPopupClosed;
            vidInstance.SetVideo(address, width, height);
            if (onClick != null) vidInstance.OnClickPanel += onClick;
        }

        #region Public - Cover
        public void ShowCover(object caller, string message = null) {
            if (caller == null) {
                HLogger.Error("[Popup] ShowCover called with a null caller. Ignored.");
                return;
            }

            _EnsureCoverInstance();
            coverInstance.SetMessage(message);

            if (coverCallers.ContainsKey(caller)) {
                coverCallers[caller]++;
            }
            else {
                coverCallers[caller] = 1;
            }

            if (background != null) background.SetActive(true);
            coverInstance.Open();
        }

        public void HideCover(object caller) {
            if (!coverCallers.ContainsKey(caller)) return;

            coverCallers[caller]--;
            if (coverCallers[caller] < 1) {
                coverCallers.Remove(caller);
            }

            if (coverCallers.Count > 0) return;

            // Close 는 실제로 열려 있던 경우에만 OnClosed 를 쏘고, 그 핸들러가 배경을 갱신한다.
            if (coverInstance != null) coverInstance.Close();
            else _RefreshBackground();
        }

        /// <summary> 작업이 끝날 때까지 커버를 유지한다. 초과 시 TimeoutException 을 던진다. </summary>
        public async UniTask ShowCover(
            object caller, UniTask task,
            float timeoutSeconds = DEFAULT_COVER_TIMEOUT_SECONDS,
            string message = null,
            CancellationTokenSource taskCts = null) {
            ShowCover(caller, message);
            try {
                await task.Timeout(
                    TimeSpan.FromSeconds(timeoutSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    taskCts);
            }
            finally {
                _HideCoverSafely(caller);
            }
        }

        /// <summary> 결과를 돌려주는 작업용 오버로드. 초과 시 TimeoutException 을 던진다. </summary>
        public async UniTask<TResult> ShowCover<TResult>(
            object caller, UniTask<TResult> task,
            float timeoutSeconds = DEFAULT_COVER_TIMEOUT_SECONDS,
            string message = null,
            CancellationTokenSource taskCts = null) {
            ShowCover(caller, message);
            try {
                return await task.Timeout(
                    TimeSpan.FromSeconds(timeoutSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    taskCts);
            }
            finally {
                _HideCoverSafely(caller);
            }
        }
        #endregion

        #region Private - Cover
        private void _EnsureCoverInstance() {
            if (coverInstance != null) return;

            if (coverPrefab == null) {
                throw new InvalidOperationException(
                    "[Popup] coverPrefab is not assigned. Assign an AwaitCoverPopup prefab on the popup manager before calling ShowCover.");
            }

            coverInstance = Instantiate(coverPrefab, logParent);
            coverInstance.OnClosed += _OnPopupClosed;
            coverInstance.Close();
        }

        // finally 안에서 던져진 예외는 원본 예외를 대체한다 - 작업 실패 원인이 사라지는 것을 막는다.
        private void _HideCoverSafely(object caller) {
            try {
                HideCover(caller);
            }
            catch (Exception e) {
                HLogger.Error($"[Popup] HideCover failed while unwinding: {e}");
            }
        }

        private void _DisposeCoverInstance() {
            if (coverInstance == null) return;
            coverInstance.OnClosed -= _OnPopupClosed;
            Destroy(coverInstance.gameObject);
            coverInstance = null;
        }
        #endregion

        // 싱글톤 파괴 시 자식 팝업 인스턴스와 큐에 남은 외부 Action 참조를 모두 끊는다.
        protected override void OnDestroy() {
            _DisposeImageInstance();
            _DisposeVideoInstance();
            _DisposeCoverInstance();
            coverCallers.Clear();

            if (textInstance != null) {
                textInstance.OnClosed -= _OnPopupClosed;
                Destroy(textInstance.gameObject);
                textInstance = null;
            }

            logHistory.Clear();
            base.OnDestroy();
        }

        private void _DisposeImageInstance() {
            if (imgInstance == null) return;
            imgInstance.OnClosed -= _OnPopupClosed;
            Destroy(imgInstance.gameObject);
            imgInstance = null;
        }

        private void _DisposeVideoInstance() {
            if (vidInstance == null) return;
            vidInstance.OnClosed -= _OnPopupClosed;
            Destroy(vidInstance.gameObject);
            vidInstance = null;
        }


        // 배경 해제가 _SetTextPopup 안에만 있어서, 이미지/비디오만 사용한 흐름에서는
        // 해제 코드가 실행조차 되지 않았다. 모든 닫힘 경로가 이 함수를 거치게 한다.
        protected void _RefreshBackground() {
            if (background == null) return;
            background.SetActive(!IsAllClosed);
        }

        private void _OnPopupClosed(BasePopupUi popup) => _RefreshBackground();

        private void _SetTextPopup() {
            if (logHistory.Count == 0) {
                if (textInstance != null) textInstance.Close();
                _RefreshBackground();
                return;
            }

            LogQue log = logHistory.Dequeue();
            textInstance.SetText(
                log.Title, log.Message,
                log.OnClickOk, log.OnClickCancel,
                log.OkText, log.CancelText,
                log.HasOk);
            textInstance.Open();
        }

#if UNITY_EDITOR
        protected bool _IsPlaying => Application.isPlaying;
        int _testId = 0;

        [HButton("Test Text Popup")]
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
 * 3. 비동기 대기 구간을 덮는 AwaitCover Popup을 참조 카운트로 관리합니다.
 * 4. Popup Background 활성 상태를 자동 관리합니다.
 *
 * 사용법 ::
 * 1. ShowLog()를 호출하여 Text Popup 메시지를 표시합니다.
 * 2. ShowImage() 또는 ShowVideo()를 통해 미디어 Popup을 생성합니다.
 * 3. await ShowCover(this, task, timeoutSeconds, message) 로 비동기 구간을 덮습니다.
 *
 * 기타 ::
 * 1. Popup 로그는 Queue<LogQue> 구조로 관리됩니다.
 * 2. TextPopup / AwaitCoverPopup 인스턴스는 최초 1회 생성 후 재사용됩니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.16 AwaitCover 팝업 API 추가
 *
 * # 추가
 * - coverPrefab / coverInstance / coverCallers 와 DEFAULT_COVER_TIMEOUT_SECONDS(30초).
 * - ShowCover(caller, message) / HideCover(caller) - 호출자별 참조 카운트.
 * - ShowCover(caller, UniTask, timeoutSeconds, message, taskCts) 와 결과 반환 오버로드. UniTask.Timeout 으로 초과 시 TimeoutException.
 * - IsAllClosed 에 커버 항 추가. OnDestroy 에서 인스턴스 파기 + 카운트 정리.
 *
 * # 설계 결정
 * - 커버 인스턴스는 TextPopup 처럼 1개만 만들어 재사용한다. 동시에 두 장을 겹칠 이유가 없고 참조 카운트가 수명을 정한다.
 * - await 오버로드는 try / finally 로 감싸 성공·실패·예외·취소·타임아웃 모든 경로에서 커버를 내린다. finally 의 예외가 원본을 덮지 않게 _HideCoverSafely 를 거친다.
 * - 타임아웃 계측은 DelayType.UnscaledDeltaTime 이다. 로딩 중 timeScale 이 0 이어도 흘러야 한다.
 * - coverPrefab 미배선은 HLogger 경고가 아니라 InvalidOperationException 이다. 커버가 안 뜨면 차단 자체가 실패하므로 조용히 넘기지 않는다.
 *
 * # 주의
 * - 타임아웃은 대기만 끊는다. 작업을 멈추려면 호출자가 taskCts 를 넘겨야 한다.
 *
 * =============================================================================
 */
#endif
