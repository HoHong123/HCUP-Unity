#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.3.0 다이얼로그 UI 컨트롤러 (프로덕션 버전).
 *
 * 특징 / 지원기능 ::
 * + 화자명 / 대사 텍스트 / Advance 힌트 표시
 * + Play / Skip / Advance 버튼 이벤트 발화
 * + Auto 토글 : Toggle.isOn ↔ OnAutoToggle(bool) 이벤트. SetAutoToggle()로 외부 갱신.
 * + 선택지 패널 : ShowChoices / HideChoices + OnSelectChoice(key) 이벤트
 *
 * 주의사항 ::
 * UI 이벤트 발화만 담당. 비즈니스 로직은 DialogueManager.
 * choiceButtons / choiceButtonTexts 배열 길이는 동일하게 설정할 것.
 * 키보드 입력은 DialogueInputController 담당 (InputSystem 연결 없음).
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HInspector;

namespace HDialogue {
    public sealed class DialogueUiController : MonoBehaviour {
        #region Fields
        [HTitle("Dialogue Box")]
        [SerializeField]
        TMP_Text speakerNameText;
        [SerializeField]
        TMP_Text dialogueContentText;
        [SerializeField]
        TMP_Text advanceHintText;

        [HTitle("Controls")]
        [SerializeField]
        Button playBtn;
        [SerializeField]
        Button skipBtn;
        [SerializeField]
        Toggle autoTg;
        [SerializeField]
        Button nextLineBtn;

        [HTitle("Choice Panel")]
        [SerializeField]
        GameObject choicePanel;
        [SerializeField]
        Button[] choiceButtons;
        [SerializeField]
        TMP_Text[] choiceButtonTexts;

        string[] choiceKeys = Array.Empty<string>();
        #endregion

        #region Events
        public event Action OnPlay;
        public event Action OnSkip;
        public event Action<bool> OnAutoToggle;
        public event Action OnAdvance;
        public event Action<string> OnSelectChoice;
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            playBtn?.onClick.AddListener(() => OnPlay?.Invoke());
            skipBtn?.onClick.AddListener(() => OnSkip?.Invoke());
            autoTg?.onValueChanged.AddListener(isOn => OnAutoToggle?.Invoke(isOn));
            nextLineBtn?.onClick.AddListener(() => OnAdvance?.Invoke());

            if (choicePanel != null) choicePanel.SetActive(false);
            for (int k = 0; k < choiceButtons.Length; k++) {
                int capturedK = k;
                choiceButtons[k]?.onClick.AddListener(() => _OnChoiceButtonClick(capturedK));
            }
        }
        #endregion

        #region Public — 디스플레이 갱신
        public void ShowSpeakerName(string name) {
            if (speakerNameText != null) speakerNameText.text = name;
        }

        public void ShowDialogueContent(string content) {
            if (dialogueContentText != null) dialogueContentText.text = content;
        }

        public void ShowAdvanceHint(bool visible) {
            if (advanceHintText != null) advanceHintText.gameObject.SetActive(visible);
        }

        public void SetPlayButtonInteractable(bool value) {
            if (playBtn != null) playBtn.interactable = value;
        }

        public void SetAutoToggle(bool isOn) {
            autoTg?.SetIsOnWithoutNotify(isOn);
        }
        #endregion

        #region Public — 선택지 패널
        public void ShowChoices(IReadOnlyList<DialogueChoiceNode.ChoiceData> choices) {
            choiceKeys = new string[choices.Count];
            for (int k = 0; k < choices.Count; k++) choiceKeys[k] = choices[k].Key;

            if (choicePanel != null) choicePanel.SetActive(true);
            for (int k = 0; k < choiceButtons.Length; k++) {
                bool show = k < choices.Count;
                if (choiceButtons[k] != null) choiceButtons[k].gameObject.SetActive(show);
                if (show && k < choiceButtonTexts.Length && choiceButtonTexts[k] != null)
                    choiceButtonTexts[k].text = choices[k].DisplayText;
            }
        }

        public void HideChoices() {
            if (choicePanel != null) choicePanel.SetActive(false);
        }
        #endregion

        #region Public — 텍스트 컴포넌트 접근
        public TMP_Text DialogueContentText => dialogueContentText;
        #endregion

        #region Private
        private void _OnChoiceButtonClick(int index) {
            if (index >= choiceKeys.Length) return;
            OnSelectChoice?.Invoke(choiceKeys[index]);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: autoBtn → autoTg(Toggle) 교체 — Auto On/Off 상태 반영
 *
 * # 변경
 * - `[SerializeField] Button autoBtn` → `[SerializeField] Toggle autoTg`.
 * - `public event Action OnAutoToggle` → `public event Action<bool> OnAutoToggle`.
 * - Awake: `onClick` → `onValueChanged.AddListener(isOn => OnAutoToggle?.Invoke(isOn))`.
 * - `public void SetAutoToggle(bool isOn)`: Toggle.SetIsOnWithoutNotify — 이벤트 재발화 없이 UI 갱신.
 * - 헤더 특징에 Toggle 동작 방식 안내 추가.
 *
 * # 이유
 * - Button은 클릭 시 단순 이벤트만 발화. Toggle은 isOn 상태를 시각적으로 보존해 Auto 모드 활성 여부를 표시.
 * - SetIsOnWithoutNotify: Manager가 키보드 입력으로 toggle 상태 갱신 시 onValueChanged 순환 호출 방지.
 * - Action<bool>: Manager가 isOn 값을 직접 수신해 별도의 상태 추론 없이 AutoAdvanceDelay 설정 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: autoBtn SerializeField + OnAutoToggle 이벤트 추가
 *
 * # 변경
 * - `[SerializeField] Button autoBtn` 추가 (Controls 그룹, skipBtn 아래).
 * - `public event Action OnAutoToggle` 추가.
 * - Awake: `autoBtn?.onClick.AddListener(() => OnAutoToggle?.Invoke())` 추가.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 2-C — Auto 모드 토글 버튼 연결 지원.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (최초 설계) :: DialogueUiController 생성 — 프로덕션 UI 컨트롤러
 *
 * # 변경
 * - DialogueTestUiController(SWU.Dialogue)에서 디버그 표면 제거 후 HDialogue 네임스페이스로 재정의.
 *   제거 항목: stateText / tokenDebugText / lineIndexText / ShowState / ShowTokenDebug / ShowLineIndex.
 * - Update() 키보드 입력 제거 — DialogueInputController(P2 예정)로 책임 이관.
 * - [Header] → [HTitle] 마이그레이션.
 *
 * # 이유
 * - DialogueManager 도입 전제 컨트롤러. 테스트씬 전용 디버그 UI와 프로덕션 UI 경계 분리.
 *
 * =============================================================================
 */
#endif
