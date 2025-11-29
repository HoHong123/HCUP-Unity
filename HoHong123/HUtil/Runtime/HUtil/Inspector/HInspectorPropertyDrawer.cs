using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HUtil.Inspector {
    /// <summary>
    /// 공통 HInspectorAttribute 처리용 PropertyDrawer.
    /// 하나의 Drawer가 필드에 붙은 모든 HInspectorAttribute들을 Order 순서대로 처리한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(HInspectorAttribute), true)]
    public class HInspectorPropertyDrawer : PropertyDrawer {
        #region Title
        const float TitleTopPadding = 6f;  // 타이틀 블록 위 여백
        const float TitleToLineGap = 3f;  // 타이틀 텍스트 ↔ 라인 사이
        const float TitleLineThickness = 1f;  // 라인 두께
        const float TitleLineToFieldGap = 4f;  // 라인 ↔ 첫 필드 사이

        static readonly GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        static float _GetTitleExtraHeight() {
            return TitleTopPadding
                   + EditorGUIUtility.singleLineHeight
                   + TitleToLineGap
                   + TitleLineThickness
                   + TitleLineToFieldGap;
        }
        #endregion

        #region Internal Fields
        static readonly Dictionary<string, object> previousValueMap = new Dictionary<string, object>();
        #endregion

        #region Unity Life Cycle
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes))
                return 0f;

            float height = EditorGUI.GetPropertyHeight(property, label, true);

            if (attributes.OfType<HTitleAttribute>().Any())
                height += _GetTitleExtraHeight();

            bool hasMinMax = attributes.OfType<HMinMaxSliderAttribute>().Any();
            if (hasMinMax && property.propertyType == SerializedPropertyType.Vector2) {
                height += EditorGUIUtility.singleLineHeight
                          + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes))
                return;

            object target = _GetTargetObject(property);
            if (target == null) {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // ───── 1) Title 영역 분리 ─────
            Rect fieldRect = position;

            HTitleAttribute titleAttr = attributes.OfType<HTitleAttribute>().FirstOrDefault();
            if (titleAttr != null) {
                float extra = _GetTitleExtraHeight();

                // Title 텍스트
                float titleY = position.y + TitleTopPadding;
                Rect titleRect = new Rect(
                    position.x,
                    titleY,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.LabelField(titleRect, titleAttr.Title, titleStyle);

                // 라인
                float lineY = titleRect.y + titleRect.height + TitleToLineGap;
                Color lineColor = EditorGUIUtility.isProSkin
                    ? new Color(0.45f, 0.45f, 0.45f)
                    : new Color(0.55f, 0.55f, 0.55f);

                Rect lineRect = new Rect(
                    position.x,
                    lineY,
                    position.width,
                    TitleLineThickness
                );
                EditorGUI.DrawRect(lineRect, lineColor);

                // 실제 필드를 그릴 영역: Title 블록만큼 아래로 내리고, 그만큼 높이를 줄인다.
                fieldRect = new Rect(
                    position.x,
                    position.y + extra,
                    position.width,
                    position.height - extra
                );
            }

            // ───── 2) ReadOnly, Min/Max, OnValueChanged는 fieldRect 기준으로 처리 ─────

            bool isReadOnly = _EvaluateReadOnly(attributes, target);
            bool prevEnabled = GUI.enabled;
            if (isReadOnly)
                GUI.enabled = false;

            _DrawWithConstraints(fieldRect, property, label, attributes);

            GUI.enabled = prevEnabled;

            _ProcessOnValueChanged(property, attributes, target);
        }
        #endregion

        #region Member Functions
        // 이 필드에 붙은 모든 HInspectorAttribute를 Order 순서대로 가져온다.
        HInspectorAttribute[] _GetAttributes() {
            if (fieldInfo == null)
                return Array.Empty<HInspectorAttribute>();

            return fieldInfo
                .GetCustomAttributes(typeof(HInspectorAttribute), true)
                .Cast<HInspectorAttribute>()
                .OrderBy(a => a.Order)
                .ToArray();
        }

        bool _IsVisible(SerializedProperty property, HInspectorAttribute[] attributes) {
            object target = _GetTargetObject(property);
            if (target == null)
                return true;

            // Order 순서를 보장하기 위해, HHideIf / HShowIf 도 정렬된 리스트에서 뽑는다.
            foreach (HInspectorAttribute attr in attributes) {
                if (attr is HHideIfAttribute hide) {
                    if (_EvaluateCondition(hide, target))
                        return false;
                }
                else if (attr is HShowIfAttribute show) {
                    if (!_EvaluateCondition(show, target))
                        return false;
                }
            }

            return true;
        }

        bool _EvaluateReadOnly(HInspectorAttribute[] attributes, object targetObject) {
            // 첫 번째 HReadOnlyAttribute 기준 (Order가 작은 것부터)
            HReadOnlyAttribute attr = attributes.OfType<HReadOnlyAttribute>().FirstOrDefault();
            if (attr == null)
                return false;

            // 조건 없이 사용 → 항상 ReadOnly
            if (string.IsNullOrEmpty(attr.ConditionMemberName))
                return true;

            if (!_TryGetMemberValue(targetObject, attr.ConditionMemberName, out object value))
                return true;

            if (value is bool b)
                return attr.Inverse ? !b : b;

            return true;
        }

        void _DrawWithConstraints(Rect position, SerializedProperty property, GUIContent label, HInspectorAttribute[] attributes) {
            HMinMaxSliderAttribute minMax = attributes.OfType<HMinMaxSliderAttribute>().FirstOrDefault();
            HMinAttribute minAttr = attributes.OfType<HMinAttribute>().FirstOrDefault();
            HMaxAttribute maxAttr = attributes.OfType<HMaxAttribute>().FirstOrDefault();

            if (minMax != null) {
                _DrawMinMaxSlider(position, property, label, minMax);
                return;
            }

            if (minAttr == null && maxAttr == null) {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label, true);
            if (EditorGUI.EndChangeCheck())
                _ApplyMinMax(property, minAttr, maxAttr);
        }

        void _DrawMinMaxSlider(Rect position, SerializedProperty property, GUIContent label, HMinMaxSliderAttribute attr) {
            switch (property.propertyType) {
            case SerializedPropertyType.Vector2: {
                    // "레이블 영역 + 실제 컨트롤 영역" 분리
                    Rect controlRect = EditorGUI.PrefixLabel(position, label);

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float vSpace = EditorGUIUtility.standardVerticalSpacing;

                    // 1) 위 줄: 슬라이더
                    Rect sliderRect = new Rect(
                        controlRect.x,
                        controlRect.y,
                        controlRect.width,
                        lineHeight
                    );

                    // 2) 아래 줄: Min / Max 숫자 필드
                    float halfWidth = (controlRect.width - 4f) * 0.5f;

                    Rect minRect = new Rect(
                        controlRect.x,
                        controlRect.y + lineHeight + vSpace,
                        halfWidth,
                        lineHeight
                    );

                    Rect maxRect = new Rect(
                        minRect.x + halfWidth + 4f,
                        minRect.y,
                        halfWidth,
                        lineHeight
                    );

                    Vector2 v = property.vector2Value;
                    float min = v.x;
                    float max = v.y;

                    // 슬라이더로 대략 범위 조정
                    EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, attr.Min, attr.Max);

                    // 숫자 입력으로 정확한 값 조정
                    min = EditorGUI.FloatField(minRect, min);
                    max = EditorGUI.FloatField(maxRect, max);

                    // 범위 보정
                    min = Mathf.Clamp(min, attr.Min, attr.Max);
                    max = Mathf.Clamp(max, attr.Min, attr.Max);
                    if (max < min)
                        max = min;

                    property.vector2Value = new Vector2(min, max);
                    break;
                }

            case SerializedPropertyType.Float: {
                    float value = property.floatValue;
                    value = EditorGUI.Slider(position, label, value, attr.Min, attr.Max);
                    property.floatValue = Mathf.Clamp(value, attr.Min, attr.Max);
                    break;
                }

            case SerializedPropertyType.Integer: {
                    int value = property.intValue;
                    float f = EditorGUI.Slider(position, label, value, attr.Min, attr.Max);
                    value = Mathf.RoundToInt(Mathf.Clamp(f, attr.Min, attr.Max));
                    property.intValue = value;
                    break;
                }

            default:
                EditorGUI.PropertyField(position, property, label, true);
                break;
            }
        }

        void _ApplyMinMax(SerializedProperty property, HMinAttribute minAttr, HMaxAttribute maxAttr) {
            switch (property.propertyType) {
            case SerializedPropertyType.Integer: {
                    int value = property.intValue;
                    if (minAttr != null)
                        value = Mathf.Max(value, Mathf.RoundToInt(minAttr.Min));
                    if (maxAttr != null)
                        value = Mathf.Min(value, Mathf.RoundToInt(maxAttr.Max));
                    property.intValue = value;
                    break;
                }
            case SerializedPropertyType.Float: {
                    float value = property.floatValue;
                    if (minAttr != null)
                        value = Mathf.Max(value, minAttr.Min);
                    if (maxAttr != null)
                        value = Mathf.Min(value, maxAttr.Max);
                    property.floatValue = value;
                    break;
                }
            case SerializedPropertyType.Vector2: {
                    Vector2 value = property.vector2Value;
                    if (minAttr != null) {
                        value.x = Mathf.Max(value.x, minAttr.Min);
                        value.y = Mathf.Max(value.y, minAttr.Min);
                    }
                    if (maxAttr != null) {
                        value.x = Mathf.Min(value.x, maxAttr.Max);
                        value.y = Mathf.Min(value.y, maxAttr.Max);
                    }
                    property.vector2Value = value;
                    break;
                }
            }
        }

        void _ProcessOnValueChanged(SerializedProperty property, HInspectorAttribute[] attributes, object targetObject) {
            HOnValueChangedAttribute attr = attributes.OfType<HOnValueChangedAttribute>().FirstOrDefault();
            if (attr == null)
                return;

            if (!_TryGetSerializedValue(property, out object current))
                return;

            string key = _GetPropertyKey(property);
            previousValueMap.TryGetValue(key, out object previous);

            bool changed = previous == null
                ? current != null
                : !previous.Equals(current);

            if (!changed)
                return;

            // 변경 감지 → 캐시 갱신
            previousValueMap[key] = current;

            if (string.IsNullOrEmpty(attr.MethodName))
                return;

            // 1. 먼저 target 객체의 실제 필드에 새 값 반영
            if (fieldInfo != null) {
                object boxedValue;
                if (_TryConvertToFieldType(current, fieldInfo.FieldType, out boxedValue)) {
                    fieldInfo.SetValue(targetObject, boxedValue);
                }
            }

            // 2. 그런 다음 콜백 메서드 호출 (파라미터 0개 또는 1개 지원)
            _InvokeOnValueChanged(targetObject, attr.MethodName, current);
        }

        void _InvokeOnValueChanged(object targetObject, string methodName, object currentValue) {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = targetObject.GetType();

            // 1순위: 매개변수 1개 (속성 타입과 호환)
            MethodInfo methodWithParam = type
                .GetMethods(flags)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 1);

            if (methodWithParam != null) {
                ParameterInfo p = methodWithParam.GetParameters()[0];
                object arg;

                if (_TryConvertToFieldType(currentValue, p.ParameterType, out arg)) {
                    methodWithParam.Invoke(targetObject, new[] { arg });
                    return;
                }
            }

            // 2순위: 매개변수 없는 메서드
            MethodInfo methodNoParam = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
            if (methodNoParam != null) {
                methodNoParam.Invoke(targetObject, null);
            }
        }

        bool _TryConvertToFieldType(object src, Type targetType, out object result) {
            try {
                if (src == null) {
                    result = null;
                    return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
                }

                Type srcType = src.GetType();

                // 같은 타입이면 그대로
                if (targetType.IsAssignableFrom(srcType)) {
                    result = src;
                    return true;
                }

                // enum ← int
                if (targetType.IsEnum && _IsNumeric(srcType)) {
                    result = Enum.ToObject(targetType, src);
                    return true;
                }

                // 숫자형 변환
                if (_IsNumeric(srcType) && _IsNumeric(targetType)) {
                    result = Convert.ChangeType(src, targetType);
                    return true;
                }

                // 그 외에는 일반 ChangeType 시도
                result = Convert.ChangeType(src, targetType);
                return true;
            }
            catch {
                result = null;
                return false;
            }
        }

        bool _EvaluateCondition(HShowIfAttribute attr, object targetObject) {
            if (string.IsNullOrEmpty(attr.MemberName)) return true;

            if (!_TryGetMemberValue(targetObject, attr.MemberName, out object value)) return true;

            if (!attr.HasCompareValue) {
                if (value is bool b) return b;
                return true;
            }

            object compareValue = attr.CompareValue;

            if (!_TryCompare(value, compareValue, out int compareResult)) return true;

            switch (attr.CompareType) {
            case HCompareType.Equals:
                return compareResult == 0;
            case HCompareType.NotEquals:
                return compareResult != 0;
            case HCompareType.Greater:
                return compareResult > 0;
            case HCompareType.Less:
                return compareResult < 0;
            case HCompareType.GreaterOrEqual:
                return compareResult >= 0;
            case HCompareType.LessOrEqual:
                return compareResult <= 0;
            default:
                return true;
            }
        }

        bool _TryGetMemberValue(object targetObject, string memberName, out object value) {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = targetObject.GetType();

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null) {
                value = field.GetValue(targetObject);
                return true;
            }

            PropertyInfo prop = type.GetProperty(memberName, flags);
            if (prop != null) {
                value = prop.GetValue(targetObject);
                return true;
            }

            MethodInfo method = type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
            if (method != null) {
                value = method.Invoke(targetObject, null);
                return true;
            }

            value = null;
            return false;
        }

        bool _TryCompare(object left, object right, out int result) {
            result = 0;
            if (left == null || right == null) return false;

            Type lt = left.GetType();
            Type rt = right.GetType();

            if (lt == rt && left is IComparable comp) {
                result = comp.CompareTo(right);
                return true;
            }

            if (_IsNumeric(lt) && _IsNumeric(rt)) {
                double l = Convert.ToDouble(left);
                double r = Convert.ToDouble(right);
                result = l.CompareTo(r);
                return true;
            }

            if (lt.IsEnum && rt.IsEnum && lt == rt) {
                int l = Convert.ToInt32(left);
                int r = Convert.ToInt32(right);
                result = l.CompareTo(r);
                return true;
            }

            return false;
        }

        bool _IsNumeric(Type type) {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        bool _TryGetSerializedValue(SerializedProperty property, out object value) {
            switch (property.propertyType) {
            case SerializedPropertyType.Integer:
                value = property.intValue;
                return true;
            case SerializedPropertyType.Boolean:
                value = property.boolValue;
                return true;
            case SerializedPropertyType.Float:
                value = property.floatValue;
                return true;
            case SerializedPropertyType.String:
                value = property.stringValue;
                return true;
            case SerializedPropertyType.Vector2:
                value = property.vector2Value;
                return true;
            case SerializedPropertyType.Vector3:
                value = property.vector3Value;
                return true;
            case SerializedPropertyType.Enum:
                value = property.enumValueIndex;
                return true;
            case SerializedPropertyType.ObjectReference:
                value = property.objectReferenceInstanceIDValue;
                return true;
            default:
                value = null;
                return false;
            }
        }

        object _GetTargetObject(SerializedProperty property) {
            return property.serializedObject?.targetObject;
        }

        string _GetPropertyKey(SerializedProperty property) {
            UnityEngine.Object target = property.serializedObject.targetObject;
            return target.GetInstanceID() + ":" + property.propertyPath;
        }

        void _InvokeMethod(object targetObject, string methodName) {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = targetObject.GetType();

            MethodInfo method = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
            if (method == null) return;

            method.Invoke(targetObject, null);
        }
        #endregion
    }
}
