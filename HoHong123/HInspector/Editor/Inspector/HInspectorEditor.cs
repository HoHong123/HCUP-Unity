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

        const float TITLE_TOP_PADDING = 6f;
        const float TITLE_TO_LINE_GAP = 3f;
        const float TITLE_LINE_THICKNESS = 1f;
        const float TITLE_LINE_TO_FIELD_GAP = 4f;

        const float BUTTONS_TOP_PADDING = 4f;
        const float SHOW_IN_INSPECTOR_TOP_PADDING = 6f;
        #endregion

        #region Static Fields
        static GUIStyle boxGroupStyle;
        static GUIStyle titleStyle;
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

                for (int j = 0; j < targets.Length; j++) {
                    method.Invoke(targets[j], null);
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
            // HBoxGroup 상단에 그룹명을 HTitle 스타일(bold + 구분선)로 렌더.
            // TITLE_TOP_PADDING은 적용하지 않는다 — BoxGroup의 내부 padding이 이미 존재.
            Rect blockRect = GUILayoutUtility.GetRect(0, _GetTitleBlockHeight(), GUILayout.ExpandWidth(true));
            _DrawTitleCore(blockRect, groupName);
            GUILayout.Space(TITLE_LINE_TO_FIELD_GAP);
        }

        private static void _DrawTitleIndependent(HTitleAttribute titleAttribute) {
            GUILayout.Space(TITLE_TOP_PADDING);
            Rect blockRect = GUILayoutUtility.GetRect(0, _GetTitleBlockHeight(), GUILayout.ExpandWidth(true));
            _DrawTitleCore(blockRect, titleAttribute.Title);
            GUILayout.Space(TITLE_LINE_TO_FIELD_GAP);
        }

        private static void _DrawTitleCore(Rect blockRect, string title) {
            // HTitle 시각 규격 (볼드 라벨 + 구분선)의 단일 구현.
            // _DrawTitleIndependent(HTitle 어트리뷰트)와 _DrawBoxGroupHeader(HBoxGroup 그룹명) 양쪽에서 사용.
            Rect titleRect = new Rect(blockRect.x, blockRect.y, blockRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, title, _GetTitleStyle());

            float lineY = titleRect.yMax + TITLE_TO_LINE_GAP;
            Color lineColor = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.45f, 0.45f)
                : new Color(0.55f, 0.55f, 0.55f);
            Rect lineRect = new Rect(blockRect.x, lineY, blockRect.width, TITLE_LINE_THICKNESS);
            EditorGUI.DrawRect(lineRect, lineColor);
        }

        private static float _GetTitleBlockHeight() {
            return EditorGUIUtility.singleLineHeight + TITLE_TO_LINE_GAP + TITLE_LINE_THICKNESS;
        }

        private static GUIStyle _GetTitleStyle() {
            if (titleStyle != null) return titleStyle;

            titleStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            return titleStyle;
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
            Type current = targetType;
            while (current != null && current != typeof(object)) {
                FieldInfo field = current.GetField(fieldName, MEMBER_FLAGS);
                if (field != null) return field;
                current = current.BaseType;
            }

            return null;
        }

        private bool _HasAnyHInspectorAttribute(Type type) {
            Type current = type;
            while (current != null && current != typeof(object)) {
                FieldInfo[] fields = current.GetFields(MEMBER_FLAGS);
                for (int k = 0; k < fields.Length; k++) {
                    if (fields[k].IsDefined(typeof(HInspectorAttribute), true)) return true;
                    if (fields[k].IsDefined(typeof(HShowInInspectorAttribute), true)) return true;
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
