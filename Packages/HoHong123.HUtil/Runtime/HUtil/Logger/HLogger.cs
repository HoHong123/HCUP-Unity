#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 일정 개수의 로그를 저장하는 커스텀 로그 시스템입니다.
 * 
 * ** 사용법 **
 * 1. 각 전역 함수를 호출하여 원하는 레벨의 메시지를 출력합니다.
 * 2. 필요에 따라 게임오브젝트를 전달하여 로그 클릭시 해당 오브젝트를 하이라이트할 수 있습니다.
 * Ps. 필요에 따라 서버에 현재 로그스택을 전달하도록 할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;


namespace HUtil.Logger {
    public class HLogger : MonoBehaviour {
        const int MAX_QUE_SIZE = 1000;

        readonly static Queue<string> logQue = new();
        readonly static Queue<string> warningQue = new();
        readonly static Queue<string> errorQue = new();
        readonly static Queue<string> fatalQue = new();

        static string utcNow => DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");


        public static void Log(string message, GameObject target = null, bool popupActivate = false) {
            string log = $"@1 [{utcNow}] {message}";

#if UNITY_EDITOR
            if (target == null) {
                Debug.Log(log);
            }
            else {
                Debug.Log(log, target);
            }
#endif
            if (popupActivate) {
                // TODO :: Connect with local PopupManager
                //PopupManager.Instance.AddAlert("Log", message);
            }

            logQue.Enqueue(log);
            if (logQue.Count > MAX_QUE_SIZE)
                logQue.Dequeue();
        }

        public static void Warning(string message, GameObject target = null, bool popupActivate = false) {
            string log = $"@2 [{utcNow}] {message}";

#if UNITY_EDITOR
            if (target == null) {
                Debug.LogWarning(log);
            }
            else {
                Debug.LogWarning(log, target);
            }
#endif
            if (popupActivate) {
                // TODO :: Connect with local PopupManager
                //PopupManager.Instance.AddAlert("Warning", message);
            }

            warningQue.Enqueue(message);
            if (warningQue.Count > MAX_QUE_SIZE)
                warningQue.Dequeue();
        }

        public static void Error(string message, GameObject target = null, bool showPopup = false, string debug = "") {
            string log =
                $"@3 [{utcNow}] {message}\n" +
                $"@3 Debug :: {debug}";

#if UNITY_EDITOR
            if (target == null) {
                Debug.LogError(log);
            }
            else {
                Debug.LogError(log, target);
            }
#endif

            if (showPopup) {
                // TODO :: Connect with local PopupManager
                //PopupManager.Instance.AddAlert("Error", message);
            }

            errorQue.Enqueue(message);
            if (errorQue.Count > MAX_QUE_SIZE)
                errorQue.Dequeue();

            // TODO :: Decide what to do with log stack
            // ...
        }

        public static void Exception(Exception ex, string extra = "") {
            string extraInfo = string.IsNullOrEmpty(extra) ? "" : $"{extra} ";
            string log =
                $"@4 [{utcNow}] {ex.Message}\n" +
                $"{extraInfo}";

#if UNITY_EDITOR
            Debug.LogError(log);
#endif

            fatalQue.Enqueue(log);
        }

        public static Exception Throw(Exception ex, string extra = "") {
            Exception(ex, extra);
            throw ex;
        }
    }
}
