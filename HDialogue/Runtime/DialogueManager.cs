#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.3.0 다이얼로그 시스템 씬 단위 싱글톤 매니저.
 *
 * 특징 / 지원기능 ::
 * + HCore.SingletonBehaviour 기반 씬 종속 싱글톤 (dontDestroyOnLoad = false 기본)
 * + Canvas / 컨트롤러 참조 보유. Awake에서 Bind · 이벤트 wiring 일괄 수행.
 * + audioController(선택) — Awake Bind(director)로 BGM·sfx.* SFX 이벤트 자동 처리.
 * + inputController(선택) — Advance·Skip·AutoToggle·HistoryToggle 키 이벤트 구독·해제 (Input System 연결).
 * + historyController(선택) — OnLineEnter 이력 누적 + H키 ToggleHistory() 위임.
 * + PlayCatalog(catalog) / PlayDefault() / PlayByKey(key) / Stop() 공개 API.
 * + catalogMap(HDictionary<string, DialogueCatalogSO>) — 키로 카탈로그 선택 재생.
 * + PlayCatalog 진입 시 catalog.Registry/Layout으로 stageDirector 재바인드 (null 시 씬 기본값 폴백).
 * + OnCatalogStart / OnCatalogExit 이벤트 — 외부 게임 코드 연결 전용.
 * + 에디터 전용 로컬리제이션 소스 토글 :
 *   Manager(기본) = HTextLocalizer.GetText 유지 (LocalizationManager 경유).
 *   PerCatalog    = catalog.editorLocalizationSO 직조회, miss 시 UID 리터럴.
 *
 * 주의사항 ::
 * stageDirector는 Awake에서 씬 기본값으로 초기 바인드, PlayCatalog 시 카탈로그 전용값으로 재바인드.
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
        [SerializeField]
        DialogueAudioController audioController;
        [SerializeField]
        DialogueInputController inputController;
        [SerializeField]
        DialogueHistoryController historyController;

        [HTitle("Stage Data")]
        [SerializeField]
        CharacterRegistrySO defaultRegistry;
        [SerializeField]
        StageLayoutSO defaultLayout;

        [HTitle("Catalogs")]
        [SerializeField]
        DialogueCatalogSO targetCatalog;
        [SerializeField]
        HDictionary<string, DialogueCatalogSO> catalogMap = new();

        [HTitle("Auto Mode")]
        [SerializeField]
        float autoModeDelay = 1.5f;

#if UNITY_EDITOR
        [HTitle("Localization (Editor Only)")]
        [SerializeField]
        LocalizationSourceMode editorLocalizationSource = LocalizationSourceMode.Manager;
#endif

        DialogueCatalogSO currentCatalog;
        CharacterRegistrySO activeRegistry;
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
        public bool IsSkipping {
            get => director.IsSkipping;
            set => director.IsSkipping = value;
        }
        public float AutoAdvanceDelay {
            get => director.AutoAdvanceDelay;
            set => director.AutoAdvanceDelay = value;
        }
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            base.Awake();
            if (instance != this) return;
            if (!_ValidateRefs()) return;
            _Bind();
            _SubscribeUiEvents();
            _SubscribeInputEvents();
            _SubscribeDirectorEvents();
        }

        protected override void OnDestroy() {
            _UnsubscribeInputEvents();
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
            _RebindStageDirector(catalog);
            director.PlayCatalog(catalog);
        }

        public void PlayDefault() {
            DialogueCatalogSO target = targetCatalog;
            if (target == null) {
                foreach (DialogueCatalogSO cat in catalogMap.Values) {
                    target = cat;
                    break;
                }
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
            if (stageDirector != null && defaultRegistry != null && defaultLayout != null)
                stageDirector.Bind(defaultRegistry, defaultLayout, textController);
            director.Bind(textController, variableContext);
            audioController?.Bind(director);
            historyController?.Bind(director);
        }

        private void _RebindStageDirector(DialogueCatalogSO catalog) {
            if (stageDirector == null) return;
            CharacterRegistrySO reg = catalog.Registry != null ? catalog.Registry : defaultRegistry;
            StageLayoutSO lay = catalog.Layout != null ? catalog.Layout : defaultLayout;
            activeRegistry = reg;
            stageDirector.Bind(reg, lay, textController);
        }

        private void _SubscribeUiEvents() {
            uiController.OnPlay += _OnUiPlay;
            uiController.OnSkip += _OnUiSkip;
            uiController.OnAutoToggle += _OnUiAutoToggle;
            uiController.OnAdvance += _OnUiAdvance;
            uiController.OnSelectChoice += _OnUiSelectChoice;
        }

        private void _UnsubscribeUiEvents() {
            if (uiController == null) return;
            uiController.OnPlay -= _OnUiPlay;
            uiController.OnSkip -= _OnUiSkip;
            uiController.OnAutoToggle -= _OnUiAutoToggle;
            uiController.OnAdvance -= _OnUiAdvance;
            uiController.OnSelectChoice -= _OnUiSelectChoice;
        }

        private void _SubscribeInputEvents() {
            if (inputController == null) return;
            inputController.OnInputAdvance += _OnInputAdvance;
            inputController.OnInputSkip += _OnInputSkip;
            inputController.OnInputAutoToggle += _OnInputAutoToggle;
            inputController.OnInputHistoryToggle += _OnInputHistoryToggle;
        }

        private void _UnsubscribeInputEvents() {
            if (inputController == null) return;
            inputController.OnInputAdvance -= _OnInputAdvance;
            inputController.OnInputSkip -= _OnInputSkip;
            inputController.OnInputAutoToggle -= _OnInputAutoToggle;
            inputController.OnInputHistoryToggle -= _OnInputHistoryToggle;
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
            director.IsSkipping = true;
            textController.SkipToEnd();
            textController.RequestAdvance();
        }

        private void _OnUiAutoToggle(bool isOn) {
            director.AutoAdvanceDelay = isOn ? autoModeDelay : -1f;
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

        #region Private — 이벤트 핸들러 (Input)
        private void _OnInputAdvance() => _OnUiAdvance();
        private void _OnInputSkip() => _OnUiSkip();
        private void _OnInputHistoryToggle() => historyController?.ToggleHistory();
        private void _OnInputAutoToggle() {
            bool newIsOn = director.AutoAdvanceDelay < 0f;
            director.AutoAdvanceDelay = newIsOn ? autoModeDelay : -1f;
            uiController.SetAutoToggle(newIsOn);
        }
        #endregion

        #region Private — 이벤트 핸들러 (Director)
        private void _OnDirectorCatalogStart(DialogueCatalogSO catalog) {
#if UNITY_EDITOR
            _ApplyLocalizationOverride(catalog);
#endif
            OnCatalogStart?.Invoke(catalog);
        }

        private void _OnDirectorLineEnter(DialogueLineNode node) {
            string displayName = string.Empty;
            if (!string.IsNullOrEmpty(node.SpeakerKey)
                && activeRegistry != null
                && activeRegistry.TryGet(node.SpeakerKey, out CharacterPortraitSetSO portraitSet)
                && !string.IsNullOrEmpty(portraitSet.DisplayName)) {
                displayName = portraitSet.DisplayName;
            }
            uiController.ShowSpeakerName(displayName);
            uiController.ShowAdvanceHint(false);
        }

        private void _OnDirectorChoicePresent(DialogueChoiceNode node, IReadOnlyList<DialogueChoiceNode.ChoiceData> validChoices) {
            uiController.ShowChoices(validChoices);
        }

        private void _OnDirectorCatalogExit(DialogueCatalogSO catalog, string exitKey) {
#if UNITY_EDITOR
            _RestoreLocalizationOverride();
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

        #region Editor — 인스펙터 테스트 버튼
        [HButton("Test Play(Current Catalog)")]
        private void _EditorTestPlayCurrentCatalog() {
            if (!Application.isPlaying) {
                HLogger.Warning("[DialogueManager] Play 모드에서만 실행 가능합니다.");
                return;
            }
            PlayDefault();
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: _OnUiAutoToggle bool 파라미터 + _OnInputAutoToggle 로직 갱신
 *
 * # 변경
 * - `_OnUiAutoToggle()` → `_OnUiAutoToggle(bool isOn)`: `isOn ? autoModeDelay : -1f` 직접 설정.
 * - `_OnInputAutoToggle()`: 위임 제거. `director.AutoAdvanceDelay < 0f` 로 현재 off 판단 → toggle → `uiController.SetAutoToggle(newIsOn)` 호출.
 *
 * # 이유
 * - DialogueUiController.OnAutoToggle 시그니처가 Action → Action<bool> 로 변경됨에 따른 동기화.
 * - 키보드 입력으로 Auto 토글 시 UI Toggle도 함께 갱신해야 시각 상태 일관성 유지.
 * - SetAutoToggle(SetIsOnWithoutNotify) 사용: Toggle.isOn 설정이 onValueChanged를 다시 발화하지 않도록 방지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: inputController SerializeField + Input 이벤트 구독 연결 추가
 *
 * # 변경
 * - [Controllers] 그룹에 `DialogueInputController inputController` SerializeField 추가.
 * - Awake: `_SubscribeInputEvents()` 추가 (UiEvents 구독 직후).
 * - OnDestroy: `_UnsubscribeInputEvents()` 추가.
 * - `_SubscribeInputEvents()` / `_UnsubscribeInputEvents()`: inputController null 가드 포함.
 * - `_OnInputAdvance/Skip/AutoToggle`: 동명 UI 핸들러에 위임 (로직 중복 회피).
 * - 헤더 특징에 inputController(선택) 안내 1줄 추가.
 *
 * # 이유
 * - HCUP-2.5.0 Phase 3 — DialogueInputController(InputSystem) 이벤트를 Manager가 구독.
 * - inputController 선택 슬롯: 씬에 GO 없어도 Manager 정상 동작 (audioController와 동일 처리).
 * - UI/Input 핸들러 로직 동일 → UI 핸들러 위임으로 DRY 유지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: historyController SerializeField + _Bind() + Input 이벤트 추가 — HCUP-2.6.0 Phase 3
 *
 * # 변경
 * - [Controllers] 필드 그룹에 `DialogueHistoryController historyController` SerializeField 추가.
 * - _Bind(): `historyController?.Bind(director)` 추가.
 * - _SubscribeInputEvents / _UnsubscribeInputEvents: `OnInputHistoryToggle` 구독/해제 추가.
 * - `_OnInputHistoryToggle()` → `historyController?.ToggleHistory()` 위임.
 * - 헤더 특징에 historyController 선택 기능 안내 1줄 추가.
 *
 * # 이유
 * - H키로 히스토리 패널 토글. inputController.OnInputHistoryToggle 구독 후 Manager가 위임.
 * - null-safe 호출: historyController 미연결 씬에서도 정상 동작.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: Auto 버튼 토글 — autoModeDelay + _OnUiAutoToggle 추가
 *
 * # 변경
 * - `[HTitle("Auto Mode")] [SerializeField] float autoModeDelay = 1.5f` 필드 추가.
 * - `_SubscribeUiEvents / _UnsubscribeUiEvents`: `OnAutoToggle` 구독/해제 추가.
 * - `_OnUiAutoToggle`: `AutoAdvanceDelay >= 0` → -1f(off) / else → autoModeDelay(on) 토글.
 *
 * # 이유
 * - Auto 버튼(BTN_Auto) 씬 배선 지원. 버튼 클릭마다 Auto on/off 전환.
 * - autoModeDelay SerializeField: Inspector에서 자동 진행 간격 조정 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: _OnUiSkip — Skip 버튼을 카탈로그 전체 Skip으로 격상
 *
 * # 변경
 * - `_OnUiSkip`: `textController.SkipToEnd()` 단독 → 아래 3단계로 교체.
 *   1. `director.IsSkipping = true`  — 이후 모든 라인 스킵 플래그.
 *   2. `textController.SkipToEnd()`  — 타이핑 중이면 현재 라인 즉시 완료.
 *   3. `textController.RequestAdvance()` — Advance 대기 중이면 즉시 TCS resolve.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 2-C.
 * - IsSkipping = true 만으로는 이미 await 중인 advanceTcs가 멈추지 않음.
 *   RequestAdvance()로 현재 대기를 즉시 해제해야 다음 라인부터 Skip이 적용됨.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: IsSkipping / AutoAdvanceDelay 패스스루 프로퍼티 추가
 *
 * # 변경
 * - `bool IsSkipping { get; set; }` — director.IsSkipping 패스스루.
 * - `float AutoAdvanceDelay { get; set; }` — director.AutoAdvanceDelay 패스스루.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 2 — 외부 UI/게임 코드가 Manager 경유 Auto/Skip 모드 제어 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: audioController SerializeField + _Bind() 연결 추가
 *
 * # 변경
 * - [Controllers] 필드 그룹에 `DialogueAudioController audioController` SerializeField 추가.
 * - _Bind(): `audioController?.Bind(director)` 추가.
 * - 헤더 특징에 audioController 선택 기능 안내 1줄 추가.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 1-E — DialogueAudioController를 씬 매니저가 Awake에서 자동 wire.
 * - null-safe 호출: 오디오 없이 쓰는 씬에서 매니저 비정상 종료 방지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: _OnDirectorLineEnter — SpeakerKey → DisplayName 조회
 *
 * # 변경
 * - activeRegistry 필드 추가 — _RebindStageDirector 시점에 현재 활성 레지스트리 저장.
 * - _OnDirectorLineEnter: speakerKey 직접 출력 → activeRegistry.TryGet → DisplayName 조회 후 출력.
 *   레지스트리 미설정, 키 미등록, DisplayName 빈 문자열인 경우 빈 문자열 표시(기존 동작 유지).
 *
 * # 이유
 * - SpeakerKey는 내부 식별자 (레지스트리 조회·블립 사운드 매핑용).
 *   UI에 노출되는 이름은 CharacterPortraitSetSO.DisplayName 이어야 한다.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: PlayCatalog — 카탈로그 전용 stageDirector 재바인드
 *
 * # 추가
 * - PlayCatalog: director.PlayCatalog 호출 전 _RebindStageDirector(catalog) 추가.
 * - _RebindStageDirector: catalog.Registry/Layout null 시 매니저 기본값(registry/layout) 폴백.
 *
 * # 이유
 * - 1 DialogueSO = 1 StageLayoutSO = 1 CharacterRegistrySO 규칙.
 * - 씬에서 서로 다른 카탈로그 재생 시 각 카탈로그의 캐릭터/스테이지 셋으로 자동 전환.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: _ApplyLocalizationOverride — PlayCatalog → _OnDirectorCatalogStart 이동
 *
 * # 변경
 * - PlayCatalog()에서 `_ApplyLocalizationOverride(catalog)` 호출 제거.
 * - `_OnDirectorCatalogStart()`에 `#if UNITY_EDITOR` 블록으로 `_ApplyLocalizationOverride(catalog)` 추가.
 *
 * # 이유
 * - 2회차 이상 PlayCatalog() 호출 시 override race condition:
 *   PlayCatalog → _ApplyLocalizationOverride(설정) → director.PlayCatalog →
 *   _CancelCurrentCatalog("Replaced") → OnCatalogExit("Replaced") → _RestoreLocalizationOverride(제거!)
 *   → _PlayCatalogAsync → _BuildLine() — override 없이 실행 → UID 리터럴 출력.
 * - _OnDirectorCatalogStart는 _CancelCurrentCatalog 이후, _PlayCatalogAsync.Forget() 직전에
 *   동기 발화되므로 override가 텍스트 해석 전에 확실히 적용됨.
 * - 첫 번째 플레이(state=Idle)도 동일 경로로 처리되어 동작 일관성 확보.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: catalogMap + PlayDefault / PlayByKey API 추가
 *
 * # 변경
 * - using HCollection 추가. HCUP.HDialogue.asmdef에 HCUP.HCollection 참조 추가.
 * - [HButton("Test Play(Current Catalog)")] _EditorTestPlayCurrentCatalog 추가 (#if UNITY_EDITOR).
 *   Play 모드 전용 — 아닐 경우 HLogger.Warning.
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
