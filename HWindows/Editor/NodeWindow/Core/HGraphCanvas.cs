using System.Collections.Generic;
using HDiagnosis.Logger;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphCanvas : GraphView {
        #region Const
        const string USS_ASSET_NAME = "HGraphWindow";
        #endregion

        #region Fields
        NodeCatalogSO currentCatalog;
        VisualElement emptyStateHint;
        readonly Dictionary<NodeUID, HGraphNode> nodeLookup = new();
        int lastCatalogHash;
        #endregion

        #region Constructor + Lifecycle
        public HGraphCanvas() {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            _LoadStyleSheet();

            _BuildEmptyStateHint();
            graphViewChanged = _OnGraphViewChanged;

            // catalog mutation 이벤트 구독 (Author 호출 + Inspector 직접 수정 watcher 모두 통합).
            // VisualElement detach 시 unsubscribe 로 메모리 누수 방지.
            NodeCatalogAuthor.CatalogMutated += _OnCatalogMutated;

            // hash polling fallback: ObjectChangeEvents 가 즉시 발송 안 되는 케이스 대응.
            // 매 frame hash 비교 후 변경 시에만 Repopulate. 계산 비용 미미 (노드 N=100 ≈ 3μs).
            EditorApplication.update += _PollCatalogChanges;

            RegisterCallback<DetachFromPanelEvent>(_ => {
                NodeCatalogAuthor.CatalogMutated -= _OnCatalogMutated;
                EditorApplication.update -= _PollCatalogChanges;
            });
        }

        private void _PollCatalogChanges() {
            if (currentCatalog == null) return;
            int currentHash = _CalculateCatalogHash();
            if (currentHash != lastCatalogHash) {
                lastCatalogHash = currentHash;
                _RepopulateNoViewportReset();
            }
        }

        private int _CalculateCatalogHash() {
            if (currentCatalog == null) return 0;
            int hash = 17;
            hash = hash * 31 + currentCatalog.NodeCount;
            hash = hash * 31 + currentCatalog.RootUID.Value;
            foreach (KeyValuePair<NodeUID, BaseNode> pair in currentCatalog.Nodes) {
                hash = hash * 31 + pair.Key.Value;
                if (pair.Value != null) {
                    hash = hash * 31 + (pair.Value.Title ?? string.Empty).GetHashCode();
                }
            }
            return hash;
        }

        private void _OnCatalogMutated(NodeCatalogSO catalog) {
            if (currentCatalog == null || catalog != currentCatalog) return;
            // viewport 위치는 사용자가 조정해 둔 상태일 수 있으므로 _Populate 가
            // 강제 리셋하지 않도록 별도 경로. 현재 구현은 _Populate 가 viewport 리셋을 포함하므로
            // 빈번한 mutation 에서 깜빡일 가능성 있음. 필요 시 viewport 보존 분기 추가.
            _RepopulateNoViewportReset();
        }

        public Vector2 GetViewportCenterWorld() {
            Vector3 vt = viewTransform.position;
            float scale = viewTransform.scale.x;
            if (scale == 0f) scale = 1f;
            Rect rect = contentRect;
            Vector2 screenCenter = new Vector2(rect.width / 2f, rect.height / 2f);
            return (screenCenter - new Vector2(vt.x, vt.y)) / scale;
        }
        #endregion

        #region Navigation (Phase 1-C)
        // 지정 graph 좌표를 viewport 중앙으로 pan 이동. zoom (viewTransform.scale) 은 보존.
        // GetViewportCenterWorld 의 역수 : graphPos = (screenPos - position) / scale 을
        // position = screenPos - graphPos * scale 로 풀어 새 pan 계산.
        public void CenterViewportOn(Vector2 worldPos) {
            Rect rect = contentRect;
            Vector2 screenCenter = new Vector2(rect.width / 2f, rect.height / 2f);
            float scale = viewTransform.scale.x;
            if (scale == 0f) scale = 1f;
            Vector3 newPos = new Vector3(
                screenCenter.x - worldPos.x * scale,
                screenCenter.y - worldPos.y * scale,
                0f);
            UpdateViewTransform(newPos, viewTransform.scale);
        }

        // 현재 catalog 의 RootUID layout 으로 viewport 이동. 루트 미보유 시 false.
        public bool GoToRoot() {
            if (currentCatalog == null || !currentCatalog.HasRoot) return false;
            NodeUID root = currentCatalog.RootUID;
            Vector2 pos = Vector2.zero;
#if UNITY_EDITOR
            if (currentCatalog.EditorNodeLayouts.TryGetValue(root, out Vector2 saved)) pos = saved;
#endif
            CenterViewportOn(pos);
            return true;
        }

        // selection 에서 HGraphNode 만 추려 반환. Window 측이 ISelectable (Experimental
        // .GraphView 타입) 을 직접 다루지 않도록 어댑터 경계 (P1-3) 를 보존하는 헬퍼.
        public IReadOnlyList<HGraphNode> GetSelectedNodes() {
            List<HGraphNode> result = new();
            foreach (ISelectable s in selection) {
                if (s is HGraphNode n) result.Add(n);
            }
            return result;
        }
        #endregion

        #region USS
        private void _LoadStyleSheet() {
            string[] guids = AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}");
            if (guids.Length == 0) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet '{USS_ASSET_NAME}' not found in project. " +
                    "Grid/style will fall back to GraphView defaults. " +
                    "Verify USS file presence or asset name.");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet at '{path}' failed to load. " +
                    "Grid/style will fall back to GraphView defaults.");
                return;
            }

            styleSheets.Add(sheet);
        }
        #endregion

        #region Empty State
        private void _BuildEmptyStateHint() {
            emptyStateHint = new VisualElement();
            emptyStateHint.style.position = Position.Absolute;
            emptyStateHint.style.left = 0;
            emptyStateHint.style.right = 0;
            emptyStateHint.style.top = 0;
            emptyStateHint.style.bottom = 0;
            emptyStateHint.style.alignItems = Align.Center;
            emptyStateHint.style.justifyContent = Justify.Center;
            emptyStateHint.pickingMode = PickingMode.Ignore;

            Label hintLabel = new Label(
                "No Node Catalog bound.\n\nDrop a Node Catalog here,\nor use the Open Catalog button.");
            hintLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            hintLabel.style.fontSize = 14;
            hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            hintLabel.style.whiteSpace = WhiteSpace.Normal;
            emptyStateHint.Add(hintLabel);

            Add(emptyStateHint);
        }

        private void _ShowEmptyStateHint() {
            emptyStateHint.style.display = DisplayStyle.Flex;
        }

        private void _HideEmptyStateHint() {
            emptyStateHint.style.display = DisplayStyle.None;
        }
        #endregion

        #region Bind
        public void Bind(NodeCatalogSO catalog) {
            if (currentCatalog == catalog) return;
            currentCatalog = catalog;
            _Populate();
        }

        public void Unbind() {
            Bind(null);
        }
        #endregion

        #region Populate
        private void _Populate() {
            _PopulateInternal();
            lastCatalogHash = _CalculateCatalogHash();
            // 새 Bind 직후에만 viewport 를 원점으로 리셋. 자동 배치 노드 영역이 보이도록 보장.
            UpdateViewTransform(Vector3.zero, Vector3.one);
        }

        private void _RepopulateNoViewportReset() {
            // CatalogMutated 이벤트 또는 polling 후 호출. 사용자의 viewport 팬/줌 상태 보존.
            _PopulateInternal();
            lastCatalogHash = _CalculateCatalogHash();
        }

        private void _PopulateInternal() {
            _ClearAllNodes();

            if (currentCatalog == null) {
                _ShowEmptyStateHint();
                return;
            }

            _HideEmptyStateHint();

            foreach (KeyValuePair<NodeUID, BaseNode> pair in currentCatalog.Nodes) {
                BaseNode data = pair.Value;
                if (data == null) {
                    HLogger.Warning($"[HGraphCanvas] Null BaseNode at UID {pair.Key}, skipped.");
                    continue;
                }

                // Phase 1-A : layout (위치). Author.CreateNode 가 자동 layout 부여.
                // Phase 1-B : foldout state + open size (P1B-b/c). 두 맵 모두 미보유 시 default fallback.
                Vector2 pos = Vector2.zero;
                bool isExpanded = false;
                Vector2 openSize = Vector2.zero;
#if UNITY_EDITOR
                if (currentCatalog.EditorNodeLayouts.TryGetValue(pair.Key, out Vector2 savedPos)) {
                    pos = savedPos;
                }
                if (currentCatalog.EditorNodeFoldoutOpen.TryGetValue(pair.Key, out bool savedOpen)) {
                    isExpanded = savedOpen;
                }
                if (currentCatalog.EditorNodeOpenSizes.TryGetValue(pair.Key, out Vector2 savedSize)) {
                    openSize = savedSize;
                }
#endif

                bool isRoot = pair.Key == currentCatalog.RootUID;
                HGraphNode view = new HGraphNode(data, isRoot);
                view.SetPosition(new Rect(pos, Vector2.zero));
                view.ApplyEditorState(isExpanded, openSize);

                // 이벤트 구독으로 catalog 갱신 진입점 통합 (Phase 1-A 의 graphViewChanged 와 같은 책임 분리).
                // closure 캡처용 로컬 변수 - foreach 변수 직접 캡처는 일부 C# 버전에서 stale 가능.
                NodeUID uid = pair.Key;
                view.FoldoutChanged += isOpen => NodeCatalogAuthor.SetFoldoutOpen(currentCatalog, uid, isOpen);
                view.OpenSizeChanged += size => NodeCatalogAuthor.SetOpenSize(currentCatalog, uid, size);

                AddElement(view);
                nodeLookup[pair.Key] = view;
            }

            // GraphView 가 자식 변경 후 자동 redraw 안 하는 케이스 방지.
            // CatalogMutated 이벤트로 호출된 Repopulate 가 다음 사용자 인터랙션까지
            // 미뤄지지 않도록 명시적으로 dirty 표시.
            MarkDirtyRepaint();
        }

        private void _ClearAllNodes() {
            foreach (HGraphNode node in nodeLookup.Values) {
                RemoveElement(node);
            }
            nodeLookup.Clear();
        }
        #endregion

        #region GraphView Change Hook
        private GraphViewChange _OnGraphViewChanged(GraphViewChange change) {
            if (currentCatalog == null) return change;

            if (change.movedElements != null) {
                foreach (GraphElement elem in change.movedElements) {
                    if (elem is HGraphNode node) {
                        Vector2 newPos = node.GetPosition().position;
                        NodeCatalogAuthor.SetLayout(currentCatalog, node.UID, newPos);
                    }
                }
            }

            return change;
        }
        #endregion
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Dev Log
// ─────────────────────────────────────────────────────────────────────────────
// 2026-04-21 · USS 로드 전략 메모
//
//   [현재 방식] AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}")
//              · 이름 기반 검색. 경로/리네임/UPM 이전에 전부 생존.
//              · L1 베이스 수준에서 충분히 견고함.
//
//   [추후 전환 요청] Option 3 — 고정 GUID 상수 방식
//              · 왜 전환 필요:
//                 1. HWindows 내 USS 자산이 2개 이상으로 늘어날 때 동명 충돌 방지 필요
//                 2. 자산 참조 계약을 엄격화(리뷰 게이트, 계약적 참조)해야 할 때
//                 3. 프로덕션 품질 수준에서 Unity 자산 시스템과 정합(GUID는 일등 시민)
//              · 전환 절차:
//                 (a) HGraphWindow.uss.meta 에서 guid 값 확인
//                 (b) private const string USS_GUID = "<해당 guid>"; 로 교체
//                 (c) _LoadStyleSheet 내부 FindAssets 호출을
//                     AssetDatabase.GUIDToAssetPath(USS_GUID) 로 치환
//                 (d) USS_ASSET_NAME 상수 제거
//              · 장점: 동명 자산 애매성 0, 리네임·이동 완전 무관, 계약 명시적.
//              · 단점: .meta 재생성 등 드문 상황에서 GUID 수동 업데이트 필요.
// ─────────────────────────────────────────────────────────────────────────────

// =============================================================================
// Dev Log - Phase 1-A 확장 (2026-04-24)
// =============================================================================
// - Bind(catalog) / Unbind(): catalog 주입 + Populate 트리거. 같은 catalog 재진입 시 조기 return.
// - _Populate(): 하이브리드 전략 - 전체 재구성 (기존 HGraphNode 전부 제거 후 catalog.Nodes 순회 생성).
//   + Bind 는 드문 이벤트 (Selection 변경/드래그드롭/Open 버튼) 라 매번 전체 재구성해도 체감 지연 0.
//   + 드래그 이동 같은 고빈도 변경은 graphViewChanged 훅에서 위치만 증분 반영.
// - _OnGraphViewChanged: GraphView.graphViewChanged 에 등록된 콜백.
//   change.movedElements 순회하며 HGraphNode 의 새 위치를 Author.SetLayout 으로 catalog 에 반영.
// - _emptyStateHint: catalog 미바인드 상태에서 중앙에 "Drop a Node Catalog here..." 안내 표시.
//   pickingMode=Ignore 로 드래그드롭/클릭 이벤트를 하단 GraphView 로 pass-through.
// - _nodeLookup: UID -> HGraphNode 역매핑. Phase 1-D 선택 하이라이트/Phase 1-G Floating GUI
//   에서 "UID 로 VisualElement 찾기" 경로에 활용.
//
// [Stage 4 검증 보정 - 2026-04-25]
// - 자동 배치 분산: 스펙 P1-e 의 (0, 0) 고정에서 (autoIndex * 220, 0) 분산으로 변경.
//   + 다중 노드를 한 번에 Bind 하면 모두 같은 좌표에 겹쳐 사용자가 식별 못 하던 문제 해소.
//   + 220 = USS min-width 180 + 여백 40. 노드끼리 안 겹치는 최소 간격.
//   + saved layout 이 있는 노드는 그대로 사용. 신규 노드만 분산 인덱스 증가.
// - viewport 원점 리셋: Populate 끝에 UpdateViewTransform(Vector3.zero, Vector3.one) 호출.
//   + 새 catalog 를 Bind 한 직후 viewport 가 어디인지 모호한 상태를 차단.
//   + 자동 배치 노드들이 (0~N*220, 0) 영역에 위치하므로 원점 viewport 에서 보임.
//   + Phase 1-C "Go To Root" 가 들어오면 더 정교한 framing 으로 대체될 수 있음.
//
// [StretchToParentSize 제거 이유]
// - L1 에서는 생성자 마지막에 this.StretchToParentSize() 호출 (Canvas 가 혼자 root 를 채움).
// - Phase 1-A 에서 HGraphWindow 에 Toolbar 가 추가되어 root 가 Column flex 레이아웃이 됨.
// - StretchToParentSize() = position:absolute + left/right/top/bottom:0 → flex 레이아웃 무시, root 전체 덮음.
// - 결과: Toolbar 가 Canvas 아래 숨어 보이지 않음.
// - 해결: StretchToParentSize() 제거. HGraphWindow 가 canvas.style.flexGrow = 1 로 영역 할당.
// - 내부 GridBackground 의 StretchToParentSize() 는 canvas 내부를 채우는 용도로 유지.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-B 확장 (2026-05-07)
// =============================================================================
// - _PopulateInternal: 노드 생성 시 catalog 의 두 보조 맵 적용 (P1B-b, c).
//   + EditorNodeFoldoutOpen 에서 isExpanded 읽어 ApplyEditorState 호출.
//   + EditorNodeOpenSizes 에서 openSize 읽어 ApplyEditorState 호출.
//   + 두 맵 미보유 시 (false, Vector2.zero) fallback. CreateNode 자동 초기화 미적용 정책과 정합.
// - HGraphNode 의 두 이벤트 구독:
//   + FoldoutChanged → Author.SetFoldoutOpen(catalog, uid, open) 호출.
//   + OpenSizeChanged → Author.SetOpenSize(catalog, uid, size) 호출.
//   + Phase 1-A 의 graphViewChanged 가 layout 갱신을 단일 진입점으로 흡수한 것과 같은 분리.
//     Foldout/OpenSize 갱신도 이벤트 구독을 단일 진입점으로 통합.
//   + closure 캡처 안전성을 위해 foreach 안에서 NodeUID uid = pair.Key 로컬 변수 명시.
// - hash polling 영향:
//   + Author.SetFoldoutOpen / SetOpenSize 는 _NotifyMutated 호출 없음 (P1B-i 고빈도 분류).
//   + Foldout 토글은 본인 노드만 갱신하므로 hash polling 진입 불필요.
//   + Inspector 에서 editorNodeFoldoutOpen 직접 수정은 ObjectChangeWatcher 가 처리.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-C 확장 (2026-05-07)
// =============================================================================
// - CenterViewportOn(worldPos) : 지정 graph 좌표를 viewport 중앙으로 pan 이동.
//   + GetViewportCenterWorld 의 역수 (position = screenCenter - graphPos * scale).
//   + viewTransform.scale 보존 — 줌 상태 유지하며 pan 만 갱신해 사용자 컨텍스트 깨지 않음.
// - GoToRoot() : currentCatalog.RootUID 의 EditorNodeLayouts 위치로 CenterViewportOn 호출.
//   + 루트 미보유 시 false — 호출자(HGraphWindow) 가 Warning 으로 사용자 피드백.
//   + layout 미보유 fallback (0,0) — Author.CreateNode 가 자동 layout 부여하므로
//     실 발생 케이스는 데이터 호환성 이슈 한정.
//   + Phase 5 메뉴바 이관 시 본 메서드 시그니처 그대로, 호출 진입점만 메뉴로 교체.
// - GetSelectedNodes() : selection 에서 HGraphNode 만 추려 IReadOnlyList 반환.
//   + 목적 = 어댑터 경계 (P1-3) 보존. Window 가 ISelectable (Experimental.GraphView 타입)
//     을 직접 다루지 않도록 한 겹의 헬퍼로 캡슐화.
//   + Phase 1-D 우클릭 메뉴, Phase 1-E 다중 선택 일괄 처리에서도 같은 헬퍼 재사용 예정.
// =============================================================================
