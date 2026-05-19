#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 카탈로그 정적 검증기.
 *
 * 특징 / 지원기능 ::
 * + Validate(DialogueCatalogSO) : DialogueValidationReport 반환
 * + 강제 규칙 10종 (E001~E010) : EntryNode 수/RootNode 일치/출구 엣지/ChoiceNode/BranchNode/FallbackKey/IntRange/Switch
 * + 경고 규칙 7종 (W001~W007) : 도달 불가 노드/무한루프/빈 라인/미설정 선택지 프롬프트/portrait.*미지동사/Cinematic빈목록/Cinematic빈타겟
 *
 * 주의사항 ::
 * 순수 정적 클래스. 상태 없음. UnityEditor.EditorWindow에서 호출 예정.
 * 사이클 탐지는 DFS 기반 — 대화 그래프 규모에서 성능 이슈 없음.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;

namespace HDialogue.Editor {
    public static class DialogueCatalogValidator {
        #region Error / Warning Codes
        const string ERR_ENTRY_COUNT = "E001";
        const string ERR_ROOT_NOT_ENTRY = "E002";
        const string ERR_NO_OUTGOING = "E003";
        const string ERR_CHOICE_TOO_FEW = "E004";
        const string ERR_CHOICE_KEY_MISMATCH = "E005";
        const string ERR_BRANCH_BOOL_KEYS = "E006";
        const string ERR_FALLBACK_KEY_MISSING = "E007";
        const string ERR_BRANCH_INTRANGE_FORMAT = "E008";
        const string ERR_BRANCH_INTRANGE_OVERLAP = "E009";
        const string ERR_BRANCH_SWITCH_KEYS = "E010";

        const string WARN_UNREACHABLE = "W001";
        const string WARN_INFINITE_LOOP = "W002";
        const string WARN_EMPTY_LINE = "W003";
        const string WARN_CHOICE_NO_PROMPT = "W004";
        const string WARN_PORTRAIT_UNKNOWN_VERB = "W005";
        const string WARN_CINEMATIC_EMPTY_INSTRUCTIONS = "W006";
        const string WARN_CINEMATIC_EMPTY_TARGET = "W007";
        #endregion

        public static DialogueValidationReport Validate(DialogueCatalogSO catalog) {
            var errors = new List<DialogueValidationIssue>();
            var warnings = new List<DialogueValidationIssue>();

            if (catalog == null) {
                errors.Add(new DialogueValidationIssue(NodeUID.None, "E000", "Catalog is null."));
                return new DialogueValidationReport { Errors = errors, Warnings = warnings };
            }

            _CheckErrors(catalog, errors);
            _CheckWarnings(catalog, warnings);

            return new DialogueValidationReport { Errors = errors, Warnings = warnings };
        }

        #region Error Rules
        static void _CheckErrors(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            var entryNodes = catalog.Nodes.Values.OfType<DialogueEntryNode>().ToList();

            _CheckEntryCount(entryNodes, errors);

            DialogueEntryNode entryNode = entryNodes.Count == 1 ? entryNodes[0] : null;
            _CheckRootIsEntry(catalog, entryNode, errors);
            _CheckOutgoingEdges(catalog, errors);
            _CheckChoiceNodeEdges(catalog, errors);
            _CheckChoiceKeySync(catalog, errors);
            _CheckBranchBooleanKeys(catalog, errors);
            _CheckFallbackChoiceKey(catalog, errors);
            _CheckBranchIntRange(catalog, errors);
            _CheckBranchSwitchKeys(catalog, errors);
        }

        static void _CheckEntryCount(List<DialogueEntryNode> entryNodes, List<DialogueValidationIssue> errors) {
            if (entryNodes.Count == 1) return;
            errors.Add(new DialogueValidationIssue(
                NodeUID.None, ERR_ENTRY_COUNT,
                $"Catalog must have exactly 1 EntryNode, found {entryNodes.Count}."
            ));
        }

        static void _CheckRootIsEntry(DialogueCatalogSO catalog, DialogueEntryNode entryNode,
            List<DialogueValidationIssue> errors) {
            if (entryNode == null) return;
            if (catalog.HasRoot && catalog.RootUID == entryNode.UID) return;
            errors.Add(new DialogueValidationIssue(
                entryNode.UID, ERR_ROOT_NOT_ENTRY,
                $"EntryNode '{entryNode.Title}' is not set as RootNode (catalog.RootUID mismatch)."
            ));
        }

        static void _CheckOutgoingEdges(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (BaseNode node in catalog.Nodes.Values) {
                if (node is DialogueExitNode) continue;
                if (catalog.GetOutgoingEdges(node.UID).Any()) continue;
                errors.Add(new DialogueValidationIssue(
                    node.UID, ERR_NO_OUTGOING,
                    $"Node '{node.Title}' ({node.GetType().Name}) has no outgoing edges."
                ));
            }
        }

        static void _CheckChoiceNodeEdges(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueChoiceNode choice in catalog.Nodes.Values.OfType<DialogueChoiceNode>()) {
                int outCount = catalog.GetOutgoingEdges(choice.UID).Count();
                if (outCount >= 2) continue;
                errors.Add(new DialogueValidationIssue(
                    choice.UID, ERR_CHOICE_TOO_FEW,
                    $"ChoiceNode '{choice.Title}' needs ≥ 2 outgoing edges, found {outCount}."
                ));
            }
        }

        static void _CheckChoiceKeySync(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueChoiceNode choice in catalog.Nodes.Values.OfType<DialogueChoiceNode>()) {
                var choices = choice.Choices;
                var entries = choice.Entries;
                if (choices.Count != entries.Count) {
                    errors.Add(new DialogueValidationIssue(
                        choice.UID, ERR_CHOICE_KEY_MISMATCH,
                        $"ChoiceNode '{choice.Title}': choices.Count({choices.Count}) != port entries({entries.Count})."
                    ));
                    continue;
                }
                var choiceKeySet = new HashSet<string>(choices.Select(c => c.Key));
                var entryKeySet = new HashSet<string>(entries.Select(e => e.Key));
                foreach (string key in choiceKeySet) {
                    if (entryKeySet.Contains(key)) continue;
                    errors.Add(new DialogueValidationIssue(
                        choice.UID, ERR_CHOICE_KEY_MISMATCH,
                        $"ChoiceNode '{choice.Title}': choice key '{key}' has no matching port entry."
                    ));
                }
                foreach (string key in entryKeySet) {
                    if (choiceKeySet.Contains(key)) continue;
                    errors.Add(new DialogueValidationIssue(
                        choice.UID, ERR_CHOICE_KEY_MISMATCH,
                        $"ChoiceNode '{choice.Title}': port entry key '{key}' has no matching choice."
                    ));
                }
            }
        }

        static void _CheckBranchBooleanKeys(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueBranchNode branch in catalog.Nodes.Values.OfType<DialogueBranchNode>()) {
                if (branch.Mode != BranchMode.Boolean) continue;
                foreach (BaseNodeEdge edge in catalog.GetOutgoingEdges(branch.UID)) {
                    if (edge is not HubNodeEdge hubEdge) continue;
                    string key = hubEdge.BranchPortKey;
                    if (key == "true" || key == "false") continue;
                    errors.Add(new DialogueValidationIssue(
                        branch.UID, ERR_BRANCH_BOOL_KEYS,
                        $"BranchNode '{branch.Title}' Boolean mode has invalid exit key '{key}' (must be 'true'/'false')."
                    ));
                }
            }
        }

        private static void _CheckFallbackChoiceKey(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueChoiceNode choice in catalog.Nodes.Values.OfType<DialogueChoiceNode>()) {
                string fallback = choice.FallbackChoiceKey;
                if (string.IsNullOrEmpty(fallback)) continue;
                if (choice.Choices.Any(c => c.Key == fallback)) continue;
                errors.Add(new DialogueValidationIssue(
                    choice.UID, ERR_FALLBACK_KEY_MISSING,
                    $"ChoiceNode '{choice.Title}': FallbackChoiceKey '{fallback}' not found in choices list."
                ));
            }
        }

        private static void _CheckBranchIntRange(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueBranchNode branch in catalog.Nodes.Values.OfType<DialogueBranchNode>()) {
                if (branch.Mode != BranchMode.IntRange) continue;
                var ranges = new List<(int min, int max, string key)>();
                foreach (BaseNodeEdge edge in catalog.GetOutgoingEdges(branch.UID)) {
                    if (edge is not HubNodeEdge hubEdge) continue;
                    string key = hubEdge.BranchPortKey;
                    if (!_TryParseIntRangeKey(key, out int min, out int max)) {
                        errors.Add(new DialogueValidationIssue(
                            branch.UID, ERR_BRANCH_INTRANGE_FORMAT,
                            $"BranchNode '{branch.Title}' IntRange: invalid key format '{key}' (expected 'min_max', e.g. '0_5')."
                        ));
                        continue;
                    }
                    if (min > max) {
                        errors.Add(new DialogueValidationIssue(
                            branch.UID, ERR_BRANCH_INTRANGE_FORMAT,
                            $"BranchNode '{branch.Title}' IntRange: key '{key}' has min({min}) > max({max})."
                        ));
                        continue;
                    }
                    ranges.Add((min, max, key));
                }
                for (int k = 0; k < ranges.Count; k++) {
                    for (int m = k + 1; m < ranges.Count; m++) {
                        if (ranges[k].min > ranges[m].max || ranges[m].min > ranges[k].max) continue;
                        errors.Add(new DialogueValidationIssue(
                            branch.UID, ERR_BRANCH_INTRANGE_OVERLAP,
                            $"BranchNode '{branch.Title}' IntRange: '{ranges[k].key}' and '{ranges[m].key}' overlap."
                        ));
                    }
                }
            }
        }

        private static void _CheckBranchSwitchKeys(DialogueCatalogSO catalog, List<DialogueValidationIssue> errors) {
            foreach (DialogueBranchNode branch in catalog.Nodes.Values.OfType<DialogueBranchNode>()) {
                if (branch.Mode != BranchMode.Switch) continue;
                var seenKeys = new HashSet<string>();
                foreach (BaseNodeEdge edge in catalog.GetOutgoingEdges(branch.UID)) {
                    if (edge is not HubNodeEdge hubEdge) continue;
                    string key = hubEdge.BranchPortKey;
                    if (string.IsNullOrEmpty(key)) {
                        errors.Add(new DialogueValidationIssue(
                            branch.UID, ERR_BRANCH_SWITCH_KEYS,
                            $"BranchNode '{branch.Title}' Switch: empty port key."
                        ));
                        continue;
                    }
                    if (!seenKeys.Add(key)) {
                        errors.Add(new DialogueValidationIssue(
                            branch.UID, ERR_BRANCH_SWITCH_KEYS,
                            $"BranchNode '{branch.Title}' Switch: duplicate port key '{key}'."
                        ));
                    }
                }
            }
        }

        private static bool _TryParseIntRangeKey(string key, out int min, out int max) {
            min = 0; max = 0;
            if (string.IsNullOrEmpty(key)) return false;
            int underIdx = key.IndexOf('_');
            if (underIdx <= 0) return false;
            return int.TryParse(key.Substring(0, underIdx), out min)
                && int.TryParse(key.Substring(underIdx + 1), out max);
        }
        #endregion

        #region Warning Rules
        static void _CheckWarnings(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            DialogueEntryNode entryNode = catalog.Nodes.Values.OfType<DialogueEntryNode>().FirstOrDefault();
            _CheckUnreachableNodes(catalog, entryNode, warnings);
            _CheckCyclesWithoutWait(catalog, warnings);
            _CheckEmptyLineText(catalog, warnings);
            _CheckChoiceNoPrompt(catalog, warnings);
            _CheckPortraitEventVerbs(catalog, warnings);
            _CheckCinematicInstructions(catalog, warnings);
        }

        static void _CheckUnreachableNodes(DialogueCatalogSO catalog, DialogueEntryNode entryNode,
            List<DialogueValidationIssue> warnings) {
            if (entryNode == null) return;
            var reachable = new HashSet<NodeUID>();
            var queue = new Queue<NodeUID>();
            reachable.Add(entryNode.UID);
            queue.Enqueue(entryNode.UID);
            while (queue.Count > 0) {
                NodeUID current = queue.Dequeue();
                foreach (BaseNodeEdge edge in catalog.GetOutgoingEdges(current)) {
                    if (reachable.Add(edge.LeafUID)) queue.Enqueue(edge.LeafUID);
                }
            }
            foreach (var (uid, node) in catalog.Nodes) {
                if (reachable.Contains(uid)) continue;
                warnings.Add(new DialogueValidationIssue(
                    uid, WARN_UNREACHABLE,
                    $"Node '{node.Title}' ({node.GetType().Name}) is unreachable from EntryNode."
                ));
            }
        }

        static void _CheckCyclesWithoutWait(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            var visited = new HashSet<NodeUID>();
            var inStack = new HashSet<NodeUID>();
            var path = new List<NodeUID>();
            var reportedCycles = new HashSet<string>();
            foreach (NodeUID uid in catalog.Nodes.Keys) {
                if (!visited.Contains(uid)) {
                    _DfsCycle(catalog, uid, visited, inStack, path, reportedCycles, warnings);
                }
            }
        }

        static void _DfsCycle(DialogueCatalogSO catalog, NodeUID uid,
            HashSet<NodeUID> visited, HashSet<NodeUID> inStack, List<NodeUID> path,
            HashSet<string> reportedCycles, List<DialogueValidationIssue> warnings) {
            if (inStack.Contains(uid)) {
                int cycleStart = path.IndexOf(uid);
                if (cycleStart < 0) return;
                List<NodeUID> cycle = path.GetRange(cycleStart, path.Count - cycleStart);
                var sorted = cycle.Select(n => n.Value).OrderBy(s => s).ToList();
                string cycleKey = string.Join(",", sorted);
                if (!reportedCycles.Add(cycleKey)) return;
                bool hasWait = cycle.Any(n =>
                    catalog.Nodes.TryGetValue(n, out BaseNode bn) && bn is DialogueWaitNode
                );
                if (hasWait) return;
                string cycleStr = string.Join(" → ", cycle.Select(n =>
                    catalog.Nodes.TryGetValue(n, out BaseNode bn) ? bn.Title : n.Value[..8]
                ));
                warnings.Add(new DialogueValidationIssue(
                    uid, WARN_INFINITE_LOOP,
                    $"Cycle without WaitNode: {cycleStr}. Risk of infinite loop at runtime."
                ));
                return;
            }
            if (visited.Contains(uid)) return;
            visited.Add(uid);
            inStack.Add(uid);
            path.Add(uid);
            foreach (BaseNodeEdge edge in catalog.GetOutgoingEdges(uid)) {
                _DfsCycle(catalog, edge.LeafUID, visited, inStack, path, reportedCycles, warnings);
            }
            inStack.Remove(uid);
            path.RemoveAt(path.Count - 1);
        }

        static void _CheckEmptyLineText(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            foreach (DialogueLineNode line in catalog.Nodes.Values.OfType<DialogueLineNode>()) {
                if (!string.IsNullOrEmpty(line.LocalizationUID)) continue;
                warnings.Add(new DialogueValidationIssue(
                    line.UID, WARN_EMPTY_LINE,
                    $"LineNode '{line.Title}' has empty LocalizationUID."
                ));
            }
        }

        static void _CheckChoiceNoPrompt(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            foreach (DialogueChoiceNode choice in catalog.Nodes.Values.OfType<DialogueChoiceNode>()) {
                if (!string.IsNullOrEmpty(choice.PromptText)) continue;
                bool hasIncomingLine = catalog.GetBranchNodes(choice.UID).Any(n => n is DialogueLineNode);
                if (hasIncomingLine) continue;
                warnings.Add(new DialogueValidationIssue(
                    choice.UID, WARN_CHOICE_NO_PROMPT,
                    $"ChoiceNode '{choice.Title}' has no PromptText and is not preceded by a LineNode."
                ));
            }
        }

        static void _CheckPortraitEventVerbs(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            const string PORTRAIT_PREFIX = "portrait.";
            foreach (DialogueLineNode line in catalog.Nodes.Values.OfType<DialogueLineNode>()) {
                if (string.IsNullOrEmpty(line.LocalizationUID)) continue;
                if (!catalog.EditorTryGetLocalizedText(line.LocalizationUID, out string text)) continue;
                foreach (string eventKey in _ExtractEventKeys(text)) {
                    if (!eventKey.StartsWith(PORTRAIT_PREFIX, StringComparison.Ordinal)) continue;
                    if (_IsKnownPortraitVerb(eventKey, PORTRAIT_PREFIX.Length)) continue;
                    warnings.Add(new DialogueValidationIssue(
                        line.UID, WARN_PORTRAIT_UNKNOWN_VERB,
                        $"LineNode '{line.Title}': unknown portrait verb in '<event={eventKey}>'."
                    ));
                }
            }
        }

        static void _CheckCinematicInstructions(DialogueCatalogSO catalog, List<DialogueValidationIssue> warnings) {
            foreach (DialogueCinematicNode cinematic in catalog.Nodes.Values.OfType<DialogueCinematicNode>()) {
                if (cinematic.Instructions.Count == 0) {
                    warnings.Add(new DialogueValidationIssue(
                        cinematic.UID, WARN_CINEMATIC_EMPTY_INSTRUCTIONS,
                        $"CinematicNode '{cinematic.Title}' has no instructions."
                    ));
                    continue;
                }
                for (int k = 0; k < cinematic.Instructions.Count; k++) {
                    CinematicInstruction ins = cinematic.Instructions[k];
                    if (!string.IsNullOrEmpty(ins.TargetCharacterKey)) continue;
                    warnings.Add(new DialogueValidationIssue(
                        cinematic.UID, WARN_CINEMATIC_EMPTY_TARGET,
                        $"CinematicNode '{cinematic.Title}' instructions[{k}] ({ins.Verb}): empty targetCharacterKey."
                    ));
                }
            }
        }

        static bool _IsKnownPortraitVerb(string eventKey, int prefixLen) {
            string body = eventKey.Substring(prefixLen);
            int end = body.Length;
            int atIdx = body.IndexOf('@');
            int colonIdx = body.IndexOf(':');
            if (atIdx >= 0 && atIdx < end) end = atIdx;
            if (colonIdx >= 0 && colonIdx < end) end = colonIdx;
            return Enum.TryParse<PortraitVerb>(body.Substring(0, end), ignoreCase: true, out _);
        }

        static IEnumerable<string> _ExtractEventKeys(string text) {
            const string OPEN = "<event=";
            int start = 0;
            while (start < text.Length) {
                int idx = text.IndexOf(OPEN, start, StringComparison.Ordinal);
                if (idx < 0) break;
                int contentStart = idx + OPEN.Length;
                int closeIdx = text.IndexOf('>', contentStart);
                if (closeIdx < 0) break;
                yield return text.Substring(contentStart, closeIdx - contentStart);
                start = closeIdx + 1;
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (수정) :: W005~W007 — portrait.* 미지 동사 / CinematicNode 빈 목록·빈 타겟 검증 추가
 *
 * # 변경
 * - using System 추가 (Enum.TryParse<PortraitVerb> 사용).
 * - W005 _CheckPortraitEventVerbs: LineNode 로컬라이즈 텍스트에서 <event=portrait.*> 추출.
 *   _ExtractEventKeys(string text): "<event=" ~ ">" 단순 스캔, yield return.
 *   _IsKnownPortraitVerb: @ / : 이전까지 동사 파트 추출 후 Enum.TryParse<PortraitVerb>.
 * - W006 _CheckCinematicInstructions: instructions.Count == 0 → Warning.
 * - W007 _CheckCinematicInstructions(겸용): 각 instruction targetCharacterKey 빈 문자열 → Warning.
 *   (W006·W007 동일 이터레이터 내 처리 — _CheckCinematicInstructions 단일 메서드)
 * - 헤더 경고 규칙 4종 → 7종 업데이트.
 *
 * # 이유
 * - HCUP-2.4.0 Phase 3-B.
 * - W005: PortraitEventParser.TryParse 대신 인라인 Enum.TryParse — TryParse 호출 시
 *   HLogger.Warning side-effect가 에디터 콘솔을 오염시키므로 검증기에서 재사용 금지.
 * - W006/W007: CinematicNode 노드 뷰에서 이미 "?" 표시로 경고하지만 Validator로
 *   공식 Warning 코드 부여해 DialogueCatalogValidatorWindow에서 일괄 확인 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: _CheckEmptyLineText — RawText → LocalizationUID
 *
 * # 변경
 * - `line.RawText` → `line.LocalizationUID`.
 * - 경고 메시지: "has empty RawText" → "has empty LocalizationUID".
 *
 * # 이유
 * - DialogueLineNode.rawText 제거(→ localizationUID) 에 따른 컴파일 에러 수정.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: E007~E010 — FallbackKey / IntRange / Switch 검증 추가
 *
 * # 변경
 * - E007 _CheckFallbackChoiceKey: FallbackChoiceKey가 비어 있지 않으면 choices 목록에 실존 여부 검증.
 * - E008 _CheckBranchIntRange (형식): IntRange 모드 포트 키가 "min_max" 형식인지 검증.
 *   _TryParseIntRangeKey 헬퍼 추가 — IndexOf('_')로 분리 후 int.TryParse, min > max도 잡음.
 * - E009 _CheckBranchIntRange (겹침): 유효 범위 간 O(n²) 겹침 검사. 포트 수가 적으므로 허용.
 * - E010 _CheckBranchSwitchKeys: Switch 모드 포트 키 빈 문자열·중복 검증.
 * - private static 접근제어자 명시 (새 메서드만 — 기존 메서드 변경 금지).
 *
 * # 이유
 * - AgentReview Warning #5 (2026-05-17 19:13:03).
 * - IntRange/Switch 분기는 에디터 단계에서 키 형식을 검증하지 않으면 런타임에서
 *   매칭 실패 → 조용히 Finished 전이. 사전 차단이 필수.
 * - FallbackChoiceKey가 choices에 없으면 director가 유효하지 않은 분기로 진입.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueCatalogValidator 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 3 — 카탈로그 사전 검증으로 런타임 에러 사전 차단
 *
 * # 규칙 목록
 * 강제(Error): E001 EntryNode 수, E002 RootNode 일치, E003 출구 엣지,
 *              E004 ChoiceNode 엣지 수, E005 Choice↔Port 키 동기화, E006 BranchBoolean 키
 * 경고(Warning): W001 도달 불가 노드(BFS), W002 WaitNode 없는 사이클(DFS),
 *               W003 빈 LineNode, W004 PromptText 미설정 ChoiceNode
 *
 * # 사이클 탐지 알고리즘
 * - DFS + inStack HashSet (화이트/그레이/블랙 착색 패턴)
 * - 그레이 노드 재진입 시 사이클 경로 추출 → WaitNode 포함 여부 확인
 * - 중복 사이클은 정렬 후 string join으로 canonical 키 생성해 dedup
 *
 * =============================================================================
 */
#endif
