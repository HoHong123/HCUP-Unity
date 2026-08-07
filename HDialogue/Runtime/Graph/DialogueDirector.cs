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

        // WaitNode 에 도달하기 전에 도착한 조건 신호는 종전에 소실됐고, 이후 WaitNode 는
        // 이미 보내진 신호를 영원히 기다렸다 (탈출구는 Stop() 뿐). 래치로 한 번 보관한다.
        bool waitConditionLatched;

        // 그래프 순회 폭주 방어. Branch→Branch 순환은 await 없이 완전 동기로 돌아
        // 메인 스레드를 하드 프리즈시킨다 (검증기 W002 는 에디터 수동 실행이라 런타임 방어 불가).
        const int MAX_NODE_TRANSITIONS = 10000;
        const string ERROR_EXIT_KEY = "Error";
        const int SYNC_YIELD_INTERVAL = 32;

        float autoAdvanceOverride = -1f;
        bool isSkipping;
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
        // 게터가 override 원본(-1 = 미설정)을 그대로 반환해, 설정 UI 가 읽으면 -1 이 표시됐다.
        // 실제 적용되는 유효값을 반환하고, 해제는 별도 API 로 분리한다.
        public float AutoAdvanceDelay {
            get => autoAdvanceOverride >= 0f ? autoAdvanceOverride : autoAdvanceDelay;
            set => autoAdvanceOverride = value;
        }

        public bool HasAutoAdvanceOverride => autoAdvanceOverride >= 0f;
        public void ClearAutoAdvanceOverride() => autoAdvanceOverride = -1f;
        public bool IsSkipping {
            get => isSkipping;
            set => isSkipping = value;
        }
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
            // 토큰을 지역으로 캡처한다 — OnCatalogStart 구독자가 PlayCatalog 를 재호출하면
            // cts 필드가 교체되어, 이 카탈로그의 시작 노드가 남의 토큰으로 실행됐다.
            var token = cts.Token;
            isSkipping = false;

            currentCatalog = catalog;
            currentNode = startNode;
            state = DialogueDirectorState.Idle;

            // 루프를 먼저 기동한 뒤 알린다 — 구독자 재진입이 기동 자체를 오염시키지 않게 한다.
            _PlayCatalogAsync(startNode, token).Forget();
            OnCatalogStart?.Invoke(catalog);
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
            if (waitConditionTcs != null) {
                waitConditionTcs.TrySetResult();
                return;
            }
            waitConditionLatched = true;
        }
        #endregion

        #region Private — Main Loop
        private async UniTaskVoid _PlayCatalogAsync(BaseNode startNode, CancellationToken ct) {
            currentNode = startNode;
            int transitionCount = 0;
            try {
                while (currentNode != null && state != DialogueDirectorState.Finished) {
                    if (++transitionCount > MAX_NODE_TRANSITIONS) {
                        _FinishWithError(
                            $"Node transition limit ({MAX_NODE_TRANSITIONS}) exceeded at '{currentNode.Title}' — probable graph cycle, or an unusually long catalog.");
                        break;
                    }

                    // 동기 노드(Branch/Variable 등)만 이어지는 구간에서도 주기적으로 프레임을 양보해
                    // 순환이 있어도 에디터/게임이 멈추지 않고 위 상한 로그에 도달하게 한다.
                    if (transitionCount % SYNC_YIELD_INTERVAL == 0) {
                        await UniTask.Yield(ct);
                    }

                    string hubKey = await _ProcessNode(currentNode, ct);
                    if (state == DialogueDirectorState.Finished) break;
                    currentNode = _ResolveNextNode(currentNode, hubKey);
                }
            } catch (OperationCanceledException) {
            } catch (System.Exception e) {
                // 로그만 남기면 state 가 예외 시점 값에 고정되고 종료 이벤트도 발행되지 않아
                // 대화 UI 가 열린 채 좀비 상태가 된다.
                HLogger.Error($"[DialogueDirector] Unexpected exception in _PlayCatalogAsync: {e}");
                _FinishWithError("Unhandled exception during catalog playback.");
            } finally {
                _ReleaseFinishedCts();
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

        private bool _HasHubEdge(BaseNode node, string hubKey) {
            foreach (BaseNodeEdge edge in currentCatalog.GetOutgoingEdges(node.UID)) {
                if (edge is HubNodeEdge hubEdge && hubEdge.BranchPortKey == hubKey) return true;
            }
            return false;
        }

        private BaseNode _ResolveNextNode(BaseNode node, string hubKey) {
            if (node is HubNode && hubKey != null) {
                foreach (BaseNodeEdge edge in currentCatalog.GetOutgoingEdges(node.UID)) {
                    if (edge is HubNodeEdge hubEdge && hubEdge.BranchPortKey == hubKey) {
                        // 일반 엣지 경로(:264-267)와 동일하게 방어한다 — 종전에는 TryGetValue 실패를
                        // 무시하고 null 을 그대로 반환해, 경고도 종료 이벤트도 없이 대화가 멎었다.
                        if (!currentCatalog.Nodes.TryGetValue(hubEdge.LeafUID, out BaseNode hubNext) || hubNext == null) {
                            _FinishWithError($"Hub edge target '{hubEdge.LeafUID}' from node '{node.Title}' (port '{hubKey}') is missing in the catalog.");
                            return null;
                        }
                        return hubNext;
                    }
                }
                _FinishWithError($"No hub edge matching key '{hubKey}' from node '{node.Title}'.");
                return null;
            }

            foreach (BaseNodeEdge edge in currentCatalog.GetOutgoingEdges(node.UID)) {
                // 종전에는 TryGetValue 실패를 무시하고 null 을 반환해, 경고도 종료 이벤트도 없이
                // 루프가 빠져나갔다 (구독자 입장에서는 대화가 영원히 진행 중).
                if (!currentCatalog.Nodes.TryGetValue(edge.LeafUID, out BaseNode next) || next == null) {
                    _FinishWithError($"Edge target '{edge.LeafUID}' from node '{node.Title}' is missing in the catalog.");
                    return null;
                }
                return next;
            }

            if (!(node is DialogueExitNode)) {
                _FinishWithError($"Node '{node.Title}' ({node.GetType().Name}) has no outgoing edges.");
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

        // 정상 종료도 CTS 를 해제해야 다음 PlayCatalog 까지 떠 있지 않는다.
        private void _ReleaseFinishedCts() {
            if (state != DialogueDirectorState.Finished) return;
            _CancelAndDisposeCts();
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

            if (isSkipping) textController.ForceSkipToEnd();

            state = DialogueDirectorState.WaitingForLineEnd;
            try {
                await lineCompleteTcs.Task.AttachExternalCancellation(ct);
            } finally {
                textController.OnLineComplete -= onLineComplete;
            }

            if (isSkipping) return;

            bool isCutscene = currentCatalog.CatalogTag == DialogueCatalogTag.Cutscene;
            if (autoAdvanceOverride >= 0f || isCutscene) {
                float delay = autoAdvanceOverride >= 0f ? autoAdvanceOverride : autoAdvanceDelay;
                await UniTask.WaitForSeconds(delay, ignoreTimeScale: true, cancellationToken: ct);
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
                    // 검증 없이 넘기면 진단이 허브 엣지 쪽으로 어긋난다 — 원인은 ChoiceNode 설정이다.
                    if (!_HasHubEdge(node, node.FallbackChoiceKey)) {
                        _FinishWithError(
                            $"ChoiceNode '{node.Title}' fallback key '{node.FallbackChoiceKey}' has no matching hub edge.");
                        return null;
                    }
                    return node.FallbackChoiceKey;
                }
                _FinishWithError($"ChoiceNode '{node.Title}' has no valid choices and no fallback.");
                return null;
            }

            validChoiceKeys.Clear();
            for (int k = 0; k < validChoices.Count; k++) validChoiceKeys.Add(validChoices[k].Key);

            state = DialogueDirectorState.WaitingForChoice;
            OnChoicePresent?.Invoke(node, validChoices);

            var myChoiceTcs = new UniTaskCompletionSource<string>();
            choiceTcs = myChoiceTcs;
            try {
                return await myChoiceTcs.Task.AttachExternalCancellation(ct);
            } finally {
                // 구 루프의 취소 연속이 뒤늦게 도착해도 신 루프의 대기 채널을 지우지 않는다.
                if (ReferenceEquals(choiceTcs, myChoiceTcs)) {
                    choiceTcs = null;
                    validChoiceKeys.Clear();
                }
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
                        _FinishWithError($"BranchNode '{node.Title}' — int key '{node.ConditionKey}' not found.");
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
                    _FinishWithError($"BranchNode '{node.Title}' — int value {intVal} matched no IntRange port.");
                    return null;
                }
                case BranchMode.Switch: {
                    if (!variables.TryGetString(node.ConditionKey, out string strVal)) {
                        _FinishWithError($"BranchNode '{node.Title}' — string key '{node.ConditionKey}' not found.");
                        return null;
                    }
                    // 검증 없이 반환하면 _ResolveNextNode 의 허브 엣지 미스매치 진단이 대신 떠서
                    // "그래프 배선 문제"로 오인된다 — 진짜 원인(변수 값이 어떤 포트와도 안 맞음)을 여기서 짚는다.
                    if (!_HasHubEdge(node, strVal)) {
                        _FinishWithError($"BranchNode '{node.Title}' — string value '{strVal}' matched no Switch port.");
                        return null;
                    }
                    return strVal;
                }
                default:
                    _FinishWithError($"BranchNode '{node.Title}' — unknown BranchMode.");
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
                    if (waitConditionLatched) {
                        waitConditionLatched = false;
                        await UniTask.NextFrame(cancellationToken: ct);
                        break;
                    }
                    var conditionTcs = new UniTaskCompletionSource();
                    waitConditionTcs = conditionTcs;
                    try {
                        await conditionTcs.Task.AttachExternalCancellation(ct);
                    } finally {
                        // 소유권 확인 없이 필드를 지우면 새 카탈로그 루프의 대기 채널을 없앨 수 있다.
                        if (ReferenceEquals(waitConditionTcs, conditionTcs)) waitConditionTcs = null;
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
            string blipToken = node.OverrideBlipToken;

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
        // 비정상 종료 경로가 state 만 바꾸고 OnCatalogExit 를 발행하지 않아,
        // 종료를 이벤트로 구독하는 UI 가 닫히지 않았다. 실패 종료를 한 곳으로 모은다.
        private void _FinishWithError(string reason) {
            HLogger.Warning($"[DialogueDirector] {reason} Finishing.");
            if (state != DialogueDirectorState.Finished) {
                state = DialogueDirectorState.Finished;
                OnCatalogExit?.Invoke(currentCatalog, ERROR_EXIT_KEY);
            }
        }

        private void _CancelCurrentCatalog(string exitKey) {
            // Finished 는 이미 종료 이벤트를 발행한 상태다 — 종전에는 여기서 한 번 더 발행되어
            // 보상 지급 같은 구독자가 두 번 실행됐다.
            if (state != DialogueDirectorState.Idle
                && state != DialogueDirectorState.Finished
                && currentCatalog != null) {
                OnCatalogExit?.Invoke(currentCatalog, exitKey);
            }
            _CancelAndDisposeCts();
            waitConditionLatched = false;
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
 * @Jason - PKH 2026.08.07 (수정) :: Hub 경로 엣지 부재 방어 + Switch 반환 키 검증 추가
 *
 * # 변경
 * - `_ResolveNextNode` Hub 경로(:250-259): `TryGetValue` 실패를 무시하던 부분을 일반 엣지
 *   경로(:264-267)와 동일하게 `_FinishWithError` 로 통일.
 * - `_ProcessBranchNode` BranchMode.Switch: `variables.TryGetString` 성공 후 반환 전
 *   `_HasHubEdge(node, strVal)` 로 실제 매칭 포트 존재 여부 검증.
 *
 * # 이유
 * - 케이스리포트 HDialogue 미검증 목록 재확인. Hub 경로만 방어가 빠져 있어 그래프 조작 시
 *   경고 없이 대화가 멎었다. Switch 는 미검증 반환값이 `_ResolveNextNode` 의 범용 진단으로
 *   흘러가 "허브 엣지 없음"으로만 보이고 "변수 값이 어떤 포트와도 안 맞음"이라는 진짜 원인이
 *   가려졌다 — ChoiceNode.FallbackChoiceKey 가 이미 쓰던 `_HasHubEdge` 재사용으로 해소.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: Auto/Skip 모드 — autoAdvanceOverride + isSkipping 추가
 *
 * # 변경
 * - `float autoAdvanceOverride = -1f` / `bool isSkipping` 필드 추가.
 * - `AutoAdvanceDelay { get; set; }` / `IsSkipping { get; set; }` 공개 프로퍼티 추가.
 * - `PlayCatalog`: `_CancelCurrentCatalog` 직후 `isSkipping = false` 리셋 (새 카탈로그마다 초기화).
 * - `_ProcessLineNode`: PlayLine 직후 `if (isSkipping) ForceSkipToEnd()`. 라인 완료 후
 *   `if (isSkipping) return` 으로 Advance 대기 전체 스킵.
 *   Advance 분기: `autoAdvanceOverride >= 0` 또는 Cutscene이면 delay 자동 진행, 아니면 기존 TCS 대기.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 2 — Auto/Skip 모드 구현.
 * - autoAdvanceOverride(-1=비활성): 런타임 플레이어 설정으로 전체 대화 자동 진행 제어.
 * - isSkipping: 카탈로그 단위 즉시 스킵. PlayCatalog 진입 시 리셋 (Stop→새카탈로그 케이스 포함).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: _BuildLine blipToken — AudioClips 시도 후 string 토큰 롤백
 *
 * # 변경
 * - _BuildLine: `AudioClips blipToken = node.OverrideBlipToken` → `string blipToken = node.OverrideBlipToken`.
 *
 * # 이유
 * - AudioClips enum은 레거시 SoundManager 전용; 새 AudioManager는 string 토큰만 수락.
 *
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
