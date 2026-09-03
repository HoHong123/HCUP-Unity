#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * [HDropdown] 필드의 실제 그리기 담당입니다. HInspectorPropertyDrawer 가 호출합니다.
 *
 * 주요 기능 ::
 * 1. 등록소에서 항목을 받아 검색 팝업(HDropdownSearchPopup)으로 고르게 합니다.
 * 2. 값이 목록에 없으면 "Missing (값)" 을 붉게 표시합니다.
 * 3. HDropdownAttribute.SearchThreshold 로 검색 필드 표시를 정합니다.
 *
 * 주의 ::
 * 1. int 필드 전용. 다른 타입은 기본 필드로 폴백합니다.
 * 2. 쓰기 경로가 이중이다 - 콜백에서 즉시 쓰고(빠른 경로), 실패에 대비해 보류 큐에도
 *    넣어 다음 OnGUI 에서 살아있는 프로퍼티로 다시 쓴다. 이유는 데브로그 참조.
 * 3. 이 파일은 Odin Inspector 미설치 환경에서만 실제로 그려집니다. Odin 설치 시에는
 *    HInspectorToOdinBridge 가 대신 처리합니다 (HInspector/Editor/Odin/README.md).
 * =========================================================
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace HInspector.Editor {
    public static class HDropdownField {
        #region Constants
        const int NONE_VALUE = 0;
        const string NONE_LABEL = "(None)";
        static readonly Color MISSING_TINT = new Color(1f, 0.55f, 0.55f);
        #endregion

        #region Fields
        // 드롭다운 콜백 → 다음 OnGUI 로 값을 넘기는 보류 큐.
        static readonly Dictionary<string, int> pendingPicks = new Dictionary<string, int>(StringComparer.Ordinal);
        #endregion

        #region Public
        public static void Draw(Rect position, SerializedProperty property, GUIContent label, HDropdownAttribute attribute) {
            if (property.propertyType != SerializedPropertyType.Integer) {
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.LabelField(position, " ", "[HDropdown] int only");
                return;
            }

            _ApplyPendingPick(property);

            bool hasSource = HDropdownSourceRegistry.TryGetOptions(attribute.SourceId, out var options);
            int value = property.intValue;

            string text;
            bool isMissing = false;

            if (value == NONE_VALUE && attribute.AllowNone) {
                text = NONE_LABEL;
            }
            else if (!hasSource) {
                text = $"Source not registered: {attribute.SourceId}";
                isMissing = true;
            }
            else if (_TryFindLabel(options, value, out string found)) {
                text = found;
            }
            else {
                text = $"Missing ({value})";
                isMissing = true;
            }

            Rect controlRect = EditorGUI.PrefixLabel(position, label);

            Color previousColor = GUI.color;
            if (isMissing) GUI.color = MISSING_TINT;

            bool pressed = EditorGUI.DropdownButton(controlRect, new GUIContent(text), FocusType.Keyboard);

            GUI.color = previousColor;

            if (!pressed) return;
            if (!hasSource) return;

            _OpenSelector(controlRect, property, options, attribute.AllowNone, attribute.SearchThreshold);
        }
        #endregion

        #region Private - Write
        /// <summary>
        /// 보류된 선택값을 지금 프레임의 살아있는 프로퍼티에 기록한다.
        /// 콜백의 즉시 쓰기가 통했다면 값이 이미 같아 아무 일도 하지 않는다.
        /// </summary>
        static void _ApplyPendingPick(SerializedProperty property) {
            if (pendingPicks.Count < 1) return;

            string key = _BuildKey(property);
            if (key == null) return;
            if (!pendingPicks.TryGetValue(key, out int picked)) return;

            pendingPicks.Remove(key);

            if (property.intValue == picked) return;

            property.intValue = picked;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 콜백 시점의 즉시 쓰기. SerializedProperty 를 새로 찾아 쓴다.
        /// 성공하면 보류 경로는 값이 같아 자동으로 무효화된다.
        /// </summary>
        static void _WriteNow(SerializedObject serializedObject, string propertyPath, int picked) {
            if (serializedObject == null) return;
            if (serializedObject.targetObject == null) return;

            try {
                serializedObject.Update();

                SerializedProperty target = serializedObject.FindProperty(propertyPath);
                if (target == null) return;

                target.intValue = picked;
                serializedObject.ApplyModifiedProperties();
            }
            catch (Exception) {
                // 콜백 시점 쓰기 실패는 무시한다 - _ApplyPendingPick 보류 경로가 다음 OnGUI 에서 재시도한다.
            }
        }
        #endregion

        #region Private - Helpers
        static string _BuildKey(SerializedProperty property) {
            SerializedObject serializedObject = property.serializedObject;
            if (serializedObject == null) return null;

            UnityEngine.Object target = serializedObject.targetObject;
            if (target == null) return null;

            return $"{target.GetInstanceID()}|{property.propertyPath}";
        }

        static bool _TryFindLabel(IReadOnlyList<HDropdownOption> options, int value, out string label) {
            label = string.Empty;

            for (int k = 0; k < options.Count; k++) {
                if (options[k].Value != value) continue;
                label = options[k].Label;
                return true;
            }

            return false;
        }

        static void _OpenSelector(Rect anchor, SerializedProperty property, IReadOnlyList<HDropdownOption> options, bool allowNone, int searchThreshold) {
            string key = _BuildKey(property);
            if (key == null) return;

            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            int currentValue = property.intValue;

            var popup = new HDropdownSearchPopup(options, allowNone, searchThreshold, currentValue, anchor.width, picked => {
                // 빠른 경로 : 콜백에서 바로 쓴다.
                _WriteNow(serializedObject, propertyPath, picked);

                // 보험 : 위 쓰기가 에디터의 Update/Apply 주기 밖이라 덮였을 경우를 대비해
                // 다음 OnGUI 에서 살아있는 프로퍼티로 다시 쓴다.
                pendingPicks[key] = picked;

                InternalEditorUtility.RepaintAllViews();
            });

            PopupWindow.Show(anchor, popup);
        }
        #endregion
    }

    /// <summary>
    /// 이전 선택 UI. HDropdownSearchPopup 으로 대체되어 현재 호출되지 않는다.
    /// AdvancedDropdown 은 검색 필드를 켤 공개 API 가 없어 교체했다 (메타데이터 실측).
    /// 라벨 '/' 계층 접기는 이쪽에만 있으므로, 삭제는 사용자 승인 후에 한다.
    /// </summary>
    internal sealed class HDropdownSelector : AdvancedDropdown {
        #region Constants
        const int NONE_ID = -1;
        const string NONE_LABEL = "(None)";
        #endregion

        #region Fields
        // id → 실제 값. 어떤 id 도 중복되지 않게 만든다 -
        // AdvancedDropdownState 가 id 를 키로 선택 위치를 추적하기 때문이다.
        readonly Dictionary<int, int> valueById = new Dictionary<int, int>();
        readonly List<HDropdownOption> items = new List<HDropdownOption>();
        readonly bool allowNone;
        readonly Action<int> onPicked;

        int nextGroupId = -2;
        #endregion

        #region Constructors
        public HDropdownSelector(IReadOnlyList<HDropdownOption> options, bool allowNone, Action<int> onPicked)
            : base(new AdvancedDropdownState()) {

            this.allowNone = allowNone;
            this.onPicked = onPicked;

            for (int k = 0; k < options.Count; k++) items.Add(options[k]);

            minimumSize = new Vector2(280f, 340f);
        }
        #endregion

        #region AdvancedDropdown
        protected override AdvancedDropdownItem BuildRoot() {
            valueById.Clear();
            nextGroupId = -2;

            var root = new AdvancedDropdownItem("Select");

            if (allowNone) {
                root.AddChild(new AdvancedDropdownItem(NONE_LABEL) { id = NONE_ID });
                valueById[NONE_ID] = 0;
                root.AddSeparator();
            }

            var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);

            for (int k = 0; k < items.Count; k++) {
                string label = items[k].Label ?? string.Empty;

                int cut = label.LastIndexOf('/');
                AdvancedDropdownItem parent = root;
                string leaf = label;

                if (cut >= 0) {
                    parent = _EnsureGroup(root, groups, label.Substring(0, cut));
                    leaf = label.Substring(cut + 1);
                }

                parent.AddChild(new AdvancedDropdownItem(leaf) { id = k });
                valueById[k] = items[k].Value;
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item) {
            if (item == null) return;
            if (!valueById.TryGetValue(item.id, out int value)) return;

            onPicked?.Invoke(value);
        }
        #endregion

        #region Private
        AdvancedDropdownItem _EnsureGroup(
            AdvancedDropdownItem root,
            Dictionary<string, AdvancedDropdownItem> groups,
            string path) {

            if (groups.TryGetValue(path, out var existing)) return existing;

            int cut = path.LastIndexOf('/');
            AdvancedDropdownItem parent = cut >= 0 ? _EnsureGroup(root, groups, path.Substring(0, cut)) : root;
            string name = cut >= 0 ? path.Substring(cut + 1) : path;

            var created = new AdvancedDropdownItem(name) { id = nextGroupId };
            nextGroupId--;

            parent.AddChild(created);
            groups[path] = created;

            return created;
        }
        #endregion
    }

}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-04 (수정) :: 선택 UI 를 HDropdownSearchPopup 으로 교체
 *
 * # 변경
 * - _OpenSelector 가 searchThreshold 와 현재 값을 받아 HDropdownSearchPopup 을 연다.
 * - HDropdownSelector(AdvancedDropdown) 는 호출되지 않는다. 삭제는 승인 대기.
 *
 * # 이유
 * - AdvancedDropdown 은 검색 필드를 띄울 수 없다. 그 타입이 서브클래스에 여는 멤버는
 *   minimumSize / BuildRoot / ItemSelected 뿐이고 검색 상태는 internal
 *   AdvancedDropdownWindow 소유다 (UnityEditor.CoreModule.dll 메타데이터 실측).
 *   그래서 SearchThreshold 가 Odin 경로에서만 동작하는 상태였다.
 * - HInspector 는 Odin 에 종속되지 않아야 한다. 어트리뷰트가 정의한 계약을 이 렌더러가
 *   자기 수단으로 지켜야 하므로, 위젯을 갈아끼우는 것이 유일한 정공법이었다.
 *
 * # 결과
 * - Odin 유무와 무관하게 [HDropdown] 에 검색 필드가 뜬다.
 * - 라벨 '/' 계층 접기는 사라졌다. 평탄 목록 + 검색으로 대체된다.
 *
 * # 주의
 * - 쓰기 이중화(_WriteNow + _ApplyPendingPick)는 그대로 유지했다. 팝업 교체와 무관한
 *   별개의 안전망이고, 콜백 시점 문제는 PopupWindow 에서도 동일하다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.07 진단 스캐폴딩 제거 - 원인이 이 파일 밖(Odin 브릿지)이었음이 확정됨
 *
 * # 변경
 * - HDropdownDiagnostics 클래스, 모든 Log() 호출 지점(PRESS/OPEN/PICK/BUILD-ROOT/
 *   ITEM-SELECTED/WRITE-NOW/PENDING-*), 그 전용으로만 쓰이던 _DescribeTarget 삭제.
 * - 쓰기 이중화(_WriteNow + _ApplyPendingPick)는 그대로 유지 - Odin 문제와 무관한
 *   별개의 안전망이고, 이 IMGUI 경로 자체는 원래 정상 동작이었다.
 *
 * # 이유
 * - "선택해도 값이 안 바뀐다" 증상의 실제 원인은 Odin 설치 환경에서 이 파일의 Draw() 가
 *   아예 호출되지 않는 것이었다(HInspectorToOdinBridge 의 매핑 누락). 재현 클릭 후에도
 *   _diag/hdropdown.log 가 한 줄도 안 찍힌 것으로 확정 - 이 파일 로직은 무죄였다.
 *   자세한 근본 원인은 HInspectorToOdinBridge.cs 데브로그(2026.08.07) 참조.
 * - 원인이 확정된 이상 리페인트마다 파일 I/O 를 도는 진단 코드를 남겨둘 이유가 없다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.06 쓰기 이중화 + 파일 진단 로그 (원인 규명용, 임시)
 *
 * # 변경
 * - 콜백에서 즉시 쓰기(_WriteNow) + 다음 OnGUI 보류 쓰기(_ApplyPendingPick) 이중 경로.
 *   둘 중 하나만 통해도 값이 남는다. 즉시 쓰기가 성공하면 보류 경로는 값이 같아 무시된다.
 * - HDropdownDiagnostics 신설 - <프로젝트 루트>/_diag/hdropdown.log 에 단계별 기록.
 *   PRESS → OPEN → BUILD-ROOT → ITEM-SELECTED → PICK → WRITE-NOW → PENDING-* 순서로
 *   남으므로 어느 단계에서 끊기는지 파일 하나로 확정된다.
 *
 * # 이유
 * - "선택해도 값이 안 바뀐다" 의 원인을 코드 정독만으로 확정하지 못했다. 추측으로
 *   고치는 대신 어느 단계가 실행되는지 실측한다. Unity 콘솔이 아니라 파일에 남기는
 *   이유는 사람이 옮겨 적는 과정에서 정보가 잘리기 때문이다.
 * - 원인 확정 후 HDropdownDiagnostics 와 Log 호출은 전부 제거한다. 남기면 인스펙터를
 *   그릴 때마다 파일 I/O 가 도는 코드를 방치하는 셈이다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.06 선택한 값이 반영되지 않던 문제 1차 수정
 *
 * # 변경 1 - 모든 AdvancedDropdownItem 에 고유 id 부여
 * - 이전: 그룹 노드 전부가 id = int.MinValue 를 공유했다.
 * - 이유: AdvancedDropdownState 는 id 를 키로 선택 위치를 추적한다. 중복 id 는 그 추적을
 *   엉키게 해 ItemSelected 가 엉뚱한 항목으로 오거나 오지 않을 수 있다.
 *
 * # 변경 2 - id → 값 매핑을 명시 테이블(valueById)로
 * - 인덱스 산술에 기대지 않으므로 id 규칙을 바꿔도 해석이 깨지지 않는다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 설계 결정
 * - EditorGUI.Popup 이 아니라 AdvancedDropdown 을 썼다. Popup 은 검색이 없고 항목이
 *   수백 개가 되면 쓸 수 없다.
 * - 라벨의 '/' 를 하위 메뉴로 접는다. 도메인이 "UI/600003_Click" 처럼 분류를 라벨에
 *   실어 보내면 별도 API 없이 계층이 생긴다.
 *
 * =============================================================================
 */
#endif
