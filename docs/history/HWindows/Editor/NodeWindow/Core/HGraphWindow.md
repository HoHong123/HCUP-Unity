---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphWindow.cs
script_name: HGraphWindow
latest_log_id: LOG-20260511-3
total_entries: 8
created: 2026-05-12
updated: 2026-05-12
---

# HGraphWindow Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphWindow.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.11 Phase 5 — 메뉴바 신설 + Go To Root / Close All Toolbar 이관 [LOG-20260511-3]

변경 / _BuildMenuBar() 신설 (height=22, #2A2A2A).
       _BuildViewMenu() : Go To Root (Ctrl+Home) + Close All Foldouts (Ctrl+0). catalog null 시 Disabled.
       _BuildEditMenu() : Select All (Ctrl+A) + Set as Root. 각 동적 status callback 적용.
       _GetEditMenuStatus_SetAsRoot : 단일 선택 + non-root 조건만 Normal.
       CreateGUI : root.Add(_BuildMenuBar()) 를 _BuildToolbar 앞에 삽입.
       _BuildToolbar : _CreateGoToRootButton / _CreateCloseAllButton 두 Add 제거.
       _CreateGoToRootButton / _CreateCloseAllButton 메서드 삭제 — 메뉴바로 이관.
이유 / milestone §5 스펙: Go To Root + Close All 을 메뉴바로 이관. Select All / Set as Root 은
       단축키/우클릭 만 진입 가능했던 기능의 메뉴바 비-컨텍스트 진입 경로 추가.
결과 / Toolbar 7 항목 → 5 항목 (CatalogField/Search/Count/ViewportLabel/Settings 잔존).
       [View ▾] / [Edit ▾] 2 카테고리 풀다운 메뉴바 상단 추가.
       ToolbarMenu.menu.AppendAction statusCallback 파라미터로 메뉴 열 때마다 동적 재평가.
주의 / _GetEditMenuStatus_SetAsRoot 는 canvas.GetSingleSelectedHGraphNode() 를 호출.
       메뉴 열 때마다 selection 순회 (O(M)) — N=100 이하 실사용 범위에서 무시 가능.

=============================================================================
@Jason - PKH 2026.05.11 툴바 순서 조정 — 검색 앞, 좌표값 뒤 + viewportCenterLabel 고정 너비 [LOG-20260511-2]

변경 / _BuildToolbar: SearchField + SearchCountLabel 을 ViewportCenterLabel 앞으로 이동.
       _CreateViewportCenterLabel: width=128, flexShrink=0, marginLeft=6 추가.
이유 / 검색 입력 필드가 카탈로그 ObjectField 바로 옆에 위치해야 UX 흐름이 자연스러움.
       좌표값은 보조 정보이므로 우측 끝(Settings 토글 직전)에 배치.
       flexShrink=0 미설정 시 flex 컨테이너가 레이블 너비를 임의 수축 가능.
결과 / 툴바 순서: GoToRoot / CloseAll / CatalogField / SearchField / SearchCount / ViewportCenter / Settings.
       ViewportCenterLabel 은 항상 128px 고정 — 좌표 자릿수 변동에도 레이아웃 흔들림 없음.

=============================================================================
@Jason - PKH 2026.05.11 CreateGUI 분해 + CS1061 SetPlaceholderText 제거 [LOG-20260511-1]

변경 / CreateGUI 를 _BuildToolbar / _BuildContentRow / _WireUpEvents / _InitialBind 로 분해.
       각 툴바 항목은 _CreateGoToRootButton / _CreateCloseAllButton / _CreateCatalogField /
       _CreateViewportCenterLabel / _CreateSearchField / _CreateSearchCountLabel / _CreateSettingsToggle.
       searchField.SetPlaceholderText("Search...") 제거 (CS1061: Unity 6000.3.11f1 미지원 API).
이유 / CreateGUI 가 단일 메서드에 툴바 항목 + 캔버스 + 이벤트 + 바인드 책임 혼재 → 기능별 분리.
       _CreateXxx 는 해당 필드(catalogField 등)를 초기화하고 반환 — 설정 책임 캡슐화.
       _WireUpEvents 에 CatalogMutated 구독 통합 → OnDisable 구독 해제 대상과 짝 추적 용이.
결과 / CreateGUI 는 5줄의 조립 흐름만 표현. 항목별 세부 설정은 각 _Create* 로 이동.
주의 / _CreateSettingsToggle 람다는 settingsPanel 필드를 늦게 평가.
       toggle 생성(_BuildToolbar) 전에 settingsPanel(_BuildContentRow) 이 null 이어도
       클릭 시점에는 유효 — CreateGUI 종료 후에만 사용자 인터랙션 가능하므로 안전.

=============================================================================
@Jason - PKH 2026.05.11 Phase 4 — 타이틀 검색 UI (TextField + 카운트 레이블) [LOG-20260511-0]

변경 / searchField(TextField 150px) + searchCountLabel(Label) 툴바 추가 (viewportCenterLabel 뒤, settingsToggle 앞).
       Search 영역 신설: _OnSearchValueChanged / _OnSearchKeyDown / _ClearSearchUI.
       _BindCatalog 에 _ClearSearchUI() 추가 — 카탈로그 전환 시 검색 UI 초기화.
이유 / value changed → canvas.SearchNodes(query) → 첫 결과 이동 + "1/3" 표시.
       Enter → canvas.AdvanceSearch() → 다음 결과 순환.
       ESC → SetValueWithoutNotify("") + canvas.ClearSearch() — valueChanged 발화 없이 UI만 초기화.
       SetValueWithoutNotify 채택 이유: valueChanged 재진입으로 ClearSearch 가 두 번 호출되는 것을 차단.
결과 / 카탈로그 전환(_BindCatalog) 시 _ClearSearchUI 호출 → canvas.Bind→_PopulateInternal→ClearSearch 와 쌍으로
       canvas 상태(count/index) + 툴바 UI(텍스트/카운트) 양쪽 일관 초기화.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3 — CatalogNode 드래그드롭 + 카탈로그 스위치 구독 [LOG-20260510-2]

변경 / _OnDragPerform: catalog 이미 바인드 상태 + 다른 SO 드롭 →
       NodeCatalogAuthor.CreateCatalogNodeAt(currentCatalog, catalog, dropPos).
       카탈로그 미바인드 상태 → 기존 _BindCatalog 경로 유지.
       동일 카탈로그 드롭 → 무반응.
     canvas.ToGraphPosition(evt.localMousePosition) — pan/zoom 보정된 드롭 위치.
     canvas.CatalogSwitchRequested 구독 → CatalogNode 더블클릭 시 _BindCatalog 호출.
     using Object = UnityEngine.Object 추가 — CS0104 (Object 모호성) 해소.
이유 / 카탈로그가 이미 바인드된 상태에서 드롭은 "다른 카탈로그로 전환"이 아닌
       "현재 캔버스에 참조 노드 추가"가 자연스러운 UX.
       카탈로그 전환은 ObjectField 피커로, 노드 추가는 드래그드롭으로 역할 분리.
결과 / canvas.CatalogSwitchRequested += _BindCatalog — 이벤트 구독 단방향.
       canvas가 HGraphWindow를 모르고, HGraphWindow가 canvas를 구독하는 단방향 의존.

=============================================================================
@Jason - PKH 2026.05.10 Phase 2 — Toolbar ObjectField 전환 (Lock 제거) [LOG-20260510-1]

변경 / Lock 버튼 + Open Catalog 버튼 + catalogNameLabel 제거. ObjectField(catalogField) 로 대체.
       OnSelectionChange / OnGUI 제거 (Project 창 클릭 자동 바인드 해제).
       DragDrop 등록 대상: rootVisualElement → canvas (ObjectField 드래그와 충돌 방지).
이유 / 이름만 표시하는 Label 로는 동일명 파일 구분 불가, 클릭 하이라이트 없음.
       Lock 이 필요했던 이유(자동 바인드 차단)가 ObjectField 전환으로 자연스럽게 해소.
       ObjectField 의 ping-on-click / 아이콘 표시가 SO 식별 방식을 개선.
결과 / canvas 드래그드롭 또는 ObjectField 피커로만 카탈로그 교체 가능 (명시 적용 UX).
       _BindCatalog 에 catalog == currentCatalog 가드 추가 (이중 바인드 방지).
주의 / catalogField.SetValueWithoutNotify — UI↔데이터 동기화. valueChanged 재진입 차단.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — Close All 버튼 추가 [LOG-20260509-1]

변경 / Toolbar 에 [Close All] 버튼 추가. 순서: Lock / Open Catalog / Go To Root / Close All / ...
이유 / 다수 노드 펼친 상태에서 한 번에 닫기 위한 직접 진입점.
결과 / _CloseAllFoldouts() → canvas.CloseAllFoldouts() → 각 HGraphNode.CloseIfExpanded().
주의 / catalog 없을 때 canvas.CloseAllFoldouts 호출해도 무반응 (graphElements 순회 결과 0).

=============================================================================
@Jason - PKH 2026-04-24 HGraphWindow 의 역할 - EditorWindow 진입점 + Toolbar + Bind 입구 [LOG-20260424-1]
=============================================================================

[Phase 1-A 확장]
- Toolbar: [Lock 아이콘] + [Open Catalog...] 두 버튼.
  - Lock 아이콘 토글로 selectionLocked 제어. Unity Inspector padlock 관용.
  - Open Catalog 는 EditorGUIUtility.ShowObjectPicker 경유.
- OnSelectionChange: Lock OFF 시 Selection 자동 Bind. 잘못된 타입 early return.
- DragDrop: rootVisualElement 에 DragUpdated/DragPerform 콜백 등록.
  - NodeCatalogSO 만 AcceptDrag, 나머지는 Warning + 거부.
  - Lock 상태 무관 - 명시 사용자 의도로 간주.
- Object picker 수신: OnGUI (IMGUI) 의 ObjectSelectorUpdated 이벤트 경로 경유.
  + UIElements Toolbar 와 IMGUI ObjectPicker 의 공존은 Unity 관용 (Unity 자체도 동일 패턴).

[Lock 버튼 텍스트 - ASCII "Lock"/"Locked" 채택]
- 초안에서는 유니코드 자물쇠 이모지 (\U0001F513 / \U0001F512) 사용.
+ 그러나 Unity Editor 기본 폰트가 이모지 글리프 미지원 - 시각적으로 빈 사각형 표시됨.
+ 최종: "Lock" (OFF, 중립색) / "Locked" (ON, 붉은 배경) 로 상태 2가지를 명확히 구분.

[Catalog Name Label 추가 - 스모크 피드백 반영]
- Toolbar 에 catalogNameLabel 추가: "Catalog: {asset name}" 또는 "(no catalog bound)" 표시.
- _BindCatalog(catalog) 헬퍼로 3곳 중복 (OnSelectionChange/OnGUI/DragPerform) 의 bind 로직 단일화.

[Lock 계약 변경 - Stage 3 피드백 반영 (2026-04-24)]
- 초안: "Lock ON 은 Selection 자동 동기화만 차단. 드래그드롭·Open 버튼은 명시 의도로 간주해 Lock 무관"
- 변경: "Lock ON 은 모든 Bind 경로 차단. Selection/Drag/Open 전부 동일하게 막힘"
+ 사용자 의도: "데이터 오버라이드 방지" - 실수로 드래그드롭 해도 기존 catalog 안전 보장.

[상태 영속화 - 부분 적용]
- selectionLocked → [SerializeField] 적용. 세션 내 Lock 상태 안정성 확보.
- currentCatalog → [SerializeField] 미적용. Close/Reopen 시 참조 유실은 사용자 의도.

[Phase 1-C 확장 - 2026-05-07]
- Toolbar 에 [Go To Root] [Set as Root] 두 버튼 추가.
- "Set as Root" 는 임시 진입점. Phase 1-D 우클릭 메뉴 도입 시 제거 예정.

[Phase 1-E 버그픽스 - canvas mutation 후 실시간 갱신 - 2026-05-09]
- 증상: Cut/Paste/Duplicate/Delete/Create 후 화면 미갱신.
- 원인: MarkDirtyRepaint() 는 pass 예약이지 pass 유발이 아님.
- 픽스: HGraphWindow 에서 CatalogMutated 구독 + Repaint() 호출.

[Phase 1-E Toolbar [Settings] 사이드패널 - 2026-05-08]
- Toolbar 우측 끝 ToolbarToggle [Settings]. IMGUIContainer settingsPanel 280px 고정 너비.
- _OnSettingsPanelGUI → NodeWindowSettingsProvider.DrawSettingsGUI 위임 — DRY.

[Phase 1-D 정식 이양 후 제거 - 2026-05-08]
- Toolbar [Set as Root] 버튼 + _SetSelectedAsRoot 메서드 제거.
+ 정식 위치 = HGraphNode 우클릭 메뉴 "루트 노드 재설정 (Set as Root)".

=============================================================================
