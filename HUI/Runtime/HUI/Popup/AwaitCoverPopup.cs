#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 비동기 작업이 끝날 때까지 화면을 덮고 안내 문구를 보여주는 팝업입니다.
 * 결제 승인, 데이터 로드처럼 결과가 나오기 전 조작을 막아야 하는 구간에 씁니다.
 *
 * 특징 / 지원기능 ::
 * + 닫기 버튼이 없습니다. 열고 닫는 주체는 PopupManager 의 ShowCover / HideCover 뿐입니다.
 * + SetMessage 로 대기 안내 문구를 갱신합니다.
 *
 * 주의사항 ::
 * 1. base.Start 를 호출하지 않습니다. closeBtn 슬롯은 비워 둡니다.
 * 2. 입력 차단은 프리팹 책임입니다. panel 에 화면을 덮는 Raycast Target 그래픽이 있어야 합니다.
 * 3. 타임아웃 판단은 이 팝업이 하지 않습니다. PopupManager 가 합니다.
 *
 * 사용법 ::
 * PopupManager<T> 의 coverPrefab 슬롯에 이 컴포넌트를 가진 프리팹을 배선합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using TMPro;
using HInspector;

namespace HUI.Popup {
    public class AwaitCoverPopup : BasePopupUi {
        #region Fields
        [HTitle("Texts")]
        [SerializeField]
        TMP_Text messageTxt;
        #endregion

        #region Protected - Unity Life Cycle
        // base.Start 는 closeBtn 에 null 검사 없이 리스너를 붙이고 OnClickCancel 에 Close 를 건다.
        // 커버 팝업은 사용자가 닫을 수 없어야 하므로 두 배선 모두 두지 않는다.
        protected override void Start() { }
        #endregion

        #region Public - UI
        public void SetMessage(string message) {
            if (messageTxt == null || message == null) return;
            messageTxt.text = message;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.16 AwaitCoverPopup 베이스 코드 생성
 *
 * # 목적
 * - 비동기 작업 구간에 화면을 덮고 안내 문구만 보여주는 팝업. 시작 시 열리고 결과와 무관하게 닫힌다.
 *
 * # 사용 흐름
 * - PopupManager.ShowCover 가 인스턴스를 만들어 Open / SetMessage 를 호출하고, HideCover 가 Close 한다.
 *
 * # 설계 결정
 * - base.Start 를 호출하지 않아 닫기 버튼 배선을 없앤다. 사용자가 커버를 스스로 닫으면 차단 구간이 무너진다.
 * - 타임아웃과 참조 카운팅은 매니저에 둔다. 이 팝업은 문구 갱신과 열기/닫기만 하는 UI 층이다.
 * - 문구 텍스트가 비어 있거나 인자가 null 이면 기존 문구를 유지한다. 후행 호출이 선행 안내를 지우지 않는 SpinnerManager 규약과 같다.
 *
 * =============================================================================
 */
#endif
