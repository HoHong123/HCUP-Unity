#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.3.0 다이얼로그 시스템 씬 단위 싱글톤 매니저.
 *
 * 특징 / 지원기능 ::
 * + HCore.SingletonBehaviour 기반 씬 종속 싱글톤 (dontDestroyOnLoad = false 기본)
 * + Canvas / 컨트롤러 참조 보유. Awake에서 Bind · 이벤트 wiring 일괄 수행.
 * + PlayCatalog(catalog) / PlayDefault() / PlayByKey(key) / Stop() 공개 API.
 * + catalogMap(HDictionary<string, DialogueCatalogSO>) — 키로 카탈로그 선택 재생.
 * + OnCatalogStart / OnCatalogExit 이벤트 — 외부 게임 코드 연결 전용.
 * + 에디터 전용 로컬리제이션 소스 토글 :
 *   Manager(기본) = HTextLocalizer.GetText 유지 (LocalizationManager 경유).
 *   PerCatalog    = catalog.editorLocalizationSO 직조회, miss 시 UID 리터럴.
 *
 * 주의사항 ::
 * PerCatalog 모드: HTextLocalizer.GetText를 PlayCatalog 진입 시 snapshot → OnCatalogExit/Stop 시 복원.
 * 플레이어 빌드는 항상 LocalizationManager 단일 경로 (에디터 전용 토글 컴파일 제외).
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using HCollection;
using HDiagnosis.Logger;
using HInspector;
using HUI.TextUI;
using HCore;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueManager : SingletonBehaviour<DialogueManager> {
        #region Type
#if UNITY_EDITOR
        public enum LocalizationSourceMode {
            Manager,
            PerCatalog
        }
#endif
        #endregion

        #region Fields
        [HTitle("Canvas")]
        [SerializeField]
        Canvas dialogueCanvas;

        [HTitle("Controllers")]
        [SerializeField]
        DialogueDirector director;
        [SerializeField]
        CharacterStageDirector stageDirector;
        [SerializeField]
        DialogueTextController textController;
        [SerializeField]
        DialogueUiController uiController;

        [HTitle("Stage Data")]
        [SerializeField]
        CharacterRegistrySO registry;
        [SerializeField]
        StageLayoutSO layout;

        [HTitle("Catalogs")]
        [SerializeField]
        DialogueCatalogSO targetCatalog;
        [SerializeField]
        HDictionary<string, DialogueCatalogSO> catalogMap = new();

#if UNITY_EDITOR
        [HTitle("Localization (Editor Only)")]
        [SerializeField]
        LocalizationSourceMode editorLocalizationSource = LocalizationSourceMode.Manager;
#endif

        DialogueCatalogSO currentCatalog;
        readonly MemoryDialogueVariableContext variableContext = new MemoryDialogueVariableContext();
        #endregion

        #region Events
        public event Action<DialogueCatalogSO> OnCatalogStart;
        public event Action<DialogueCatalogSO, string> OnCatalogExit;
        #endregion

        #region Getter
        public Canvas DialogueCanvas => dialogueCanvas;
        public DialogueDirector Director => director;
        public DialogueTextController TextController => textController;
        public CharacterStageDirector StageDirector => stageDirector;
        public DialogueUiController UiController => uiController;
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            base.Awake();
            if (instance != this) return;
            if (!_ValidateRefs()) return;
            _Bind();
            _SubscribeUiEvents();
            _SubscribeDirectorEvents();
        }

        protected override void OnDestroy() {
            _UnsubscribeUiEvents();
            _UnsubscribeDirectorEvents();
#if UNITY_EDITOR
            _RestoreLocalizationOverride();
#endif
            base.OnDestroy();
        }
        #endregion

        #region Public API
        public void PlayCatalog(DialogueCatalogSO catalog) {
            if (catalog == null) {
                HLogger.Error("[DialogueManager] PlayCatalog: catalog is null.");
                return;
            }
            currentCatalog = catalog;
#if UNITY_EDITOR
            _ApplyLocalizationOverride(catalog);
#endif
            director.PlayCatalog(catalog);
        }

        public void PlayDefault() {
            DialogueCatalogSO target = targetCatalog;
            if (target == null) {
                foreach (DialogueCatalogSO cat in catalogMap.Values) { target = cat; break; }
            }
            if (target == null) {
                HLogger.Error("[DialogueManager] PlayDefault: targetCatalog is not assigned and catalogMap is empty.");
                return;
            }
            PlayCatalog(target);
        }

        public bool PlayByKey(string key) {
            if (string.IsNullOrEmpty(key)) {
                HLogger.Error("[DialogueManager] PlayByKey: key is null or empty.");
                return false;
            }
            if (!catalogMap.TryGetValue(key, out DialogueCatalogSO catalog) || catalog == null) {
                HLogger.Error($"[DialogueManager] PlayByKey: key '{key}' not found in catalogMap.");
                return false;
            }
            PlayCatalog(catalog);
            return true;
        }

        public void Stop() {
            director.Stop();
#if UNITY_EDITOR
            _RestoreLocalizationOverride();
#endif
        }
        #endregion

        #region Private — 초기화
        private bool _ValidateRefs() {
            if (director == null) {
                HLogger.Error("[DialogueManager] director is not assigned.");
                return false;
            }
            if (textController == null) {
                HLogger.Error("[DialogueManager] textController is not assigned.");
                return false;
            }
            if (uiController == null) {
                HLogger.Error("[DialogueManager] uiController is not assigned.");
                return false;
            }
            return true;
        }

        private void _Bind() {
            if (stageDirector != null && registry != null && layout != null)
                stageDirector.Bind(registry, layout, textController);
            director.Bind(textController, variableContext);
        }

        private void _SubscribeUiEvents() {
            uiController.OnPlay += _OnUiPlay;
            uiController.OnSkip += _OnUiSkip;
            uiController.OnAdvance += _OnUiAdvance;
            uiController.OnSelectChoice += _OnUiSelectChoice;
        }

        private void _UnsubscribeUiEvents() {
            if (uiController == null) return;
            uiController.OnPlay -= _OnUiPlay;
            uiController.OnSkip -= _OnUiSkip;
            uiController.OnAdvance -= _OnUiAdvance;
            uiController.OnSelectChoice -= _OnUiSelectChoice;
        }

        private void _SubscribeDirectorEvents() {
            director.OnCatalogStart += _OnDirectorCatalogStart;
            director.OnCatalogExit += _OnDirectorCatalogExit;
            director.OnLineEnter += _OnDirectorLineEnter;
            director.OnChoicePresent += _OnDirectorChoicePresent;
            textController.OnLineComplete += _OnLineComplete;
        }

        private void _UnsubscribeDirectorEvents() {
            if (director != null) {
                director.OnCatalogStart -= _OnDirectorCatalogStart;
                director.OnCatalogExit -= _OnDirectorCatalogExit;
                director.OnLineEnter -= _OnDirectorLineEnter;
                director.OnChoicePresent -= _OnDirectorChoicePresent;
            }
            if (textController != null) textController.OnLineComplete -= _OnLineComplete;
        }
        #endregion

        #region Private — 이벤트 핸들러 (UI)
        private void _OnUiPlay() {
            if (currentCatalog != null) PlayCatalog(currentCatalog);
        }

        private void _OnUiSkip() {
            textController.SkipToEnd();
        }

        private void _OnUiAdvance() {
            switch (textController.State) {
                case TextDisplayState.Typing:
                case TextDisplayState.Paused:
                    textController.SkipToEnd();
                    break;
                case TextDisplayState.Waiting:
                case TextDisplayState.Skipped:
                    uiController.ShowAdvanceHint(false);
                    textController.RequestAdvance();
                    break;
            }
        }

        private void _OnUiSelectChoice(string key) {
            director.SelectChoice(key);
            uiController.HideChoices();
        }
        #endregion

        #region Private — 이벤트 핸들러 (Director)
        private void _OnDirectorCatalogStart(DialogueCatalogSO catalog) {
            OnCatalogStart?.Invoke(catalog);
        }

        private void _OnDirectorLineEnter(DialogueLineNode node) {
            string speakerKey = string.IsNullOrEmpty(node.SpeakerKey) ? string.Empty : node.SpeakerKey;
            uiController.ShowSpeakerName(speakerKey);
            uiController.ShowAdvanceHint(false);
        }

        private void _OnDirectorChoicePresent(DialogueChoiceNode node, IReadOnlyList<DialogueChoiceNode.ChoiceData> validChoices) {
            uiController.ShowChoices(validChoices);
        }

        private void _OnDirectorCatalogExit(DialogueCatalogSO catalog, string exitKey) {
#if UNITY_EDITOR
            _RestoreLocalizationOverride();
            Debug.Log($"[DEBUG] OnCatalogExit: {catalog?.name}/{exitKey}");
#endif
            uiController.ShowSpeakerName(string.Empty);
            uiController.ShowAdvanceHint(false);
            uiController.HideChoices();
            OnCatalogExit?.Invoke(catalog, exitKey);
        }

        private void _OnLineComplete() {
            uiController.ShowAdvanceHint(true);
        }
        #endregion

#if UNITY_EDITOR
        #region Private — 로컬리제이션 오버라이드 (Editor Only)
        bool overrideActive;
        Func<string, string> previousGetText;
        DialogueCatalogSO activeCatalogForLoc;

        private void _ApplyLocalizationOverride(DialogueCatalogSO catalog) {
            if (editorLocalizationSource != LocalizationSourceMode.PerCatalog) return;
            if (overrideActive) return;
            previousGetText = HTextLocalizer.GetText;
            activeCatalogForLoc = catalog;
            HTextLocalizer.GetText = _PerCatalogResolve;
            overrideActive = true;
        }

        private void _RestoreLocalizationOverride() {
            if (!overrideActive) return;
            HTextLocalizer.GetText = previousGetText;
            previousGetText = null;
            activeCatalogForLoc = null;
            overrideActive = false;
        }

        private string _PerCatalogResolve(string uid) {
            if (activeCatalogForLoc != null && activeCatalogForLoc.EditorTryGetLocalizedText(uid, out string text))
                return text;
            return uid;
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: catalogMap + PlayDefault / PlayByKey API 추가
 *
 * # 변경
 * - using HCollection 추가. HCUP.HDialogue.asmdef에 HCUP.HCollection 참조 추가.
 * - [HTitle("Catalogs")] 필드 그룹 추가:
 *   defaultCatalog(DialogueCatalogSO) — 기본 단일 카탈로그.
 *   catalogMap(HDictionary<string, DialogueCatalogSO>) — 키 기반 다중 카탈로그.
 * - PlayDefault(): defaultCatalog를 PlayCatalog로 위임.
 * - PlayByKey(string key) → bool: catalogMap 조회 후 PlayCatalog 위임. 미존재 키 false 반환.
 * - 헤더 주의사항에서 DialogueTestSceneManager 언급 제거.
 *
 * # 이유
 * - 외부 게임 코드에서 카탈로그 SO 참조 없이 키 문자열만으로 대화 재생 가능.
 * - Inspector에서 사용할 카탈로그 세트를 HDictionary로 시각적으로 관리.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: _OnDirectorCatalogExit — UNITY_EDITOR 로그 추가
 *
 * # 변경
 * - _OnDirectorCatalogExit: `#if UNITY_EDITOR` 블록에 Debug.Log(`[DEBUG] OnCatalogExit: {catalog}/{exitKey}`) 추가.
 *
 * # 이유
 * - MCP 테스트 환경에서 OnCatalogExit 발화 여부를 콘솔로 직접 확인하기 위해.
 * - 플레이어 빌드에서는 `#if UNITY_EDITOR` 가드로 컴파일 제외.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (최초 설계) :: DialogueManager 생성
 *
 * # 변경
 * - HCore.SingletonBehaviour<DialogueManager> 상속. 씬 종속 기본값 (dontDestroyOnLoad = false).
 * - Canvas / Director / StageDirector / TextController / UiController / Registry / Layout SerializeField.
 * - Awake: instance != this 중복 가드 → _ValidateRefs → _Bind → Subscribe.
 * - _Bind: stageDirector.Bind(registry, layout, textController) + director.Bind(textController, variableContext).
 * - PlayCatalog / Stop 공개 API. Director 위임 + 에디터 로컬리제이션 save/restore.
 * - OnCatalogStart / OnCatalogExit 공개 이벤트 — 외부 게임 코드 연결용.
 * - 에디터 전용 LocalizationSourceMode 토글. PerCatalog 선택 시 HTextLocalizer.GetText 일시 오버라이드.
 *   PlayCatalog 진입 시 snapshot → OnCatalogExit/Stop/OnDestroy 시 복원.
 * - DialogueTestSceneManager._SetupDirectorMode 이벤트 wiring 흡수.
 *
 * # 이유
 * - 씬 단위 다이얼로그 시스템 owner 부재 해소. 재사용 씬마다 wiring 코드 복사 제거.
 * - 에디터 미리보기에서 카탈로그별 LocalizationSO 검수 워크플로우 추가.
 *
 * =============================================================================
 */
#endif
