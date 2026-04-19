#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HDictionary<TKey, TValue>의 Inspector 렌더를 Odin Dictionary 수준으로 재구성하는
 * 제네릭 PropertyDrawer입니다.
 *
 * 렌더 구성 (외곽 박스로 감싸 시각 경계를 명확히 함) ::
 * 1. Header Row 1 - Foldout + Count 뱃지 + 추가(+) 버튼
 * 2. Header Row 2 - Sort by Key 버튼 (접힌 상태에서는 숨김)
 * 3. Search       - Entry 수가 SEARCH_THRESHOLD 이상이면 표시되는 Key 검색 필드
 * 4. Entries      - ReorderableList 기반 드래그 재정렬 + 한 행 [Key | Value | X]
 *
 * 특수 동작 ::
 * 중복 Key 감지 시 해당 행들에 붉은 오버레이 배경 표시 (하드 에러 정책의 시각적 경고)
 * 빈 Dictionary는 "Dictionary is empty - use + to add an entry." 안내 메시지 표시
 * Value가 복합 타입(Vector3, struct 등)이면 자동 foldout + 자식 필드 표시
 * + 버튼으로 신규 Entry 추가 시 직전 요소의 값이 아닌 기본값(0/null/empty)으로 초기화
 *   (SerializedProperty.InsertArrayElementAtIndex는 기존 요소를 복제하므로,
 *    삽입 직후 모든 하위 프로퍼티를 타입별로 기본값 설정하는 재귀 리셋을 수행)
 *
 * 중복 검증 정책 ::
 * 중복 Key가 존재하는 상태에서는 HDictionaryValidator가 Play Mode 진입, Build,
 * Scene/Asset Save를 모두 차단하고 Debug.LogError로 해당 경로를 출력한다.
 * 이 Drawer는 그 상태를 UI에서 시각화하는 역할만 맡는다.
 *
 * 캐시 전략 ::
 * ReorderableList / 검색어 / 중복 인덱스 집합은 (InstanceID + propertyPath) 키로 캐시
 * 이유: ReorderableList는 드래그 hot-index 등 UI 상태를 내부에 보유하므로
 * 매 OnGUI마다 새로 만들면 드래그가 풀린다
 *
 * 주의사항 ::
 * SerializedProperty에서 private 필드명 "entries"/"logDuplicateKeyWarning"을 직접 참조
 * → HDictionary 쪽 필드명을 바꾸면 이 Drawer도 동기화해야 함 (호환성 계약)
 * Sort/Search는 PropertyToString 기반 단순 문자열 비교이므로
 * 사용자 정의 타입 Key는 ToString()에 의존
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HCollection.Editor {
    [CustomPropertyDrawer(typeof(HDictionary<,>), true)]
    public class HDictionaryDrawer : PropertyDrawer {
        #region Constants
        const string ENTRIES_FIELD = "entries";
        const string KEY_FIELD = "Key";
        const string VALUE_FIELD = "Value";

        const float HEADER_HEIGHT = 20f;
        const float HEADER_ROW_GAP = 2f;
        const float BOX_PADDING_HORIZONTAL = 10f;
        const float BOX_PADDING_VERTICAL = 8f;
        const float FOLDOUT_INNER_OFFSET = 4f;
        const float ADD_BUTTON_RIGHT_PADDING = 2f;
        const float SEARCH_HEIGHT = 20f;
        const float SEARCH_TOP_GAP = 2f;
        const float EMPTY_HEIGHT = 24f;
        const float ADD_BUTTON_WIDTH = 22f;
        const float REMOVE_BUTTON_WIDTH = 22f;
        const float KEY_VALUE_GAP = 4f;
        const float ROW_VERTICAL_PADDING = 2f;
        const float SECTION_GAP = 2f;

        // 컨테이너 타입(struct/class/array) Value를 감싸는 내부 박스의 패딩
        const float CONTAINER_BOX_PADDING = 4f;
        // HTitle 스타일 헤더 치수 (타이틀 라인 + 구분선)
        const float CONTAINER_TITLE_LINE_GAP = 2f;
        const float CONTAINER_TITLE_LINE_THICKNESS = 1f;
        const float CONTAINER_TITLE_BOTTOM_GAP = 3f;
        const float CONTAINER_CHILD_GAP = 2f;
        // 좁은 Value 영역 안에서 자식 필드의 라벨 비율과 최소 폭
        const float VALUE_LABEL_WIDTH_RATIO = 0.4f;
        const float VALUE_LABEL_WIDTH_MIN = 40f;

        const int SEARCH_THRESHOLD = 10;

        static readonly Color DUPLICATE_COLOR = new Color(0.85f, 0.20f, 0.20f, 0.22f);
        #endregion

        #region Static Caches
        static GUIStyle emptyStyle;
        static GUIStyle boxStyle;
        static GUIStyle innerBoxStyle;
        static GUIStyle titleStyle;

        static readonly Dictionary<string, ReorderableList> listCache = new();
        static readonly Dictionary<string, string> searchCache = new();
        static readonly Dictionary<string, HashSet<int>> duplicateCache = new();
        #endregion

        #region IDisposable Scopes
        // 좁은 셀 안에서 PropertyField를 그릴 때 라벨이 입력란을 삼키는 현상을 막기 위해
        // EditorGUIUtility.labelWidth와 EditorGUI.indentLevel(둘 다 IMGUI 전역 상태)을
        // 임시 축소/0으로 바꿨다가 using 블록이 끝나면 복원한다.
        private readonly struct CompactLabelScope : System.IDisposable {
            private readonly float _previousLabelWidth;
            private readonly int _previousIndent;

            public CompactLabelScope(float contentWidth) {
                _previousLabelWidth = EditorGUIUtility.labelWidth;
                _previousIndent = EditorGUI.indentLevel;
                EditorGUIUtility.labelWidth = Mathf.Max(VALUE_LABEL_WIDTH_MIN, contentWidth * VALUE_LABEL_WIDTH_RATIO);
                EditorGUI.indentLevel = 0;
            }

            public void Dispose() {
                EditorGUIUtility.labelWidth = _previousLabelWidth;
                EditorGUI.indentLevel = _previousIndent;
            }
        }
        #endregion

        #region PropertyDrawer Overrides
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            SerializedProperty entriesProperty = property.FindPropertyRelative(ENTRIES_FIELD);
            if (entriesProperty == null) return EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return BOX_PADDING_VERTICAL * 2f + HEADER_HEIGHT;

            float height = BOX_PADDING_VERTICAL * 2f;
            height += HEADER_HEIGHT;
            height += HEADER_ROW_GAP + HEADER_HEIGHT;
            height += SECTION_GAP;

            bool showSearch = entriesProperty.arraySize >= SEARCH_THRESHOLD;
            if (showSearch) height += SEARCH_TOP_GAP + SEARCH_HEIGHT + SECTION_GAP;

            bool filtered = showSearch && _HasActiveSearch(property);
            if (entriesProperty.arraySize == 0) {
                height += EMPTY_HEIGHT;
            } else if (filtered) {
                height += _GetFilteredListHeight(property, entriesProperty);
            } else {
                ReorderableList list = _GetOrCreateList(property, entriesProperty);
                height += list.GetHeight();
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty entriesProperty = property.FindPropertyRelative(ENTRIES_FIELD);
            if (entriesProperty == null) {
                EditorGUI.LabelField(position, label.text, "(invalid HDictionary)");
                return;
            }

            _UpdateDuplicateIndices(property, entriesProperty);
            _DrawBox(position);

            Rect cursor = new Rect(
                position.x + BOX_PADDING_HORIZONTAL,
                position.y + BOX_PADDING_VERTICAL,
                position.width - BOX_PADDING_HORIZONTAL * 2f,
                position.height - BOX_PADDING_VERTICAL * 2f
            );

            Rect titleRect = new Rect(cursor.x, cursor.y, cursor.width, HEADER_HEIGHT);
            _DrawHeaderTitle(titleRect, property, entriesProperty, label);
            cursor.y += HEADER_HEIGHT;

            if (!property.isExpanded) return;

            cursor.y += HEADER_ROW_GAP;
            Rect controlsRect = new Rect(cursor.x, cursor.y, cursor.width, HEADER_HEIGHT);
            _DrawHeaderControls(controlsRect, property, entriesProperty);
            cursor.y += HEADER_HEIGHT + SECTION_GAP;

            bool showSearch = entriesProperty.arraySize >= SEARCH_THRESHOLD;
            if (showSearch) {
                cursor.y += SEARCH_TOP_GAP;
                Rect searchRect = new Rect(cursor.x, cursor.y, cursor.width, SEARCH_HEIGHT);
                _DrawSearch(searchRect, property);
                cursor.y += SEARCH_HEIGHT + SECTION_GAP;
            }

            bool filtered = showSearch && _HasActiveSearch(property);
            if (entriesProperty.arraySize == 0) {
                Rect emptyRect = new Rect(cursor.x, cursor.y, cursor.width, EMPTY_HEIGHT);
                _DrawEmptyMessage(emptyRect);
            } else if (filtered) {
                float filteredHeight = _GetFilteredListHeight(property, entriesProperty);
                Rect listRect = new Rect(cursor.x, cursor.y, cursor.width, filteredHeight);
                _DrawFilteredList(listRect, property, entriesProperty);
            } else {
                ReorderableList list = _GetOrCreateList(property, entriesProperty);
                float listHeight = list.GetHeight();
                Rect listRect = new Rect(cursor.x, cursor.y, cursor.width, listHeight);
                list.DoList(listRect);
            }
        }
        #endregion

        #region Box
        private static void _DrawBox(Rect rect) {
            if (Event.current.type != EventType.Repaint) return;
            _GetBoxStyle().Draw(rect, false, false, false, false);
        }

        private static GUIStyle _GetBoxStyle() {
            if (boxStyle != null) return boxStyle;

            // Unity ReorderableList가 내부적으로 사용하는 boxBackground를 그대로 재사용하면
            // List 컨테이너와 픽셀 단위로 동일한 외형을 얻는다.
            // 공개 API: UnityEditorInternal.ReorderableList.defaultBehaviours.boxBackground
            GUIStyle listBoxBackground = ReorderableList.defaultBehaviours?.boxBackground;
            if (listBoxBackground != null && listBoxBackground.normal.background != null) {
                boxStyle = listBoxBackground;
                return boxStyle;
            }

            // Unity 버전/스킨에 따라 접근이 실패하면 helpBox로 폴백
            boxStyle = new GUIStyle(EditorStyles.helpBox) {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            return boxStyle;
        }
        #endregion

        #region Header
        private void _DrawHeaderTitle(Rect rect, SerializedProperty property, SerializedProperty entriesProperty, GUIContent label) {
            Rect foldoutRect = new Rect(
                rect.x + FOLDOUT_INNER_OFFSET,
                rect.y,
                rect.width - ADD_BUTTON_WIDTH - ADD_BUTTON_RIGHT_PADDING - FOLDOUT_INNER_OFFSET - 4f,
                rect.height);
            Rect addButtonRect = new Rect(
                rect.xMax - ADD_BUTTON_WIDTH - ADD_BUTTON_RIGHT_PADDING,
                rect.y + 1f,
                ADD_BUTTON_WIDTH,
                rect.height - 2f);

            string countText = $"  ({entriesProperty.arraySize})";
            GUIContent headerLabel = new GUIContent(label.text + countText, label.tooltip);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, headerLabel, true);

            if (GUI.Button(addButtonRect, "+", EditorStyles.miniButton)) {
                int insertIndex = entriesProperty.arraySize;
                entriesProperty.InsertArrayElementAtIndex(insertIndex);
                SerializedProperty inserted = entriesProperty.GetArrayElementAtIndex(insertIndex);
                _ResetElementToDefault(inserted);
                property.isExpanded = true;
                property.serializedObject.ApplyModifiedProperties();
                GUI.FocusControl(null);
            }
        }

        private void _DrawHeaderControls(Rect rect, SerializedProperty property, SerializedProperty entriesProperty) {
            // 현재는 Sort 버튼 1개만 배치. 향후 추가 컨트롤이 생기면 이 레이아웃을 분할 확장.
            bool canSort = entriesProperty.arraySize >= 2;
            using (new EditorGUI.DisabledScope(!canSort)) {
                if (GUI.Button(rect, "Sort by Key", EditorStyles.miniButton)) {
                    _SortByKey(property, entriesProperty);
                }
            }
        }
        #endregion

        #region Search
        private void _DrawSearch(Rect rect, SerializedProperty property) {
            string cacheKey = _GetCacheKey(property);
            searchCache.TryGetValue(cacheKey, out string current);
            current ??= string.Empty;

            Rect labelRect = new Rect(rect.x, rect.y, 50f, rect.height);
            Rect fieldRect = new Rect(rect.x + 54f, rect.y, rect.width - 54f, rect.height);

            EditorGUI.LabelField(labelRect, "Search");
            string next = EditorGUI.TextField(fieldRect, current);
            if (next != current) searchCache[cacheKey] = next;
        }

        private bool _HasActiveSearch(SerializedProperty property) {
            string cacheKey = _GetCacheKey(property);
            return searchCache.TryGetValue(cacheKey, out string s) && !string.IsNullOrEmpty(s);
        }
        #endregion

        #region Empty State
        private void _DrawEmptyMessage(Rect rect) {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.08f));
            EditorGUI.LabelField(rect, "Dictionary is empty — use + to add an entry.", _GetEmptyStyle());
        }
        #endregion

        #region ReorderableList
        private ReorderableList _GetOrCreateList(SerializedProperty property, SerializedProperty entriesProperty) {
            string cacheKey = _GetCacheKey(property);
            if (listCache.TryGetValue(cacheKey, out ReorderableList cached) && _IsCachedListValid(cached, property)) {
                return cached;
            }
            // stale 캐시는 제거. 아래에서 새로 생성된다.
            if (cached != null) listCache.Remove(cacheKey);

            ReorderableList list = new ReorderableList(
                property.serializedObject,
                entriesProperty,
                draggable: true,
                displayHeader: false,
                displayAddButton: false,
                displayRemoveButton: false
            );

            SerializedProperty capturedEntries = entriesProperty;
            SerializedProperty capturedRoot = property;

            list.elementHeightCallback = (index) => _GetRowHeight(capturedEntries, index);
            list.drawElementCallback = (rect, index, active, focused) => {
                _DrawRow(rect, capturedRoot, capturedEntries, index);
            };
            list.drawElementBackgroundCallback = (rect, index, active, focused) => {
                if (Event.current.type != EventType.Repaint) return;
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, true);
                if (_IsDuplicateRow(capturedRoot, index)) EditorGUI.DrawRect(rect, DUPLICATE_COLOR);
            };
            list.onReorderCallback = (_) => {
                capturedRoot.serializedObject.ApplyModifiedProperties();
            };

            listCache[cacheKey] = list;
            return list;
        }

        private float _GetRowHeight(SerializedProperty entriesProperty, int index) {
            if (index < 0 || index >= entriesProperty.arraySize) return EditorGUIUtility.singleLineHeight + ROW_VERTICAL_PADDING * 2f;

            SerializedProperty element = entriesProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProperty = element.FindPropertyRelative(KEY_FIELD);
            SerializedProperty valueProperty = element.FindPropertyRelative(VALUE_FIELD);

            float keyHeight = _GetCellHeight(keyProperty);
            float valueHeight = _GetCellHeight(valueProperty);

            return Mathf.Max(keyHeight, valueHeight) + ROW_VERTICAL_PADDING * 2f;
        }

        private static float _GetCellHeight(SerializedProperty property) {
            if (property == null) return EditorGUIUtility.singleLineHeight;
            if (!_IsContainerType(property)) return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            // 컨테이너: 박스 패딩 + 타이틀 헤더 + (expanded일 때 자식 합산)
            float height = CONTAINER_BOX_PADDING * 2f + _GetContainerTitleHeight();
            if (property.isExpanded) {
                height += CONTAINER_TITLE_BOTTOM_GAP + _GetContainerChildrenHeight(property);
            }
            return height;
        }

        private static float _GetContainerChildrenHeight(SerializedProperty property) {
            float total = 0f;
            bool isFirst = true;
            foreach (SerializedProperty child in _EnumerateImmediateChildren(property)) {
                if (!isFirst) total += CONTAINER_CHILD_GAP;
                total += EditorGUI.GetPropertyHeight(child, true);
                isFirst = false;
            }
            return total;
        }

        private void _DrawRow(Rect rect, SerializedProperty property, SerializedProperty entriesProperty, int index) {
            if (index < 0 || index >= entriesProperty.arraySize) return;

            SerializedProperty element = entriesProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProperty = element.FindPropertyRelative(KEY_FIELD);
            SerializedProperty valueProperty = element.FindPropertyRelative(VALUE_FIELD);

            rect.y += ROW_VERTICAL_PADDING;
            rect.height -= ROW_VERTICAL_PADDING * 2f;

            float contentWidth = rect.width - REMOVE_BUTTON_WIDTH - KEY_VALUE_GAP;
            float halfWidth = (contentWidth - KEY_VALUE_GAP) * 0.5f;

            Rect keyRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
            Rect valueRect = new Rect(rect.x + halfWidth + KEY_VALUE_GAP, rect.y, halfWidth, rect.height);
            Rect removeRect = new Rect(rect.xMax - REMOVE_BUTTON_WIDTH, rect.y, REMOVE_BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);

            _DrawCell(keyRect, keyProperty);
            _DrawCell(valueRect, valueProperty);

            if (GUI.Button(removeRect, "X", EditorStyles.miniButton)) {
                entriesProperty.DeleteArrayElementAtIndex(index);
                property.serializedObject.ApplyModifiedProperties();
                GUI.FocusControl(null);
            }
        }

        private static void _DrawCell(Rect rect, SerializedProperty property) {
            if (property == null) return;

            if (_IsContainerType(property)) {
                _DrawContainerCell(rect, property);
                return;
            }

            _DrawSimpleCell(rect, property);
        }

        private static void _DrawSimpleCell(Rect rect, SerializedProperty property) {
            using (new CompactLabelScope(rect.width)) {
                EditorGUI.PropertyField(rect, property, GUIContent.none, true);
            }
        }

        private static void _DrawContainerCell(Rect rect, SerializedProperty property) {
            // 1단: 외곽 내부 박스
            if (Event.current.type == EventType.Repaint) {
                _GetInnerBoxStyle().Draw(rect, false, false, false, false);
            }

            // 2단: HTitle 스타일 헤더 (클릭 시 접기/펼치기 토글)
            Rect titleRect = new Rect(
                rect.x + CONTAINER_BOX_PADDING,
                rect.y + CONTAINER_BOX_PADDING,
                rect.width - CONTAINER_BOX_PADDING * 2f,
                _GetContainerTitleHeight());
            _DrawContainerTitle(titleRect, property);

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && titleRect.Contains(Event.current.mousePosition)) {
                property.isExpanded = !property.isExpanded;
                Event.current.Use();
                GUI.changed = true;
            }

            if (!property.isExpanded) return;

            // 3단: 자식 필드 영역 (수작업 iteration으로 foldout 중복 제거)
            Rect childrenRect = new Rect(
                rect.x + CONTAINER_BOX_PADDING,
                titleRect.yMax + CONTAINER_TITLE_BOTTOM_GAP,
                rect.width - CONTAINER_BOX_PADDING * 2f,
                rect.height - CONTAINER_BOX_PADDING * 2f - _GetContainerTitleHeight() - CONTAINER_TITLE_BOTTOM_GAP);

            using (new CompactLabelScope(childrenRect.width)) {
                _DrawContainerChildren(childrenRect, property);
            }
        }

        private static void _DrawContainerTitle(Rect rect, SerializedProperty property) {
            string typeName = _GetDisplayTypeName(property);

            Rect labelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, typeName, _GetTitleStyle());

            float lineY = labelRect.yMax + CONTAINER_TITLE_LINE_GAP;
            Color lineColor = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.45f, 0.45f)
                : new Color(0.55f, 0.55f, 0.55f);
            Rect lineRect = new Rect(rect.x, lineY, rect.width, CONTAINER_TITLE_LINE_THICKNESS);
            EditorGUI.DrawRect(lineRect, lineColor);
        }

        private static void _DrawContainerChildren(Rect rect, SerializedProperty property) {
            float cursorY = rect.y;
            bool isFirst = true;
            foreach (SerializedProperty child in _EnumerateImmediateChildren(property)) {
                if (!isFirst) cursorY += CONTAINER_CHILD_GAP;
                float h = EditorGUI.GetPropertyHeight(child, true);
                Rect childRect = new Rect(rect.x, cursorY, rect.width, h);
                EditorGUI.PropertyField(childRect, child, true);
                cursorY += h;
                isFirst = false;
            }
        }

        private static IEnumerable<SerializedProperty> _EnumerateImmediateChildren(SerializedProperty property) {
            // Generic 타입의 immediate children만 순회하는 정규 이터레이터.
            // property.Copy()로 독립 핸들을 만들고, GetEndProperty()로 경계를 확보한 뒤,
            // NextVisible(true)로 첫 자식 진입 + NextVisible(false)로 형제 이동을 반복한다.
            // _DrawContainerChildren(렌더)과 _GetContainerChildrenHeight(높이 합산) 양쪽에서 재사용.
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            if (!iterator.NextVisible(enterChildren: true)) yield break;

            while (!SerializedProperty.EqualContents(iterator, end)) {
                yield return iterator;
                if (!iterator.NextVisible(enterChildren: false)) yield break;
            }
        }

        private static float _GetContainerTitleHeight() {
            return EditorGUIUtility.singleLineHeight + CONTAINER_TITLE_LINE_GAP + CONTAINER_TITLE_LINE_THICKNESS;
        }

        private static string _GetDisplayTypeName(SerializedProperty property) {
            string type = property.type;
            if (string.IsNullOrEmpty(type)) return property.displayName;
            // 제네릭 컬렉션은 Unity 내부 표현("vector<T>")이 나오므로 현재는 그대로 노출.
            // 필요 시 후속 작업에서 정규화 로직 추가.
            return type;
        }

        private static bool _IsContainerType(SerializedProperty property) {
            if (property == null) return false;
            if (property.propertyType != SerializedPropertyType.Generic) return false;
            return property.hasVisibleChildren;
        }

        private static GUIStyle _GetInnerBoxStyle() {
            if (innerBoxStyle != null) return innerBoxStyle;
            // 외곽 HDictionary 박스는 ReorderableList.boxBackground이고, 내부 컨테이너는 helpBox
            // 서로 다른 9-slice 스타일이라 "외곽 리스트 박스 안의 컨테이너"라는 계층이 시각적으로 구분된다.
            innerBoxStyle = new GUIStyle(EditorStyles.helpBox) {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            return innerBoxStyle;
        }

        private static GUIStyle _GetTitleStyle() {
            if (titleStyle != null) return titleStyle;
            // HInspector의 HTitle과 동일한 볼드/정렬 방침을 HDictionary 쪽에서 재현.
            // asmdef 경계를 넘지 않기 위해 참조 대신 동일한 수치로 자체 스타일을 구성한다.
            titleStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            return titleStyle;
        }
        #endregion

        #region Filtered List (Search Active)
        private float _GetFilteredListHeight(SerializedProperty property, SerializedProperty entriesProperty) {
            string search = _GetSearch(property);
            float total = 0f;
            int matched = 0;

            for (int i = 0; i < entriesProperty.arraySize; i++) {
                if (!_MatchesSearch(entriesProperty, i, search)) continue;
                total += _GetRowHeight(entriesProperty, i);
                matched++;
            }

            if (matched == 0) return EMPTY_HEIGHT;
            return total;
        }

        private void _DrawFilteredList(Rect rect, SerializedProperty property, SerializedProperty entriesProperty) {
            string search = _GetSearch(property);
            float cursorY = rect.y;
            int matched = 0;

            for (int i = 0; i < entriesProperty.arraySize; i++) {
                if (!_MatchesSearch(entriesProperty, i, search)) continue;

                float rowHeight = _GetRowHeight(entriesProperty, i);
                Rect rowRect = new Rect(rect.x, cursorY, rect.width, rowHeight);

                if (Event.current.type == EventType.Repaint && _IsDuplicateRow(property, i)) {
                    EditorGUI.DrawRect(rowRect, DUPLICATE_COLOR);
                }

                _DrawRow(rowRect, property, entriesProperty, i);
                cursorY += rowHeight;
                matched++;
            }

            if (matched == 0) {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EMPTY_HEIGHT),
                    $"No keys match '{search}'.", _GetEmptyStyle());
            }
        }

        private bool _MatchesSearch(SerializedProperty entriesProperty, int index, string search) {
            if (string.IsNullOrEmpty(search)) return true;
            SerializedProperty keyProperty = entriesProperty.GetArrayElementAtIndex(index).FindPropertyRelative(KEY_FIELD);
            string keyText = _PropertyToString(keyProperty);
            return keyText != null && keyText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string _GetSearch(SerializedProperty property) {
            string cacheKey = _GetCacheKey(property);
            return searchCache.TryGetValue(cacheKey, out string s) ? s : string.Empty;
        }
        #endregion

        #region Duplicate Detection
        private void _UpdateDuplicateIndices(SerializedProperty property, SerializedProperty entriesProperty) {
            string cacheKey = _GetCacheKey(property);
            if (!duplicateCache.TryGetValue(cacheKey, out HashSet<int> dupes)) {
                dupes = new HashSet<int>();
                duplicateCache[cacheKey] = dupes;
            }
            dupes.Clear();

            // 중복 키는 하드 에러 정책이므로 편집 도중이든 commit 후이든 일관되게 시각화한다.
            // 상위 HDictionaryValidator가 Play/Build/Save 경로를 차단하므로, 여기서는 UI 피드백에
            // 집중한다. 사용자가 "1" → "11"로 확장하는 중간에 "1"이 기존 "1"과 중복 표시되는 것은
            // 정상적인 상태 반영이며, 두 번째 "1" 입력 즉시 해소된다.
            Dictionary<string, int> firstSeen = new();
            for (int i = 0; i < entriesProperty.arraySize; i++) {
                SerializedProperty keyProperty = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative(KEY_FIELD);
                string keyText = _PropertyToString(keyProperty) ?? string.Empty;

                if (firstSeen.TryGetValue(keyText, out int firstIndex)) {
                    dupes.Add(firstIndex);
                    dupes.Add(i);
                    continue;
                }
                firstSeen[keyText] = i;
            }
        }

        private bool _IsDuplicateRow(SerializedProperty property, int index) {
            string cacheKey = _GetCacheKey(property);
            return duplicateCache.TryGetValue(cacheKey, out HashSet<int> dupes) && dupes.Contains(index);
        }
        #endregion

        #region Sort
        private void _SortByKey(SerializedProperty property, SerializedProperty entriesProperty) {
            int count = entriesProperty.arraySize;
            for (int i = 0; i < count - 1; i++) {
                for (int j = 0; j < count - i - 1; j++) {
                    SerializedProperty aKey = entriesProperty.GetArrayElementAtIndex(j).FindPropertyRelative(KEY_FIELD);
                    SerializedProperty bKey = entriesProperty.GetArrayElementAtIndex(j + 1).FindPropertyRelative(KEY_FIELD);
                    string aText = _PropertyToString(aKey) ?? string.Empty;
                    string bText = _PropertyToString(bKey) ?? string.Empty;
                    if (string.Compare(aText, bText, StringComparison.Ordinal) > 0) {
                        entriesProperty.MoveArrayElement(j, j + 1);
                    }
                }
            }
            property.serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Reset After Insert
        private static void _ResetElementToDefault(SerializedProperty property) {
            SerializedProperty end = property.GetEndProperty();
            SerializedProperty iterator = property.Copy();
            if (!iterator.Next(enterChildren: true)) return;

            while (!SerializedProperty.EqualContents(iterator, end)) {
                _ResetPropertyToDefault(iterator);
                if (!iterator.Next(enterChildren: false)) break;
            }
        }

        private static void _ResetPropertyToDefault(SerializedProperty property) {
            switch (property.propertyType) {
            case SerializedPropertyType.Integer:         property.intValue = 0; break;
            case SerializedPropertyType.Boolean:         property.boolValue = false; break;
            case SerializedPropertyType.Float:           property.floatValue = 0f; break;
            case SerializedPropertyType.String:          property.stringValue = string.Empty; break;
            case SerializedPropertyType.Color:           property.colorValue = default; break;
            case SerializedPropertyType.ObjectReference: property.objectReferenceValue = null; break;
            case SerializedPropertyType.LayerMask:       property.intValue = 0; break;
            case SerializedPropertyType.Enum:            property.enumValueIndex = 0; break;
            case SerializedPropertyType.Vector2:         property.vector2Value = default; break;
            case SerializedPropertyType.Vector3:         property.vector3Value = default; break;
            case SerializedPropertyType.Vector4:         property.vector4Value = default; break;
            case SerializedPropertyType.Rect:            property.rectValue = default; break;
            case SerializedPropertyType.Character:       property.intValue = 0; break;
            case SerializedPropertyType.AnimationCurve:  property.animationCurveValue = new AnimationCurve(); break;
            case SerializedPropertyType.Bounds:          property.boundsValue = default; break;
            case SerializedPropertyType.Quaternion:      property.quaternionValue = Quaternion.identity; break;
            case SerializedPropertyType.Vector2Int:      property.vector2IntValue = default; break;
            case SerializedPropertyType.Vector3Int:      property.vector3IntValue = default; break;
            case SerializedPropertyType.RectInt:         property.rectIntValue = default; break;
            case SerializedPropertyType.BoundsInt:       property.boundsIntValue = default; break;
            case SerializedPropertyType.Hash128:         property.hash128Value = default; break;
            case SerializedPropertyType.ArraySize:       property.intValue = 0; break;
            case SerializedPropertyType.ManagedReference: property.managedReferenceValue = null; break;
            case SerializedPropertyType.Generic:
                if (property.isArray) {
                    property.arraySize = 0;
                } else {
                    _ResetElementToDefault(property);
                }
                break;
            }
        }
        #endregion

        #region Helpers
        private static bool _IsCachedListValid(ReorderableList list, SerializedProperty currentProperty) {
            if (list == null) return false;

            SerializedProperty cachedProperty = list.serializedProperty;
            if (cachedProperty == null) return false;

            SerializedObject cachedSerializedObject = cachedProperty.serializedObject;
            if (cachedSerializedObject == null) return false;

            // Editor 세션이 바뀌면 SerializedObject가 새로 만들어진다. 참조가 다르면 stale.
            if (!ReferenceEquals(cachedSerializedObject, currentProperty.serializedObject)) return false;

            // 타겟 UnityEngine.Object가 파괴된 상태면 stale (Unity의 fake-null 포함).
            if (cachedSerializedObject.targetObject == null) return false;

            return true;
        }

        private static string _GetCacheKey(SerializedProperty property) {
            int instanceId = property.serializedObject.targetObject != null
                ? property.serializedObject.targetObject.GetInstanceID()
                : 0;
            return $"{instanceId}:{property.propertyPath}";
        }

        private static string _PropertyToString(SerializedProperty property) {
            if (property == null) return string.Empty;
            switch (property.propertyType) {
            case SerializedPropertyType.String:
                return property.stringValue ?? string.Empty;
            case SerializedPropertyType.Integer:
                return property.longValue.ToString();
            case SerializedPropertyType.Float:
                return property.doubleValue.ToString("R");
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.Character:
                return ((char)property.intValue).ToString();
            case SerializedPropertyType.Enum:
                int enumIndex = property.enumValueIndex;
                if (property.enumNames != null && enumIndex >= 0 && enumIndex < property.enumNames.Length) {
                    return property.enumNames[enumIndex];
                }
                return enumIndex.ToString();
            case SerializedPropertyType.ObjectReference:
                UnityEngine.Object reference = property.objectReferenceValue;
                return reference != null ? reference.name : "<null>";
            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();
            default:
                return property.propertyPath;
            }
        }

        private static GUIStyle _GetEmptyStyle() {
            if (emptyStyle != null) return emptyStyle;
            emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };
            return emptyStyle;
        }
        #endregion
    }
}
#endif
