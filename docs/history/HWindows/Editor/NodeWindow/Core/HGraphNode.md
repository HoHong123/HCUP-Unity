---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphNode.cs
script_name: HGraphNode
latest_log_id: LOG-20260511-3
total_entries: 10
created: 2026-05-12
updated: 2026-05-12
---

# HGraphNode Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphNode.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.11 — 리사이즈 시스템 전면 제거 (최후순위 이월) [LOG-20260511-3]

# 변경
- MIN_RESIZE_WIDTH / MIN_RESIZE_HEIGHT 상수 제거.
- resizeHandle / openSize / isResizing / resizeStartMousePos / resizeStartSize 필드 제거.
- ResizeHandle / OpenSize 프로퍼티 제거.
- OpenSizeChanged 이벤트 제거.
- NotifyResizeFinished / _BuildResizeHandle / _ApplyOpenSize / _ApplyResizeHandleVisibility
  / _OnResizeHandleMouseDown/Move/Up / _GetGraphViewScale 메서드 제거.
- _ToggleExpanded / CloseIfExpanded / ApplyEditorState: openSize 파라미터·호출 제거.
- ApplyEditorState 시그니처 (bool isExpanded) 로 단순화.

# 이유
- GraphView.Node 의 contentContainer/mainContainer 레이아웃 제약(flex-basis:auto)으로
  핸들 위치·FoldOut 이벤트 차단·크기 복원 등 3중 충돌 발생. 단기 해결 복잡도 > 기능 가치.
- 기능 상세 및 시도 이력은 이 엔트리 아래 세 개 이전 엔트리 (삭제됨) 참고.
- 이월 항목: 메모리 project_deferred_features.md 에 등록.

=============================================================================
@Jason - PKH 2026.05.11 — RefreshPortLabels() virtual 추가 [LOG-20260511-2]

# 변경
- public virtual RefreshPortLabels(): inputPort/outputPort 의 portName 을
  "Input (N)" / "Output (N)" 형식으로 갱신. N = port.connections.Count().

# 이유
- 포트 라벨에 연결 엣지 수를 시각 노출 — 노드 연결 상태 한눈에 파악.
- virtual — HGraphHubNode 가 키별 카운트 형식으로 override.
- HGraphCanvas._PopulateInternal 의 edges 루프 종료 직후 단일 호출 지점.

=============================================================================
@Jason - PKH 2026.05.11 — 포트 portName 기본값 부여 (Input/Output) [LOG-20260511-1]

# 변경
- _BuildPorts(): inputPort.portName = "Input", outputPort.portName = "Output".
  기존 "" 빈 문자열은 USS 라벨 노출 후 빈 공간으로 남아 도트 외관이 어색해짐.

# 이유
- HGraphNode.uss 의 `.port > #type { display: none }` 전역 규칙 제거와 짝.
  SimpleNode / CatalogNode 가 본 베이스를 그대로 사용하므로 양쪽이 동시에 라벨을 표시.

=============================================================================
@Jason - PKH 2026.05.10 — 노드 선택 시 Inspector 동기화 [LOG-20260510-5]

# 변경
- OnSelected(): Selection.activeObject = dataNode 추가.
  노드 클릭 시 대응 BaseNode ScriptableObject 가 Unity Inspector 에 표시됨.

# 이유
- GraphView 선택 상태와 Unity 전역 Selection 이 분리되어 있어 Inspector 에 아무것도 표시되지 않았음.
  단 한 줄로 Inspector 동기화를 달성할 수 있는 가장 직접적인 진입점.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — 동적 출력 포트 확장점 (inputPort/outputPort/portRow protected, _BuildPorts virtual) [LOG-20260510-4]

# 변경
- inputPort / outputPort / portRow : private → protected (파생 클래스 직접 접근용).
- _BuildPorts() : private → protected virtual → HGraphCatalogNode 가 override 가능.
- portRow : _BuildPorts 로컬 변수 → 클래스 필드 (파생 클래스에서 Add 가능).
- GetOutputPort(int index) public virtual 추가 : 기본 노드는 index 무관 outputPort 반환.
  CatalogNode 가 override 해 인덱스별 동적 포트 반환.

# 이유
- CatalogNode 는 출구 포트 N개(연결선 수 + 1 스페어)를 동적으로 생성.
  파생 클래스가 _BuildPorts 를 override 해 portRow 에 포트를 추가/제거해야 함.
- HGraphCanvas 엣지 연결 시 GetOutputPort(index) 로 포트를 조회 → 단일 노드/다중 포트 노드 통합.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3 — 파생 타입 확장점 추가 (sealed 제거) [LOG-20260510-3]

# 변경
- sealed 제거 → HGraphCatalogNode 등 도메인별 파생 타입 상속 허용.
- bodyArea: private → protected → 파생 타입이 body 콘텐츠 교체 가능.
- _OnHeaderMouseDown: 더블클릭 동작을 OnHeaderDoubleClick(protected virtual) 로 위임.
  기본 동작 = _ToggleExpanded (기존과 동일). 파생 타입은 override 로 교체.

# 이유
- CatalogNode 는 헤더 더블클릭 시 foldout 토글 대신 카탈로그 전환 동작 필요.
- body 에 ObjectField 를 표시하려면 base._BuildBody 가 추가한 UID Label 을
  파생 타입에서 Clear 후 재구성 가능해야 함.

=============================================================================
@Jason - PKH 2026.05.10 포트 위치 수정 + 타이틀 헤더 이동 [LOG-20260510-2]

# 변경
- _BuildPorts(): inputContainer/outputContainer → mainContainer 직속 portRow 로 이동.
  mainContainer.Insert(1, portRow) — headerBar(0) 바로 다음, 바디(extensionContainer) 위.
- _ForcePortContainersVisible() 헬퍼 및 4개 호출 지점(생성자/ApplyEditorState/CloseIfExpanded/_ToggleExpanded) 전부 제거.
- _BuildHeader(): column 레이아웃 전환. headerRow(화살표+타입명) + titleLabel 2행 구조.
- _BuildTitle() 메서드 제거. titleLabel 이 headerBar 안으로 이동.

# 이유
- _ForcePortContainersVisible() 는 GraphView 내부 레이아웃 패스와 싸우는 임시 우회.
  portRow → mainContainer 이동이 닫힌 노드 엣지 어긋남 이슈의 구조적 해결.
  mainContainer 는 GraphView 의 expanded 상태 변화 영향 밖 → worldBound 항상 유효.
- 타이틀을 headerBar 안으로 이동해 닫힘/열림 무관 노드 정보를 헤더에 집중.

=============================================================================
@Jason - PKH 2026.05.10 닫힌 노드 포트 항상 표시 + 엣지 worldBound 보장 [LOG-20260510-1b]

# 변경
- _ForcePortContainersVisible() 헬퍼 추가.
  RefreshExpandedState() 직후 호출로 top / inputContainer / outputContainer 를 Flex 강제 복원.
  RefreshExpandedState() 4개 호출 지점(생성자 / ApplyEditorState / CloseIfExpanded / _ToggleExpanded) 모두 적용.
- 이전 RefreshExpandedState() override 제거 (CS0506 — Node 베이스에서 virtual 미노출).
- Public - GraphView Override 영역 이름에서 "(Phase 1-E 실시간 스냅)" 접미 제거.

# 이유
- GraphView base.RefreshExpandedState() 가 collapsed 시 top 컨테이너를 숨김.
  포트가 hidden 부모 안에 있으면 worldBound = {0,0,0,0} → 엣지가 (0,0)으로 잘못 연결.
  (Repopulate 후 닫힌 노드 엣지 어긋남 이슈의 직접 원인)
- 닫힌 상태에서도 포트 dot 를 표시해 연결 상태 시각 확인 + 드래그 연결 가능.

=============================================================================
@Jason - PKH 2026.05.10 Phase 2 — Port 추가 (inputPort / outputPort) [LOG-20260510-1]

# 추가
- _BuildPorts(): InputPort(Direction.Input) + OutputPort(Direction.Output) 생성.
  Orientation.Horizontal, Capacity.Multi, portName="" (레이블 숨김).
  inputContainer(좌) / outputContainer(우) 에 각각 Add.
- InputPort / OutputPort public 프로퍼티 노출 (HGraphCanvas 포트 연결용).

# 이유
- HGraphCanvas._PopulateInternal 이 HGraphEdge.input / output 에 포트를 할당해야 함.
- portName="" 로 레이블 숨겨 컴팩트 노드 외관 유지.

=============================================================================
@Jason - PKH 2026-04-24 HGraphNode 의 역할 - BaseNode 1개에 대응하는 시각 객체 [LOG-20260424-1]
=============================================================================

[역할]
- catalog.Nodes 의 BaseNode 1개 = HGraphNode VisualElement 1개.
- GraphView.Node 상속으로 Manipulator 자동 인식 (Selection·Drag·RectSelect).
- 도메인 데이터와 시각 레이어를 이어주는 얇은 어댑터.

[Experimental API 어댑터 경계 2파일 확장]
- L1 에서는 HGraphCanvas.cs 1파일이 유일한 Experimental using 지점이었음.
+ Phase 1-A 에서 HGraphNode.cs 도 Experimental.GraphView.Node 상속 필수.
+ 원칙 위반이 아닌 예외적 확장 (Q3 A 안 채택 - 대안 비용 폭증 때문).
+ grep 회귀 가드: "UnityEditor.Experimental" 참조가 이 2파일로만 국한.

[Phase 1-B 확장 - 2026-05-07]
- extensionContainer 에 bodyArea 추가 + GraphView.Node 의 expanded 활성화 (P1B-a, d).
- 토글 진입점 2종 (P1B-e A+B 둘 다 활성):
  + (1) 헤더 좌측 ▶/▼ 아이콘 클릭 (toggleArrow.MouseDown 캡처)
  + (2) 헤더 더블클릭 (headerBar.MouseDown clickCount==2 캡처)
- FoldoutChanged / OpenSizeChanged 이벤트 (HGraphCanvas 구독 → Author 호출).

[Phase 1-D 추가 - 2026-05-07]
- BuildContextualMenu override (P1D-a): 복사 / 복제 / 루트 재설정 / 삭제 4 항목.
- capabilities 플래그 차단 (Copiable | Deletable): GraphView 기본 메뉴 자동 추가 막음.

[Phase 1-D Cut/Paste 확장 - 2026-05-08]
- 메뉴 6 항목으로 확장 (잘라내기 + 붙여넣기 추가).
- _GetEffectiveTargets 헬퍼: selection 포함 여부에 따라 단일/다중 target 결정.

[Phase 1-B-3 Task G - 2026-05-07]
- OnSelected / OnUnselected override: .hgraph-node--selected USS 클래스 토글.

[Phase 1-B-2 Task F - 2026-05-07]
- 인라인 Resize Manipulator: resizeHandle 에 MouseDown/Move/Up 직접 등록.
  capture + StopPropagation 둘 다 필수 (capture 만 있으면 SelectionDragger 가 먼저 받음).

[Phase 5 - 2026-05-11]
- _OnContextSetAsRoot → canvas.SetSelectedAsRoot(UID) 위임.
  이유 / Set as Root 로직(Author.SetRoot) 을 HGraphCanvas.SetSelectedAsRoot 에 통합.
         메뉴바 진입점과 우클릭 메뉴 진입점이 같은 Author.SetRoot 경유 — DRY.
         명시 UID 오버로드 사용: 우클릭 시 GraphView selection 갱신 X 이므로 this.UID 직접 전달.
         canvas null fallback 유지 — Panel 밖에서 호출되는 극단 케이스 방어.

[Phase 1-F CloseIfExpanded 추가 - 2026-05-09]
- Internal - Foldout State (Phase 1-F) 영역 신설:
  + IsExpanded: expanded 프로퍼티 노출 — HGraphCanvas.CloseAllFoldouts 에서 일괄 확인용.
  + CloseIfExpanded(): 이미 닫혀 있으면 early return. 열린 경우에만 닫힘 상태로 전환.

[Phase 1-E SetPosition override + _ApplySnap - 2026-05-08]
- SetPosition override: SelectionDragger 매 frame 호출 → quantize 삽입.
  NodeSnapSettings.Mode + Event.current.shift 분기 — Off/OnShiftHold/Always.
  Mathf.Round 좌상단 기준. GridUnit <= 0 시 quantize skip (DivByZero 가드).

=============================================================================
