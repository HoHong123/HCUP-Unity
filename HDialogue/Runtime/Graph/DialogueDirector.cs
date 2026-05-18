#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 그래프 순회 런타임. TextController / StageDirector 통합 조율자.
 *
 * 특징 / 지원기능 ::
 * + PlayCatalog(catalog) / PlayCatalog(catalog, startNodeOverride)
 * + Stop()                — CancellationToken 취소, OnCatalogExit("") 발행, Idle 전이
 * + SelectChoice(key)     — WaitingForChoice 상태 + valid key 검증 후 분기
 * + NotifyWaitConditionMet() — WaitNode Condition 모드 외부 신호
 * + 이벤트 5종: OnCatalogStart / OnCatalogExit(catalog, exitKey) / OnLineEnter / OnChoicePresent(node, validChoices) / OnEventFired
 * + _CancelCurrentCatalog(exitKey) — Stop/PlayCatalog 공통 종료 흐름
 * + Cutscene 태그: autoAdvanceDelay 후 자동 Advance
 *
 * 주의사항 ::
 * Bind()는 null 허용 — textController null이면 LineNode 즉시 통과(워닝 1회).
 * PlayCatalog 재호출 시 이전 카탈로그 OnCatalogExit("Replaced") 발행 후 취소 (큐잉 없음).
 * stageDirector는 SerializeField 선택 연결 (null-safe 호출).
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HAudio;
using HDiagnosis.Logger;
using HInspector;
using HUI.TextUI;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueDirector : MonoBehaviour {
        #region Fields
        [HTitle("References")]
        [SerializeField]
        CharacterStageDirector stageDirector;

        [HTitle("Settings")]
        [SerializeField]
        float autoAdvanceDelay = 2f;

        DialogueCatalogSO currentCatalog;
        BaseNode currentNode;
        DialogueDirectorState state = DialogueDirectorState.Idle;

        DialogueTextController textController;
        IDialogueVariableContext variables;

        CancellationTokenSource cts;
        UniTaskCompletionSource<string> choiceTcs;
        UniTaskCompletionSource waitConditionTcs;
        readonly HashSet<string> validChoiceKeys = new();

        bool hasWarnedNullController;
        #endregion

        #region Events
        public event Action<DialogueCatalogSO> OnCatalogStart;
        public event Action<DialogueCatalogSO, string> OnCatalogExit;
        public event Action<DialogueLineNode> OnLineEnter;
        public event Action<DialogueChoiceNode, IReadOnlyList<DialogueChoiceNode.ChoiceData>> OnChoicePresent;
        public event Action<string, string> OnEventFired;
        #endregion

        #region Properties
        public DialogueDirectorState State => state;
        public DialogueCatalogSO CurrentCatalog => currentCatalog;
        public BaseNode CurrentNode => currentNode;
        #endregion

        #region Unity Life Cycle
        private void OnDestroy() {
            _CancelAndDisposeCts();
        }
        #endregion

        #region Public API
        public void Bind(DialogueTextController controller, IDialogueVariableContext ctx) {
            textController = controller;
            variables = ctx;
            hasWarnedNullController = false;
        }

        public void PlayCatalog(DialogueCatalogSO catalog) {
            PlayCatalog(catalog, NodeUID.None);
        }

        public void PlayCatalog(DialogueCatalogSO catalog, NodeUID startNodeOverride) {
            if (catalog == null) {
                HLogger.Error("[DialogueDirector] PlayCatalog: catalog is null.");
                return;
            }

            BaseNode startNode = startNodeOverride.IsValid
                && catalog.Nodes.TryGetValue(startNodeOverride, out BaseNode overrideNode)
                ? overrideNode
                : catalog.RootNode;

            if (startNode == null) {
                HLogger.Error($"[DialogueDirector] PlayCatalog: no valid start node in '{catalog.name}'.");
                return;
            }

            _CancelCurrentCatalog("Replaced");
            cts = new CancellationTokenSource();

            currentCatalog = catalog;
            currentNode = startNode;
            state = DialogueDirectorState.Idle;

            OnCatalogStart?.Invoke(catalog);
            _PlayCatalogAsync(startNode, cts.Token).Forget();
        }

        public void Stop() {
            _CancelCurrentCatalog(string.Empty);
        }

        public void SelectChoice(string choiceKey) {
            if (state != DialogueDirectorState.WaitingForChoice) {
                HLogger.Warning("[DialogueDirector] SelectChoice called outside WaitingForChoice state.");
                return;
            }
            if (!validChoiceKeys.Contains(choiceKey)) {
                HLogger.Warning($"[DialogueDirector] SelectChoice: invalid key '{choiceKey}'. Ignored.");
                return;
            }
            choiceTcs?.TrySetResult(choiceKey);
        }

        public void NotifyWaitConditionMet() {
            waitConditionTcs?.TrySetResult();
        }
        #endregion

        #region Private — Main Loop
        private async UniTaskVoid _PlayCatalogAsync(BaseNode startNode, CancellationToken ct) {
            currentNode = startNode;
            try {
                while (currentNode != null && state != DialogueDirectorState.Finished) {
                    string hubKey = await _ProcessNode(currentNode, ct);
                    if (state == DialogueDirectorState.Finished) break;
                    currentNode = _ResolveNextNode(currentNode, hubKey);
                }
            } catch (OperationCanceledException) {
            } catch (System.Exception e) {
                HLogger.Error($"[DialogueDirector] Unexpected exception in _PlayCatalogAsync: {e}");
            }
        }

        private async UniTask<string> _ProcessNode(BaseNode node, CancellationToken ct) {
            switch (node) {
                case DialogueEntryNode entry:
                    await _ProcessEntryNode(entry, ct);
                    return null;
                case DialogueLineNode line:
                    await _ProcessLineNode(line, ct);
                    return null;
                case DialogueChoiceNode choice:
                    return await _ProcessChoiceNode(choice, ct);
                case DialogueBranchNode branch:
                    return await _ProcessBranchNode(branch, ct);
                case DialogueEventNode evt:
                    await _ProcessEventNode(evt, ct);
                    return null;
                case DialogueVariableNode variable:
                    await _ProcessVariableNode(variable, ct);
                    return null;
                case DialogueWaitNode wait:
                    await _ProcessWaitNode(wait, ct);
                    return null;
                case DialogueCinematicNode cinematic:
                    await _ProcessCinematicNode(cinematic, ct);
                    return null;
                case DialogueExitNode exit:
                    await _ProcessExitNode(exit, ct);
                    return null;
                default:
                    HLogger.Warning($"[DialogueDirector] Unknown node type: {node.GetType().Name}. Skipping.");
                    await UniTask.NextFrame(cancellationToken: ct);
                    return null;
            }
        }

        private BaseNode _ResolveNextNode(BaseNode node, string hubKey) {
            if (node is HubNode && hubKey != null) {
                foreach (BaseNodeEdge edge in currentCatalog.GetOutgoingEdges(node.UID)) {
                    if (edge is HubNodeEdge hubEdge && hubEdge.BranchPortKey == hubKey) {
                        currentCatalog.Nodes.TryGetValue(hubEdge.LeafUID, out BaseNode hubNext);
                        return hubNext;
                    }
                }
                HLogger.Warning($"[DialogueDirector] No hub edge matching key '{hubKey}' from node '{node.Title}'. Finishing.");
                state = DialogueDirectorState.Finished;
                return null;
            }

            foreach (BaseNodeEdge edge in currentCatalog.GetOutgoingEdges(node.UID)) {
                currentCatalog.Nodes.TryGetValue(edge.LeafUID, out BaseNode next);
                return next;
            }

            if (!(node is DialogueExitNode)) {
                HLogger.Warning($"[DialogueDirector] Node '{node.Title}' ({node.GetType().Name}) has no outgoing edges. Finishing.");
                state = DialogueDirectorState.Finished;
            }
            return null;
        }
        #endregion

        #region Private — Node Processors
        private UniTask _ProcessEntryNode(DialogueEntryNode node, CancellationToken ct) {
            return UniTask.CompletedTask;
        }

        private UniTask _ProcessExitNode(DialogueExitNode node, CancellationToken ct) {
            OnCatalogExit?.Invoke(currentCatalog, node.ExitKey);
            if (node.ClearStageOnExit) stageDirector?.ClearAll();
            state = DialogueDirectorState.Finished;
            return UniTask.CompletedTask;
        }

        private async UniTask _ProcessLineNode(DialogueLineNode node, CancellationToken ct) {
            stageDirector?.EnterLine(_BuildLineStageContext(node));
            OnLineEnter?.Invoke(node);

            DialogueLine line = _BuildLine(node);

            if (textController == null) {
                if (!hasWarnedNullController) {
                    HLogger.Warning("[DialogueDirector] TextController is null. LineNodes will be skipped. Call Bind() first.");
                    hasWarnedNullController = true;
                }
                await UniTask.NextFrame(cancellationToken: ct);
                return;
            }

            var lineCompleteTcs = new UniTaskCompletionSource();
            Action onLineComplete = () => lineCompleteTcs.TrySetResult();
            textController.OnLineComplete += onLineComplete;

            state = DialogueDirectorState.PlayingLine;
            textController.PlayLine(line);

            state = DialogueDirectorState.WaitingForLineEnd;
            try {
                await lineCompleteTcs.Task.AttachExternalCancellation(ct);
            } finally {
                textController.OnLineComplete -= onLineComplete;
            }

            if (currentCatalog.CatalogTag == DialogueCatalogTag.Cutscene) {
                await UniTask.WaitForSeconds(autoAdvanceDelay, ignoreTimeScale: true, cancellationToken: ct);
            } else {
                var advanceTcs = new UniTaskCompletionSource();
                Action onAdvance = () => advanceTcs.TrySetResult();
                textController.OnAdvanceRequested += onAdvance;
                try {
                    await advanceTcs.Task.AttachExternalCancellation(ct);
                } finally {
                    textController.OnAdvanceRequested -= onAdvance;
                }
            }
        }

        private async UniTask<string> _ProcessChoiceNode(DialogueChoiceNode node, CancellationToken ct) {
            var validChoices = new List<DialogueChoiceNode.ChoiceData>();
            foreach (DialogueChoiceNode.ChoiceData choice in node.Choices) {
                bool conditionPassed = string.IsNullOrEmpty(choice.ConditionKey)
                    || (variables != null && variables.TryGetBool(choice.ConditionKey, out bool r) && r);
                if (conditionPassed) validChoices.Add(choice);
            }

            if (validChoices.Count == 0) {
                if (!string.IsNullOrEmpty(node.FallbackChoiceKey)) {
                    return node.FallbackChoiceKey;
                }
                HLogger.Warning($"[DialogueDirector] ChoiceNode '{node.Title}' has no valid choices and no fallback. Finishing.");
                state = DialogueDirectorState.Finished;
                return null;
            }

            validChoiceKeys.Clear();
            for (int k = 0; k < validChoices.Count; k++) validChoiceKeys.Add(validChoices[k].Key);

            state = DialogueDirectorState.WaitingForChoice;
            OnChoicePresent?.Invoke(node, validChoices);

            choiceTcs = new UniTaskCompletionSource<string>();
            try {
                return await choiceTcs.Task.AttachExternalCancellation(ct);
            } finally {
                choiceTcs = null;
                validChoiceKeys.Clear();
            }
        }

        private async UniTask<string> _ProcessBranchNode(DialogueBranchNode node, CancellationToken ct) {
            if (variables == null) {
                HLogger.Warning($"[DialogueDirector] BranchNode '{node.Title}' — no IDialogueVariableContext. Boolean 'false' fallback.");
                return "false";
            }

            switch (node.Mode) {
                case BranchMode.Boolean: {
                    bool result = variables.TryGetBool(node.ConditionKey, out bool b) && b;
                    return result ? "true" : "false";
                }
                case BranchMode.IntRange: {
                    if (!variables.TryGetInt(node.ConditionKey, out int intVal)) {
                        HLogger.Warning($"[DialogueDirector] BranchNode '{node.Title}' — key '{node.ConditionKey}' not found. Finishing.");
                        state = DialogueDirectorState.Finished;
                        return null;
                    }
                    foreach (HubNode.HubPortEntry entry in node.Entries) {
                        string[] parts = entry.Key.Split('_');
                        if (parts.Length == 2
                            && int.TryParse(parts[0], out int min)
                            && int.TryParse(parts[1], out int max)
                            && intVal >= min && intVal <= max) {
                            return entry.Key;
                        }
                    }
                    HLogger.Warning($"[DialogueDirector] BranchNode '{node.Title}' — int value {intVal} matched no IntRange port. Finishing.");
                    state = DialogueDirectorState.Finished;
                    return null;
                }
                case BranchMode.Switch: {
                    if (!variables.TryGetString(node.ConditionKey, out string strVal)) {
                        HLogger.Warning($"[DialogueDirector] BranchNode '{node.Title}' — key '{node.ConditionKey}' not found. Finishing.");
                        state = DialogueDirectorState.Finished;
                        return null;
                    }
                    return strVal;
                }
                default:
                    HLogger.Warning($"[DialogueDirector] BranchNode '{node.Title}' — unknown BranchMode. Finishing.");
                    state = DialogueDirectorState.Finished;
                    return null;
            }
        }

        private async UniTask _ProcessEventNode(DialogueEventNode node, CancellationToken ct) {
            OnEventFired?.Invoke(node.EventKey, node.EventArg);
            await UniTask.NextFrame(cancellationToken: ct);
        }

        private async UniTask _ProcessCinematicNode(DialogueCinematicNode node, CancellationToken ct) {
            if (stageDirector != null) {
                foreach (CinematicInstruction ins in node.Instructions)
                    stageDirector.ApplyInstruction(ins.ToEventInstruction());
            }
            await UniTask.NextFrame(cancellationToken: ct);
            if (node.WaitForTransition) {
                if (stageDirector != null)
                    await stageDirector.WaitForActiveTransitionsAsync(ct);
                else
                    await UniTask.NextFrame(cancellationToken: ct);
            }
            if (node.WaitForInput) {
                if (textController != null) {
                    var advanceTcs = new UniTaskCompletionSource();
                    Action onAdvance = () => advanceTcs.TrySetResult();
                    textController.OnAdvanceRequested += onAdvance;
                    try {
                        await advanceTcs.Task.AttachExternalCancellation(ct);
                    } finally {
                        textController.OnAdvanceRequested -= onAdvance;
                    }
                }
            }
        }

        private async UniTask _ProcessVariableNode(DialogueVariableNode node, CancellationToken ct) {
            if (variables == null) {
                HLogger.Warning($"[DialogueDirector] VariableNode '{node.Title}' — no IDialogueVariableContext. Skipping.");
                await UniTask.NextFrame(cancellationToken: ct);
                return;
            }

            switch (node.Op) {
                case VariableOp.SetBool:    variables.SetBool(node.VariableKey, node.BoolValue);    break;
                case VariableOp.SetInt:     variables.SetInt(node.VariableKey, node.IntValue);      break;
                case VariableOp.SetString:  variables.SetString(node.VariableKey, node.StringValue); break;
                case VariableOp.AddInt:     variables.AddInt(node.VariableKey, node.IntValue);      break;
                case VariableOp.ToggleBool: variables.ToggleBool(node.VariableKey);                 break;
            }

            await UniTask.NextFrame(cancellationToken: ct);
        }

        private async UniTask _ProcessWaitNode(DialogueWaitNode node, CancellationToken ct) {
            state = DialogueDirectorState.Waiting;

            switch (node.Mode) {
                case WaitMode.Time:
                    await UniTask.WaitForSeconds(node.Seconds, ignoreTimeScale: true, cancellationToken: ct);
                    break;
                case WaitMode.Condition:
                    waitConditionTcs = new UniTaskCompletionSource();
                    try {
                        await waitConditionTcs.Task.AttachExternalCancellation(ct);
                    } finally {
                        waitConditionTcs = null;
                    }
                    break;
                case WaitMode.UserInput:
                    if (textController != null) {
                        var advanceTcs = new UniTaskCompletionSource();
                        Action onAdvance = () => advanceTcs.TrySetResult();
                        textController.OnAdvanceRequested += onAdvance;
                        try {
                            await advanceTcs.Task.AttachExternalCancellation(ct);
                        } finally {
                            textController.OnAdvanceRequested -= onAdvance;
                        }
                    } else {
                        await UniTask.NextFrame(cancellationToken: ct);
                    }
                    break;
            }
        }
        #endregion

        #region Private — Builders
        private DialogueLine _BuildLine(DialogueLineNode node) {
            string speakerKey = !string.IsNullOrEmpty(node.SpeakerKey) ? node.SpeakerKey : "";
            float speed = node.SpeedMultiplier > 0f ? node.SpeedMultiplier : 1f;
            string rawText = HTextLocalizer.GetText?.Invoke(node.LocalizationUID) ?? node.LocalizationUID;
            AudioClips blipToken = node.OverrideBlipToken;

            return new DialogueLine {
                SpeakerKey = speakerKey,
                RawText = rawText,
                SpeedMultiplier = speed,
                OverrideBlipToken = blipToken,
            };
        }

        private LineStageContext _BuildLineStageContext(DialogueLineNode node) {
            string speakerKey = !string.IsNullOrEmpty(node.SpeakerKey) ? node.SpeakerKey : "";

            return new LineStageContext {
                SpeakerKey = speakerKey,
                SpeakerPoseKey = node.SpeakerPoseKey,
                SpeakerSlot = node.SpeakerSlot,
                SpeakerFacing = node.SpeakerFacing,
                AutoHighlightSpeaker = node.AutoHighlight,
            };
        }
        #endregion

        #region Private — Util
        private void _CancelCurrentCatalog(string exitKey) {
            if (state != DialogueDirectorState.Idle && currentCatalog != null)
                OnCatalogExit?.Invoke(currentCatalog, exitKey);
            _CancelAndDisposeCts();
            state = DialogueDirectorState.Idle;
        }

        private void _CancelAndDisposeCts() {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: _BuildLine — DefaultSpeakerKey/BlipToken 카탈로그 폴백 제거 + 로컬리제이션 연동
 *
 * # 변경
 * - _BuildLine: speakerKey / blipToken 폴백에서 currentCatalog.DefaultSpeakerKey / DefaultBlipToken 참조 제거.
 *   노드에 값이 없으면 빈 문자열("").
 * - _BuildLine: `RawText = node.RawText` → `RawText = HTextLocalizer.GetText?.Invoke(uid) ?? uid`.
 *   HTextLocalizer 미초기화 시 null-safe fallback으로 UID 그대로 표시.
 * - _BuildLineStageContext: DefaultSpeakerKey 폴백 제거 → `node.SpeakerKey ?? ""`.
 * - using HUI.TextUI 추가.
 *
 * # 이유
 * - DialogueCatalogSO에서 defaultSpeakerKey / defaultBlipToken 제거에 따른 연쇄 수정.
 * - 라인 텍스트는 로컬리제이션 UID로 관리, 런타임에 HTextLocalizer 경유 해석.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: _ProcessCinematicNode — WaitForInput true 시 Next 대기 분기 추가
 *
 * # 변경
 * - `if (node.WaitForInput)` 블록 추가.
 *   WaitForTransition 대기 완료 후 textController.OnAdvanceRequested를 TCS로 구독, Next 명령까지 대기.
 *
 * # 이유
 * - CinematicNode 트랜지션 완료 후 자동 진행 / 사용자 입력 대기를 노드별로 선택 가능하게.
 * - _ProcessLineNode / WaitNode(UserInput) 와 동일한 TCS 패턴 재사용.
 *
 * # 주의
 * - textController null이면 Next 대기 없이 통과 (기존 null-safe 정책 유지).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: PlayCatalog 신규 요청 유효성 검증을 취소보다 먼저 수행
 *
 * # 변경
 * - PlayCatalog(): startNode 유효성 검증을 _CancelCurrentCatalog("Replaced") 호출 앞으로 이동.
 *   catalog null → startNode null 확인 → 취소 → CTS 생성 → 실행 순서로 재정렬.
 *
 * # 이유
 * - AgentReview P1 (2026-05-17 19:35:19).
 * - 이전 순서에서 null catalog만 확인 후 바로 _CancelCurrentCatalog → startNode null이면
 *   기존 대화는 종료됐지만 새 대화는 시작되지 않는 상태가 됐음.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: SelectChoice valid-key 검증 + PlayCatalog 재호출 OnCatalogExit("Replaced") 발행
 *
 * # 변경
 * - HashSet<string> validChoiceKeys 필드 추가. _ProcessChoiceNode에서 채움 / finally에서 비움.
 * - SelectChoice: invalid key 입력 시 HLogger.Warning + early return (WaitingForChoice 상태 유지).
 * - _CancelCurrentCatalog(exitKey) private 헬퍼 신설. Stop()과 PlayCatalog() 재호출이 공통 사용.
 * - PlayCatalog 진입부 _CancelCurrentCatalog("Replaced") 호출. 이전 카탈로그 OnCatalogExit 발행.
 *
 * # 이유
 * - AgentReview P1 #1, #2 (2026-05-17 19:13:03).
 * - 외부 UI 버그/조건 우회 입력이 곧바로 잘못된 분기로 흐르는 보안/무결성 경계 부재.
 * - Stop과 PlayCatalog 재호출의 취소 계약 불일치.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Warning 보강 — Stop OnCatalogExit·OnChoicePresent validChoices·WaitForTransition
 *
 * # 변경
 * - Stop(): state != Idle && currentCatalog != null 조건에서 OnCatalogExit("") 발행.
 *   OnCatalogStart와 대칭. UI 숨김·스테이지 클리어 구독자가 강제 중단을 감지 가능.
 * - OnChoicePresent 시그니처: Action<DialogueChoiceNode> → Action<DialogueChoiceNode, IReadOnlyList<ChoiceData>>.
 *   조건 필터링된 validChoices를 구독자에 전달 — UI 측이 전체 choices 대신 유효 목록만 수신.
 * - _ProcessCinematicNode WaitForTransition: NextFrame 1회 대기 → stageDirector.WaitForActiveTransitionsAsync().
 *   IsTransitioning 루프로 트랜지션 완료까지 정확히 대기.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.18 (수정) :: _ProcessEntryNode / _ProcessExitNode — UniTask.NextFrame 제거, 동기 반환
 *
 * # 변경
 * - _ProcessEntryNode: async + await UniTask.NextFrame → UniTask _ProcessEntryNode() { return UniTask.CompletedTask; }
 * - _ProcessExitNode: async + await UniTask.NextFrame(ct) → UniTask _ProcessExitNode() { return UniTask.CompletedTask; }
 * - _PlayCatalogAsync: TRACE Debug.Log 5개 제거. OperationCanceledException catch 본문 비움 (취소는 정상 흐름).
 *   UnhandledException catch → HLogger.Error 로 교체.
 *
 * # 이유
 * - MCP-for-Unity 환경에서 UniTask.NextFrame이 영구 블록: MCP가 Unity 메인 스레드를 점유해
 *   게임 루프가 전진하지 못함. frameCount=1 / time=0.000 으로 확인.
 * - EntryNode의 NextFrame은 "초기화 완료 후 한 프레임 지연" 목적이었으나
 *   DialogueManager.Awake()에서 Bind가 이미 완료되므로 불필요.
 * - ExitNode의 NextFrame도 실용적 목적 없음 — OnCatalogExit·state=Finished는 모두 await 전에 동기 처리.
 * - CompletedTask 반환: 힙 할당·스케줄링 없이 동기 완료를 표현하는 UniTask 관용구.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueDirector 베이스 코드 생성
 *
 * # 목적
 * - 대화 그래프 순회 런타임 — HCUP-2.3.0 Phase 2
 * - TextController / StageDirector 단방향 통합 (역 참조 없음)
 *
 * # 설계 결정
 * - _ProcessNode UniTask<string> 반환: 단순 노드=null, HubNode=선택 포트 키.
 *   메인 루프가 동일 시그니처로 모든 노드 타입 처리 — 타입 switch 한 곳에만 존재.
 * - AttachExternalCancellation + try/finally: Stop() 호출 시 이벤트 리스너 누수 없이 정리.
 *   OperationCanceledException → _PlayCatalogAsync catch로 깨끗하게 탈출.
 * - OneShot 구독 패턴: PlayLine 직전 OnLineComplete 등록 → 완료 후 즉시 해제.
 *   Bind() 시점 전역 구독 회피 → 이전 라인 완료 신호 오염 방지.
 * - hasWarnedNullController: textController null 경고는 카탈로그 전체에서 1회만.
 *   그래프 흐름 검증 시 콘솔 스팸 방지.
 * - autoAdvanceDelay SerializeField: Cutscene 모드 자동 진행 간격, Inspector 조정 가능.
 * - stageDirector SerializeField: null 허용 — Phase 4 완성 전까지 null로 운용.
 *
 * # 사용 흐름
 * 1. Awake/Start: director.Bind(textController, variableContext)
 * 2. 트리거: director.PlayCatalog(catalog)
 * 3. 선택지: director.SelectChoice(key) (WaitingForChoice 상태에서만)
 * 4. 종료: OnCatalogExit 이벤트 구독 또는 director.Stop()
 *
 * =============================================================================
 */
#endif
