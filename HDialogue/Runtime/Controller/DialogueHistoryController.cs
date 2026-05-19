#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.6.0 대화 이력 컨트롤러.
 *
 * 특징 / 지원기능 ::
 * + Bind(DialogueDirector) / Unbind() — 디렉터 이벤트 구독·해제
 * + OnLineEnter → entries 누적 + 히스토리 패널 자동 닫기
 * + OnCatalogStart → 이력 클리어 + 패널 닫기 (카탈로그 단위 리셋)
 * + ShowHistory() / HideHistory() / ToggleHistory() — 패널 표시 제어
 * + maxEntries 초과 시 가장 오래된 항목 자동 제거 (FIFO)
 * + 단일 TMP_Text 레이지 렌더링 — 패널 오픈 시 한 번만 text 재조합
 *
 * 주의사항 ::
 * historyText SerializeField 필수. null이면 표시 안 됨.
 * historyScrollRect는 Pan_History의 ScrollRect 참조.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HInspector;
using HUI.TextUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HDialogue {
    public sealed class DialogueHistoryController : MonoBehaviour {
        #region Fields
        [HTitle("Panel")]
        [SerializeField]
        GameObject historyPanel;
        [SerializeField]
        ScrollRect historyScrollRect;
        [SerializeField]
        TMP_Text historyText;

        [HTitle("Entry")]
        [SerializeField]
        int maxEntries = 50;

        DialogueDirector director;
        readonly List<string> entries = new();
        #endregion

        #region Unity Life Cycle
        private void OnDestroy() {
            Unbind();
        }
        #endregion

        #region Public
        public void Bind(DialogueDirector target) {
            if (director != null) Unbind();
            director = target;
            director.OnLineEnter += _OnLineEnter;
            director.OnCatalogStart += _OnCatalogStart;
        }

        public void Unbind() {
            if (director == null) return;
            director.OnLineEnter -= _OnLineEnter;
            director.OnCatalogStart -= _OnCatalogStart;
            director = null;
        }

        public void ShowHistory() {
            if (historyPanel == null) return;
            if (historyText != null) historyText.text = string.Join("\n", entries);
            historyPanel.SetActive(true);
            _ScrollToBottom();
        }

        public void HideHistory() {
            if (historyPanel != null) historyPanel.SetActive(false);
        }

        public void ToggleHistory() {
            if (historyPanel == null) return;
            if (historyPanel.activeSelf) HideHistory();
            else ShowHistory();
        }
        #endregion

        #region Private
        private void _OnLineEnter(DialogueLineNode node) {
            HideHistory();
            string speaker = node.SpeakerKey;
            string text = HTextLocalizer.GetText?.Invoke(node.LocalizationUID) ?? node.LocalizationUID;
            _AddEntry(speaker, text);
        }

        private void _OnCatalogStart(DialogueCatalogSO catalog) {
            _ClearHistory();
            HideHistory();
        }

        private void _AddEntry(string speaker, string text) {
            string line = string.IsNullOrEmpty(speaker) ? text : $"[{speaker}] {text}";
            if (entries.Count >= maxEntries) entries.RemoveAt(0);
            entries.Add(line);
        }

        private void _ClearHistory() {
            entries.Clear();
            if (historyText != null) historyText.text = string.Empty;
        }

        private void _ScrollToBottom() {
            if (historyScrollRect == null || historyText == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyText.rectTransform);
            historyScrollRect.verticalNormalizedPosition = 0f;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: 단일 TMP_Text 레이지 렌더링으로 리팩토링 — HCUP-2.6.0
 *
 * # 변경
 * - `entryPrefab`, `entryContainer` 제거 → `historyText` (TMP_Text) 단일 필드로 교체.
 * - `List<TMP_Text> entryViews` → `List<string> entries` 교체.
 * - `_AddEntry`: Instantiate 제거, string 누적 전용으로 단순화.
 * - `ShowHistory`: string.Join으로 전체 text 재조합 후 SetActive + ScrollToBottom.
 * - `_OnLineEnter`: HideHistory() 선행 호출 추가.
 * - `_ClearHistory`: entries.Clear() + historyText.text = "".
 * - `_ScrollToBottom`: historyText.rectTransform 대상으로 ForceRebuildLayoutImmediate.
 *
 * # 이유
 * - 기존 구현: 패널 열림 여부 무관하게 라인마다 TMP_Text Instantiate → GC 압박.
 * - 단일 TMP_Text + 레이지 렌더링: 패널 닫힌 동안 string 누적만 수행,
 *   ShowHistory() 호출 시 한 번에 text 갱신 → Instantiate/Destroy 0회.
 * - 대화진행·오토진행·스킵 모두 OnLineEnter로 수렴 → HideHistory() 단일 진입점.
 * - 히스토리 열린 채 대화진행 시 패널 닫힘 → 사용자 제약 준수.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (최초 설계) :: DialogueHistoryController 생성 — HCUP-2.6.0 Phase 1
 *
 * # 변경
 * - `DialogueHistoryController : MonoBehaviour` 신설.
 * - Bind/Unbind: DialogueAudioController 동일 패턴. OnDestroy Unbind 리스너 누수 방지.
 * - 구독 2종: OnLineEnter(DialogueLineNode) + OnCatalogStart(DialogueCatalogSO).
 *
 * # 이유
 * - OnCatalogStart에서 클리어 + HideHistory: 새 카탈로그 시작 시 이전 이력 노출 방지.
 * - HTextLocalizer.GetText?.Invoke(uid) ?? uid: DialogueDirector._BuildLine과 동일 null-safe 패턴.
 *
 * =============================================================================
 */
#endif
