#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HDropdown] 필드가 여는 선택 팝업입니다. 검색 필드 + 평탄한 항목 목록으로 구성됩니다.
 *
 * 특징 ::
 * PopupWindowContent 위에 직접 그립니다. UnityEditor 의 AdvancedDropdown 을 쓰지 않는 이유는
 * 그 타입이 검색 관련 멤버를 하나도 공개하지 않아 검색창을 띄울 수 없기 때문입니다.
 * (검색 상태는 internal AdvancedDropdownWindow 소유. 메타데이터 실측.)
 *
 * 렌더 구성 ::
 * 1. 상단 : SearchField. HDropdownAttribute.SearchThreshold 이하이면 그리지 않습니다.
 * 2. 하단 : 스크롤 목록. 라벨을 가공하지 않고 그대로 보여줍니다 ("UI/600003_Click").
 *
 * 사용 ::
 * PopupWindow.Show(anchor, new HDropdownSearchPopup(...)). HDropdownField 가 호출합니다.
 *
 * 주의사항 ::
 * 1. 창 크기는 열 때 한 번만 정해집니다. 검색으로 목록이 줄어도 창은 줄지 않습니다.
 * 2. 방향키 / Enter / Esc 는 SearchField 보다 먼저 가로채 소비합니다. 순서가 뒤집히면
 *    검색 필드가 방향키를 먼저 먹어 목록 이동이 죽습니다.
 * 3. 라벨의 '/' 를 계층으로 접지 않습니다. 평탄 목록이 검색과 맞물리는 형태입니다.
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
        #region Constants
        const float PADDING = 4f;
        const float SEARCH_HEIGHT = 18f;
        const float ROW_HEIGHT = 20f;
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
        readonly List<HDropdownOption> shown = new List<HDropdownOption>();
        readonly Action<int> onPicked;
        readonly bool useSearch;
        readonly int currentValue;
        readonly float width;

        SearchField searchField;
        string query = string.Empty;
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

            _Filter();
            highlight = _IndexOfValue(currentValue);
        }
        #endregion

        #region PopupWindowContent
        public override Vector2 GetWindowSize() {
            // 목록이 비어도 "No match" 한 줄 자리는 남긴다.
            float rows = Mathf.Max(shown.Count, 1);
            float listHeight = Mathf.Min(rows * ROW_HEIGHT, MAX_LIST_HEIGHT);
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
                    _Filter();
                    highlight = 0;
                    scroll = Vector2.zero;
                }

                body.y += SEARCH_HEIGHT + PADDING;
                body.height -= SEARCH_HEIGHT + PADDING;
            }

            _DrawList(body);
        }
        #endregion

        #region Private - Draw
        void _DrawList(Rect body) {
            if (shown.Count == 0) {
                GUI.Label(new Rect(body.x, body.y, body.width, ROW_HEIGHT), EMPTY_LABEL, EditorStyles.miniLabel);
                return;
            }

            Rect view = new Rect(0f, 0f, body.width - SCROLLBAR_WIDTH, shown.Count * ROW_HEIGHT);
            scroll = GUI.BeginScrollView(body, scroll, view);

            for (int k = 0; k < shown.Count; k++) {
                Rect row = new Rect(0f, k * ROW_HEIGHT, view.width, ROW_HEIGHT);

                // 현재 값 표시가 커서 표시보다 우선한다. 둘을 겹쳐 칠하면 어느 쪽인지 구분이 안 된다.
                if (shown[k].Value == currentValue) EditorGUI.DrawRect(row, SELECTED_TINT);
                else if (k == highlight) EditorGUI.DrawRect(row, HIGHLIGHT_TINT);

                if (GUI.Button(row, new GUIContent(shown[k].Label), EditorStyles.label)) _Pick(shown[k].Value);
            }

            GUI.EndScrollView();
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
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                _PickHighlighted();
                e.Use();
                break;
            case KeyCode.Escape:
                editorWindow.Close();
                e.Use();
                break;
            }
        }

        void _MoveHighlight(int delta) {
            if (shown.Count == 0) return;

            highlight = Mathf.Clamp(highlight + delta, 0, shown.Count - 1);
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

        void _PickHighlighted() {
            if (shown.Count == 0) return;
            if (highlight < 0 || highlight >= shown.Count) return;

            _Pick(shown[highlight].Value);
        }

        void _Pick(int value) {
            onPicked?.Invoke(value);
            editorWindow.Close();
        }
        #endregion

        #region Private - Filter
        void _Filter() {
            shown.Clear();

            // 항목마다 Trim 하지 않는다. 키 입력 한 번에 한 번만 다듬어 할당을 줄인다.
            string keyword = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

            for (int k = 0; k < all.Count; k++) {
                if (!_IsMatch(all[k].Label, keyword)) continue;
                shown.Add(all[k]);
            }
        }

        static bool _IsMatch(string label, string keyword) {
            if (string.IsNullOrEmpty(keyword)) return true;
            if (string.IsNullOrEmpty(label)) return false;

            return label.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int _IndexOfValue(int value) {
            for (int k = 0; k < shown.Count; k++) {
                if (shown[k].Value == value) return k;
            }

            return 0;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026-09-04 (최초 설계) :: AdvancedDropdown 을 대체하는 검색 팝업
 *
 * # 변경
 * - PopupWindowContent 기반 팝업 신설. 검색 필드 + 평탄한 스크롤 목록.
 * - HDropdownField 가 HDropdownSelector(AdvancedDropdown) 대신 이것을 연다.
 *
 * # 이유
 * - AdvancedDropdown 으로는 검색창을 띄울 수 없다. 메타데이터 실측 결과 그 타입이
 *   서브클래스에 여는 멤버는 minimumSize / BuildRoot / ItemSelected 세 개뿐이고,
 *   검색 상태(searchable, isSearchFieldDisabled)는 internal AdvancedDropdownWindow 가
 *   갖는다. 외부 어셈블리에서 상속도 호출도 불가하다.
 * - internal 멤버 리플렉션 우회는 택하지 않았다. 라이브러리 코드가 에디터 버전마다 깨진다.
 * - 계층 트리 재구현 대신 평탄 목록을 골랐다. 검색이 주 동선이면 계층 탐색의 값이 떨어지고,
 *   폴더 진입/복귀와 키보드 네비게이션을 직접 구현하는 비용이 이득보다 크다.
 *
 * # 결과
 * - SearchThreshold 가 Odin 설치 여부와 무관하게 동작한다. HInspector 가 자기 계약을
 *   자기 렌더러로 지키게 됐고, Odin 브릿지는 같은 계약의 다른 구현이 됐다.
 * - 매칭은 IndexOf(keyword, OrdinalIgnoreCase) >= 0. 런타임 HUI.Dropdown 과 같은 관용구다.
 *
 * # 주의
 * - 라벨의 '/' 계층 접기가 사라졌다. AdvancedDropdown 이 공짜로 주던 기능이다.
 *   계층이 필요해지면 이 파일에 트리 모드를 더하는 쪽이 맞다.
 * - 창 크기는 GetWindowSize 가 열 때 한 번 계산한다. 검색으로 목록이 줄어도 창은 그대로다.
 * - 실기 확인이 필요하다. 컴파일과 API 유효성은 검증했으나 Play/Inspector 에서의
 *   포커스 이동과 스크롤 감각은 눈으로 봐야 한다.
 * =============================================================================
 */
#endif
