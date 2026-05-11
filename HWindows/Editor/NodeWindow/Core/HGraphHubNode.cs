#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HubNode 전용 시각 노드 (GraphView 확장).
 *
 * 특징 ::
 * HGraphNode 상속. 입구 Port 1개 + 출구 Port N개 (HubPortEntry 키 목록 기반).
 * + body: 키 목록 표시 + Add/Remove 버튼 — Author.AddHubEntry / RemoveHubEntry 호출.
 * + EnsureOutputPorts(count): repopulate 시 canvas 가 호출.
 * + GetOutputPortByKey(key): repopulate 시 portKey → Port 역매핑.
 * + 입구 "Input (N)" + 출구 "Key (N)" — RefreshPortLabels override 가 연결 수 갱신.
 * + Inspector 키값 수정 → KeysChanged → 포트명/바디 즉시 동기화 (DetachFromPanel 구독 해제).
 *
 * 주의사항 ::
 * 복사/복제/잘라내기 컨텍스트 메뉴 차단 — HGraphNode 기본 메뉴 유지.
 * + EnsureOutputPorts: canvas repopulate 시 호출. 직접 호출 금지.
 * =========================================================
 */
#endif
using System.Collections.Generic;
using System.Linq;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphHubNode : HGraphNode {
        #region Fields
        readonly List<Port> _outputPorts = new();
        VisualElement _outputPortColumn;
        #endregion

        #region Constructor
        public HGraphHubNode(HubNode dataNode, bool isRoot = false)
            : base(dataNode, isRoot) {
            AddToClassList("hgraph-hub-node");
            _InitHubBody();
            dataNode.KeysChanged += _OnKeysChanged;
            RegisterCallback<DetachFromPanelEvent>(_ => dataNode.KeysChanged -= _OnKeysChanged);
        }
        #endregion

        #region Protected Override — 포트 레이아웃
        // 입력 1개(좌) + 출구 컬럼(우). EnsureOutputPorts 로 동적 추가.
        protected override void _BuildPorts() {
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Input";

            _outputPortColumn = new VisualElement();
            _outputPortColumn.AddToClassList("hgraph-hub-port-column");

            portRow = new VisualElement();
            portRow.AddToClassList("hgraph-node-port-row");
            portRow.Add(inputPort);
            portRow.Add(_outputPortColumn);

            mainContainer.Insert(1, portRow);
        }
        #endregion

        #region Public Override — 인덱스로 출력 포트 조회
        public override Port GetOutputPort(int index) {
            return index >= 0 && index < _outputPorts.Count ? _outputPorts[index] : null;
        }

        // 입구 "Input (N)" + 출구 각각 "Key (N)" 형식으로 갱신.
        // 키값이 비어 있는 인덱스는 빈 베이스 + 카운트만 표시.
        public override void RefreshPortLabels() {
            if (inputPort != null) inputPort.portName = $"Input ({inputPort.connections.Count()})";
            HubNode hub = DataNode as HubNode;
            for (int i = 0; i < _outputPorts.Count; i++) {
                string baseName = (hub != null && i < hub.Entries.Count) ? hub.Entries[i].Key : string.Empty;
                int count = _outputPorts[i].connections.Count();
                _outputPorts[i].portName = $"{baseName} ({count})";
            }
        }
        #endregion

        #region Internal — Canvas 전용 포트 관리
        // repopulate 시 canvas 가 호출. count 이하일 때만 포트 추가.
        internal void EnsureOutputPorts(int count) {
            while (_outputPorts.Count < count) _AddOutputPort();
        }

        // 키값으로 포트 역매핑. repopulate 시 HubNodeEdge.BranchPortKey → Port 조회.
        internal Port GetOutputPortByKey(string key) {
            HubNode hub = DataNode as HubNode;
            if (hub == null) return null;
            for (int i = 0; i < hub.Entries.Count; i++) {
                if (hub.Entries[i].Key == key && i < _outputPorts.Count)
                    return _outputPorts[i];
            }
            return null;
        }

        // 포트 인덱스 역매핑. _OnGraphViewChanged 에서 사용된 포트 키 확인용.
        internal string GetOutputPortKey(Port port) {
            int idx = _outputPorts.IndexOf(port);
            HubNode hub = DataNode as HubNode;
            if (hub == null || idx < 0 || idx >= hub.Entries.Count) return string.Empty;
            return hub.Entries[idx].Key;
        }
        #endregion

        #region Private — 포트 추가 / Body 초기화
        private Port _AddOutputPort() {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            HubNode hub = DataNode as HubNode;
            port.portName = (hub != null && _outputPorts.Count < hub.Entries.Count)
                ? hub.Entries[_outputPorts.Count].Key
                : "";
            _outputPortColumn.Add(port);
            _outputPorts.Add(port);
            RefreshPorts();
            return port;
        }

        private void _InitHubBody() {
            bodyArea.Clear();

            HubNode hub = DataNode as HubNode;
            if (hub == null) return;

            _RebuildEntryList(hub);

            Button addBtn = new Button(() => _OnAddEntry()) { text = "+ 키 추가" };
            addBtn.AddToClassList("hgraph-hub-add-btn");
            bodyArea.Add(addBtn);
        }

        // 현재 entries 를 body 에 다시 그림. Add/Remove 후 호출 (repopulate 로 재진입해도 무방).
        private void _RebuildEntryList(HubNode hub) {
            // 기존 항목 제거 (Add 버튼은 마지막에 붙이므로 Clear 후 재생성).
            VisualElement listArea = bodyArea.Q<VisualElement>("hub-entry-list");
            if (listArea != null) bodyArea.Remove(listArea);

            listArea = new VisualElement();
            listArea.name = "hub-entry-list";
            bodyArea.Insert(0, listArea);

            for (int i = 0; i < hub.Entries.Count; i++) {
                int capturedIndex = i;
                string capturedKey = hub.Entries[i].Key;

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 2;

                Label keyLabel = new Label(capturedKey);
                keyLabel.style.flexGrow = 1;
                keyLabel.style.fontSize = 11;
                keyLabel.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.85f));
                row.Add(keyLabel);

                Button removeBtn = new Button(() => _OnRemoveEntry(capturedIndex)) { text = "✕" };
                removeBtn.style.fontSize = 10;
                removeBtn.style.paddingLeft = 4;
                removeBtn.style.paddingRight = 4;
                row.Add(removeBtn);

                listArea.Add(row);
            }
        }

        private void _OnAddEntry() {
            HubNode hub = DataNode as HubNode;
            NodeCatalogSO catalog = Catalog;
            if (hub == null || catalog == null) return;

            string newKey = $"Key{hub.PortCount}";
            NodeCatalogAuthor.AddHubEntry(catalog, hub.UID, newKey);
        }

        private void _OnRemoveEntry(int index) {
            HubNode hub = DataNode as HubNode;
            NodeCatalogSO catalog = Catalog;
            if (hub == null || catalog == null) return;

            NodeCatalogAuthor.RemoveHubEntry(catalog, hub.UID, index);
        }

        // Inspector 키값 수정 → HubNode.OnValidate → KeysChanged → 호출.
        // 포트 수 동기화 + portName 갱신 + 바디 재구성. CatalogMutated repopulate 없는 경량 경로.
        private void _OnKeysChanged() {
            HubNode hub = DataNode as HubNode;
            if (hub == null) return;
            while (_outputPorts.Count < hub.Entries.Count) _AddOutputPort();
            bool removed = false;
            while (_outputPorts.Count > hub.Entries.Count) {
                int last = _outputPorts.Count - 1;
                _outputPortColumn.Remove(_outputPorts[last]);
                _outputPorts.RemoveAt(last);
                removed = true;
            }
            if (removed) RefreshPorts();
            RefreshPortLabels();
            _RebuildEntryList(hub);
        }
        #endregion
    }
}


#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * [LOG-20260511-3] RefreshPortLabels override + portName 갱신 일원화
 * [LOG-20260511-2] Inspector 키값 수정 즉시 동기화
 * [LOG-20260511-1] 입구 포트 portName “Input” 부여
 * → 전체 이력: docs/history/HWindows/Editor/NodeWindow/Core/HGraphHubNode.md
 * =============================================================================
 */
#endif
