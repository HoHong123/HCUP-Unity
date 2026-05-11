---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphCanvas.cs
script_name: HGraphCanvas
latest_log_id: LOG-20260512-2
total_entries: 21
created: 2026-05-12
updated: 2026-05-12
---

# HGraphCanvas Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphCanvas.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.12 PurgeNullNodes 호출 + _isPopulating 재진입 가드 [LOG-20260512-2]

# 변경
- _isPopulating 필드 추가 (bool, Fields region).
- _PopulateInternal: _HideEmptyStateHint 직후 _isPopulating = true + PurgeNullNodes 호출.
  MarkDirtyRepaint 직후 _isPopulating = false.
- _OnCatalogMutated: if (_isPopulating) return 가드 추가.

# 이유
- sub-asset 외부 삭제로 발생한 ghost UID 항목 자동 정리 (NodeCatalogAuthor.PurgeNullNodes).
- PurgeNullNodes 내부 AssetDatabase.SaveAssets 가 ObjectChangeWatcher 를 동기 발화하면
  _OnCatalogMutated → _PopulateInternal 재진입 위험 → _isPopulating 가드로 차단.

=============================================================================
@Jason - PKH 2026.05.12 Show Grid 미적용 버그픽스 [LOG-20260512-1]

# 변경
- _OnSnapSettingsChanged: 시그니처 void() → void(bool showGrid) — SnapSettingsChanged<bool> 수신.
- _OnSnapSettingsChanged: gridBackground.visible = ... → gridBackground.style.display = ... Flex/None.
- 생성자: gridBackground.visible = ... → gridBackground.style.display = ... 동일 패턴.

# 이유
- 이중 원인 차단.
  (1) ApplyModifiedProperties 이후 ScriptableSingleton C# 필드 갱신 지연 가능 —
      NodeWindowSettingsProvider 쪽에서 SerializedProperty.boolValue 를 캡처해 Invoke 인자로 전달,
      콜백이 NodeSnapSettings.instance.ShowGrid 를 재조회하지 않도록 구조 변경.
  (2) GraphView 렌더 패스에서 visibility:hidden 이 GridBackground generateVisualContent 를
      막지 못하는 케이스 존재 — style.display=None 은 렌더 트리에서 요소 자체를 제거하므로 더 확실.

=============================================================================
@Jason - PKH 2026.05.11 Phase 5 — 메뉴바 단축키 + Single-Selection 헬퍼 [LOG-20260511-5]

변경 / _OnKeyDown actionKey 분기에 Home / Alpha0 case 추가.
       GetSingleSelectedHGraphNode() — selection 에서 HGraphNode 정확히 1 개 반환 헬퍼.
       SetSelectedAsRoot() — 메뉴바 진입점. selection 기반.
       SetSelectedAsRoot(NodeUID uid) — 우클릭 메뉴 위임 진입점. selection 무관, 명시 UID.
이유 / Ctrl+Home / Ctrl+0 메뉴바 단축키 라벨과 실제 기능 일치.
       SetSelectedAsRoot 두 오버로드 분리 이유:
         우클릭 시 GraphView 가 selection 갱신 안 하므로, 우클릭 노드 자체의 UID 를
         직접 전달해야 안전. selection 기반 오버로드는 메뉴바 전용.
결과 / HGraphNode._OnContextSetAsRoot → canvas.SetSelectedAsRoot(UID) 위임으로 단순화.
       메뉴바 + 우클릭 메뉴 양쪽이 같은 Author.SetRoot 진입점 공유 (DRY).
주의 / Ctrl+0 글로벌 핫키 충돌 가능성 — element-scoped _OnKeyDown 으로 canvas focus 시만 발화.
       Ctrl+Home — Mac 에서는 Cmd+Home, 별도 글로벌 매핑 없음 확인됨.

=============================================================================
@Jason - PKH 2026.05.11 — ITransform Obsolete 수정 (viewTransform.position/scale → resolvedStyle) [LOG-20260511-4]

# 변경
- GetViewportCenterWorld(): viewTransform.position → contentViewContainer.resolvedStyle.translate.
  viewTransform.scale.x → contentViewContainer.resolvedStyle.scale.value.x.
- CenterViewportOn(): 동일 패턴. UpdateViewTransform 의 scale 인자도 resolvedStyle.scale.value.

# 이유
- Unity 6 에서 ITransform.position / ITransform.scale getter 가 Obsolete 처리됨.
  "읽기 → resolvedStyle, 쓰기 → UpdateViewTransform 유지" 권장 패턴 적용.
  contentViewContainer.transform = viewTransform 이므로 읽기 값 동일.

=============================================================================
@Jason - PKH 2026.05.11 — GridBackground StretchToParentSize() → 인라인 스타일 교체 [LOG-20260511-3]

# 변경
- gridBackground.StretchToParentSize() → style.position/left/top/right/bottom 4줄 인라인.
  동작 동일 (position:absolute + 상하좌우 0).

# 이유
- StretchToParentSize() 가 Unity 6000.3.11f1 에서 Obsolete 경고 발생.
  UIElements 인라인 스타일로 직접 명시해 경고 제거.

=============================================================================
@Jason - PKH 2026.05.11 Phase 4 — 타이틀 검색 (SearchNodes / AdvanceSearch / ClearSearch) [LOG-20260511-2]

# 변경
- CSS_SEARCH_ACTIVE 상수 추가 ("hgraph-node--search-active").
- _searchQuery / _searchResults / _searchIndex 필드 추가.
- Search 영역 신설: SearchNodes / AdvanceSearch / ClearSearch / _ClearSearchHighlights / _ApplySearchHighlight.
- _PopulateInternal 첫 줄 ClearSearch() 호출 추가.

# 이유
- SearchNodes: 쿼리 변경 시마다 graphElements 순회 → Title 포함 검색 → index 0. value changed 경로.
- AdvanceSearch: 동일 결과 리스트에서 index % count 순환. Enter 경로.
- ClearSearch: repopulate 전 stale HGraphNode ref 조기 해제. 카탈로그 전환·ESC 공유 진입점.
- _ApplySearchHighlight: CenterViewportOn(target.GetPosition().position) — GoToRoot / CenterOnNode 와 동일 좌표계.

=============================================================================
@Jason - PKH 2026.05.11 — _PopulateInternal 끝 RefreshPortLabels 일괄 호출 [LOG-20260511-1]

# 변경
- edges 루프 종료 후 nodeLookup 전체 순회 → view.RefreshPortLabels() 호출.
  Port.connections 가 Connect() 시점 즉시 반영되므로 카운트 정확.

# 이유
- 포트 라벨에 연결 수 표시 ("Input (N)" / "Output (N)" / "Key (N)") — 사용자 시각 피드백.
- 모든 엣지 변경은 CatalogMutated → repopulate 경로로 흐르므로 본 한 지점만 호출.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — HubNode 지원 + CatalogNode 단순화 [LOG-20260510-5]

# 변경
- _PopulateInternal 노드 생성 분기: data switch 로 HubNode → HGraphHubNode 추가.
- _PopulateInternal HubNode 포트 사전 생성: HGraphHubNode.EnsureOutputPorts(hub.PortCount).
  CatalogNode 관련 catalogIncoming / catalogOutgoing 집계 로직 전면 제거.
- _PopulateInternal 엣지 연결: HubNodeEdge 검출 → GetOutputPortByKey(portKey) 로 정확한 포트 조회.
  일반 노드 / CatalogNode 는 단일 OutputPort.
- _OnGraphViewChanged: HubNode output port 드래그 시 GetOutputPortKey → ConnectHubEdge.
  CatalogNode AddSpareOutputPort 호출 제거 (CatalogNode 단일 output port 로 복귀).

# 이유
- CatalogNode 는 외부 카탈로그 연결 표시용 단순 노드 (1 in + 1 out). 다중 포트 역할 폐기.
- 다중 포트 라우팅은 HubNode 전담. 역할 단일화로 버그 원인(incoming 집계 오류) 구조적 제거.
- HubNodeEdge.BranchPortKey 직렬화로 repopulate 시 key → Port 결정론적 매핑.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — CatalogNode 동적 포트 버그픽스 (incoming 연결 카운팅) [LOG-20260510-4]

# 변경
- _PopulateInternal: catalogOutCount(outgoing 전용) → catalogIncoming + catalogOutgoing 두 dict 로 분리.
  EnsureOutputPorts(inc + out + 1) 로 들어오는/나가는 연결 합산 포트 생성.
  엣지 연결 루프: CatalogNode 가 LeafUID 인 경우 출력 포트 UI 에 "← branch 이름" 레이블 반영 후
  GraphView 엣지는 InputPort 에 연결. CatalogNode 가 BranchUID 인 경우 incOffset+outIdx 로 포트 인덱스
  결정 + "→ leaf 이름" 레이블.

# 이유
- 버그: 사용자가 CatalogNode 에 연결선을 드래그(CatalogNode = LeafUID)할 때 outgoing 카운터는
  증가하지 않아 포트가 1개(스페어)에서 증가하지 않음. incoming 집계가 누락된 원인.
- 포트 UI 배치 암묵 규칙: [0..inc-1] = 들어오는 연결 레이블 전용, [inc..inc+out-1] = 나가는 연결.
  BaseNodeEdge 에 portIndex 필드 추가 없이 repopulate 시 결정론적 매핑 유지.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — CatalogNode 동적 포트 지원 [LOG-20260510-3]

# 변경
- _PopulateInternal: CatalogNode 별 outgoing 엣지 수 집계 → EnsureOutputPorts(N+1) 사전 생성.
  portIndexOf dict 로 CatalogNode 에 대한 엣지-포트 인덱스 카운터 관리.
  GetOutputPort(index) + UpdateOutputPortLabel(index, title) 로 각 포트에 레이블 적용.
- _OnGraphViewChanged: CatalogNode 스페어 포트 소진 시 AddSpareOutputPort() 추가 (repopulate 전 임시).
  ConnectEdge 가 repopulate 를 발동하므로 최종 포트 배치는 repopulate 담당.

# 이유
- "포트 인덱스 = catalog.Edges 에서 이 노드가 브랜치인 엣지 순서" 암묵 정의.
  BaseNodeEdge 에 branchPortIndex 필드 추가 없이 결정론적 매핑 가능.
- 모든 수정은 CatalogMutated → repopulate 를 거치므로 포트 상태는 항상 repopulate 가 단일 소스.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3 — CatalogNode 지원 (Populate 분기 + 스위치 이벤트) [LOG-20260510-2]

# 변경
- _PopulateInternal: data is CatalogNode 분기 추가 → HGraphCatalogNode 생성.
- CatalogSwitchRequested 이벤트 + RequestCatalogSwitch 메서드 추가.
  HGraphCatalogNode.OnHeaderDoubleClick → canvas.RequestCatalogSwitch → HGraphWindow._BindCatalog.
- ToGraphPosition(Vector2): canvas 로컬 → 그래프 월드 좌표 변환 헬퍼.
  HGraphWindow 드래그드롭 드롭 위치 변환용 (pan/zoom 보정 포함).
- 빈 캔버스 힌트 텍스트 갱신: "Open Catalog button" → "toolbar catalog field" (ObjectField 전환 반영).

# 이유
- nodeLookup(Dictionary<NodeUID, HGraphNode>)은 변경 불필요 — HGraphCatalogNode is-a HGraphNode.
- contentViewContainer.WorldToLocal(LocalToWorld(pos)) = 올바른 pan/zoom 보정 경로.

=============================================================================
@Jason - PKH 2026.05.10 Phase 2 — 연결선 시스템 (Port + HGraphEdge + GetCompatiblePorts) [LOG-20260510-1]

# 변경
- edgeLookup: (BranchUID, LeafUID) → HGraphEdge 역매핑 추가.
- GetCompatiblePorts override: 다른 노드 + 반대 방향 포트 조합만 허용 (self-loop 차단).
- _ClearAllNodes → _ClearAll: 엣지 먼저 제거 후 노드 제거 (포트 참조 안전 정리).
- _PopulateInternal: 노드 추가 후 엣지 populate (HGraphEdge 생성 + 포트 Connect).
- _OnGraphViewChanged: edgesToCreate 처리 — ConnectEdge<SimpleNodeEdge> 호출 후
  edgesToCreate.Clear() (GraphView 자체 시각 추가 차단, repopulate 가 HGraphEdge 생성).
- _DeleteSelectedEdges: selection 에서 HGraphEdge 추려 DisconnectEdge 일괄 호출.
- Delete 키: DeleteNodes + _DeleteSelectedEdges 동시 처리.
- CenterOnNode(uid): nodeLookup 조회 후 CenterViewportOn 진입점.
- HighlightNodesByEdge: 브랜치 + 리프 노드에 "hgraph-node--edge-highlight" CSS 토글.
- _CalculateCatalogHash: EdgeCount 포함 (엣지 변경 polling 감지).

# 이유
- edgesToCreate.Clear() 필수: ConnectEdge 가 CatalogMutated → _RepopulateNoViewportReset 를
  동기 발동. repopulate 가 HGraphEdge 를 이미 추가했으므로 GraphView 자체 추가 차단 않으면
  시각 엣지 중복 발생.
- _DeleteSelectedEdges 직접 구현 필요: HGraphEdge.Capabilities.Deletable 비활성이므로
  graphViewChanged.elementsToRemove 경로로 엣지 삭제 이벤트 미발송.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — _PopulateInternal + GoToRoot 이관 (catalog → BaseNode) [LOG-20260509-3]

# 변경
- _PopulateInternal: catalog.EditorNodeLayouts / FoldoutOpen / OpenSizes TryGetValue
  → data.EditorPosition / data.EditorFoldoutOpen / data.EditorOpenSize 직접 읽기
- GoToRoot: catalog.EditorNodeLayouts.TryGetValue(root, out saved)
  → catalog.Nodes.TryGetValue(root, out rootNode) + rootNode.EditorPosition
  (컴파일 오류 수정 — EditorNodeLayouts 프로퍼티가 NodeCatalogSO 에서 제거됨)

# 이유
- NodeCatalogSO Phase 1-F 에서 editor HDictionary 3개 전부 제거에 따른 소비처 갱신.
- GoToRoot 는 제거된 프로퍼티를 직접 호출해 컴파일 오류를 유발하므로 즉시 수정 필수.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — Close All + Undo/Redo 인프라 [LOG-20260509-2]

# 변경
- Undo.undoRedoPerformed += _OnUndoRedo (constructor) / -= (DetachFromPanelEvent)
- _OnUndoRedo: NotifyExternalMutation → canvas repopulate
- _OnKeyDown: Ctrl+Z (Undo) / Ctrl+Shift+Z + Ctrl+Y (Redo) 케이스 추가
  GraphView 포커스 시 UIElements 가 Unity 전역 단축키 차단 → 직접 호출 필요
- CloseAllFoldouts(): graphElements 순회 + CloseIfExpanded 일괄 호출

=============================================================================
@Jason - PKH 2026.05.09 hash 계산 int .Value → .GetHashCode() 전환 [LOG-20260509-1]

# 변경
- _CalculateCatalogHash: RootUID.Value + pair.Key.Value (int 산술)
  → RootUID.GetHashCode() + pair.Key.GetHashCode() (string 호환)
- NodeUID.Value 반환형 int → string 으로 바뀜에 따른 컴파일 오류 수정

=============================================================================
Dev Log - Phase 1-E 추가 (2026-05-08) [LOG-20260508-2]

- gridBackground field 끌어올림 (Phase 1-E P1E-ε):
  + 기존 로컬 변수를 fields region 의 instance field 로 변경.
  + ShowGrid 동기화 위해 인스턴스 보존 필요 — visible 속성 동적 갱신.
  + 생성자에서 초기 ShowGrid 동기화 (NodeSnapSettings.instance.ShowGrid).

- SnapSettingsChanged 구독 (Phase 1-E P1E-ε):
  + NodeWindowSettingsProvider 의 static event 구독으로 settings 변경 시 GridBackground.visible
    + MarkDirtyRepaint 호출.
  + DetachFromPanelEvent 에서 unsubscribe — 메모리 누수 방지.

- Ctrl+A / Cmd+A 단축키 (Phase 1-E P1E-α + P1E-5):
  + _OnKeyDown 의 actionKey 분기에 KeyCode.A 추가 — Phase 1-D 의 C/X/V/D 와 같은 path.
  + evt.StopPropagation() 호출 — Unity Edit > Select All 충돌 방지 (P1E-5).

- SelectAllNodes (Phase 1-E Q7 B):
  + graphElements 순회 + is HGraphNode 검사 (시각 진실성).
  + ClearSelection + AddToSelection 으로 selection state 재구성.
  + 비용: O(M), M ≈ N + 3~4. N=100 시 ~1μs (Q7 비용 분석 참조).
  + Phase 3 DepthTree 도입 시 "활성 layer 만" 의미 자동 정합 (graphElements = 화면 표시 요소).

- BuildContextualMenu "모두 선택" 항목 (Phase 1-E P1E-8):
  + 빈 캔버스 우클릭 시점만 표시 — HGraphNode 위 우클릭은 노드 메뉴 (Phase 1-D) 6 항목 유지.
  + graphElements foreach + is + break — first-match short-circuit.

=============================================================================
Dev Log - Phase 1-D 추가 (2026-05-07/08) [LOG-20260508-1]

- BuildContextualMenu override (base 호출 생략):
  + GraphView 기본 selection 메뉴 (Cut/Copy/Paste/Duplicate/Delete) 자동 추가 차단.
  + HGraphNode.capabilities 차단 (Copiable | Deletable) 만으로는 GraphView 측 메뉴가 살아남음.
    UI Toolkit 의 ContextualMenu propagation 이 leaf + parent 양쪽 BuildContextualMenu 호출.
  + 두 layer 양쪽 차단 필요 — leaf (HGraphNode capabilities) + parent (본 override) 모두.
  + Phase 1-D Stage 2 검증 도중 사용자 보고 ("Duplicate 중복 유지") 로 발견된 함정.

- Paste 메뉴 추가 (Phase 1-D Cut/Paste 확장 - 2026-05-08):
  + 빈 캔버스 우클릭 시 Paste 만 표시. 다른 GraphView 자동 항목은 base 미호출로 차단 유지.
  + 노드 위 우클릭 시는 HGraphNode 가 Paste 추가 — evt.target 의 ancestor chain 검사로 중복 회피.
  + Paste 활성/비활성 = HGraphClipboard.IsValid(systemCopyBuffer) — 우리 형식 magic 검사.

- Clipboard Actions helper + 키보드 단축키 (Phase 1-D 단축키 - 사용자 결정 2026-05-08):
  + Copy/Cut/Paste/Duplicate/Delete 5 helper (CopyNodes / CutNodes / PasteFromClipboard /
    DuplicateNodes / DeleteNodes) internal — 단축키 + HGraphNode 우클릭 메뉴가 공유 진입점.
  + DRY — 메뉴 핸들러 (HGraphNode._OnContext*) 모두 본 helper 호출로 단순화.
  + KeyDownEvent 핸들러 (_OnKeyDown) — actionKey + (C/X/V/D) + 단독 Delete 키.
  + actionKey = platform 추상화 — Mac Cmd / 그 외 Ctrl 자동 매핑.
  + capabilities 차단 (Copiable | Deletable) 과 별개 event path — 우리 핸들러는 capabilities 무관 동작.
  + Paste 단축키는 selection 무관, Copy/Cut/Duplicate/Delete 단축키는 selection 기반 (0 이면 무반응).
  + Delete 는 modifier 없는 단독 키 — actionKey 분기 외부에서 처리.
  + macOS 의 main "delete" 키는 KeyCode.Backspace (Forward Delete = KeyCode.Delete) — 사용자
    spec 그대로 Delete 만 처리. Backspace 추가 의향 시 한 줄 추가로 대응 가능.
  + KeyDownEvent 의 element callback 은 panel detach 시 자동 정리 — 명시 unregister 불필요.

=============================================================================
Dev Log - Phase 1-C 확장 (2026-05-07) [LOG-20260507-2]

- CenterViewportOn(worldPos) : 지정 graph 좌표를 viewport 중앙으로 pan 이동.
  + GetViewportCenterWorld 의 역수 (position = screenCenter - graphPos * scale).
  + viewTransform.scale 보존 — 줌 상태 유지하며 pan 만 갱신해 사용자 컨텍스트 깨지 않음.
- GoToRoot() : currentCatalog.RootUID 의 EditorNodeLayouts 위치로 CenterViewportOn 호출.
  + 루트 미보유 시 false — 호출자(HGraphWindow) 가 Warning 으로 사용자 피드백.
  + layout 미보유 fallback (0,0) — Author.CreateNode 가 자동 layout 부여하므로
    실 발생 케이스는 데이터 호환성 이슈 한정.
  + Phase 5 메뉴바 이관 시 본 메서드 시그니처 그대로, 호출 진입점만 메뉴로 교체.
- GetSelectedNodes() : selection 에서 HGraphNode 만 추려 IReadOnlyList 반환.
  + 목적 = 어댑터 경계 (P1-3) 보존. Window 가 ISelectable (Experimental.GraphView 타입)
    을 직접 다루지 않도록 한 겹의 헬퍼로 캡슐화.
  + Phase 1-D 우클릭 메뉴, Phase 1-E 다중 선택 일괄 처리에서도 같은 헬퍼 재사용 예정.

=============================================================================
Dev Log - Phase 1-B 확장 (2026-05-07) [LOG-20260507-1]

- _PopulateInternal: 노드 생성 시 catalog 의 두 보조 맵 적용 (P1B-b, c).
  + EditorNodeFoldoutOpen 에서 isExpanded 읽어 ApplyEditorState 호출.
  + EditorNodeOpenSizes 에서 openSize 읽어 ApplyEditorState 호출.
  + 두 맵 미보유 시 (false, Vector2.zero) fallback. CreateNode 자동 초기화 미적용 정책과 정합.
- HGraphNode 의 두 이벤트 구독:
  + FoldoutChanged → Author.SetFoldoutOpen(catalog, uid, open) 호출.
  + OpenSizeChanged → Author.SetOpenSize(catalog, uid, size) 호출.
  + Phase 1-A 의 graphViewChanged 가 layout 갱신을 단일 진입점으로 흡수한 것과 같은 분리.
    Foldout/OpenSize 갱신도 이벤트 구독을 단일 진입점으로 통합.
  + closure 캡처 안전성을 위해 foreach 안에서 NodeUID uid = pair.Key 로컬 변수 명시.
- hash polling 영향:
  + Author.SetFoldoutOpen / SetOpenSize 는 _NotifyMutated 호출 없음 (P1B-i 고빈도 분류).
  + Foldout 토글은 본인 노드만 갱신하므로 hash polling 진입 불필요.
  + Inspector 에서 editorNodeFoldoutOpen 직접 수정은 ObjectChangeWatcher 가 처리.

=============================================================================
Dev Log - Phase 1-A 확장 (2026-04-24) [LOG-20260424-1]

- Bind(catalog) / Unbind(): catalog 주입 + Populate 트리거. 같은 catalog 재진입 시 조기 return.
- _Populate(): 하이브리드 전략 - 전체 재구성 (기존 HGraphNode 전부 제거 후 catalog.Nodes 순회 생성).
  + Bind 는 드문 이벤트 (Selection 변경/드래그드롭/Open 버튼) 라 매번 전체 재구성해도 체감 지연 0.
  + 드래그 이동 같은 고빈도 변경은 graphViewChanged 훅에서 위치만 증분 반영.
- _OnGraphViewChanged: GraphView.graphViewChanged 에 등록된 콜백.
  change.movedElements 순회하며 HGraphNode 의 새 위치를 Author.SetLayout 으로 catalog 에 반영.
- _emptyStateHint: catalog 미바인드 상태에서 중앙에 "Drop a Node Catalog here..." 안내 표시.
  pickingMode=Ignore 로 드래그드롭/클릭 이벤트를 하단 GraphView 로 pass-through.
- _nodeLookup: UID -> HGraphNode 역매핑. Phase 1-D 선택 하이라이트/Phase 1-G Floating GUI
  에서 "UID 로 VisualElement 찾기" 경로에 활용.

[Stage 4 검증 보정 - 2026-04-25]
- 자동 배치 분산: 스펙 P1-e 의 (0, 0) 고정에서 (autoIndex * 220, 0) 분산으로 변경.
  + 다중 노드를 한 번에 Bind 하면 모두 같은 좌표에 겹쳐 사용자가 식별 못 하던 문제 해소.
  + 220 = USS min-width 180 + 여백 40. 노드끼리 안 겹치는 최소 간격.
  + saved layout 이 있는 노드는 그대로 사용. 신규 노드만 분산 인덱스 증가.
- viewport 원점 리셋: Populate 끝에 UpdateViewTransform(Vector3.zero, Vector3.one) 호출.
  + 새 catalog 를 Bind 한 직후 viewport 가 어디인지 모호한 상태를 차단.
  + 자동 배치 노드들이 (0~N*220, 0) 영역에 위치하므로 원점 viewport 에서 보임.
  + Phase 1-C "Go To Root" 가 들어오면 더 정교한 framing 으로 대체될 수 있음.

[StretchToParentSize 제거 이유]
- L1 에서는 생성자 마지막에 this.StretchToParentSize() 호출 (Canvas 가 혼자 root 를 채움).
- Phase 1-A 에서 HGraphWindow 에 Toolbar 가 추가되어 root 가 Column flex 레이아웃이 됨.
- StretchToParentSize() = position:absolute + left/right/top/bottom:0 → flex 레이아웃 무시, root 전체 덮음.
- 결과: Toolbar 가 Canvas 아래 숨어 보이지 않음.
- 해결: StretchToParentSize() 제거. HGraphWindow 가 canvas.style.flexGrow = 1 로 영역 할당.
- 내부 GridBackground 는 style.position/left/top/right/bottom 인라인으로 canvas 내부를 채움.
  (StretchToParentSize() Unity 6 Obsolete → 2026.05.11 인라인 스타일로 교체)

=============================================================================
2026-04-21 · USS 로드 전략 메모 [LOG-20260421-1]

[현재 방식] AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}")
             · 이름 기반 검색. 경로/리네임/UPM 이전에 전부 생존.
             · L1 베이스 수준에서 충분히 견고함.

[추후 전환 요청] Option 3 — 고정 GUID 상수 방식
             · 왜 전환 필요:
                1. HWindows 내 USS 자산이 2개 이상으로 늘어날 때 동명 충돌 방지 필요
                2. 자산 참조 계약을 엄격화(리뷰 게이트, 계약적 참조)해야 할 때
                3. 프로덕션 품질 수준에서 Unity 자산 시스템과 정합(GUID는 일등 시민)
             · 전환 절차:
                (a) HGraphWindow.uss.meta 에서 guid 값 확인
                (b) private const string USS_GUID = "<해당 guid>"; 로 교체
                (c) _LoadStyleSheet 내부 FindAssets 호출을
                    AssetDatabase.GUIDToAssetPath(USS_GUID) 로 치환
                (d) USS_ASSET_NAME 상수 제거
             · 장점: 동명 자산 애매성 0, 리네임·이동 완전 무관, 계약 명시적.
             · 단점: .meta 재생성 등 드문 상황에서 GUID 수동 업데이트 필요.

=============================================================================
