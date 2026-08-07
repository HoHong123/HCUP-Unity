---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogAuthor.cs
script_name: NodeCatalogAuthor
latest_log_id: LOG-20260807-1
total_entries: 12
created: 2026-05-12
updated: 2026-08-07
---

# NodeCatalogAuthor Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogAuthor.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.08.07 _FindAnyOtherNode 루트 이전 후보에서 CatalogNode 제외 [LOG-20260807-1]

# 변경
- _FindAnyOtherNode(catalog, exclude): CatalogNode 인 노드를 후보에서 skip 하도록 순회 조건 추가.
  KeyValuePair 순회로 변경해 value(BaseNode) 타입을 직접 검사.

# 이유
- RemoveNode / PurgeNullNodes 의 루트 자동 이전 경로가 _FindAnyOtherNode 의 반환값을
  그대로 catalog.InternalSetRoot() 에 전달한다. InternalSetRoot 는 SetRoot(공개 API)의
  CatalogNode 타입 가드(LOG-20260511-3)를 거치지 않으므로, 제거된 root 의 유일한 대체
  후보가 CatalogNode 였을 경우 가드가 우회되어 CatalogNode 가 root 로 지정될 수 있었다.

# 결과
- Root 자동 이전 시 CatalogNode 는 후보에서 제외 — 남은 노드가 전부 CatalogNode 면
  fallback.IsValid == false 로 InternalClearRoot() 경로를 타 (root 없음으로 정리).

# 주의
- SetRoot 의 명시적(사용자 조작) CatalogNode 지정 거부와, 이 변경(자동 이전 후보 제외)은
  같은 불변식("CatalogNode는 root가 될 수 없다")의 두 진입점을 각각 방어한다.

=============================================================================
@Jason - PKH 2026.05.12 PurgeNullNodes + _ValidateEdgeCreation null 가드 [LOG-20260512-1]

# 변경
- PurgeNullNodes(catalog) 신설: catalog.Nodes 에서 value == null 항목(ghost UID) 을
  엣지 cascade + Root 이전 포함해 일괄 제거. SetDirty + SaveAssets 호출.
  _NotifyMutated 는 미호출 — 호출자가 이미 populate 중이므로 ObjectChangeWatcher 경로로 예약.
- _ValidateEdgeCreation: ContainsKey → TryGetValue + null 가드.
  value 가 null 인 ghost UID 에 대한 엣지 생성을 거부.

# 이유
- Unity Project 창에서 sub-asset 직접 삭제 시, HDictionary UID 키는 남고 BaseNode SO 참조만
  null 이 됨. Author 정상 경로(InternalRemoveNode → Undo.DestroyObjectImmediate)를 우회.
- _PopulateInternal 가 매 repopulate 마다 Warning 을 내고, ContainsKey 는 ghost UID 를
  존재하는 노드로 판단해 ghost 노드로의 엣지 생성을 허용하는 문제.

# 결과
- 윈도우 열기 시 및 repopulate 시 ghost UID 자동 정리. Warning 소멸.
- ghost 노드로의 엣지 생성 차단.

=============================================================================
@Jason - PKH 2026.05.11 CatalogNode 루트 설정 제약 [LOG-20260511-3]

# 변경
- SetRoot: CatalogNode 타입 가드 추가 — 대상 노드가 CatalogNode 이면 Warning + false 반환.
- _CreateCatalogNodeCore: `if (!catalog.HasRoot) InternalSetRoot(uid)` 제거.
  CatalogNode 생성 시 자동 루트 지정 경로 차단.

# 이유
- CatalogNode 는 외부 카탈로그 참조 역할. 루트는 SimpleNode / HubNode 가 담당해야 일관됨.
- UI(컨텍스트 메뉴 제거)만 방어하면 SetRoot 직접 호출 경로 우회 가능 → backend 이중 방어.
- 자동 루트 지정 제거: 빈 카탈로그에 CatalogNode 가 처음 추가될 때 루트 없는 상태가 됨.
  다음 SimpleNode / HubNode 생성 시 `if (!catalog.HasRoot)` 로 자연스럽게 루트 지정됨.

=============================================================================
@Jason - PKH 2026.05.11 — CreateCatalogNodeAt 순방향 중복 거부 추가 [LOG-20260511-2]

# 변경
- CreateCatalogNodeAt: 진입부에 _HasCatalogNodeFor(catalog, referenced) 검사 추가.
  동일 referenced 를 가리키는 CatalogNode 가 이미 존재하면 Warning + null 반환.
- _HasCatalogNodeFor 주석: 역방향 전용 → 순방향/역방향 양쪽 사용 명시.

# 이유
- catalog 당 referenced 당 CatalogNode 는 최대 1개 제한 (1:1 관계 무결성).
  기존 역방향 가드(_HasCatalogNodeFor)를 순방향에도 재사용 — 추가 로직 없음.

=============================================================================
@Jason - PKH 2026.05.11 — SetOpenSize 제거 (리사이즈 기능 최후순위 이월) [LOG-20260511-1]

# 변경
- SetOpenSize 메서드 제거.
- 헤더 주석의 SetOpenSize 참조 제거.

# 이유
- 리사이즈 기능 전면 이월 결정. BaseNode.editorOpenSize 필드 제거와 연동.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — HubNode API 추가 (CreateHubNode / AddHubEntry / RemoveHubEntry / ConnectHubEdge) [LOG-20260510-4]

# 변경
- CreateHubNode(catalog, position): HubNode SO 생성 + sub-asset 등록. 초기 entries 없음.
- AddHubEntry(catalog, hubUID, key): hub.AddEntry → SetDirty(hub) → CatalogMutated.
- RemoveHubEntry(catalog, hubUID, index): 해당 key 사용 HubNodeEdge cascade 제거 후 hub.RemoveEntry.
- ConnectHubEdge(catalog, branch, leaf, portKey): HubNodeEdge 생성 + portKey 할당 + edge 등록.
  기존 ConnectEdge<TEdge> 와 별개 — portKey 할당이 필요하므로 분리.

# 이유
- HubNode 는 포트 항목 수만큼 동적으로 output port 를 시각화.
  AddEntry/RemoveEntry 가 CatalogMutated 를 발화해야 repopulate 가 포트 수 갱신.
- HubNodeEdge.BranchPortKey 로 repopulate 시 key → Port 역매핑 (암묵 인덱스 불필요).

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — 양방향 CatalogNode 생성 + _CreateCatalogNodeCore 추출 [LOG-20260510-3]

# 변경
- CreateCatalogNodeAt: 기존 로직 → _CreateCatalogNodeCore 로 위임.
  referenced 에 catalog 를 역방향으로 참조하는 CatalogNode 미존재 시 자동 생성 (1:1 양방향).
- _CreateCatalogNodeCore: 단일 CatalogNode 생성 코어 (dirty/save/notify 포함).
- _HasCatalogNodeFor(catalog, target): catalog 에 target 을 참조하는 CatalogNode 존재 여부 확인.

# 이유
- 사양: A→B 연결 시 B에 A CatalogNode 자동 생성 (1:1 관계).
  동일 catalog 또는 already-exists 체크로 무한루프·중복 방지.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3 — CreateCatalogNodeAt 추가 [LOG-20260510-2]

# 변경
- CreateCatalogNodeAt(catalog, referenced, dropPosition): CatalogNode 생성 전용 진입점.
  CreateNode<T> 와 달리 참조 카탈로그 설정 + 드롭 위치를 단일 Undo 그룹으로 처리.
  HGraphWindow 드래그드롭 (카탈로그 이미 바인드 상태) 에서 호출.

# 이유
- CreateNode<CatalogNode> 후 SetReferencedCatalog 하면 CatalogMutated가 두 번 발화해
  repopulate 가 두 번 일어남 (위치 flicker 유발). 단일 메서드로 원자 처리.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — 에디터 상태 이관 (catalog HDictionary → BaseNode) [LOG-20260509-3]

# 변경
- SetLayout / SetFoldoutOpen / SetOpenSize: catalog.InternalSet* → node.Set* + SetDirty(node)
- CreateNode 자동 배치: catalog.InternalSetLayout → node.SetEditorPosition 직접 호출
- DuplicateNode 위치 읽기: catalog.EditorNodeLayouts[sourceUID] → sourceTyped.EditorPosition
- RemoveNode cascade: InternalRemoveLayout/FoldoutOpen/OpenSize 3줄 제거 (node 소멸로 자동 처리)
- _RestoreFromEntry: catalog.InternalSet* 3줄 제거 — FromJsonOverwrite 가 nodeJson 에서 자동 복원

# 이유
- 삭제 Undo 후 노드 위치 (0,0) 리셋 버그: catalog HDictionary Undo 복원이
  DisconnectEdge cascade 의 AssetDatabase.SaveAssets 호출로 손상됨.
- 이관 후 Undo.DestroyObjectImmediate(node) 가 editorPosition/FoldoutOpen/OpenSize
  포함 노드 전체 상태를 원자 복원 → catalog 단계 Undo 불필요.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — Undo 레이어 추가 [LOG-20260509-2]

# 변경
- CreateNode: Undo.RecordObject(catalog) + Undo.RegisterCreatedObjectUndo(node)
- DuplicateNode: 동일 패턴
- RemoveNode: Undo.RecordObject(catalog) + Undo.DestroyObjectImmediate(node)
  (AssetDatabase.RemoveObjectFromAsset + DestroyImmediate 대체)
- ConnectEdge / DisconnectEdge / SetRoot: Undo.RecordObject(catalog)
- PasteNodes: Undo.RecordObject(catalog) + SetCurrentGroupName("Paste Nodes")
- _RestoreFromEntry: Undo.RegisterCreatedObjectUndo(node, "Paste Nodes")
- 고빈도 (SetLayout / SetFoldoutOpen / SetOpenSize): Undo 미적용 유지 (고빈도 히스토리 오염 방지)

=============================================================================
@Jason - PKH 2026.05.09 NodeUID.New() 전환 — NodeUIDRegistry 의존 제거 [LOG-20260509-1]

# 변경
- CreateNode / DuplicateNode / _RestoreFromEntry 의 NodeUIDRegistry.instance.Issue() → NodeUID.New()
- 자동 타이틀 fallback: $"Node_{uid.Value}" → $"Node_{uid.Value[..8]}" (GUID 8자 단축)
- using HWindows.Editor.NodeWindow.Identity 제거 (Registry 삭제로 불필요)

=============================================================================
@Jason - PKH 2026-04-22 NodeCatalogAuthor 의 역할 - mutation 단일 게이트 [LOG-20260422-1]
=============================================================================

[역할]
- catalog 변경의 모든 경로가 통과해야 하는 Editor-only 정적 게이트.
- 상태 0, 필드 0. 모든 컨텍스트는 파라미터로 전달.
- 존재 이유: Runtime SO 를 순수 데이터로 유지 + 깨진 상태 생성 경로 차단.

[Author 가 하는 일]
- UID 발급 (Registry 호출) + sub-asset 생성 + AssetDatabase.AddObjectToAsset
- catalog.Internal* 메서드 호출 (HDictionary / List / rootUID 갱신)
- Validation 강제 (5가지 규칙)
- Cascade delete (노드 삭제 시 관련 엣지 일괄)
- Root 자동 배정 (첫 노드) + Root 이전 (root 노드 삭제 시 다른 노드로)
- EditorUtility.SetDirty + AssetDatabase.SaveAssets 페어 호출

[Validation Rules]
- self-loop 금지: branch == leaf → _ValidateEdgeCreation 거부
- parallel edge 금지: HasEdgeBetween 체크 → 거부
- 노드 존재 확인: Nodes.ContainsKey
- UID 유효성: branch.IsValid && leaf.IsValid
- catalog null 거부

[Phase 1-A 확장 - 2026-04-24]
- CreateNode<T>: title 파라미터 default = null. null/whitespace 시 $"Node_{uid.Value}" fallback.
- SetLayout 신설. SetDirty 만 호출 (SaveAssets 생략) - "고빈도 상태 업데이트" 분류.

[Phase 1-D 확장 - 2026-05-07]
- DuplicateNode<T> 신설: ScriptableObject.Instantiate + ResetIdentity + AssignIdentity.
  위치 = 원본 + (40, 40) 자동 layout. 엣지 연결은 복제 X.

[Phase 1-D Cut/Paste 확장 - 2026-05-08]
- CutNodes(catalog, uids): HGraphClipboard.Serialize 후 RemoveNode cascade.
- PasteNodes(catalog, clipboardJson): TryParse + _RestoreFromEntry per entry.
- UID 처리 정책: "항상 새 UID 발급" — NodeUIDRegistry.instance.Issue() 호출.

[Phase 1-B 확장 - 2026-05-07]
- SetFoldoutOpen / SetOpenSize 신설. SetDirty 만 호출 (고빈도 분류).
- RemoveNode cascade: InternalRemoveFoldoutOpen + InternalRemoveOpenSize 추가.

=============================================================================
