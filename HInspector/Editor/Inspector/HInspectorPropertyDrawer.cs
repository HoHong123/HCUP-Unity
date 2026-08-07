#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HInspector.Editor {
    [CustomPropertyDrawer(typeof(HInspectorAttribute), true)]
    public class HInspectorPropertyDrawer : PropertyDrawer {
        #region Private Fields
        const float RequiredBoxHeight = 24f;
        const float RequiredBoxTopGap = 2f;

        // HListDrawer의 DefaultExpandedState는 세션당 1회만 초기화되어 사용자의 접기 조작을 방해하지 않아야 한다.
        static readonly HashSet<string> _listDefaultExpandedApplied = new HashSet<string>();

        // 속성 수집은 리페인트마다 바뀌지 않는 정적 메타데이터다. FieldInfo 하나당 1회만 계산한다.
        // (GetPropertyHeight + OnGUI 가 프레임당 각각 호출되므로 캐시가 없으면 프레임당 2회
        //  GetCustomAttributes + LINQ 체인이 돌면서 배열·이터레이터를 계속 새로 할당한다.)
        // 도메인 리로드 시 FieldInfo 인스턴스와 함께 통째로 무효화되므로 별도 리셋 훅이 필요 없다.
        static readonly Dictionary<FieldInfo, HInspectorAttribute[]> _attributeCache = new Dictionary<FieldInfo, HInspectorAttribute[]>();
        #endregion

        #region Public Functions
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes)) return 0f;

            _ApplyListDrawerState(property, attributes);

            float totalHeight = EditorGUI.GetPropertyHeight(property, label, true);

            HMinMaxSliderAttribute minMaxSliderAttribute = _FindAttribute<HMinMaxSliderAttribute>(attributes);
            if (minMaxSliderAttribute != null && property.propertyType == SerializedPropertyType.Vector2) {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            HRequiredAttribute requiredAttribute = _FindAttribute<HRequiredAttribute>(attributes);
            if (requiredAttribute != null && _IsRequiredEmpty(property)) {
                totalHeight += RequiredBoxHeight + RequiredBoxTopGap;
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            HInspectorAttribute[] attributes = _GetAttributes();
            if (!_IsVisible(property, attributes)) return;

            _ApplyListDrawerState(property, attributes);

            bool isReadOnly = _EvaluateReadOnly(property, attributes);
            GUIContent resolvedLabel = _ResolveLabel(label, attributes);

            bool previousEnabled = GUI.enabled;
            if (isReadOnly) GUI.enabled = false;

            EditorGUI.BeginChangeCheck();
            _DrawProperty(position, property, resolvedLabel, attributes);
            bool isChanged = EditorGUI.EndChangeCheck();

            GUI.enabled = previousEnabled;

            _DrawRequiredWarning(position, property, attributes);

            if (!isChanged) return;

            _ApplyPostConstraints(property, attributes);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            _ProcessOnValueChanged(property, attributes);
        }
        #endregion

        #region Private Functions
        HInspectorAttribute[] _GetAttributes() {
            if (fieldInfo == null) return Array.Empty<HInspectorAttribute>();

            if (_attributeCache.TryGetValue(fieldInfo, out var cached)) return cached;

            var resolved = fieldInfo
                .GetCustomAttributes(typeof(HInspectorAttribute), true)
                .Cast<HInspectorAttribute>()
                .OrderBy(attribute => attribute.Order)
                .ToArray();

            _attributeCache[fieldInfo] = resolved;
            return resolved;
        }

        // OfType<T>().FirstOrDefault() 는 호출마다 이터레이터를 할당한다. 리페인트 경로에서
        // 프레임당 여러 번 도는 조회라 할당 없는 루프로 대체한다.
        static T _FindAttribute<T>(HInspectorAttribute[] attributes) where T : HInspectorAttribute {
            for (int k = 0; k < attributes.Length; k++) {
                if (attributes[k] is T typed) return typed;
            }

            return null;
        }

        bool _IsVisible(SerializedProperty property, HInspectorAttribute[] attributes) {
            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            for (int k = 0; k < attributes.Length; k++) {
                if (attributes[k] is HHideIfAttribute hideIfAttribute) {
                    if (_TryEvaluateCondition(parentObject, hideIfAttribute, out bool hideResult)) {
                        if (hideResult) return false;
                    }

                    continue;
                }

                if (attributes[k] is HShowIfAttribute showIfAttribute) {
                    if (_TryEvaluateCondition(parentObject, showIfAttribute, out bool showResult)) {
                        if (!showResult) return false;
                    }
                    else {
                        return false;
                    }
                }
            }

            return true;
        }

        bool _EvaluateReadOnly(SerializedProperty property, HInspectorAttribute[] attributes) {
            HListDrawerAttribute listAttribute = _FindAttribute<HListDrawerAttribute>(attributes);
            if (listAttribute != null && listAttribute.IsReadOnly && _IsCollectionField()) return true;

            HEnableIfAttribute enableIfAttribute = _FindAttribute<HEnableIfAttribute>(attributes);
            if (enableIfAttribute != null) return !_EvaluateEnableIf(property, enableIfAttribute);

            HReadOnlyAttribute readOnlyAttribute = _FindAttribute<HReadOnlyAttribute>(attributes);
            if (readOnlyAttribute == null) return false;

            if (string.IsNullOrEmpty(readOnlyAttribute.ConditionMemberName)) {
                return true;
            }

            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, readOnlyAttribute.ConditionMemberName, out object value)) {
                return true;
            }

            if (value is bool boolValue) {
                return readOnlyAttribute.Inverse ? !boolValue : boolValue;
            }

            return true;
        }

        bool _EvaluateEnableIf(SerializedProperty property, HEnableIfAttribute attribute) {
            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return true;

            if (attribute.IsExpression)
                return HInspectorExpressionUtility.TryEvaluate(parentObject, attribute.Expression, out bool exprResult) && exprResult;

            if (string.IsNullOrEmpty(attribute.Condition))
                return true;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, attribute.Condition, out object value))
                return false;

            if (value is bool boolValue)
                return boolValue;

            return true;
        }

        GUIContent _ResolveLabel(GUIContent originalLabel, HInspectorAttribute[] attributes) {
            HHideLabelAttribute hideLabelAttribute = _FindAttribute<HHideLabelAttribute>(attributes);
            if (hideLabelAttribute != null) return GUIContent.none;

            HLabelTextAttribute labelTextAttribute = _FindAttribute<HLabelTextAttribute>(attributes);
            if (labelTextAttribute != null) return new GUIContent(labelTextAttribute.Text, originalLabel.tooltip);

            return originalLabel;
        }

        bool _IsRequiredEmpty(SerializedProperty property) {
            switch (property.propertyType) {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null;
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue);
            case SerializedPropertyType.ExposedReference:
                return property.exposedReferenceValue == null;
            default:
                return false;
            }
        }

        void _DrawRequiredWarning(Rect fieldRect, SerializedProperty property, HInspectorAttribute[] attributes) {
            HRequiredAttribute requiredAttribute = _FindAttribute<HRequiredAttribute>(attributes);
            if (requiredAttribute == null) return;

            if (!_IsRequiredEmpty(property)) return;

            string message = string.IsNullOrEmpty(requiredAttribute.Message)
                ? $"'{property.displayName}' is required"
                : requiredAttribute.Message;

            float fieldBottom = fieldRect.y + EditorGUI.GetPropertyHeight(property, true);
            Rect warningRect = new Rect(fieldRect.x, fieldBottom + RequiredBoxTopGap, fieldRect.width, RequiredBoxHeight);
            EditorGUI.HelpBox(warningRect, message, MessageType.Warning);
        }

        bool _TryEvaluateCondition(object parentObject, HShowIfAttribute attribute, out bool result) {
            result = false;

            if (parentObject == null) return false;

            if (attribute.IsExpression)
                return HInspectorExpressionUtility.TryEvaluate(parentObject, attribute.Expression, out result);

            if (string.IsNullOrEmpty(attribute.MemberName))
                return false;

            if (!HInspectorPropertyUtility.TryGetMemberValue(parentObject, attribute.MemberName, out object currentValue))
                return false;

            if (!attribute.HasCompareValue) {
                if (currentValue is bool boolValue) {
                    result = boolValue;
                    return true;
                }

                return false;
            }

            if (!HInspectorPropertyUtility.TryCompare(currentValue, attribute.CompareValue, out int compareResult))
                return false;

            switch (attribute.CompareType) {
            case HCompareType.Equals:
                result = compareResult == 0;
                return true;
            case HCompareType.NotEquals:
                result = compareResult != 0;
                return true;
            case HCompareType.Greater:
                result = compareResult > 0;
                return true;
            case HCompareType.Less:
                result = compareResult < 0;
                return true;
            case HCompareType.GreaterOrEqual:
                result = compareResult >= 0;
                return true;
            case HCompareType.LessOrEqual:
                result = compareResult <= 0;
                return true;
            default:
                return false;
            }
        }

        void _DrawProperty(Rect position, SerializedProperty property, GUIContent label, HInspectorAttribute[] attributes) {
            // 드롭다운이 먼저다. 값의 출처를 목록으로 제한하는 속성이므로 다른 그리기와 겹치면
            // 제한이 무의미해진다.
            HDropdownAttribute dropdownAttribute = _FindAttribute<HDropdownAttribute>(attributes);
            if (dropdownAttribute != null) {
                HDropdownField.Draw(position, property, label, dropdownAttribute);
                return;
            }

            HMinMaxSliderAttribute minMaxSliderAttribute = _FindAttribute<HMinMaxSliderAttribute>(attributes);
            if (minMaxSliderAttribute != null) {
                _DrawMinMaxSlider(position, property, label, minMaxSliderAttribute);
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        void _DrawMinMaxSlider(Rect position, SerializedProperty property, GUIContent label, HMinMaxSliderAttribute attribute) {
            switch (property.propertyType) {
            case SerializedPropertyType.Vector2: {
                    Rect controlRect = EditorGUI.PrefixLabel(position, label);

                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

                    Rect sliderRect = new Rect(controlRect.x, controlRect.y, controlRect.width, lineHeight);

                    float halfWidth = (controlRect.width - 4f) * 0.5f;

                    Rect minRect = new Rect(controlRect.x, controlRect.y + lineHeight + verticalSpacing, halfWidth, lineHeight);
                    Rect maxRect = new Rect(minRect.x + halfWidth + 4f, minRect.y, halfWidth, lineHeight);

                    Vector2 currentValue = property.vector2Value;
                    float minValue = currentValue.x;
                    float maxValue = currentValue.y;

                    EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, attribute.Min, attribute.Max);
                    minValue = EditorGUI.FloatField(minRect, minValue);
                    maxValue = EditorGUI.FloatField(maxRect, maxValue);

                    minValue = Mathf.Clamp(minValue, attribute.Min, attribute.Max);
                    maxValue = Mathf.Clamp(maxValue, attribute.Min, attribute.Max);

                    if (maxValue < minValue)
                        maxValue = minValue;

                    property.vector2Value = new Vector2(minValue, maxValue);
                    break;
                }

            case SerializedPropertyType.Float: {
                    float currentValue = property.floatValue;
                    float nextValue = EditorGUI.Slider(position, label, currentValue, attribute.Min, attribute.Max);
                    property.floatValue = Mathf.Clamp(nextValue, attribute.Min, attribute.Max);
                    break;
                }

            case SerializedPropertyType.Integer: {
                    int currentValue = property.intValue;
                    float sliderValue = EditorGUI.Slider(position, label, currentValue, attribute.Min, attribute.Max);
                    property.intValue = Mathf.RoundToInt(Mathf.Clamp(sliderValue, attribute.Min, attribute.Max));
                    break;
                }

            default:
                EditorGUI.PropertyField(position, property, label, true);
                break;
            }
        }

        void _ApplyPostConstraints(SerializedProperty property, HInspectorAttribute[] attributes) {
            HMinAttribute minAttribute = _FindAttribute<HMinAttribute>(attributes);
            HMaxAttribute maxAttribute = _FindAttribute<HMaxAttribute>(attributes);

            if (minAttribute == null && maxAttribute == null)
                return;

            switch (property.propertyType) {
            case SerializedPropertyType.Integer: {
                    int value = property.intValue;

                    if (minAttribute != null)
                        value = Mathf.Max(value, Mathf.RoundToInt(minAttribute.Min));

                    if (maxAttribute != null)
                        value = Mathf.Min(value, Mathf.RoundToInt(maxAttribute.Max));

                    property.intValue = value;
                    break;
                }

            case SerializedPropertyType.Float: {
                    float value = property.floatValue;

                    if (minAttribute != null)
                        value = Mathf.Max(value, minAttribute.Min);

                    if (maxAttribute != null)
                        value = Mathf.Min(value, maxAttribute.Max);

                    property.floatValue = value;
                    break;
                }

            case SerializedPropertyType.Vector2: {
                    Vector2 value = property.vector2Value;

                    if (minAttribute != null) {
                        value.x = Mathf.Max(value.x, minAttribute.Min);
                        value.y = Mathf.Max(value.y, minAttribute.Min);
                    }

                    if (maxAttribute != null) {
                        value.x = Mathf.Min(value.x, maxAttribute.Max);
                        value.y = Mathf.Min(value.y, maxAttribute.Max);
                    }

                    property.vector2Value = value;
                    break;
                }
            }
        }

        void _ProcessOnValueChanged(SerializedProperty property, HInspectorAttribute[] attributes) {
            HOnValueChangedAttribute onValueChangedAttribute = _FindAttribute<HOnValueChangedAttribute>(attributes);
            if (onValueChangedAttribute == null) return;

            if (string.IsNullOrEmpty(onValueChangedAttribute.MethodName)) return;

            object parentObject = HInspectorPropertyUtility.GetParentObject(property);
            if (parentObject == null) return;

            if (!HInspectorPropertyUtility.TryGetSerializedValue(property, out object currentValue))
                currentValue = null;

            HInspectorPropertyUtility.TryInvokeParameterlessOrSingleParameterMethod(parentObject, onValueChangedAttribute.MethodName, currentValue);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        void _ApplyListDrawerState(SerializedProperty property, HInspectorAttribute[] attributes) {
            if (!_IsCollectionField()) return;

            HListDrawerAttribute listAttribute = _FindAttribute<HListDrawerAttribute>(attributes);
            if (listAttribute == null) return;
            if (!listAttribute.DefaultExpandedState) return;

            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (targetObject == null) return;

            // 세션당 1회만 isExpanded를 강제. 이후 프레임은 사용자 조작을 존중한다.
            string key = targetObject.GetInstanceID() + ":" + property.propertyPath;
            if (_listDefaultExpandedApplied.Contains(key)) return;

            _listDefaultExpandedApplied.Add(key);
            property.isExpanded = true;
        }

        bool _IsCollectionField() {
            if (fieldInfo == null) return false;

            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray) return true;
            if (typeof(IList).IsAssignableFrom(fieldType)) return true;

            return false;
        }
        #endregion
    }
}
#endif
