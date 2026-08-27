#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HInspector 시스템의 추상 CustomEditor 베이스입니다.
 * 모든 실제 렌더링 로직을 이 클래스가 보유하며, 직접 등록되지 않습니다.
 *
 * 등록 지점 ::
 * HMonoBehaviourInspector (HInspectorBehaviour 타겟)
 * HScriptableObjectInspector (HInspectorScriptableObject 타겟)
 *
 * 역할 ::
 * H-어트리뷰트가 감지되지 않는 타겟은 Unity 기본 인스펙터로 폴백합니다.
 * HBoxGroup / HHorizontalGroup / HVerticalGroup으로 묶인 필드들을 해당 모드로 레이아웃합니다.
 * HTitle을 필드와 독립된 레이아웃 슬롯으로 렌더합니다 (그룹 경계 밖, 선언 위치).
 * PropertyDrawer가 처리하지 못하는 메서드 버튼, 비직렬화 멤버 노출을
 * 후속 단계에서 이 클래스 위에 얹습니다.

 * HTitle 처리 위치 ::
 * HTitle은 PropertyDrawer가 아닌 이 클래스에서 처리됩니다. 이유는 타이틀이
 * 그룹 경계를 넘어 "선언 위치의 독립 아이템"으로 렌더되어야 하기 때문입니다.
 * 따라서 HInspectorBehaviour / HInspectorScriptableObject를 상속받지 않은
 * 일반 MonoBehaviour / ScriptableObject에서는 HTitle이 시각적으로 그려지지 않습니다.
 *
 * 주의사항 ::
 * [CustomEditor]는 AllowMultiple = false이므로 타겟별 쉘 클래스가 필요합니다.
 * 새로운 베이스 타입을 추가하려면 이 클래스를 상속받는 빈 쉘을 만들고
 * [CustomEditor] + [CanEditMultipleObjects]만 선언하면 됩니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HInspector.Editor {
    public abstract class HInspectorEditor : UnityEditor.Editor {
        #region Types
        enum GroupMode {
            None,
            Horizontal,
            Vertical,
            Box
        }
        #endregion

        #region Constants
        const string SCRIPT_FIELD_PATH = "m_Script";
        const BindingFlags MEMBER_FLAGS =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        const float BUTTONS_TOP_PADDING = 4f;
        const float SHOW_IN_INSPECTOR_TOP_PADDING = 6f;
        #endregion

        #region Static Fields
        static GUIStyle boxGroupStyle;

        // 타입 계층 전체를 GetFields/GetProperties/GetMethods 로 훑는 수집이라 리페인트마다
        // 돌리면 비용이 크다. 결과는 타입의 정적 메타데이터이므로 타입당 1회만 계산한다.
        // 도메인 리로드 시 Type 인스턴스와 함께 무효화되므로 별도 리셋 훅이 필요 없다.
        static readonly Dictionary<Type, List<(MemberInfo member, HShowInInspectorAttribute attribute)>> _showInInspectorCache = new();
        static readonly Dictionary<Type, List<(MethodInfo method, HButtonAttribute attribute)>> _buttonMethodCache = new();

        // _GetTitle/_GetGroupInfo가 가시 프로퍼티마다 _FindField로 타입 계층을 재순회한다.
        // FieldInfo는 (타입, 필드명) 쌍의 정적 메타데이터이므로 쌍당 1회만 계산한다.
        static readonly Dictionary<(Type type, string fieldName), FieldInfo> _fieldLookupCache = new();
        #endregion

        #region Fields
        bool useDefaultInspector = true;
        #endregion

        #region Unity Lifecycle
        private void OnEnable() {
            if (target == null) return;
            useDefaultInspector = !_HasAnyHInspectorAttribute(target.GetType());
        }
        #endregion

        #region Public - OnInspectorGUI
        public override void OnInspectorGUI() {
            if (useDefaultInspector) {
                DrawDefaultInspector();
                return;
            }

            _DrawHInspector();
        }
        #endregion

        #region Private - Draw Logic
        private void _DrawHInspector() {
            serializedObject.Update();

            Type targetType = target.GetType();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool isEnterChildren = true;
            string currentGroupName = null;
            GroupMode currentGroupMode = GroupMode.None;

            while (iterator.NextVisible(isEnterChildren)) {
                isEnterChildren = false;

                HTitleAttribute titleAttribute = _GetTitle(targetType, iterator);
                if (titleAttribute != null) {
                    _CloseGroup(currentGroupMode);
                    currentGroupName = null;
                    currentGroupMode = GroupMode.None;
                    _DrawTitleIndependent(titleAttribute);
                }

                (string nextGroupName, GroupMode nextGroupMode) = _GetGroupInfo(targetType, iterator);
                if (nextGroupName != currentGroupName || nextGroupMode != currentGroupMode) {
                    _CloseGroup(currentGroupMode);
                    _OpenGroup(nextGroupMode, nextGroupName);
                    currentGroupName = nextGroupName;
                    currentGroupMode = nextGroupMode;
                }

                _DrawIteratedProperty(iterator);
            }

            _CloseGroup(currentGroupMode);

            serializedObject.ApplyModifiedProperties();

            _DrawButtons(targetType);
            _DrawShowInInspectorMembers(targetType);
        }

        private void _OpenGroup(GroupMode mode, string groupName) {
            switch (mode) {
            case GroupMode.Horizontal:
                EditorGUILayout.BeginHorizontal();
                break;
            case GroupMode.Vertical:
                EditorGUILayout.BeginVertical();
                break;
            case GroupMode.Box:
                GUILayout.Space(2);
                EditorGUILayout.BeginVertical(_GetBoxGroupStyle());
                if (!string.IsNullOrEmpty(groupName)) _DrawBoxGroupHeader(groupName);
                break;
            }
        }

        private void _CloseGroup(GroupMode mode) {
            switch (mode) {
            case GroupMode.Horizontal:
                EditorGUILayout.EndHorizontal();
                break;
            case GroupMode.Vertical:
                EditorGUILayout.EndVertical();
                break;
            case GroupMode.Box:
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
                break;
            }
        }

        private void _DrawIteratedProperty(SerializedProperty property) {
            if (property.propertyPath == SCRIPT_FIELD_PATH) {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(property);
                GUI.enabled = previousEnabled;
                return;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        private void _DrawButtons(Type targetType) {
            List<(MethodInfo method, HButtonAttribute attribute)> buttonEntries = _CollectButtonMethods(targetType);
            if (buttonEntries.Count == 0) return;

            GUILayout.Space(BUTTONS_TOP_PADDING);
            for (int k = 0; k < buttonEntries.Count; k++) {
                MethodInfo method = buttonEntries[k].method;
                HButtonAttribute attribute = buttonEntries[k].attribute;
                string label = string.IsNullOrEmpty(attribute.Label) ? method.Name : attribute.Label;

                if (!GUILayout.Button(label)) continue;

                // 버튼이 대상 오브젝트를 바꾸는 것이 정상 사용이다. Undo 등록과 SetDirty 가 없으면
                // 변경이 되돌릴 수도, 저장될 수도 없다 (씬/프리팹이 더티로 표시되지 않는다).
                //
                // 규약(케이스 리포트 08 COR-2) :: RecordObjects 는 대상의 "직렬화 필드 스냅샷"만
                // 기록한다. [HButton] 메서드가 Instantiate/DestroyImmediate/컴포넌트 추가처럼
                // 계층을 바꾸면 그 변경은 Undo 스택에 남지 않는다 — 계층을 바꾸는 버튼 메서드는
                // 스스로 Undo.RegisterCreatedObjectUndo / Undo.DestroyObjectImmediate 를 불러야
                // 한다. 여기서 일괄 처리하지 않는 이유는 무엇을 생성·파괴했는지가 메서드 내부
                // 로직에만 있어 이 지점에서는 알 수 없기 때문이다.
                Undo.RecordObjects(targets, label);

                for (int j = 0; j < targets.Length; j++) {
                    try {
                        method.Invoke(targets[j], null);
                    }
                    catch (TargetInvocationException e) {
                        // 예외가 OnInspectorGUI 밖으로 나가면 인스펙터 전체가 그리기를 멈춘다.
                        Debug.LogException(e.InnerException ?? e, targets[j]);
                        continue;
                    }

                    EditorUtility.SetDirty(targets[j]);
                }
            }
        }

        private void _DrawShowInInspectorMembers(Type targetType) {
            List<(MemberInfo member, HShowInInspectorAttribute attribute)> entries = _CollectShowInInspectorMembers(targetType);
            if (entries.Count == 0) return;

            GUILayout.Space(SHOW_IN_INSPECTOR_TOP_PADDING);

            for (int k = 0; k < entries.Count; k++) {
                MemberInfo member = entries[k].member;
                HShowInInspectorAttribute attribute = entries[k].attribute;
                string label = string.IsNullOrEmpty(attribute.Label)
                    ? ObjectNames.NicifyVariableName(member.Name)
                    : attribute.Label;

                _DrawShowInInspectorMember(label, member);
            }
        }

        private void _DrawShowInInspectorMember(string label, MemberInfo member) {
            // _ReadMember가 (value, type, error) 튜플로 성공/실패를 데이터 흐름화한다.
            // 호출자는 단순 if 분기로 에러 경로를 처리하며, 타입 미스매치 캐스트 문제가 구조적으로 제거됐다.
            (object value, Type valueType, Exception error) = _ReadMember(member, target);
            if (error != null) {
                _DrawErrorValue(label, error);
                return;
            }
            if (valueType == null) return;

            _DrawReadOnlyValue(label, value, valueType);
        }

        private static (object value, Type type, Exception error) _ReadMember(MemberInfo member, object instance) {
            try {
                switch (member) {
                case FieldInfo field:
                    return (field.GetValue(instance), field.FieldType, null);
                case PropertyInfo property:
                    return (property.GetValue(instance), property.PropertyType, null);
                default:
                    return (null, null, null);
                }
            }
            catch (TargetInvocationException targetException) {
                // reflection invoke가 래핑한 실제 예외를 벗겨낸다
                return (null, null, targetException.InnerException ?? targetException);
            }
            catch (Exception exception) {
                return (null, null, exception);
            }
        }

        private static void _DrawErrorValue(string label, Exception exception) {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            try {
                EditorGUILayout.LabelField(label, $"<error: {exception.GetType().Name}: {exception.Message}>");
            }
            finally {
                GUI.enabled = previousEnabled;
            }
        }

        private List<(MemberInfo member, HShowInInspectorAttribute attribute)> _CollectShowInInspectorMembers(Type targetType) {
            if (_showInInspectorCache.TryGetValue(targetType, out var cachedEntries)) return cachedEntries;

            var collected = _BuildShowInInspectorMembers(targetType);
            _showInInspectorCache[targetType] = collected;
            return collected;
        }

        private static List<(MemberInfo member, HShowInInspectorAttribute attribute)> _BuildShowInInspectorMembers(Type targetType) {
            List<(MemberInfo, HShowInInspectorAttribute)> entries = new List<(MemberInfo, HShowInInspectorAttribute)>();
            Type current = targetType;
            while (current != null && current != typeof(object)) {
                FieldInfo[] fields = current.GetFields(MEMBER_FLAGS);
                for (int k = 0; k < fields.Length; k++) {
                    object[] attrs = fields[k].GetCustomAttributes(typeof(HShowInInspectorAttribute), true);
                    if (attrs.Length == 0) continue;
                    entries.Add((fields[k], (HShowInInspectorAttribute)attrs[0]));
                }

                PropertyInfo[] properties = current.GetProperties(MEMBER_FLAGS);
                for (int k = 0; k < properties.Length; k++) {
                    if (!properties[k].CanRead) continue;
                    // 인덱서(예: this[int i])는 파라미터가 있어 GetValue 호출이 어려우므로 제외
                    if (properties[k].GetIndexParameters().Length > 0) continue;
                    object[] attrs = properties[k].GetCustomAttributes(typeof(HShowInInspectorAttribute), true);
                    if (attrs.Length == 0) continue;
                    entries.Add((properties[k], (HShowInInspectorAttribute)attrs[0]));
                }

                current = current.BaseType;
            }
            return entries;
        }

        private void _DrawReadOnlyValue(string label, object value, Type valueType) {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            try {
                if (value == null) {
                    EditorGUILayout.LabelField(label, "<null>");
                } else if (valueType == typeof(int)) {
                    EditorGUILayout.IntField(label, (int)value);
                } else if (valueType == typeof(long)) {
                    EditorGUILayout.LongField(label, (long)value);
                } else if (valueType == typeof(float)) {
                    EditorGUILayout.FloatField(label, (float)value);
                } else if (valueType == typeof(double)) {
                    EditorGUILayout.DoubleField(label, (double)value);
                } else if (valueType == typeof(bool)) {
                    EditorGUILayout.Toggle(label, (bool)value);
                } else if (valueType == typeof(string)) {
                    EditorGUILayout.TextField(label, (string)value);
                } else if (valueType == typeof(Vector2)) {
                    EditorGUILayout.Vector2Field(label, (Vector2)value);
                } else if (valueType == typeof(Vector3)) {
                    EditorGUILayout.Vector3Field(label, (Vector3)value);
                } else if (valueType == typeof(Vector4)) {
                    EditorGUILayout.Vector4Field(label, (Vector4)value);
                } else if (valueType == typeof(Vector2Int)) {
                    EditorGUILayout.Vector2IntField(label, (Vector2Int)value);
                } else if (valueType == typeof(Vector3Int)) {
                    EditorGUILayout.Vector3IntField(label, (Vector3Int)value);
                } else if (valueType == typeof(Color)) {
                    EditorGUILayout.ColorField(label, (Color)value);
                } else if (typeof(UnityEngine.Object).IsAssignableFrom(valueType)) {
                    EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, valueType, true);
                } else if (valueType.IsEnum) {
                    EditorGUILayout.EnumPopup(label, (Enum)value);
                } else {
                    EditorGUILayout.LabelField(label, value.ToString());
                }
            }
            finally {
                GUI.enabled = previousEnabled;
            }
        }

        private List<(MethodInfo method, HButtonAttribute attribute)> _CollectButtonMethods(Type targetType) {
            if (_buttonMethodCache.TryGetValue(targetType, out var cachedEntries)) return cachedEntries;

            var collected = _BuildButtonMethods(targetType);
            _buttonMethodCache[targetType] = collected;
            return collected;
        }

        private static List<(MethodInfo method, HButtonAttribute attribute)> _BuildButtonMethods(Type targetType) {
            List<(MethodInfo, HButtonAttribute)> entries = new List<(MethodInfo, HButtonAttribute)>();
            Type current = targetType;
            while (current != null && current != typeof(object)) {
                MethodInfo[] methods = current.GetMethods(MEMBER_FLAGS);
                for (int k = 0; k < methods.Length; k++) {
                    if (methods[k].GetParameters().Length > 0) continue;

                    object[] buttonAttributes = methods[k].GetCustomAttributes(typeof(HButtonAttribute), true);
                    if (buttonAttributes.Length == 0) continue;

                    entries.Add((methods[k], (HButtonAttribute)buttonAttributes[0]));
                }
                current = current.BaseType;
            }

            return entries;
        }

        private HTitleAttribute _GetTitle(Type targetType, SerializedProperty property) {
            if (property.propertyPath == SCRIPT_FIELD_PATH) return null;
            if (property.propertyPath.IndexOf('.') >= 0) return null;

            FieldInfo field = _FindField(targetType, property.propertyPath);
            if (field == null) return null;

            object[] titleAttributes = field.GetCustomAttributes(typeof(HTitleAttribute), true);
            if (titleAttributes.Length == 0) return null;

            return (HTitleAttribute)titleAttributes[0];
        }

        private static void _DrawBoxGroupHeader(string groupName) {
            // HBoxGroup 상단 그룹명 렌더 — BoxGroup 내부 padding 이 이미 존재해 상단 여백 생략.
            Rect blockRect = GUILayoutUtility.GetRect(0, HTitleDrawer._GetTitleBlockHeight(),
                                                       GUILayout.ExpandWidth(true));
            HTitleDrawer._DrawTitleCore(blockRect, groupName);
            GUILayout.Space(4f);
        }

        private static void _DrawTitleIndependent(HTitleAttribute titleAttribute) {
            HTitleDrawer.Draw(titleAttribute.Title);
        }

        private (string name, GroupMode mode) _GetGroupInfo(Type targetType, SerializedProperty property) {
            if (property.propertyPath == SCRIPT_FIELD_PATH) return (null, GroupMode.None);
            if (property.propertyPath.IndexOf('.') >= 0) return (null, GroupMode.None);

            FieldInfo field = _FindField(targetType, property.propertyPath);
            if (field == null) return (null, GroupMode.None);

            object[] boxAttributes = field.GetCustomAttributes(typeof(HBoxGroupAttribute), true);
            if (boxAttributes.Length > 0) {
                return (((HBoxGroupAttribute)boxAttributes[0]).GroupName, GroupMode.Box);
            }

            object[] horizontalAttributes = field.GetCustomAttributes(typeof(HHorizontalGroupAttribute), true);
            if (horizontalAttributes.Length > 0) {
                return (((HHorizontalGroupAttribute)horizontalAttributes[0]).GroupName, GroupMode.Horizontal);
            }

            object[] verticalAttributes = field.GetCustomAttributes(typeof(HVerticalGroupAttribute), true);
            if (verticalAttributes.Length > 0) {
                return (((HVerticalGroupAttribute)verticalAttributes[0]).GroupName, GroupMode.Vertical);
            }

            return (null, GroupMode.None);
        }

        private FieldInfo _FindField(Type targetType, string fieldName) {
            var key = (targetType, fieldName);
            if (_fieldLookupCache.TryGetValue(key, out FieldInfo cached)) return cached;

            FieldInfo found = null;
            Type current = targetType;
            while (current != null && current != typeof(object)) {
                found = current.GetField(fieldName, MEMBER_FLAGS);
                if (found != null) break;
                current = current.BaseType;
            }

            _fieldLookupCache[key] = found;
            return found;
        }

        private bool _HasAnyHInspectorAttribute(Type type) {
            Type current = type;
            while (current != null && current != typeof(object)) {
                FieldInfo[] fields = current.GetFields(MEMBER_FLAGS);
                for (int k = 0; k < fields.Length; k++) {
                    if (fields[k].IsDefined(typeof(HInspectorAttribute), true)) return true;
                    if (fields[k].IsDefined(typeof(HShowInInspectorAttribute), true)) return true;
                    // HTitle은 HInspectorAttribute 계열 밖(System.Attribute 직접 상속)이므로 별도 체크한다.
                    if (fields[k].IsDefined(typeof(HTitleAttribute), true)) return true;
                }

                PropertyInfo[] properties = current.GetProperties(MEMBER_FLAGS);
                for (int k = 0; k < properties.Length; k++) {
                    if (properties[k].IsDefined(typeof(HShowInInspectorAttribute), true)) return true;
                }

                MethodInfo[] methods = current.GetMethods(MEMBER_FLAGS);
                for (int k = 0; k < methods.Length; k++) {
                    if (methods[k].IsDefined(typeof(HButtonAttribute), true)) return true;
                    if (methods[k].IsDefined(typeof(HShowInInspectorAttribute), true)) return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static GUIStyle _GetBoxGroupStyle() {
            if (boxGroupStyle != null) return boxGroupStyle;

            GUIStyle listBoxBackground = ReorderableList.defaultBehaviours?.boxBackground;
            GUIStyle baseStyle = (listBoxBackground != null && listBoxBackground.normal.background != null)
                ? listBoxBackground
                : EditorStyles.helpBox;

            boxGroupStyle = new GUIStyle(baseStyle) {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 4, 4)
            };
            return boxGroupStyle;
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 [HButton] 계층 변경 Undo 규약 문서화 (케이스 리포트 08 COR-2)
 *
 * # 변경
 * - `_DrawButtons` 의 `Undo.RecordObjects` 호출부에 규약 주석 추가. 코드 동작은 변경 없음
 *
 * # 이유
 * - 근거: `Docs/02_Code/01_CaseReport/04_HInspector 에디터 캐시.md` COR-2 — `RecordObjects` 는
 *   직렬화 필드 스냅샷만 기록해 버튼이 오브젝트를 생성/삭제하면 Undo 가 포착하지 못한다
 * - 이 지점에서 무엇이 생성·파괴됐는지 알 수 없어(버튼 메서드 내부 로직) 여기서 일괄
 *   `RegisterCreatedObjectUndo` 를 부를 수 없다 — 계층 변경 책임을 버튼 메서드 쪽 규약으로 명문화
 *
 * =============================================================================
 * @Jason - PKH 2026.08.07 _FindField 캐싱 추가
 *
 * # 변경
 * - _FindField(Type, string) : (타입, 필드명) 쌍을 키로 하는 static Dictionary 캐시 추가.
 *   최초 1회만 BaseType 계층을 순회하고 이후 재조회는 캐시 히트.
 *
 * # 이유
 * - _GetTitle / _GetGroupInfo 가 가시 프로퍼티마다 _FindField 를 호출해, 필드 N개면
 *   프레임(OnInspectorGUI 는 Layout/Repaint 로 프레임당 2회 이상 호출)당 2N 회 이상의
 *   계층 리플렉션 순회가 발생했다. _CollectButtonMethods / _CollectShowInInspectorMembers
 *   는 이미 타입 단위 캐시가 있었으나 이 경로는 빠져 있었다.
 *
 * =============================================================================
 */
#endif
