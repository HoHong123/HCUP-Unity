#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HDropdown] 필드가 여는 선택 팝업입니다. 검색 필드 + 계층 목록으로 구성됩니다.
 *
 * 특징 ::
 * PopupWindowContent 위에 직접 그립니다. UnityEditor 의 AdvancedDropdown 을 쓰지 않는 이유는
 * 그 타입이 검색 관련 멤버를 하나도 공개하지 않아 검색창을 띄울 수 없기 때문입니다.
 * (검색 상태는 internal AdvancedDropdownWindow 소유. 메타데이터 실측.)
 *
 * 렌더 구성 ::
 * 1. 상단 : SearchField. HDropdownAttribute.SearchThreshold 이하이면 그리지 않습니다.
 * 2. 하단 : 스크롤 목록. 라벨의 '/' 를 폴더로 접습니다 ("UI/600003_Click" -> UI > 600003_Click).
 *
 * 동작 경계 ::
 * 검색어가 없으면 계층 모드입니다. 폴더를 접고 펼치며 탐색합니다.
 * 검색어가 있으면 평탄 모드입니다. 계층을 무시하고 매칭된 잎만 전체 라벨로 보여줍니다.
 *
 * 사용 ::
 * PopupWindow.Show(anchor, new HDropdownSearchPopup(...)). HDropdownField 가 호출합니다.
 *
 * 주의사항 ::
 * 1. 창 크기는 열 때 한 번만 정해집니다. 접고 펼쳐도 창은 커지거나 줄지 않습니다.
 * 2. 방향키 / Enter / Esc 는 SearchField 보다 먼저 가로채 소비합니다. 순서가 뒤집히면
 *    검색 필드가 방향키를 먼저 먹어 목록 이동이 죽습니다.
 * 3. 현재 값이 든 폴더는 열린 상태로 시작합니다. 그 외 폴더는 접힌 상태입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace HInspector.Editor {
    internal sealed class HDropdownSearchPopup : PopupWindowContent {
        #region Inner Class
        /// <summary> 계층 노드. 폴더이거나 잎이며, 잎만 Value 를 가진다. </summary>
        sealed class Node {
            public string Name;         // 이 단계에서 보여줄 이름
            public string FullLabel;    // 잎의 원본 라벨. 검색 모드에서 이걸 보여준다
            public int Value;
            public bool IsFolder;
            public bool Expanded;
            public readonly List<Node> Children = new List<Node>();
        }

        struct Row {
            public Node Node;
            public int Depth;
        }
        #endregion

        #region Constants
        const float PADDING = 4f;
        const float SEARCH_HEIGHT = 18f;
        const float ROW_HEIGHT = 20f;
        const float INDENT_WIDTH = 14f;
        const float MIN_WIDTH = 240f;
        const float MAX_LIST_HEIGHT = 300f;
        const float SCROLLBAR_WIDTH = 16f;

        const int NONE_VALUE = 0;
        const string NONE_LABEL = "(None)";
        const string EMPTY_LABEL = "No match";

        static readonly Color SELECTED_TINT = new Color(0.24f, 0.48f, 0.90f, 0.35f);
        static readonly Color HIGHLIGHT_TINT = new Color(1f, 1f, 1f, 0.08f);
        #endregion

        #region Fields
        readonly List<HDropdownOption> all = new List<HDropdownOption>();
        readonly List<Row> rows = new List<Row>();
        readonly Action<int> onPicked;
        readonly bool useSearch;
        readonly int currentValue;
        readonly float width;

        Node root;
        SearchField searchField;
        string query = string.Empty;
        string keyword = string.Empty;
        Vector2 scroll;
        int highlight;
        #endregion

        #region Constructors
        public HDropdownSearchPopup(
            IReadOnlyList<HDropdownOption> options,
            bool allowNone,
            int searchThreshold,
            int currentValue,
            float width,
            Action<int> onPicked) {

            this.onPicked = onPicked;
            this.currentValue = currentValue;
            this.width = Mathf.Max(width, MIN_WIDTH);

            if (allowNone) all.Add(new HDropdownOption(NONE_VALUE, NONE_LABEL));
            for (int k = 0; k < options.Count; k++) all.Add(options[k]);

            // 검색창 표시 여부는 HDropdownAttribute.SearchThreshold 가 정한다.
            // 기본값 0 이면 항목이 하나라도 있는 한 항상 켜진다.
            useSearch = all.Count > searchThreshold;

            _BuildTree();
            _ExpandTowardCurrent(root);
            _RebuildRows();
            highlight = _IndexOfValue(currentValue);
        }
        #endregion

        #region PopupWindowContent
        public override Vector2 GetWindowSize() {
            // 목록이 비어도 "No match" 한 줄 자리는 남긴다.
            float count = Mathf.Max(rows.Count, 1);
            float listHeight = Mathf.Min(count * ROW_HEIGHT, MAX_LIST_HEIGHT);
            float searchHeight = useSearch ? SEARCH_HEIGHT + PADDING : 0f;

            return new Vector2(width, searchHeight + listHeight + PADDING * 2f);
        }

        public override void OnOpen() {
            if (!useSearch) return;

            searchField = new SearchField();
            searchField.SetFocus();
        }

        public override void OnClose() {
            searchField = null;
        }

        public override void OnGUI(Rect rect) {
            // SearchField 가 방향키를 먹기 전에 먼저 처리한다 (헤더 주의사항 2).
            _HandleKeys();

            Rect body = new Rect(
                rect.x + PADDING,
                rect.y + PADDING,
                rect.width - PADDING * 2f,
                rect.height - PADDING * 2f);

            if (useSearch) {
                Rect searchRect = new Rect(body.x, body.y, body.width, SEARCH_HEIGHT);
                string next = searchField.OnToolbarGUI(searchRect, query);

                if (!string.Equals(next, query, StringComparison.Ordinal)) {
                    query = next;
                    keyword = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
                    _RebuildRows();
                    highlight = 0;
                    scroll = Vector2.zero;
                }

                body.y += SEARCH_HEIGHT + PADDING;
                body.height -= SEARCH_HEIGHT + PADDING;
            }

            _DrawList(body);
        }
        #endregion

        #region Private - Tree
        void _BuildTree() {
            root = new Node { IsFolder = true, Expanded = true };

            var folders = new Dictionary<string, Node>(StringComparer.Ordinal);

            for (int k = 0; k < all.Count; k++) {
                string label = all[k].Label ?? string.Empty;
                int cut = label.LastIndexOf('/');

                Node parent = root;
                string leafName = label;

                if (cut >= 0) {
                    parent = _EnsureFolder(folders, label.Substring(0, cut));
                    leafName = label.Substring(cut + 1);
                }

                parent.Children.Add(new Node {
                    Name = leafName,
                    FullLabel = label,
                    Value = all[k].Value,
                    IsFolder = false,
                });
            }
        }

        Node _EnsureFolder(Dictionary<string, Node> folders, string path) {
            if (folders.TryGetValue(path, out Node found)) return found;

            int cut = path.LastIndexOf('/');
            Node parent = cut >= 0 ? _EnsureFolder(folders, path.Substring(0, cut)) : root;
            string name = cut >= 0 ? path.Substring(cut + 1) : path;

            var created = new Node { Name = name, IsFolder = true, Expanded = false };
            parent.Children.Add(created);
            folders[path] = created;

            return created;
        }

        /// <summary> 현재 값이 든 폴더 사슬만 펼친다. 선택된 항목이 처음부터 보이도록. </summary>
        bool _ExpandTowardCurrent(Node node) {
            bool hit = false;

            for (int k = 0; k < node.Children.Count; k++) {
                Node c = node.Children[k];

                if (!c.IsFolder) {
                    if (c.Value == currentValue) hit = true;
                    continue;
                }

                if (!_ExpandTowardCurrent(c)) continue;

                c.Expanded = true;
                hit = true;
            }

            return hit;
        }
        #endregion

        #region Private - Rows
        void _RebuildRows() {
            rows.Clear();

            // 검색 중에는 계층을 접지 않고 매칭된 잎만 평탄하게 보여준다.
            if (keyword.Length > 0) _CollectMatchingLeaves(root);
            else _CollectVisible(root, 0);
        }

        void _CollectVisible(Node node, int depth) {
            for (int k = 0; k < node.Children.Count; k++) {
                Node c = node.Children[k];
                rows.Add(new Row { Node = c, Depth = depth });

                if (c.IsFolder && c.Expanded) _CollectVisible(c, depth + 1);
            }
        }

        void _CollectMatchingLeaves(Node node) {
            for (int k = 0; k < node.Children.Count; k++) {
                Node c = node.Children[k];

                if (c.IsFolder) {
                    _CollectMatchingLeaves(c);
                    continue;
                }

                if (!_IsMatch(c.FullLabel, keyword)) continue;

                rows.Add(new Row { Node = c, Depth = 0 });
            }
        }

        static bool _IsMatch(string label, string key) {
            if (string.IsNullOrEmpty(key)) return true;
            if (string.IsNullOrEmpty(label)) return false;

            return label.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int _IndexOfValue(int value) {
            for (int k = 0; k < rows.Count; k++) {
                if (rows[k].Node.IsFolder) continue;
                if (rows[k].Node.Value == value) return k;
            }

            return 0;
        }
        #endregion

        #region Private - Draw
        void _DrawList(Rect body) {
            if (rows.Count == 0) {
                GUI.Label(new Rect(body.x, body.y, body.width, ROW_HEIGHT), EMPTY_LABEL, EditorStyles.miniLabel);
                return;
            }

            Rect view = new Rect(0f, 0f, body.width - SCROLLBAR_WIDTH, rows.Count * ROW_HEIGHT);
            scroll = GUI.BeginScrollView(body, scroll, view);

            for (int k = 0; k < rows.Count; k++) {
                Rect row = new Rect(0f, k * ROW_HEIGHT, view.width, ROW_HEIGHT);
                Node node = rows[k].Node;

                // 현재 값 표시가 커서 표시보다 우선한다. 둘을 겹쳐 칠하면 어느 쪽인지 구분이 안 된다.
                if (!node.IsFolder && node.Value == currentValue) EditorGUI.DrawRect(row, SELECTED_TINT);
                else if (k == highlight) EditorGUI.DrawRect(row, HIGHLIGHT_TINT);

                Rect content = row;
                content.x += rows[k].Depth * INDENT_WIDTH;
                content.width -= rows[k].Depth * INDENT_WIDTH;

                if (node.IsFolder) _DrawFolder(content, node);
                else _DrawLeaf(content, node);
            }

            GUI.EndScrollView();
        }

        void _DrawFolder(Rect rect, Node node) {
            bool next = EditorGUI.Foldout(rect, node.Expanded, node.Name, true);
            if (next == node.Expanded) return;

            node.Expanded = next;
            _RebuildRows();
        }

        void _DrawLeaf(Rect rect, Node node) {
            // 검색 중에는 어느 폴더의 항목인지 알아야 하므로 전체 라벨을 보여준다.
            string text = keyword.Length > 0 ? node.FullLabel : node.Name;

            if (GUI.Button(rect, new GUIContent(text), EditorStyles.label)) _Pick(node.Value);
        }
        #endregion

        #region Private - Input
        void _HandleKeys() {
            Event e = Event.current;
            if (e == null) return;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode) {
            case KeyCode.DownArrow:
                _MoveHighlight(1);
                e.Use();
                break;
            case KeyCode.UpArrow:
                _MoveHighlight(-1);
                e.Use();
                break;
            case KeyCode.RightArrow:
                _SetHighlightedFolder(true);
                e.Use();
                break;
            case KeyCode.LeftArrow:
                _SetHighlightedFolder(false);
                e.Use();
                break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                _SubmitHighlighted();
                e.Use();
                break;
            case KeyCode.Escape:
                editorWindow.Close();
                e.Use();
                break;
            }
        }

        void _MoveHighlight(int delta) {
            if (rows.Count == 0) return;

            highlight = Mathf.Clamp(highlight + delta, 0, rows.Count - 1);
            _ScrollToHighlight();
            editorWindow.Repaint();
        }

        /// <summary> 강조 행이 보이도록 스크롤을 최소한만 민다. </summary>
        void _ScrollToHighlight() {
            float top = highlight * ROW_HEIGHT;
            float bottom = top + ROW_HEIGHT;

            if (top < scroll.y) scroll.y = top;
            else if (bottom > scroll.y + MAX_LIST_HEIGHT) scroll.y = bottom - MAX_LIST_HEIGHT;
        }

        /// <summary> 강조된 폴더를 펼치거나 접는다. 잎이면 아무것도 하지 않는다. </summary>
        void _SetHighlightedFolder(bool expanded) {
            if (!_TryGetHighlighted(out Node node)) return;
            if (!node.IsFolder) return;
            if (node.Expanded == expanded) return;

            node.Expanded = expanded;
            _RebuildRows();
            highlight = Mathf.Clamp(highlight, 0, Mathf.Max(rows.Count - 1, 0));
            editorWindow.Repaint();
        }

        /// <summary> 폴더면 접기/펼치기 토글, 잎이면 선택. </summary>
        void _SubmitHighlighted() {
            if (!_TryGetHighlighted(out Node node)) return;

            if (!node.IsFolder) {
                _Pick(node.Value);
                return;
            }

            _SetHighlightedFolder(!node.Expanded);
        }

        bool _TryGetHighlighted(out Node node) {
            node = null;
            if (rows.Count == 0) return false;
            if (highlight < 0 || highlight >= rows.Count) return false;

            node = rows[highlight].Node;
            return true;
        }

        void _Pick(int value) {
            onPicked?.Invoke(value);
            editorWindow.Close();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026-09-04 (수정) :: 계층 접기 복구
 *
 * # 변경
 * - 평탄 목록을 계층 트리로 되돌렸다. 라벨의 '/' 로 폴더 노드를 만들고 접고 펼친다.
 * - 검색어가 있으면 계층을 무시하고 매칭된 잎만 전체 라벨로 평탄하게 보여준다.
 * - 좌우 방향키로 폴더를 접고 펼친다. Enter 는 폴더면 토글, 잎이면 선택.
 * - 현재 값이 든 폴더 사슬만 열린 상태로 시작한다.
 *
 * # 이유
 * - 최초 구현에서 계층을 버렸는데, 그건 AdvancedDropdown 이 제공하던 기존 동작이었다.
 *   사용자가 지적해 드러났다. 검색을 넣자고 이미 있던 기능을 없애는 것은 교환이 아니라 손실이다.
 * - "평탄 목록 + 검색" 을 고를 때 그것이 기존 기능의 제거라는 점을 알리지 않은 것이 잘못이다.
 *   선택지 자체가 잘못 제시됐다.
 *
 * # 결과
 * - 계층 탐색과 검색을 둘 다 쓴다. 검색어 유무가 두 모드를 가른다.
 *
 * # 주의
 * - 창 크기는 GetWindowSize 가 열 때 한 번 계산한다. 폴더를 펼쳐 행이 늘어도 창은 그대로고
 *   스크롤로 처리된다. 창을 다시 재는 공개 경로가 없다.
 *
 * =============================================================================
 * @Jason - PKH 2026-09-04 (최초 설계) :: AdvancedDropdown 을 대체하는 검색 팝업
 *
 * # 변경
 * - PopupWindowContent 기반 팝업 신설. HDropdownField 가 이것을 연다.
 *
 * # 이유
 * - AdvancedDropdown 으로는 검색창을 띄울 수 없다. 메타데이터 실측 결과 그 타입이
 *   서브클래스에 여는 멤버는 minimumSize / BuildRoot / ItemSelected 세 개뿐이고,
 *   검색 상태(searchable, isSearchFieldDisabled)는 internal AdvancedDropdownWindow 가
 *   갖는다. 외부 어셈블리에서 상속도 호출도 불가하다.
 * - internal 멤버 리플렉션 우회는 택하지 않았다. 라이브러리 코드가 에디터 버전마다 깨진다.
 *
 * # 결과
 * - SearchThreshold 가 Odin 설치 여부와 무관하게 동작한다. HInspector 가 자기 계약을
 *   자기 렌더러로 지키게 됐고, Odin 브릿지는 같은 계약의 다른 구현이 됐다.
 * - 매칭은 IndexOf(keyword, OrdinalIgnoreCase) >= 0. 런타임 HUI.Dropdown 과 같은 관용구다.
 *
 * # 주의
 * - 실기 확인이 필요하다. 컴파일과 API 유효성은 검증했으나 Play/Inspector 에서의
 *   포커스 이동과 스크롤 감각은 눈으로 봐야 한다.
 * =============================================================================
 */
#endif
